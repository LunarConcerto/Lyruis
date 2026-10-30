using System;
using Unity.Profiling;

namespace LunarConcerto.Lyruis
{
    /// <summary>Optional fine-grained markers. Coarse root markers remain available regardless.</summary>
    public static class LyruisDiagnostics
    {
        public static bool DetailedProfiling { get; set; }
        internal static readonly ProfilerMarker Rebuild = new ProfilerMarker("Lyruis.Rebuild");
        internal static readonly ProfilerMarker Flush = new ProfilerMarker("Lyruis.FlushRequests");
        internal static readonly ProfilerMarker Complete = new ProfilerMarker("Lyruis.CompletionCallbacks");
        internal static readonly ProfilerMarker RootLookup = new ProfilerMarker("Lyruis.RootLookup");
        internal static readonly ProfilerMarker Topology = new ProfilerMarker("Lyruis.CollectChildren");
        internal static readonly ProfilerMarker Tracker = new ProfilerMarker("Lyruis.ClearDrivenTracker");
        internal static readonly ProfilerMarker Intrinsic = new ProfilerMarker("Lyruis.LeafIntrinsicSize");
        internal static readonly ProfilerMarker Bounds = new ProfilerMarker("Lyruis.ManualBounds");
        internal static readonly ProfilerMarker Measure = new ProfilerMarker("Lyruis.MeasureContainer");
        internal static readonly ProfilerMarker Arrange = new ProfilerMarker("Lyruis.ArrangeGeometry");
        internal static readonly ProfilerMarker Self = new ProfilerMarker("Lyruis.ApplySelfSize");
        internal static readonly ProfilerMarker Children = new ProfilerMarker("Lyruis.ApplyChildRects");
        internal static readonly ProfilerMarker Dimensions = new ProfilerMarker("Lyruis.DimensionChanged");
        internal static Scope Sample(ProfilerMarker marker) => new Scope(marker, DetailedProfiling);
        internal readonly struct Scope : IDisposable
        {
            private readonly ProfilerMarker marker;
            private readonly bool enabled;
            internal Scope(ProfilerMarker marker, bool enabled)
            { this.marker = marker; this.enabled = enabled; if (enabled) marker.Begin(); }
            public void Dispose() { if (enabled) marker.End(); }
        }
    }
}
