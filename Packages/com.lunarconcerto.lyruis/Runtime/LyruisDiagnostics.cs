using System;
using System.Diagnostics;
using Unity.Profiling;

namespace LunarConcerto.Lyruis
{
    /// <summary>Optional markers and window-independent, main-thread diagnostic measurements.</summary>
    public static class LyruisDiagnostics
    {
        public enum Phase { Rebuild, FlushRequests, CompletionCallbacks, RootLookup, CollectChildren, ClearDrivenTracker, LeafIntrinsicSize, ManualBounds, MeasureContainer, ArrangeGeometry, ApplySelfSize, ApplyChildRects, DimensionChanged, Count }
        [Serializable] public struct PhaseResult
        {
            public string name;
            public long calls;
            public double inclusiveMs, selfMs;
        }
        public static bool DetailedProfiling { get; set; }
        public static bool IsMeasuring { get; private set; }
        private struct Entry { internal int phase; internal long start, children; }
        private static readonly Entry[] Stack = new Entry[128];
        private static readonly long[] Inclusive = new long[(int)Phase.Count], Self = new long[(int)Phase.Count], Calls = new long[(int)Phase.Count];
        private static int depth;
        public static void BeginMeasurement()
        {
            if (IsMeasuring) throw new InvalidOperationException("A Lyruis measurement is already active.");
            Array.Clear(Inclusive, 0, Inclusive.Length); Array.Clear(Self, 0, Self.Length); Array.Clear(Calls, 0, Calls.Length);
            depth = 0; IsMeasuring = true;
        }
        public static PhaseResult[] EndMeasurement()
        {
            if (!IsMeasuring || depth != 0) throw new InvalidOperationException("Measurement must end outside all layout scopes.");
            IsMeasuring = false;
            var result = new PhaseResult[(int)Phase.Count];
            double scale = 1000d / Stopwatch.Frequency;
            for (int i = 0; i < result.Length; i++) result[i] = new PhaseResult { name = ((Phase)i).ToString(), calls = Calls[i], inclusiveMs = Inclusive[i] * scale, selfMs = Self[i] * scale };
            return result;
        }
        private static int Push(Phase phase)
        {
            if (depth == Stack.Length) throw new InvalidOperationException("Lyruis measurement scope depth exceeded.");
            int index = depth++;
            Stack[index] = new Entry { phase = (int)phase, start = Stopwatch.GetTimestamp() };
            return index;
        }
        private static void Pop(int index)
        {
            if (index != depth - 1) throw new InvalidOperationException("Lyruis diagnostic scopes must be nested.");
            long elapsed = Stopwatch.GetTimestamp() - Stack[index].start;
            var entry = Stack[index]; depth--;
            Inclusive[entry.phase] += elapsed; Self[entry.phase] += Math.Max(0, elapsed - entry.children); Calls[entry.phase]++;
            if (depth > 0) Stack[depth - 1].children += elapsed;
        }
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
        internal static readonly ProfilerMarker SelfSize = new ProfilerMarker("Lyruis.ApplySelfSize");
        internal static readonly ProfilerMarker Children = new ProfilerMarker("Lyruis.ApplyChildRects");
        internal static readonly ProfilerMarker Dimensions = new ProfilerMarker("Lyruis.DimensionChanged");
        internal static Scope Sample(ProfilerMarker marker, Phase phase) => new Scope(marker, phase, DetailedProfiling);
        internal static Scope Coarse(ProfilerMarker marker, Phase phase) => new Scope(marker, phase, true);
        internal readonly struct Scope : IDisposable
        {
            private readonly ProfilerMarker marker;
            private readonly bool enabled;
            private readonly int index;
            internal Scope(ProfilerMarker marker, Phase phase, bool enabled)
            {
                this.marker = marker; this.enabled = enabled;
                index = IsMeasuring ? Push(phase) : -1;
                if (enabled) marker.Begin();
            }
            public void Dispose() { if (enabled) marker.End(); if (index >= 0) Pop(index); }
        }
    }
}
