using System.Collections.Generic;
using UnityEngine;

namespace LunarConcerto.Lyruis
{
    public enum LayoutMode { Overlay, Horizontal, Vertical, Grid, Flow }
    public enum FlowAxis { Horizontal, Vertical }
    public enum SizeMode { Keep, Fixed, Content }

    /// <summary>Geometry only. All coordinates are measured from the top-left.</summary>
    internal static class LayoutEngine
    {
        internal static Vector2 Solve(LayoutMode mode, FlowAxis axis, IList<Vector2> sizes,
            Vector2 gap, Vector2 cell, int count, float limit, List<Rect> slots)
        {
            slots.Clear();
            bool transpose = mode == LayoutMode.Vertical ||
                ((mode == LayoutMode.Flow || mode == LayoutMode.Grid) && axis == FlowAxis.Vertical);
            Vector2 step = transpose ? new Vector2(gap.y, gap.x) : gap;
            Vector2 tile = transpose ? new Vector2(cell.y, cell.x) : cell;
            float cursor = 0, cross = 0, line = 0, maxMain = 0, maxCross = 0;
            int inLine = 0;
            for (int i = 0; i < sizes.Count; i++)
            {
                Vector2 size = transpose ? new Vector2(sizes[i].y, sizes[i].x) : sizes[i];
                if (mode == LayoutMode.Grid) size = tile;
                bool wrap = mode == LayoutMode.Grid ? inLine >= Mathf.Max(1, count) :
                    mode == LayoutMode.Flow && inLine > 0 && cursor + size.x > limit;
                if (wrap) { cross += line + step.y; cursor = 0; line = 0; inLine = 0; }
                Rect slot = new Rect(cursor, cross, size.x, size.y);
                if (mode == LayoutMode.Overlay) slot.position = Vector2.zero;
                slots.Add(transpose ? new Rect(slot.y, slot.x, slot.height, slot.width) : slot);
                maxMain = Mathf.Max(maxMain, slot.xMax);
                maxCross = Mathf.Max(maxCross, slot.yMax);
                if (mode != LayoutMode.Overlay) cursor += size.x + step.x;
                line = Mathf.Max(line, size.y);
                inLine++;
            }
            return transpose ? new Vector2(maxCross, maxMain) : new Vector2(maxMain, maxCross);
        }
    }
}
