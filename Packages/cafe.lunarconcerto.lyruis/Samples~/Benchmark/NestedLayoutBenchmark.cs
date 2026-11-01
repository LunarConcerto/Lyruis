using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace LunarConcerto.Lyruis.Samples
{
    /// <summary>Matched alternating-axis, content-sized trees; no rendering or Canvas scheduling.</summary>
    public static class NestedLayoutBenchmark
    {
        [Serializable] public sealed class Result
        {
            public int depth, branching, containers, leaves, totalNodes;
            public bool resizeLeaf;
            public double lyruisMedianMs, nativeMedianMs, lyruisP95Ms, nativeP95Ms;
            public float maxGeometryError;
        }
        [Serializable] public sealed class Report
        {
            public string unity = Application.unityVersion, platform = Application.platform.ToString();
            public string processor = SystemInfo.processorType, utc = DateTime.UtcNow.ToString("O");
            public bool editor = Application.isEditor, playMode = Application.isPlaying;
            public string engine = "Lyruis independent postorder tree";
            public int warmup = 20, samples = 40, rebuildsPerSample = 10;
            public List<Result> results = new List<Result>();
        }
        private sealed class Tree
        {
            public RectTransform root;
            public readonly List<RectTransform> nodes = new List<RectTransform>();
            public LayoutElement leaf;
            public int containers, leaves;
        }

        public static string RunSuite()
        {
            var report = new Report();
            int[] depths = { 3, 5, 7, 3 }, branches = { 2, 2, 2, 4 };
            for (int shape = 0; shape < depths.Length; shape++)
            foreach (bool resize in new[] { false, true })
            {
                var a = new Tree(); var b = new Tree();
                try
                {
                    a.root = Create(a, null, 0, depths[shape], branches[shape], true);
                    b.root = Create(b, null, 0, depths[shape], branches[shape], false);
                    for (int i = 0; i < report.warmup; i++)
                    { Step(a, resize, i); Step(b, resize, i); }
                    float error = Compare(a, b);
                    // Verify both mutation states, not just the state left by a timed batch.
                    if (resize)
                    {
                        for (int state = 0; state < 2; state++)
                        {
                            Step(a, true, state); Step(b, true, state);
                            error = Mathf.Max(error, Compare(a, b));
                        }
                    }
                    var ta = new double[report.samples]; var tb = new double[report.samples];
                    for (int i = 0; i < report.samples; i++)
                    {
                        if (i % 2 == 0)
                        { ta[i] = Time(a, resize, report.rebuildsPerSample); tb[i] = Time(b, resize, report.rebuildsPerSample); }
                        else
                        { tb[i] = Time(b, resize, report.rebuildsPerSample); ta[i] = Time(a, resize, report.rebuildsPerSample); }
                        error = Mathf.Max(error, Compare(a, b));
                    }
                    Array.Sort(ta); Array.Sort(tb);
                    report.results.Add(new Result { depth = depths[shape], branching = branches[shape],
                        containers = a.containers, leaves = a.leaves, totalNodes = a.nodes.Count, resizeLeaf = resize,
                        lyruisMedianMs = (ta[19] + ta[20]) / 2, nativeMedianMs = (tb[19] + tb[20]) / 2,
                        lyruisP95Ms = ta[37], nativeP95Ms = tb[37], maxGeometryError = error });
                }
                finally
                {
                    if (a.root != null) UnityEngine.Object.DestroyImmediate(a.root.gameObject);
                    if (b.root != null) UnityEngine.Object.DestroyImmediate(b.root.gameObject);
                }
            }
            return JsonUtility.ToJson(report, true);
        }
        private static void Step(Tree tree, bool resize, int index)
        {
            if (resize)
            {
                tree.leaf.minWidth = tree.leaf.preferredWidth = index % 2 == 0 ? 40 : 73;
                tree.leaf.minHeight = tree.leaf.preferredHeight = index % 2 == 0 ? 24 : 39;
            }
            if (tree.root.TryGetComponent(out LyruisLayout layout)) layout.RebuildNow();
            else LayoutRebuilder.ForceRebuildLayoutImmediate(tree.root);
        }
        private static double Time(Tree tree, bool resize, int repetitions)
        {
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < repetitions; i++) Step(tree, resize, i);
            return (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency / repetitions;
        }
        private static float Compare(Tree a, Tree b)
        {
            if (a.nodes.Count != b.nodes.Count) throw new InvalidOperationException("Tree topology differs.");
            float error = 0;
            for (int i = 0; i < a.nodes.Count; i++)
            {
                var x = a.nodes[i]; var y = b.nodes[i];
                if (!float.IsFinite(x.rect.width) || !float.IsFinite(x.rect.height) ||
                    !float.IsFinite(y.rect.width) || !float.IsFinite(y.rect.height))
                    throw new InvalidOperationException("Non-finite layout geometry.");
                error = Mathf.Max(error, Vector2.Distance(x.rect.size, y.rect.size), Vector2.Distance(x.anchoredPosition, y.anchoredPosition));
            }
            if (error > .01f) throw new InvalidOperationException("Nested geometry differs by " + error + " pixels; timing rejected.");
            return error;
        }
        private static RectTransform Create(Tree tree, Transform parent, int level, int depth, int branching, bool lyruis)
        {
            var rect = new GameObject("Level " + level, typeof(RectTransform)).GetComponent<RectTransform>();
            if (parent != null) rect.SetParent(parent, false);
            else tree.root = rect;
            tree.nodes.Add(rect);
            if (level == depth)
            {
                var element = rect.gameObject.AddComponent<LayoutElement>();
                int variant = tree.leaves++ % 4;
                element.minWidth = element.preferredWidth = 40 + variant * 7;
                element.minHeight = element.preferredHeight = 24 + variant * 3;
                if (tree.leaf == null) tree.leaf = element;
                if (lyruis) { var item = rect.gameObject.AddComponent<LyruisLayout>(); item.WidthMode = item.HeightMode = SizeMode.Content; }
                return rect;
            }
            tree.containers++;
            var padding = new RectOffset(3 + level, 5, 4, 6 + level);
            if (lyruis)
            {
                var layout = rect.gameObject.AddComponent<LyruisLayout>();
                layout.Mode = level % 2 == 0 ? LayoutMode.Vertical : LayoutMode.Horizontal;
                layout.WidthMode = layout.HeightMode = SizeMode.Content;
                layout.Spacing = new Vector2(4, 4); layout.padding = padding;
            }
            else
            {
                HorizontalOrVerticalLayoutGroup layout = level % 2 == 0 ?
                    (HorizontalOrVerticalLayoutGroup)rect.gameObject.AddComponent<VerticalLayoutGroup>() : rect.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 4; layout.padding = padding;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;
                // Parents already assign preferred sizes to nested groups. Only the root needs a fitter.
                if (parent == null)
                {
                    var fit = rect.gameObject.AddComponent<ContentSizeFitter>();
                    fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
            }
            for (int i = 0; i < branching; i++) Create(tree, rect, level + 1, depth, branching, lyruis);
            return rect;
        }
    }
}
