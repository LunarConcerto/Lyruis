using System.Collections.Generic;
using UnityEngine;

namespace LunarConcerto.Lyruis
{
    public enum LayoutMode { None = 0, Horizontal = 1, Vertical = 2, Grid = 3, Flow = 4, Overlay = 5 }
    public enum FlowAxis { Horizontal, Vertical }
    public enum SizeMode { Keep, Fixed, Content }

    internal static class LayoutEngine
    {
        internal struct Row { internal int begin, end; internal float main, cross; }
        private static Vector2 Flip(Vector2 value, bool flip) => flip ? new Vector2(value.y, value.x) : value;
        internal static Vector2 Measure(LyruisLayout node, Vector2 keep)
        {
            node.Rows.Clear();
            if (node.Mode == LayoutMode.Grid)
            {
                Vector2 cell = node.ResolveCell();
                int count = node.GridCount == 0 ? 2 : node.GridCount;
                int first = Mathf.Min(count, node.Sizes.Count), second = (node.Sizes.Count + count - 1) / count;
                var dimensions = node.Axis == FlowAxis.Horizontal ? new Vector2(first, second) : new Vector2(second, first);
                return Vector2.Scale(dimensions, cell) + Vector2.Scale(dimensions - Vector2.one, node.Spacing);
            }
            bool flip = node.Mode == LayoutMode.Vertical || (node.Mode == LayoutMode.Flow && node.Axis == FlowAxis.Vertical);
            Vector2 gap = Flip(node.Spacing, flip);
            int axis = flip ? 1 : 0;
            var rule = axis == 0 ? node.WidthMode : node.HeightMode;
            float limit = rule == SizeMode.Content ? float.PositiveInfinity : Mathf.Max(0,
                (rule == SizeMode.Fixed ? node.FixedSize[axis] : keep[axis]) - (axis == 0 ? node.padding.horizontal : node.padding.vertical));
            Row row = default;
            Vector2 block = Vector2.zero;
            for (int i = 0; i < node.Sizes.Count; i++)
            {
                Vector2 size = Flip(node.Sizes[i], flip);
                if (node.Mode == LayoutMode.Overlay) { block = Vector2.Max(block, size); continue; }
                float next = row.end == row.begin ? size.x : row.main + gap.x + size.x;
                if (node.Mode == LayoutMode.Flow && limit > 0 && row.end > row.begin && next > limit)
                {
                    node.Rows.Add(row); block.x = Mathf.Max(block.x, row.main); block.y += row.cross + gap.y;
                    row = new Row { begin = i, end = i }; next = size.x;
                }
                row.end = i + 1; row.main = next; row.cross = Mathf.Max(row.cross, size.y);
            }
            if (node.Mode != LayoutMode.Overlay)
            {
                node.Rows.Add(row); block.x = Mathf.Max(block.x, row.main); block.y += row.cross;
            }
            return Flip(block, flip);
        }
        private static float Factor(LyruisLayout node, int axis) => axis == 0 ? (int)node.childAlignment % 3 * .5f : (int)node.childAlignment / 3 * .5f;
        // Legacy linear/grid alignment uses the selected edge's padding, rather than interpolated padding.
        private static float EdgeOffset(LyruisLayout node, int axis, float occupied)
        {
            float factor = Factor(node, axis);
            float pad = factor == 0 ? (axis == 0 ? node.padding.left : node.padding.top) :
                factor == 1 ? -(axis == 0 ? node.padding.right : node.padding.bottom) : 0;
            return (node.Measured[axis] - occupied) * factor + pad;
        }
        internal static void Arrange(LyruisLayout node)
        {
            node.Slots.Clear();
            if (node.Mode == LayoutMode.Grid)
            {
                int count = node.GridCount == 0 ? 2 : node.GridCount;
                Vector2 cell = node.ResolveCell();
                int mainCount = Mathf.Min(count, node.Sizes.Count), crossCount = (node.Sizes.Count + count - 1) / count;
                Vector2 dimensions = node.Axis == FlowAxis.Horizontal ? new Vector2(mainCount, crossCount) : new Vector2(crossCount, mainCount);
                Vector2 block = Vector2.Scale(dimensions, cell) + Vector2.Scale(dimensions - Vector2.one, node.Spacing);
                Vector2 origin = new Vector2(EdgeOffset(node, 0, block.x), EdgeOffset(node, 1, block.y));
                for (int i = 0; i < node.Sizes.Count; i++)
                {
                    Vector2 index = node.Axis == FlowAxis.Horizontal ? new Vector2(i % count, i / count) : new Vector2(i / count, i % count);
                    node.Slots.Add(new Rect(origin + Vector2.Scale(index, cell + node.Spacing), node.Sizes[i]));
                }
                return;
            }
            if (node.Mode == LayoutMode.Overlay)
            {
                foreach (var size in node.Sizes) node.Slots.Add(new Rect(new Vector2(EdgeOffset(node, 0, size.x), EdgeOffset(node, 1, size.y)), size));
                return;
            }
            bool flip = node.Mode == LayoutMode.Vertical || (node.Mode == LayoutMode.Flow && node.Axis == FlowAxis.Vertical);
            int main = flip ? 1 : 0, cross = 1 - main;
            Vector2 gap = Flip(node.Spacing, flip);
            bool flow = node.Mode == LayoutMode.Flow;
            float totalCross = 0;
            foreach (var row in node.Rows) totalCross += row.cross;
            totalCross += gap.y * Mathf.Max(0, node.Rows.Count - 1);
            float mainPad = main == 0 ? node.padding.left : node.padding.top;
            float crossPad = cross == 0 ? node.padding.left : node.padding.top;
            float mainInner = Mathf.Max(0, node.Measured[main] - (main == 0 ? node.padding.horizontal : node.padding.vertical));
            float crossInner = Mathf.Max(0, node.Measured[cross] - (cross == 0 ? node.padding.horizontal : node.padding.vertical));
            float crossCursor = flow ? crossPad + Mathf.Max(0, crossInner - totalCross) * Factor(node, cross) : 0;
            foreach (var row in node.Rows)
            {
                float mainCursor = flow ? mainPad + Mathf.Max(0, mainInner - row.main) * Factor(node, main) : EdgeOffset(node, main, row.main);
                for (int i = row.begin; i < row.end; i++)
                {
                    Vector2 size = Flip(node.Sizes[i], flip);
                    float crossPosition = flow ? crossCursor + Mathf.Max(0, row.cross - size.y) * Factor(node, cross) : EdgeOffset(node, cross, size.y);
                    node.Slots.Add(new Rect(Flip(new Vector2(mainCursor, crossPosition), flip), node.Sizes[i]));
                    mainCursor += size.x + gap.x;
                }
                crossCursor += row.cross + gap.y;
            }
        }
    }
}
