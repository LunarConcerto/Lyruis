using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace LunarConcerto.Lyruis.Samples
{
    public sealed class LyruisBenchmark : MonoBehaviour
    {
        [Serializable] public sealed class Result
        {
            public string mode;
            public int children;
            public double lyruisMedianMs, nativeMedianMs, lyruisP95Ms, nativeP95Ms;
            public float maxGeometryError;
        }
        [Serializable] public sealed class Report
        {
            public string unity, platform, processor, utc;
            public bool editor;
            public bool playMode = Application.isPlaying;
            public string engine = "Lyruis independent postorder tree";
            public int warmup = 20, samples = 40, rebuildsPerSample = 10;
            public List<Result> results = new List<Result>();
        }
        public bool runOnStart = true;
        private void Start() { if (runOnStart) Run(); }

        [ContextMenu("Run Benchmark")]
        public void Run()
        {
            string path = Path.Combine(Application.persistentDataPath, "lyruis-benchmark.json");
            File.WriteAllText(path, RunSuite());
            Debug.Log("Lyruis benchmark: " + path);
            string nestedPath = Path.Combine(Application.persistentDataPath, "lyruis-nested-benchmark.json");
            File.WriteAllText(nestedPath, NestedLayoutBenchmark.RunSuite());
            Debug.Log("Lyruis nested benchmark: " + nestedPath);
        }
        public static string RunSuite()
        {
            var report = new Report { unity = Application.unityVersion, platform = Application.platform.ToString(),
                processor = SystemInfo.processorType, editor = Application.isEditor, utc = DateTime.UtcNow.ToString("O") };
            foreach (var mode in new[] { LayoutMode.Horizontal, LayoutMode.Vertical, LayoutMode.Grid })
            foreach (int count in new[] { 10, 100, 1000 })
            {
                RectTransform a = Create(mode, count, true), b = Create(mode, count, false);
                try
                {
                    for (int i = 0; i < report.warmup; i++) { Rebuild(a); Rebuild(b); }
                    float error = 0;
                    for (int i = 0; i < count; i++)
                    {
                        var x = (RectTransform)a.GetChild(i); var y = (RectTransform)b.GetChild(i);
                        error = Mathf.Max(error, Vector2.Distance(x.anchoredPosition, y.anchoredPosition), Vector2.Distance(x.rect.size, y.rect.size));
                    }
                    var ta = new double[report.samples]; var tb = new double[report.samples];
                    for (int i = 0; i < report.samples; i++)
                    {
                        if (i % 2 == 0) { ta[i] = Time(a, report.rebuildsPerSample); tb[i] = Time(b, report.rebuildsPerSample); }
                        else { tb[i] = Time(b, report.rebuildsPerSample); ta[i] = Time(a, report.rebuildsPerSample); }
                    }
                    Array.Sort(ta); Array.Sort(tb);
                    report.results.Add(new Result { mode = mode.ToString(), children = count, maxGeometryError = error,
                        lyruisMedianMs = (ta[19] + ta[20]) / 2, nativeMedianMs = (tb[19] + tb[20]) / 2,
                        lyruisP95Ms = ta[37], nativeP95Ms = tb[37] });
                }
                finally { DestroyImmediate(a.gameObject); DestroyImmediate(b.gameObject); }
            }
            return JsonUtility.ToJson(report, true);
        }
        private static void Rebuild(RectTransform root)
        {
            if (root.TryGetComponent(out LyruisLayout layout)) layout.RebuildNow();
            else LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }
        private static double Time(RectTransform root, int repetitions)
        {
            long begin = Stopwatch.GetTimestamp();
            for (int i = 0; i < repetitions; i++) Rebuild(root);
            return (Stopwatch.GetTimestamp() - begin) * 1000d / Stopwatch.Frequency / repetitions;
        }
        private static RectTransform Create(LayoutMode mode, int count, bool lyruis)
        {
            var root = new GameObject(lyruis ? "Lyruis" : "Native", typeof(RectTransform)).GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(800, 600);
            if (lyruis)
            {
                var layout = root.gameObject.AddComponent<LyruisLayout>();
                layout.Mode = mode; layout.Spacing = new Vector2(4, 4); layout.GridCount = 10; layout.CellSize = new Vector2(40, 24);
            }
            else if (mode == LayoutMode.Grid)
            {
                var layout = root.gameObject.AddComponent<GridLayoutGroup>();
                layout.cellSize = new Vector2(40, 24); layout.spacing = new Vector2(4, 4);
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = 10;
            }
            else
            {
                HorizontalOrVerticalLayoutGroup layout = mode == LayoutMode.Horizontal ?
                    (HorizontalOrVerticalLayoutGroup)root.gameObject.AddComponent<HorizontalLayoutGroup>() : root.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 4; layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            }
            for (int i = 0; i < count; i++)
            {
                var child = new GameObject("Item", typeof(RectTransform), typeof(LayoutElement));
                child.transform.SetParent(root, false);
                var element = child.GetComponent<LayoutElement>(); element.preferredWidth = element.minWidth = 40; element.preferredHeight = element.minHeight = 24;
                if (lyruis) { var item = child.AddComponent<LyruisLayout>(); item.WidthMode = item.HeightMode = SizeMode.Content; }
            }
            return root;
        }
    }
}
