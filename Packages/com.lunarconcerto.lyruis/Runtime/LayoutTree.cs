using UnityEngine;
using UnityEngine.UI;

namespace LunarConcerto.Lyruis
{
    internal static class LayoutTree
    {
        internal static void Rebuild(LyruisLayout node)
        {
            using (LyruisDiagnostics.Rebuild.Auto()) RebuildCore(node);
        }
        private static void RebuildCore(LyruisLayout node)
        {
            var root = node.Root;
            if (root.Rebuilding) return;
            root.Rebuilding = true;
            try { Visit(node); }
            finally { root.Rebuilding = false; }
            using (LyruisDiagnostics.Complete.Auto()) Complete(node);
        }
        private static void Complete(LyruisLayout node)
        {
            foreach (var child in node.Children) if (child && child.isActiveAndEnabled) Complete(child);
            node.NotifyCompleted();
        }
        private static float Resolve(SizeMode rule, float fixedValue, float keep, float content)
        => rule == SizeMode.Fixed ? fixedValue : rule == SizeMode.Content ? content : keep;
        private static void Visit(LyruisLayout node)
        {
            using (LyruisDiagnostics.Sample(LyruisDiagnostics.Topology)) node.CollectChildren();
            using (LyruisDiagnostics.Sample(LyruisDiagnostics.Tracker)) node.Tracker.Clear();
            node.Sizes.Clear();
            foreach (var child in node.Children)
            { Visit(child); node.Sizes.Add(child.Measured); }
            var rect = node.RectTransform;
            Vector2 keep = rect.sizeDelta;
            Vector2 preferred;
            if (node.Children.Count == 0)
            {
                using (LyruisDiagnostics.Sample(LyruisDiagnostics.Intrinsic))
                {
                    // uGUI is an intrinsic-size provider only; width is applied before measuring wrapped height.
                    float w = node.WidthMode == SizeMode.Content ? LayoutUtility.GetPreferredWidth(rect) : keep.x;
                    float resolvedWidth = Resolve(node.WidthMode, node.FixedSize.x, keep.x, w);
                    if (rect.sizeDelta.x != resolvedWidth) rect.sizeDelta = new Vector2(resolvedWidth, keep.y);
                    float h = node.HeightMode == SizeMode.Content ? LayoutUtility.GetPreferredHeight(rect) : keep.y;
                    preferred = new Vector2(w, h);
                }
            }
            else if (node.Mode == LayoutMode.None)
            {
                using (LyruisDiagnostics.Sample(LyruisDiagnostics.Bounds))
                {
                    var bounds = new Bounds(Vector3.zero, Vector3.zero);
                    foreach (var child in node.Children)
                    bounds.Encapsulate(RectTransformUtility.CalculateRelativeRectTransformBounds(rect, child.RectTransform));
                    preferred = (Vector2)bounds.size + new Vector2(node.padding.horizontal, node.padding.vertical);
                }
            }
            else
            {
                using (LyruisDiagnostics.Sample(LyruisDiagnostics.Measure))
                preferred = LayoutEngine.Measure(node, keep) + new Vector2(node.padding.horizontal, node.padding.vertical);
            }
            node.Measured = new Vector2(Resolve(node.WidthMode, node.FixedSize.x, keep.x, preferred.x),
            Resolve(node.HeightMode, node.FixedSize.y, keep.y, preferred.y));
            using (LyruisDiagnostics.Sample(LyruisDiagnostics.Self))
            {
                var driven = DrivenTransformProperties.None;
                if (node.WidthMode != SizeMode.Keep) driven |= DrivenTransformProperties.SizeDeltaX;
                if (node.HeightMode != SizeMode.Keep) driven |= DrivenTransformProperties.SizeDeltaY;
                node.Tracker.Add(node, rect, driven);
                if (rect.sizeDelta != node.Measured) rect.sizeDelta = node.Measured;
            }
            if (node.Mode == LayoutMode.None || node.Children.Count == 0) return;
            using (LyruisDiagnostics.Sample(LyruisDiagnostics.Arrange)) LayoutEngine.Arrange(node);
            using (LyruisDiagnostics.Sample(LyruisDiagnostics.Children))
            {
                for (int i = 0; i < node.Children.Count; i++)
                {
                    var target = node.Children[i].RectTransform;
                    var slot = node.Slots[i];
                    node.Tracker.Add(node, target, DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.SizeDelta);
                    target.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, slot.x, slot.width);
                    target.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, slot.y, slot.height);
                }
            }
        }
    }
}
