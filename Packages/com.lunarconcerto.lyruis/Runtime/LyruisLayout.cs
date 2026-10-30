using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LunarConcerto.Lyruis
{
    [AddComponentMenu("Layout/Lyruis Layout")]
    [DisallowMultipleComponent]
    public sealed class LyruisLayout : LayoutGroup, ILayoutSelfController
    {
        [SerializeField] private LayoutMode mode = LayoutMode.Vertical;
        [SerializeField] private FlowAxis flowAxis;
        [SerializeField] private Vector2 spacing = new Vector2(8, 8);
        [SerializeField] private Vector2 cellSize = new Vector2(80, 80);
        [SerializeField, Min(1)] private int gridCount = 4;
        [SerializeField] private SizeMode widthMode = SizeMode.Keep;
        [SerializeField] private SizeMode heightMode = SizeMode.Keep;
        [SerializeField] private Vector2 fixedSize = new Vector2(320, 240);
        private readonly List<Vector2> sizes = new List<Vector2>();
        private readonly List<Rect> slots = new List<Rect>();
        private Vector2 content;

        public LayoutMode Mode { get => mode; set => SetProperty(ref mode, value); }
        public FlowAxis Axis { get => flowAxis; set => SetProperty(ref flowAxis, value); }
        public Vector2 Spacing { get => spacing; set => SetProperty(ref spacing, value); }
        public Vector2 CellSize { get => cellSize; set => SetProperty(ref cellSize, value); }
        public int GridCount { get => gridCount; set => SetProperty(ref gridCount, Mathf.Max(1, value)); }
        public SizeMode WidthMode { get => widthMode; set => SetProperty(ref widthMode, value); }
        public SizeMode HeightMode { get => heightMode; set => SetProperty(ref heightMode, value); }
        public Vector2 FixedSize { get => fixedSize; set => SetProperty(ref fixedSize, value); }
        public void Refresh() => SetDirty();
        public void RebuildNow() => LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        private void Measure()
        {
            sizes.Clear();
            foreach (RectTransform child in rectChildren)
            {
                if (mode == LayoutMode.Grid) { sizes.Add(cellSize); continue; }
                float w = LayoutUtility.GetPreferredWidth(child);
                float h = LayoutUtility.GetPreferredHeight(child);
                sizes.Add(new Vector2(Mathf.Max(0, w), Mathf.Max(0, h)));
            }
            int main = flowAxis == FlowAxis.Horizontal ? 0 : 1;
            SizeMode rule = main == 0 ? widthMode : heightMode;
            float available = rule == SizeMode.Content ? float.PositiveInfinity :
                (rule == SizeMode.Fixed ? fixedSize[main] : rectTransform.rect.size[main]) -
                (main == 0 ? padding.horizontal : padding.vertical);
            content = LayoutEngine.Solve(mode, flowAxis, sizes,
                Vector2.Max(Vector2.zero, spacing), Vector2.Max(Vector2.zero, cellSize), gridCount,
                Mathf.Max(0, available), slots);
            if (rectChildren.Count == 0)
            {
                content = new Vector2(
                    LayoutUtility.GetLayoutProperty(rectTransform, e => ReferenceEquals(e, this) ? -1 : e.preferredWidth, 0),
                    LayoutUtility.GetLayoutProperty(rectTransform, e => ReferenceEquals(e, this) ? -1 : e.preferredHeight, 0));
            }
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            Measure();
            float width = widthMode == SizeMode.Fixed ? Mathf.Max(0, fixedSize.x) :
                widthMode == SizeMode.Keep ? rectTransform.rect.width : content.x + padding.horizontal;
            SetLayoutInputForAxis(width, width, 0, 0);
        }
        public override void CalculateLayoutInputVertical()
        {
            Measure();
            float height = heightMode == SizeMode.Fixed ? Mathf.Max(0, fixedSize.y) :
                heightMode == SizeMode.Keep ? rectTransform.rect.height : content.y + padding.vertical;
            SetLayoutInputForAxis(height, height, 0, 1);
        }
        public override void SetLayoutHorizontal() => Arrange(0);
        public override void SetLayoutVertical() => Arrange(1);
        private void Arrange(int axis)
        {
            SizeMode rule = axis == 0 ? widthMode : heightMode;
            if (rule != SizeMode.Keep)
            {
                m_Tracker.Add(this, rectTransform, axis == 0 ? DrivenTransformProperties.SizeDeltaX : DrivenTransformProperties.SizeDeltaY);
                rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis,
                    axis == 0 ? preferredWidth : preferredHeight);
            }
            Measure();
            float origin = GetStartOffset(axis, content[axis]);
            for (int i = 0; i < rectChildren.Count; i++)
            {
                Rect slot = slots[i];
                float extra = 0;
                if ((mode == LayoutMode.Horizontal && axis == 1) || (mode == LayoutMode.Vertical && axis == 0) || mode == LayoutMode.Overlay)
                    extra = (content[axis] - slot.size[axis]) * GetAlignmentOnAxis(axis);
                SetChildAlongAxis(rectChildren[i], axis, origin + slot.position[axis] + extra, slot.size[axis]);
            }
        }
    }
}
