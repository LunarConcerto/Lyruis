using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static LunarConcerto.Lyruis.Editor.LyruisInspectorGUI;

namespace LunarConcerto.Lyruis.Editor
{
    [CustomEditor(typeof(LyruisLayout)), CanEditMultipleObjects]
    public sealed class LyruisLayoutEditor : UnityEditor.Editor
    {
        private const string LanguageKey = "LunarConcerto.Lyruis.Inspector.Chinese";
        private const string LinkKey = "LunarConcerto.Lyruis.Inspector.LinkedPadding";
        private SerializedProperty mode, axis, width, height, fixedSize, gap, cell, count, inset, alignment;
        private bool chinese, linkPadding, contextOpen;
        private string T(string english, string chineseText) => chinese ? chineseText : english;
        private LyruisLayout Node => (LyruisLayout)target;
        private void OnEnable()
        {
            chinese = EditorPrefs.GetBool(LanguageKey, Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified || Application.systemLanguage == SystemLanguage.ChineseTraditional);
            linkPadding = EditorPrefs.GetBool(LinkKey, false);
            mode = serializedObject.FindProperty("mode"); axis = serializedObject.FindProperty("flowAxis");
            width = serializedObject.FindProperty("widthMode"); height = serializedObject.FindProperty("heightMode");
            fixedSize = serializedObject.FindProperty("fixedSize"); gap = serializedObject.FindProperty("spacing");
            cell = serializedObject.FindProperty("cellSize"); count = serializedObject.FindProperty("gridCount");
            inset = serializedObject.FindProperty("m_Padding"); alignment = serializedObject.FindProperty("m_ChildAlignment");
            Undo.undoRedoPerformed += Repaint;
        }
        private void OnDisable() => Undo.undoRedoPerformed -= Repaint;
        public override bool RequiresConstantRepaint() => Application.isPlaying;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            float previousWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Clamp(EditorGUIUtility.currentViewWidth * .3f, 78, 115);
            try
            {
                DrawHeader();
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) DrawLayout();
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) DrawSizing();
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) DrawInsets();
                DrawAdvice(); DrawActions(); DrawContext();
                if (serializedObject.ApplyModifiedProperties())
                    foreach (var item in targets) ((LyruisLayout)item).Refresh();
            }
            finally { EditorGUIUtility.labelWidth = previousWidth; }
        }
        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Lyruis Layout", EditorStyles.boldLabel);
                int language = GUILayout.Toolbar(chinese ? 1 : 0, new[] { "EN", "中文" }, EditorStyles.miniButton, GUILayout.Width(88));
                if ((language == 1) != chinese) { chinese = language == 1; EditorPrefs.SetBool(LanguageKey, chinese); }
            }
            if (targets.Length > 1) Hint(T(targets.Length + " objects selected · edits apply to all", "已选择 " + targets.Length + " 个对象 · 修改应用到全部对象"));
            else
            {
                int active = CountChildren(Node, out int missing);
                string status = !Node.isActiveAndEnabled ? T("Disabled · excluded from parent layout", "已禁用 · 不参与父节点布局") :
                    active == 0 ? T("Leaf · size comes from this object", "叶子节点 · 从自身读取尺寸") : T("Container · " + active + " participating children", "容器 · " + active + " 个参与子节点");
                Hint(status);
            }
            GUILayout.Space(5);
        }
        private void DrawLayout()
        {
            Heading(T("Layout", "布局"));
            Popup(mode, Content(T("Mode", "模式"), T("Changes child arrangement. Size rules are configured separately.", "控制子节点排布，不会重置下方尺寸规则。")),
                chinese ? new[] { "无排布 / None", "横向 / Horizontal", "纵向 / Vertical", "网格 / Grid", "换行 / Flow", "叠放 / Overlay" } :
                new[] { "None · manual positions", "Horizontal", "Vertical", "Grid", "Flow · wrap", "Overlay" });
            if (mode.hasMultipleDifferentValues) { Hint(T("Modes differ. Options below retain each object's existing values.", "所选对象模式不同；下方选项保留各自已有值。")); }
            else
            {
                string[] english = { "Preserves child positions; Content measures their bounds.", "Places children left to right.", "Places children top to bottom.", "Cell steps define slots; children keep their own size.", "Wraps children into lines within a finite main-axis size.", "Places children in a shared, aligned content box." };
                string[] translated = { "保留子节点位置；内容尺寸使用子树包围盒。", "按兄弟顺序从左向右排布。", "按兄弟顺序从上向下排布。", "格子步进决定位置；子节点保留自身尺寸。", "在有限主轴空间中自动换行或换列。", "子节点共用内容框，并按指定方式对齐。" };
                Hint(chinese ? translated[mode.intValue] : english[mode.intValue]);
            }
            bool grid = Is(LayoutMode.Grid), flow = Is(LayoutMode.Flow);
            if (grid || flow || mode.hasMultipleDifferentValues)
                Popup(axis, Content(T(grid ? "Fill axis" : "Main axis", grid ? "铺排方向" : "主轴方向")), chinese ? new[] { "横向", "纵向" } : new[] { "Horizontal", "Vertical" });
            if (grid || mode.hasMultipleDifferentValues)
            {
                Rect rect = EditorGUILayout.GetControlRect();
                Integer(rect, count, Content(T(axis.hasMultipleDifferentValues ? "Items / line" : axis.intValue == 0 ? "Columns" : "Rows", axis.hasMultipleDifferentValues ? "每行数量" : axis.intValue == 0 ? "列数" : "行数"), T("0 falls back to 2.", "0 使用默认数量 2。")), true);
                Pair(cell, Content(T("Cell size", "格子尺寸"), T("Slot dimensions, not a child-size override. Zero initializes from the first child.", "决定格子步进，不会拉伸子节点。两轴均为 0 时从首个子节点初始化。")), true);
                if (!count.hasMultipleDifferentValues && count.intValue == 0) Hint(T("Count 0 uses 2 items per line.", "数量 0 按每行 2 项处理。"));
            }
        }
        private void DrawSizing()
        {
            Heading(T("Size", "尺寸"));
            SizeRow(width, fixedSize.FindPropertyRelative("x"), 0, T("Width", "宽度"));
            SizeRow(height, fixedSize.FindPropertyRelative("y"), 1, T("Height", "高度"));
            Hint(T("Keep: authored size · Fixed: pixels · Content: children or intrinsic content", "保持：沿用当前尺寸 · 固定：指定数值 · 内容：根据子节点或自身内容计算"));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Content(T("Fit content", "适应内容"), T("Sets both axes to Content. Flow keeps a finite main axis using the current size.", "两轴设为内容。Flow 将主轴固定为当前尺寸，以保留换行。")), EditorStyles.miniButton)) FitContent();
                if (GUILayout.Button(Content(T("Freeze size", "固定当前尺寸"), T("Copies current layout dimensions into Fixed size for each selected object.", "将各对象当前布局尺寸复制到固定尺寸，保留当前外观。")), EditorStyles.miniButton)) FreezeSize();
            }
        }
        private void SizeRow(SerializedProperty rule, SerializedProperty pixels, int component, string label)
        {
            Rect line = EditorGUILayout.GetControlRect();
            GUIContent caption = Content(label, T("Keep preserves current layout dimensions; Fixed uses pixels; Content measures children or intrinsic content.", "保持沿用当前布局尺寸；固定使用指定数值；内容根据子节点或自身内容计算。"));
            EditorGUI.BeginProperty(line, caption, rule);
            Rect body = EditorGUI.PrefixLabel(line, caption);
            float choiceWidth = Mathf.Max(66, (body.width - 7) * .53f);
            Rect popup = new Rect(body.x, body.y, choiceWidth, body.height);
            bool mixed = EditorGUI.showMixedValue; EditorGUI.showMixedValue = rule.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int result = EditorGUI.Popup(popup, rule.intValue, chinese ? new[] { "保持", "固定", "内容" } : new[] { "Keep", "Fixed", "Content" });
            if (EditorGUI.EndChangeCheck()) rule.intValue = result;
            EditorGUI.showMixedValue = mixed;
            Rect value = new Rect(popup.xMax + 7, body.y, Mathf.Max(20, body.xMax - popup.xMax - 7), body.height);
            if (!rule.hasMultipleDifferentValues && rule.intValue == (int)SizeMode.Fixed) Float(value, pixels, GUIContent.none, true);
            else
            {
                float current = Node.RectTransform.sizeDelta[component]; bool different = rule.hasMultipleDifferentValues;
                foreach (var item in targets) if (((LyruisLayout)item).RectTransform.sizeDelta[component] != current) different = true;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.showMixedValue = different;
                    EditorGUI.FloatField(value, Content("", T("Current computed layout size. Edit the rule or RectTransform to change it.", "当前布局尺寸。修改尺寸规则，或在 RectTransform 中调整保持值。")), current);
                    EditorGUI.showMixedValue = mixed;
                }
            }
            EditorGUI.EndProperty();
        }
        private void DrawInsets()
        {
            Heading(T("Spacing & alignment", "间距与对齐"));
            if (mode.hasMultipleDifferentValues || Is(LayoutMode.Grid) || Is(LayoutMode.Flow)) Pair(gap, Content(T("Gap", "间距"), T("X: horizontal; Y: vertical. Negative values allow overlap.", "X 为水平间距，Y 为垂直间距。负值允许重叠。")));
            else if (Is(LayoutMode.Horizontal) || Is(LayoutMode.Vertical))
                Float(EditorGUILayout.GetControlRect(), gap.FindPropertyRelative(Is(LayoutMode.Horizontal) ? "x" : "y"), Content(T("Gap", "间距")));
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(T("Padding", "内边距"), EditorStyles.miniBoldLabel);
                bool linked = GUILayout.Toggle(linkPadding, Content(T("Link", "关联"), T("The next side edit updates all four sides. Toggling alone changes no values.", "下次修改任意一边时同步四边。切换本身不修改数值。")), EditorStyles.miniButton, GUILayout.Width(48));
                if (linked != linkPadding) { linkPadding = linked; EditorPrefs.SetBool(LinkKey, linked); }
            }
            string[] fields = { "m_Left", "m_Right", "m_Top", "m_Bottom" };
            string[] labels = chinese ? new[] { "左", "右", "上", "下" } : new[] { "Left", "Right", "Top", "Bottom" };
            float previous = EditorGUIUtility.labelWidth; EditorGUIUtility.labelWidth = chinese ? 24 : 48;
            for (int row = 0; row < 2; row++)
            {
                Rect rect = EditorGUILayout.GetControlRect(); float half = (rect.width - 8) / 2;
                for (int column = 0; column < 2; column++)
                {
                    int index = row * 2 + column;
                    var side = inset.FindPropertyRelative(fields[index]);
                    if (Integer(new Rect(rect.x + column * (half + 8), rect.y, half, rect.height), side, Content(labels[index])) && linkPadding)
                        foreach (string field in fields) inset.FindPropertyRelative(field).intValue = side.intValue;
                }
            }
            EditorGUIUtility.labelWidth = previous;
            if (!Is(LayoutMode.None))
            { GUILayout.Space(5); Alignment(alignment, chinese); }
        }
        private void DrawAdvice()
        {
            if (Is(LayoutMode.Flow) && !axis.hasMultipleDifferentValues)
            {
                var main = axis.intValue == 0 ? width : height;
                if (!main.hasMultipleDifferentValues && main.intValue == (int)SizeMode.Content)
                {
                    EditorGUILayout.HelpBox(T("A Content main axis is unbounded, so Flow will not wrap. Use Fixed or Keep on the main axis.", "主轴为内容尺寸时视为无限空间，Flow 不会换行。请将主轴设为固定或保持。"), MessageType.Info);
                    if (GUILayout.Button(T("Use current main-axis size", "用当前尺寸固定主轴"), EditorStyles.miniButton)) FreezeMainAxis();
                }
            }
            if (targets.Length != 1) return;
            if (HasNativeController(Node)) EditorGUILayout.HelpBox(T("Another enabled layout controller can compete for these transforms. Use one layout system for the controlled axes.", "当前存在已启用的原生布局控制器，可能与 Lyruis 同时驱动变换。请为受控轴选择一套布局系统。"), MessageType.Warning);
            int participating = CountChildren(Node, out int missing);
            if (missing > 0)
            {
                if (participating == 0 && !Is(LayoutMode.None))
                    EditorGUILayout.HelpBox(T("Only active direct children with LyruisLayout participate. Unregistered children keep their own placement.", "只有挂载且启用 LyruisLayout 的直接子节点参与布局。未注册的子节点保持原来的位置。"), MessageType.Info);
                if (GUILayout.Button(T("Register missing children (Keep size)", "注册缺少组件的子节点（保持尺寸）"))) RegisterChildren();
            }
        }
        private void DrawActions()
        {
            GUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!AnyActive()))
                    if (GUILayout.Button(Content(T("Rebuild root", "刷新根布局"), T("Rebuilds each connected root once, including ancestor size changes.", "每个相连根节点重建一次，包含祖先尺寸更新。")))) RebuildRoots();
                using (new EditorGUI.DisabledScope(targets.Length != 1 || FindRoot(Node) == Node))
                    if (GUILayout.Button(T("Select root", "选择根节点"))) Selection.activeObject = FindRoot(Node).gameObject;
            }
        }
        private void DrawContext()
        {
            contextOpen = EditorGUILayout.Foldout(contextOpen, T("Layout context", "布局上下文"), true);
            if (!contextOpen) return;
            if (targets.Length != 1) { Hint(T("Select one object to inspect its layout connections.", "选择单个对象以查看布局连接。")); return; }
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(T("Root", "根节点"), FindRoot(Node), typeof(LyruisLayout), true);
                EditorGUILayout.Vector2Field(T("Layout size", "布局尺寸"), Node.RectTransform.sizeDelta);
                EditorGUILayout.Vector2Field(T("Rendered rect", "实际矩形"), Node.RectTransform.rect.size);
                int active = CountChildren(Node, out int missing);
                EditorGUILayout.LabelField(T("Children", "子节点"), active + T(" active / ", " 个参与 / ") + missing + T(" missing component", " 个缺少组件"));
            }
        }
        private bool Is(LayoutMode value) => !mode.hasMultipleDifferentValues && mode.intValue == (int)value;
        private bool AnyActive() { foreach (var item in targets) if (((LyruisLayout)item).isActiveAndEnabled) return true; return false; }
        private static int CountChildren(LyruisLayout node, out int missing)
        {
            int active = 0; missing = 0;
            foreach (Transform child in node.transform)
            {
                if (!(child is RectTransform)) continue;
                var layout = child.GetComponent<LyruisLayout>();
                if (!layout) missing++; else if (layout.isActiveAndEnabled) active++;
            }
            return active;
        }
        private static LyruisLayout FindRoot(LyruisLayout node)
        {
            while (node.transform.parent && node.transform.parent.TryGetComponent(out LyruisLayout parent) && parent.isActiveAndEnabled) node = parent;
            return node;
        }
        private static bool HasNativeController(LyruisLayout node)
        {
            var group = node.GetComponent<LayoutGroup>(); var fit = node.GetComponent<ContentSizeFitter>(); var aspect = node.GetComponent<AspectRatioFitter>();
            return (group && group.isActiveAndEnabled) || (fit && fit.isActiveAndEnabled && (fit.horizontalFit != ContentSizeFitter.FitMode.Unconstrained || fit.verticalFit != ContentSizeFitter.FitMode.Unconstrained)) ||
                (aspect && aspect.isActiveAndEnabled && aspect.aspectMode != AspectRatioFitter.AspectMode.None);
        }
        private void ForEachSerialized(System.Action<LyruisLayout, SerializedObject> action, string title)
        {
            serializedObject.ApplyModifiedProperties(); Undo.SetCurrentGroupName(title);
            foreach (var item in targets)
            {
                var node = (LyruisLayout)item;
                using (var data = new SerializedObject(node))
                { data.Update(); action(node, data); data.ApplyModifiedProperties(); }
            }
            serializedObject.Update();
        }
        private void FitContent()
        {
            ForEachSerialized((node, data) =>
            {
                data.FindProperty("widthMode").intValue = (int)SizeMode.Content;
                data.FindProperty("heightMode").intValue = (int)SizeMode.Content;
                if (node.Mode == LayoutMode.Flow)
                {
                    bool horizontal = node.Axis == FlowAxis.Horizontal;
                    data.FindProperty(horizontal ? "widthMode" : "heightMode").intValue = (int)SizeMode.Fixed;
                    data.FindProperty("fixedSize").FindPropertyRelative(horizontal ? "x" : "y").floatValue = Mathf.Max(0, node.RectTransform.sizeDelta[horizontal ? 0 : 1]);
                }
            }, T("Fit Lyruis content", "Lyruis 适应内容"));
        }
        private void FreezeSize()
        {
            ForEachSerialized((node, data) =>
            {
                data.FindProperty("fixedSize").vector2Value = Vector2.Max(Vector2.zero, node.RectTransform.sizeDelta);
                data.FindProperty("widthMode").intValue = data.FindProperty("heightMode").intValue = (int)SizeMode.Fixed;
            }, T("Freeze Lyruis size", "Lyruis 固定当前尺寸"));
        }
        private void FreezeMainAxis()
        {
            ForEachSerialized((node, data) =>
            {
                bool horizontal = node.Axis == FlowAxis.Horizontal;
                data.FindProperty(horizontal ? "widthMode" : "heightMode").intValue = (int)SizeMode.Fixed;
                data.FindProperty("fixedSize").FindPropertyRelative(horizontal ? "x" : "y").floatValue = Mathf.Max(0, node.RectTransform.sizeDelta[horizontal ? 0 : 1]);
            }, T("Constrain Lyruis Flow", "Lyruis 固定换行主轴"));
        }
        private HashSet<LyruisLayout> Roots()
        { var roots = new HashSet<LyruisLayout>(); foreach (var item in targets) roots.Add(FindRoot((LyruisLayout)item)); return roots; }
        private static void RecordTransforms(HashSet<LyruisLayout> roots, string title)
        { foreach (var root in roots) Undo.RecordObjects(root.GetComponentsInChildren<RectTransform>(true), title); }
        private void RebuildRoots()
        {
            serializedObject.ApplyModifiedProperties(); var roots = Roots();
            RecordTransforms(roots, "Rebuild Lyruis layout");
            foreach (var root in roots) if (root.isActiveAndEnabled) root.RebuildNow();
            serializedObject.Update(); Repaint();
        }
        private void RegisterChildren()
        {
            serializedObject.ApplyModifiedProperties(); Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Register Lyruis children"); RecordTransforms(Roots(), "Register Lyruis children");
            foreach (var item in targets)
                foreach (Transform child in ((LyruisLayout)item).transform)
                    if (child is RectTransform && !child.GetComponent<LyruisLayout>()) Undo.AddComponent<LyruisLayout>(child.gameObject);
            Undo.CollapseUndoOperations(group); serializedObject.Update(); Repaint();
        }
    }
}
