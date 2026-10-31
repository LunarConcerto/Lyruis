using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LunarConcerto.Lyruis.Samples
{
    // Bounded stress sweep: observed tail costs, not a theoretical hardware capacity claim.
    public sealed class LyruisPeakSweep : MonoBehaviour
    {
        public int[] cardCounts = { 12, 24, 48, 80 };
        public int samples = 40, warmups = 6, diagnosticSamples = 4;
        public bool IsRunning { get; private set; }
        public string ReportPath { get; private set; }
        [Serializable] public sealed class Row
        {
            public string name;
            public int cards, nodes, batch, rebuildsPerOperation;
            public double medianMs, p95Ms, maxMs, meanRootCompletions;
            public double[] rawMs;
            public long[] allocatedBytes;
            public string geometryHash;
            public long geometryOffset;
            public List<LyruisDiagnostics.PhaseResult> phases = new List<LyruisDiagnostics.PhaseResult>();
        }
        [Serializable] public sealed class Report
        {
            public string label, utc, unity, processor, geometryFile;
            public int width, height, samples, warmups, diagnosticSamples;
            public bool externalProfiler;
            public List<Row> rows = new List<Row>();
        }
        private LyruisProfilerLab original, test;
        private bool originalActive;
        private LyruisProfilerLab.SavedState saved;
        private Coroutine routine;
        [ContextMenu("Run Current Peak Sweep (Play mode, separate GameObject)")]
        public void RunCurrent() => Run("current");
        public void Run(string label)
        {
            if (IsRunning || !Application.isPlaying) throw new InvalidOperationException("Peak sweep requires Play mode and no other sweep.");
            original = FindObjectOfType<LyruisProfilerLab>();
            if (original && original.AutomationRunning) throw new InvalidOperationException("Finish the current automatic measurement first.");
            if (original && transform.IsChildOf(original.transform))
                throw new InvalidOperationException("Attach the peak sweep to a separate GameObject so it remains active while the original lab is disabled.");
            if (original) { originalActive = original.gameObject.activeSelf; original.gameObject.SetActive(false); }
            IsRunning = true; routine = StartCoroutine(Sweep(label));
        }
        private void OnDisable() { if (IsRunning) { if (routine != null) StopCoroutine(routine); Cleanup(); } }
        private void CleanupTest()
        {
            if (!test) return;
            if (saved != null) { test.EndAutomation(saved); saved = null; }
            test.gameObject.SetActive(false);
            Destroy(test.gameObject); test = null;
        }
        private void Cleanup()
        {
            CleanupTest();
            if (original) original.gameObject.SetActive(originalActive);
            IsRunning = false; routine = null;
        }
        private IEnumerator Sweep(string label)
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkResults", "PeakSweep");
            Directory.CreateDirectory(directory);
            string stem = "peak-" + label + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var report = new Report { label = label, utc = DateTime.UtcNow.ToString("O"), unity = Application.unityVersion,
                processor = SystemInfo.processorType, width = Screen.width, height = Screen.height, samples = Mathf.Max(8, samples),
                warmups = Mathf.Max(2, warmups), diagnosticSamples = Mathf.Max(1, diagnosticSamples), externalProfiler = UnityEngine.Profiling.Profiler.enabled,
                geometryFile = stem + ".geometry.bin" };
            try
            {
                using (var geometry = new BinaryWriter(File.Create(Path.Combine(directory, report.geometryFile))))
                foreach (int cards in cardCounts)
                {
                    var go = new GameObject("Temporary peak lab " + cards); go.SetActive(false);
                    test = go.AddComponent<LyruisProfilerLab>(); test.cardCount = cards; test.Build(); go.SetActive(true);
                    yield return null; // Let Start enable providers and background execution.
                    saved = test.BeginAutomation();
                    for (int settle = 0; settle < 8; settle++) yield return null;
                    var nodes = test.LayoutRoot.GetComponentsInChildren<LyruisLayout>();
                    for (int scenario = 0; scenario < 6; scenario++)
                    {
                        var mode = scenario < 3 ? (scenario == 0 ? LyruisProfilerLab.Workload.SingleLeaf : LyruisProfilerLab.Workload.BatchLeaves) :
                            scenario < 5 ? LyruisProfilerLab.Workload.ForceRootEveryFrame : LyruisProfilerLab.Workload.ResizeRoot;
                        int repeats = scenario == 4 ? 4 : 1;
                        int batch = scenario == 2 ? test.TargetCount : scenario == 1 ? 32 : 1;
                        string name = new[] { "SingleLeaf", "Batch32", "BatchAll", "Root1", "Root4", "Resize" }[scenario];
                        test.batchSize = batch; test.PrepareMeasurement(mode);
                        test.ShowMeasurementStatus("PEAK SWEEP / " + label + " / " + cards + " cards / " + name);
                        for (int i = 0; i < report.warmups; i++) { yield return null; Step(repeats); }
                        var row = new Row { name = name, cards = cards, nodes = nodes.Length, batch = batch,
                            rebuildsPerOperation = repeats, rawMs = new double[report.samples], allocatedBytes = new long[report.samples] };
                        int completed = 0;
                        for (int i = 0; i < report.samples; i++)
                        {
                            yield return null;
                            int before = test.CompletedRebuilds;
                            long allocation = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
                            Step(repeats);
                            row.rawMs[i] = (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;
                            row.allocatedBytes[i] = GC.GetAllocatedBytesForCurrentThread() - allocation;
                            completed += test.CompletedRebuilds - before;
                        }
                        row.geometryOffset = geometry.BaseStream.Position;
                        row.geometryHash = WriteGeometry(nodes, geometry);
                        row.meanRootCompletions = completed / (double)report.samples;
                        var sorted = (double[])row.rawMs.Clone(); Array.Sort(sorted);
                        row.medianMs = (sorted[(sorted.Length - 1) / 2] + sorted[sorted.Length / 2]) / 2;
                        row.p95Ms = sorted[Mathf.CeilToInt(.95f * sorted.Length) - 1]; row.maxMs = sorted[sorted.Length - 1];
                        var totals = new LyruisDiagnostics.PhaseResult[(int)LyruisDiagnostics.Phase.Count];
                        for (int i = 0; i < report.diagnosticSamples; i++)
                        {
                            yield return null;
                            LyruisDiagnostics.BeginMeasurement();
                            try { Step(repeats); }
                            finally
                            {
                                var parts = LyruisDiagnostics.EndMeasurement();
                                for (int p = 0; p < totals.Length; p++)
                                { totals[p].name = parts[p].name; totals[p].calls += parts[p].calls; totals[p].inclusiveMs += parts[p].inclusiveMs; totals[p].selfMs += parts[p].selfMs; }
                            }
                        }
                        row.phases.AddRange(totals); report.rows.Add(row);
                    }
                    CleanupTest(); yield return null;
                }
                ReportPath = Path.Combine(directory, stem + ".json");
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
                var b = new StringBuilder("# Peak sweep / 峰值扫描\n\n" + report.utc + " / " + report.label + " / " + report.unity + "\n\n");
                b.AppendLine("Measured operation includes explicit Canvas flush, not a full frame. Root4 performs four synchronous rebuilds per operation; other cases one mutation/rebuild. P95 and Max are observed samples, not guaranteed bounds. The 16.67ms column compares only this operation with a reference budget, not measured FPS.\n");
                b.AppendLine("| Cards | Nodes | Workload | Median ms | P95 ms | Max ms | P95 > 16.67ms |\n| ---: | ---: | --- | ---: | ---: | ---: | --- |");
                foreach (var r in report.rows) b.AppendLine(string.Format(CultureInfo.InvariantCulture,"| {0} | {1} | {2} | {3:F3} | {4:F3} | {5:F3} | {6} |",r.cards,r.nodes,r.name,r.medianMs,r.p95Ms,r.maxMs,r.p95Ms>16.67));
                b.AppendLine("\nRaw timing samples and diagnostic phase totals are retained in adjacent JSON. Geometry file stores 10 Float32 values per node: sizeDelta, anchoredPosition, anchorMin, anchorMax, pivot. Offsets in JSON allow exact before/after comparison. Diagnostic samples are excluded from timing statistics. No existing scene content is overwritten.\n");
                File.WriteAllText(Path.ChangeExtension(ReportPath,".md"),b.ToString().TrimEnd()+"\n");
                Debug.Log("Lyruis peak sweep complete: " + ReportPath);
            }
            finally { Cleanup(); }
        }
        private void Step(int repeats)
        {
            for (int i = 0; i < repeats; i++) test.TickOnce();
            Canvas.ForceUpdateCanvases();
        }
        private static string WriteGeometry(LyruisLayout[] nodes, BinaryWriter writer)
        {
            ulong hash = 14695981039346656037UL;
            foreach (var node in nodes)
            {
                var r = node.RectTransform;
                Vector2[] values = { r.sizeDelta, r.anchoredPosition, r.anchorMin, r.anchorMax, r.pivot };
                if (r.sizeDelta.x < 0 || r.sizeDelta.y < 0) throw new InvalidOperationException("Negative size at " + node.name);
                foreach (var v in values) foreach (float f in new[] { v.x, v.y })
                {
                    if (!float.IsFinite(f)) throw new InvalidOperationException("Non-finite geometry at " + node.name);
                    writer.Write(f);
                    unchecked { hash = (hash ^ (ulong)(long)Math.Round(f * 1000d)) * 1099511628211UL; }
                }
            }
            return hash.ToString("x16", CultureInfo.InvariantCulture);
        }
    }
}
