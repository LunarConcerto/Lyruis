using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LunarConcerto.Lyruis.Samples
{
    public sealed class LyruisProfilerLab : MonoBehaviour
    {
        public enum Workload { Idle, SingleLeaf, BatchLeaves, ForceRootEveryFrame, ResizeRoot }
        [Serializable] public sealed class Target
        {
            public Text text;
            public LyruisLayout layout;
            public string compact, expanded;
            public bool expandedNow;
        }
        [Header("Workload (also selectable with buttons or keys 1-5)")]
        public Workload workload = Workload.Idle;
        [Min(.01f)] public float changeInterval = .1f;
        [Min(1)] public int batchSize = 32;
        [Min(0)] public int warmupFrames = 60;
        public bool detailedMarkers = true;
        public bool drawWorkloadCanvas = true;
        [Header("Scene generation")]
        [Range(4, 80)] public int cardCount = 24;
        [SerializeField] private Canvas workloadCanvas;
        [SerializeField] private CanvasGroup workloadVisibility;
        [SerializeField] private LyruisLayout root;
        [SerializeField] private Text status;
        [SerializeField] private List<Target> targets = new List<Target>();
        [SerializeField] private List<Button> controls = new List<Button>();
        [SerializeField] private int layoutNodeCount, maximumDepth;
        public int CompletedRebuilds { get; private set; }
        public int MutationTicks { get; private set; }
        public LyruisLayout LayoutRoot => root;
        public int TargetCount => targets.Count;
        public bool AutomationRunning { get; private set; }
        public sealed class SavedState
        {
            internal string[] text;
            internal bool[] expanded;
            internal int cursor, ticks, rebuilds;
            internal Workload workload;
            internal Vector2 size;
            internal bool detailed, draw;
        }
        public SavedState BeginAutomation()
        {
            if (AutomationRunning) throw new InvalidOperationException("The lab is already under automated control.");
            var state = new SavedState { text = new string[targets.Count], expanded = new bool[targets.Count], cursor = cursor,
                ticks = MutationTicks, rebuilds = CompletedRebuilds, workload = workload, size = root.FixedSize, detailed = detailedMarkers, draw = drawWorkloadCanvas };
            for (int i = 0; i < targets.Count; i++) { state.text[i] = targets[i].text.text; state.expanded[i] = targets[i].expandedNow; }
            AutomationRunning = true;
            for (int i = 0; i < Math.Min(8, controls.Count); i++) controls[i].interactable = false;
            return state;
        }
        public void PrepareMeasurement(Workload value)
        {
            cursor = 0; MutationTicks = 0;
            foreach (var target in targets) { target.expandedNow = false; target.text.text = target.compact; }
            root.FixedSize = new Vector2(1840, 0); detailedMarkers = false; ApplySettings(); SetWorkload(value);
            root.RebuildNow(); Canvas.ForceUpdateCanvases();
        }
        public void ShowMeasurementStatus(string message) { if (status) status.text = message; }
        public void EndAutomation(SavedState state)
        {
            if (state == null) return;
            for (int i = 0; i < targets.Count; i++) { targets[i].text.text = state.text[i]; targets[i].expandedNow = state.expanded[i]; }
            cursor = state.cursor; MutationTicks = state.ticks;
            detailedMarkers = state.detailed; drawWorkloadCanvas = state.draw; root.FixedSize = state.size;
            if (isActiveAndEnabled) ApplySettings();
            SetWorkload(state.workload); root.RebuildNow(); Canvas.ForceUpdateCanvases(); CompletedRebuilds = state.rebuilds;
            for (int i = 0; i < Math.Min(8, controls.Count); i++) controls[i].interactable = true;
            AutomationRunning = false;
        }
        private int cursor, frames;
        private float nextTick;
        private Workload lastWorkload;
        private bool previousDetailed, previousRunInBackground, attached;
        private static readonly ProfilerMarker TickMarker = new ProfilerMarker("LyruisLab.WorkloadTick");
        private static readonly Color Panel = new Color(.07f, .1f, .16f);
        private static readonly Color Ink = new Color(.85f, .91f, .97f);
        private Font font;

        private void Start()
        {
            if (!root) Build();
            previousDetailed = LyruisDiagnostics.DetailedProfiling;
            previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            attached = true; root.OnLayoutRefreshed += CountRebuild;
            WireControls();
            lastWorkload = workload; ApplySettings(); UpdateStatus();
            root.RebuildNow();
        }
        private void OnDisable()
        {
            if (!attached) return;
            if (root) root.OnLayoutRefreshed -= CountRebuild;
            LyruisDiagnostics.DetailedProfiling = previousDetailed; attached = false;
            Application.runInBackground = previousRunInBackground;
        }
        private void CountRebuild() => CompletedRebuilds++;
        private void WireControls()
        {
            foreach (var button in controls) button.onClick.RemoveAllListeners();
            for (int i = 0; i < 5; i++)
            {
                int selected = i;
                controls[i].onClick.AddListener(() => SetWorkload((Workload)selected));
            }
            controls[5].onClick.AddListener(() => { drawWorkloadCanvas = !drawWorkloadCanvas; ApplySettings(); });
            controls[6].onClick.AddListener(() => { detailedMarkers = !detailedMarkers; ApplySettings(); });
            controls[7].onClick.AddListener(TickOnce);
            if (controls.Count > 8) controls[8].onClick.AddListener(() => GetComponent<LyruisAutoMeasure>().ToggleMeasurement());
        }
        private void Update()
        {
            ApplySettings();
            if (AutomationRunning) return;
            if (lastWorkload != workload) { lastWorkload = workload; UpdateStatus(); }
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetWorkload(Workload.Idle);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetWorkload(Workload.SingleLeaf);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetWorkload(Workload.BatchLeaves);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetWorkload(Workload.ForceRootEveryFrame);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SetWorkload(Workload.ResizeRoot);
            if (Input.GetKeyDown(KeyCode.Space)) TickOnce();
#endif
            if (++frames <= warmupFrames || workload == Workload.Idle) return;
            if (workload == Workload.ForceRootEveryFrame || Time.unscaledTime >= nextTick)
            { RunTick(); nextTick = Time.unscaledTime + Mathf.Max(.01f, changeInterval); }
        }
        private void ApplySettings()
        {
            LyruisDiagnostics.DetailedProfiling = detailedMarkers;
            if (workloadVisibility)
            {
                float alpha = drawWorkloadCanvas ? 1 : 0;
                if (workloadVisibility.alpha != alpha) workloadVisibility.alpha = alpha;
                if (workloadVisibility.blocksRaycasts != drawWorkloadCanvas) workloadVisibility.blocksRaycasts = drawWorkloadCanvas;
            }
        }
        public void SetWorkload(Workload value) { workload = lastWorkload = value; nextTick = 0; UpdateStatus(); }
        [ContextMenu("Tick Once (Play mode)")]
        public void TickOnce() { if (Application.isPlaying && root) RunTick(); }
        private void RunTick()
        {
            using (TickMarker.Auto())
            {
                MutationTicks++;
                if (workload == Workload.Idle || workload == Workload.ForceRootEveryFrame) root.RebuildNow();
                else if (workload == Workload.ResizeRoot)
                    root.FixedSize = new Vector2(MutationTicks % 2 == 0 ? 1840 : 1380, 0);
                else
                {
                    int count = workload == Workload.BatchLeaves ? Mathf.Min(Mathf.Max(1, batchSize), targets.Count) : 1;
                    for (int i = 0; i < count && targets.Count > 0; i++)
                    {
                        var item = targets[cursor++ % targets.Count];
                        item.expandedNow = !item.expandedNow;
                        item.text.text = item.expandedNow ? item.expanded : item.compact;
                        item.layout.Refresh();
                    }
                }
            }
        }
        private void UpdateStatus()
        {
            if (status) status.text = workload + "  |  " + layoutNodeCount + " layout nodes  |  depth " + maximumDepth +
                "  |  " + targets.Count + " mutable text leaves  |  interval " + changeInterval.ToString("0.00") + "s  |  Space: single step";
        }

        // Called by the editor scene factory; all content is serialized for inspecting before Play.
        public void Build()
        {
            if (root) throw new InvalidOperationException("Create a new lab scene to change the generated topology.");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var controls = MakeCanvas("Controls Canvas");
            var heading = Label(controls.transform, "Title", "LYRUIS / NESTED PROFILER LAB", 25);
            Place(heading.rectTransform, new Vector2(24, -14), new Vector2(1800, 34));
            status = Label(controls.transform, "Status", "", 15);
            Place(status.rectTransform, new Vector2(24, -52), new Vector2(1800, 24));
            string[] names = { "1  Idle", "2  Single leaf", "3  Batch leaves", "4  Force root/frame", "5  Resize + reflow" };
            for (int i = 0; i < names.Length; i++)
            {
                int selected = i;
                Button(controls.transform, names[i], 24 + i * 190, () => SetWorkload((Workload)selected));
            }
            Button(controls.transform, "Draw UI: toggle", 974, () => { drawWorkloadCanvas = !drawWorkloadCanvas; ApplySettings(); });
            Button(controls.transform, "Detail: toggle", 1164, () => { detailedMarkers = !detailedMarkers; ApplySettings(); });
            Button(controls.transform, "Single step", 1354, TickOnce);
            Button(controls.transform, "AUTO REPORT / Stop", 1544, () => GetComponent<LyruisAutoMeasure>().ToggleMeasurement());
            gameObject.AddComponent<LyruisAutoMeasure>();
            var hint = Label(controls.transform, "Hint", "Scroll to inspect cards. Use Inspector to set interval/batch size. Detail markers add overhead; start with Deep Profile OFF.", 14);
            Place(hint.rectTransform, new Vector2(24, -116), new Vector2(1800, 22));
            workloadCanvas = MakeCanvas("Workload Canvas - disable rendering independently");
            workloadVisibility = workloadCanvas.gameObject.AddComponent<CanvasGroup>();
            var viewport = Rect("Viewport", workloadCanvas.transform);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(24, 24); viewport.offsetMax = new Vector2(-24, -150);
            viewport.gameObject.AddComponent<Image>().color = new Color(.025f, .04f, .07f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.horizontal = false; scroll.vertical = true; scroll.inertia = false;
            root = Container(viewport, "Dashboard - Flow root", LayoutMode.Flow, 1840);
            root.padding = new RectOffset(8, 8, 8, 8); root.Spacing = new Vector2(16, 16);
            Place(root.RectTransform, Vector2.zero, new Vector2(1840, 100)); scroll.content = root.RectTransform;
            for (int i = 0; i < cardCount; i++) Card(root.transform, i);
            var nodes = root.GetComponentsInChildren<LyruisLayout>(true);
            layoutNodeCount = nodes.Length; maximumDepth = 0;
            foreach (var node in nodes)
            {
                int depth = 1; Transform current = node.transform;
                while (current != root.transform) { depth++; current = current.parent; }
                maximumDepth = Mathf.Max(maximumDepth, depth);
            }
            if (!FindObjectOfType<EventSystem>())
            {
                var events = new GameObject("EventSystem", typeof(EventSystem)); events.transform.SetParent(transform, false);
#if ENABLE_LEGACY_INPUT_MANAGER
                events.AddComponent<StandaloneInputModule>();
#endif
            }
            root.RebuildNow(); UpdateStatus();
        }
        private void Card(Transform parent, int index)
        {
            var card = Container(parent, "Card " + index.ToString("00"), LayoutMode.Vertical, 440);
            card.padding = new RectOffset(12, 12, 12, 12); card.Spacing = new Vector2(6, 6);
            card.gameObject.AddComponent<Image>().color = Panel;
            var header = Container(card.transform, "Header - Horizontal", LayoutMode.Horizontal, 416, 34);
            header.Spacing = new Vector2(8, 8); header.childAlignment = TextAnchor.MiddleLeft;
            var icon = Container(header.transform, "Accent", LayoutMode.None, 8, 24);
            icon.gameObject.AddComponent<Image>().color = Color.HSVToRGB(index * .077f % 1, .55f, .95f);
            TextNode(header.transform, "Title", "MODULE " + index.ToString("00") + " / Overview", 20, 390, 30);
            var body = TextNode(card.transform, "Description - wrapped intrinsic height", "A compact report for a nested content-sized dashboard.", 15, 416);
            targets.Add(new Target { text = body, layout = body.GetComponent<LyruisLayout>(), compact = body.text,
                expanded = "A longer report changes this leaf's intrinsic height. Parent containers must measure content, preserve padding, and arrange following sections while the root maintains its wrapping card layout." });
            var tags = Container(card.transform, "Tags - Flow", LayoutMode.Flow, 416);
            tags.Spacing = new Vector2(6, 6);
            int[] widths = { 80, 112, 88, 124, 72 };
            for (int t = 0; t < widths.Length; t++)
            {
                var tag = Container(tags.transform, "Tag " + t, LayoutMode.Horizontal, widths[t], 24);
                tag.padding = new RectOffset(6, 6, 3, 3);
                tag.gameObject.AddComponent<Image>().color = new Color(.13f, .23f, .32f);
                TextNode(tag.transform, "Caption", new[] { "ACTIVE", "CONTENT", "NESTED", "INSTRUMENTED", "FLOW" }[t], 12, widths[t] - 12, 18);
            }
            var metrics = Container(card.transform, "Metrics - Grid", LayoutMode.Grid, 416);
            metrics.GridCount = 3; metrics.CellSize = new Vector2(132, 68); metrics.Spacing = new Vector2(8, 8);
            for (int m = 0; m < 6; m++)
            {
                var stat = Container(metrics.transform, "Metric " + m + " - Vertical", LayoutMode.Vertical, 128);
                stat.padding = new RectOffset(8, 8, 6, 6); stat.Spacing = new Vector2(4, 4);
                stat.gameObject.AddComponent<Image>().color = new Color(.1f, .15f, .22f);
                TextNode(stat.transform, "Label", "CHANNEL " + m, 11, 112, 16);
                var valueRow = Container(stat.transform, "Value row - Horizontal", LayoutMode.Horizontal);
                valueRow.childAlignment = TextAnchor.MiddleLeft; valueRow.Spacing = new Vector2(5, 5);
                var dot = Container(valueRow.transform, "Signal", LayoutMode.None, 7, 7);
                dot.gameObject.AddComponent<Image>().color = new Color(.35f, .85f, .7f);
                var value = TextNode(valueRow.transform, "Value - intrinsic size", "18.7K", 18);
                targets.Add(new Target { text = value, layout = value.GetComponent<LyruisLayout>(), compact = "18.7K", expanded = "129.4K" });
            }
            var footer = Container(card.transform, "Footer - Horizontal", LayoutMode.Horizontal, 416, 24);
            footer.Spacing = new Vector2(12, 12);
            TextNode(footer.transform, "Status", "READY", 13, 100, 22);
            TextNode(footer.transform, "Trace", "Grid > Vertical > Row > Text", 12, 260, 22);
            var manual = Container(footer.transform, "Manual badge - None bounds", LayoutMode.None);
            for (int b = 0; b < 2; b++)
            {
                var block = Container(manual.transform, "Manual block " + b, LayoutMode.None, b == 0 ? 10 : 12, b == 0 ? 10 : 6);
                block.RectTransform.anchorMin = block.RectTransform.anchorMax = new Vector2(.5f, .5f);
                block.RectTransform.anchoredPosition = new Vector2(b == 0 ? -6 : 8, 0);
                block.gameObject.AddComponent<Image>().color = new Color(.35f, .65f, .85f);
            }
        }
        private Canvas MakeCanvas(string name)
        {
            var rect = Rect(name, transform); var canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = name.StartsWith("Controls") ? 10 : 0;
            var scaler = rect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0;
            rect.gameObject.AddComponent<GraphicRaycaster>(); return canvas;
        }
        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); return rect;
        }
        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size; }
        private LyruisLayout Container(Transform parent, string name, LayoutMode mode, float width = -1, float height = -1)
        {
            var rect = Rect(name, parent); var layout = rect.gameObject.AddComponent<LyruisLayout>();
            layout.Mode = mode; layout.WidthMode = width < 0 ? SizeMode.Content : SizeMode.Fixed;
            layout.HeightMode = height < 0 ? SizeMode.Content : SizeMode.Fixed;
            layout.FixedSize = new Vector2(Mathf.Max(0, width), Mathf.Max(0, height)); layout.Spacing = Vector2.zero;
            return layout;
        }
        private Text Label(Transform parent, string name, string value, int size)
        {
            var rect = Rect(name, parent); var text = rect.gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = size; text.text = value; text.color = Ink; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow; return text;
        }
        private Text TextNode(Transform parent, string name, string value, int size, float width = -1, float height = -1)
        {
            var label = Label(parent, name, value, size); var layout = label.gameObject.AddComponent<LyruisLayout>();
            layout.Mode = LayoutMode.None; layout.WidthMode = width < 0 ? SizeMode.Content : SizeMode.Fixed;
            layout.HeightMode = height < 0 ? SizeMode.Content : SizeMode.Fixed;
            layout.FixedSize = new Vector2(Mathf.Max(0, width), Mathf.Max(0, height)); return label;
        }
        private void Button(Transform parent, string caption, float x, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(caption, parent); Place(rect, new Vector2(x, -80), new Vector2(180, 28));
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.12f, .28f, .38f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
            controls.Add(button);
            var label = Label(rect, "Label", caption, 14); label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        }
    }
}
