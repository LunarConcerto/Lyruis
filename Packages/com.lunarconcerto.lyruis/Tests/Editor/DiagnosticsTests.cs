using System;
using NUnit.Framework;
using UnityEngine;

namespace LunarConcerto.Lyruis.Tests
{
    public class DiagnosticsTests
    {
        [Test] public void MeasurementsWorkWithoutDetailedMarkersAndDoNotDoubleCountNestedTime()
        {
            var go = new GameObject("Diagnostic test", typeof(RectTransform));
            bool previous = LyruisDiagnostics.DetailedProfiling;
            try
            {
                var layout = go.AddComponent<LyruisLayout>();
                layout.WidthMode = layout.HeightMode = SizeMode.Fixed;
                layout.RebuildNow();
                LyruisDiagnostics.DetailedProfiling = false;
                LyruisDiagnostics.BeginMeasurement();
                layout.RebuildNow();
                var phases = LyruisDiagnostics.EndMeasurement();
                var rebuild = phases[(int)LyruisDiagnostics.Phase.Rebuild];
                Assert.AreEqual(1, rebuild.calls);
                Assert.AreEqual(1, phases[(int)LyruisDiagnostics.Phase.LeafIntrinsicSize].calls);
                double selfSum = 0;
                foreach (var phase in phases)
                {
                    Assert.GreaterOrEqual(phase.selfMs, 0);
                    Assert.GreaterOrEqual(phase.inclusiveMs + .000001, phase.selfMs);
                    selfSum += phase.selfMs;
                }
                Assert.AreEqual(rebuild.inclusiveMs, selfSum, .000001);
                LyruisDiagnostics.BeginMeasurement();
                var empty = LyruisDiagnostics.EndMeasurement();
                foreach (var phase in empty) Assert.AreEqual(0, phase.calls);
            }
            finally
            {
                if (LyruisDiagnostics.IsMeasuring) LyruisDiagnostics.EndMeasurement();
                LyruisDiagnostics.DetailedProfiling = previous;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
        [Test] public void ConcurrentMeasurementsAreRejected()
        {
            LyruisDiagnostics.BeginMeasurement();
            try { Assert.Throws<InvalidOperationException>(() => LyruisDiagnostics.BeginMeasurement()); }
            finally { LyruisDiagnostics.EndMeasurement(); }
        }
    }
}
