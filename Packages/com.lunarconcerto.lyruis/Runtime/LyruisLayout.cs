using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LunarConcerto.Lyruis
{
    [ExecuteAlways, RequireComponent(typeof(RectTransform)), DisallowMultipleComponent]
    [AddComponentMenu("Layout/Lyruis Layout")]
    public sealed class LyruisLayout : UIBehaviour
    {
        [SerializeField] private LayoutMode mode = LayoutMode.Vertical;
        [SerializeField] private FlowAxis flowAxis;
        [SerializeField] private Vector2 spacing = new Vector2(8, 8);
        [SerializeField] private Vector2 cellSize;
        [SerializeField, Min(0)] private int gridCount = 2;
        [SerializeField] private SizeMode widthMode = SizeMode.Keep;
        [SerializeField] private SizeMode heightMode = SizeMode.Keep;
        [SerializeField] private Vector2 fixedSize = new Vector2(320, 240);
        [SerializeField] private RectOffset m_Padding = new RectOffset();
        [SerializeField] private TextAnchor m_ChildAlignment;
        private RectTransform rect;
        private LyruisLayout owner;
        private bool topologyDirty = true;
        internal readonly List<LyruisLayout> Children = new List<LyruisLayout>();
        internal readonly List<Vector2> Sizes = new List<Vector2>();
        internal readonly List<Rect> Slots = new List<Rect>();
        internal readonly List<LayoutEngine.Row> Rows = new List<LayoutEngine.Row>();
        internal Vector2 Measured;
        internal DrivenRectTransformTracker Tracker;
        internal bool Rebuilding;
        public event Action OnLayoutRefreshed;
        public RectTransform RectTransform => rect ? rect : rect = (RectTransform)transform;
        public LayoutMode Mode { get => mode; set => Set(ref mode, value); }
        public FlowAxis Axis { get => flowAxis; set => Set(ref flowAxis, value); }
        public Vector2 Spacing { get => spacing; set => Set(ref spacing, value); }
        public Vector2 CellSize { get => cellSize; set => Set(ref cellSize, value); }
        public int GridCount { get => gridCount; set => Set(ref gridCount, Mathf.Max(0, value)); }
        public SizeMode WidthMode { get => widthMode; set => Set(ref widthMode, value); }
        public SizeMode HeightMode { get => heightMode; set => Set(ref heightMode, value); }
        public Vector2 FixedSize { get => fixedSize; set => Set(ref fixedSize, value); }
        public RectOffset padding { get => m_Padding; set { m_Padding = value ?? new RectOffset(); Refresh(); } }
        public TextAnchor childAlignment { get => m_ChildAlignment; set => Set(ref m_ChildAlignment, value); }
        private void Set<T>(ref T field, T value)
        { if (EqualityComparer<T>.Default.Equals(field, value)) return; field = value; Refresh(); }
        internal LyruisLayout Root
        {
            get
            {
                var node = this;
                while (node.transform.parent && node.transform.parent.TryGetComponent(out LyruisLayout parent) && parent.IsActive()) node = parent;
                return node;
            }
        }
        public void Refresh() { if (IsActive() && !Root.Rebuilding) LayoutScheduler.Request(this); }
        public void RefreshRootLayout() => Refresh();
        public void RefreshLayout() => RebuildNow();
        public void RebuildNow() { if (IsActive()) LayoutTree.Rebuild(this); }
        public void RebuildRootNow() { if (IsActive()) LayoutTree.Rebuild(Root); }
        internal void CollectChildren()
        {
            if (!topologyDirty) return;
            Children.Clear();
            for (int i = 0; i < transform.childCount; i++)
                if (transform.GetChild(i).TryGetComponent(out LyruisLayout child) && child.IsActive()) Children.Add(child);
            topologyDirty = false;
        }
        internal void NotifyCompleted() => OnLayoutRefreshed?.Invoke();
        internal Vector2 ResolveCell()
        {
            if (cellSize == Vector2.zero && Sizes.Count > 0) cellSize = Sizes[0];
            return cellSize;
        }
        private void InvalidateOwner()
        {
            if (owner) { owner.topologyDirty = true; owner.Refresh(); }
            owner = transform.parent ? transform.parent.GetComponent<LyruisLayout>() : null;
            if (owner) { owner.topologyDirty = true; owner.Refresh(); }
        }
        protected override void OnEnable() { base.OnEnable(); topologyDirty = true; InvalidateOwner(); Refresh(); }
        protected override void OnDisable() { Tracker.Clear(); InvalidateOwner(); base.OnDisable(); }
        protected override void OnTransformParentChanged() { base.OnTransformParentChanged(); InvalidateOwner(); Refresh(); }
        private void OnTransformChildrenChanged() { topologyDirty = true; Refresh(); }
        protected override void OnRectTransformDimensionsChange() { if (IsActive() && Root == this) Refresh(); }
#if UNITY_EDITOR
        protected override void OnValidate() { base.OnValidate(); topologyDirty = true; LayoutScheduler.Request(this); }
#endif
    }
}
