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
    public sealed class LyruisAutoMeasure : MonoBehaviour
    {
        [Min(4)] public int samplesPerCase = 40;
        [Min(1)] public int warmupOperations = 10;
        [Min(1)] public int settleFrames = 30;
        public bool IsRunning { get; private set; }
        public string LastReportPath { get; private set; }
        [Serializable] public sealed class Case
        {
            public string workload;
            public bool diagnostic;
            public double medianMs, p95Ms, meanRebuilds, medianAllocatedBytes;
            public double[] milliseconds;
            public long[] allocatedBytes;
            public List<LyruisDiagnostics.PhaseResult> phases = new List<LyruisDiagnostics.PhaseResult>();
        }
        [Serializable] public sealed class Report
        {
            public string utc, unity, platform, processor;
            public bool editor, externalProfilerEnabled;
            public int screenWidth, screenHeight, nodes, samplesPerCase, warmupOperations;
            public long stopwatchFrequency;
            public string measurement = "Main-thread explicit operation plus Canvas.ForceUpdateCanvases; not whole-frame or rendering time";
            public List<Case> cases = new List<Case>();
            public List<string> findings = new List<string>();
        }
        private Coroutine routine;
        private LyruisProfilerLab lab;
        private LyruisProfilerLab.SavedState saved;
        [ContextMenu("Run Automatic Measurement (Play mode)")]
        public void ToggleMeasurement()
        {
            if (IsRunning) { CancelMeasurement(); return; }
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play mode before starting automated measurement."); return; }
            lab = GetComponent<LyruisProfilerLab>();
            if (!lab || !lab.LayoutRoot) throw new InvalidOperationException("Profiler Lab must be initialized first.");
            if (LyruisDiagnostics.IsMeasuring) throw new InvalidOperationException("Another diagnostic measurement is active.");
            saved = lab.BeginAutomation(); IsRunning = true;
            routine = StartCoroutine(Run());
        }
        public void CancelMeasurement()
        {
            if (routine != null) StopCoroutine(routine);
            Restore();
        }
        private void OnDisable() { if (IsRunning) CancelMeasurement(); }
        private void Restore()
        {
            if (saved != null && lab)
            {
                var previous = saved; saved = null; lab.EndAutomation(previous);
            }
            IsRunning = false; routine = null;
        }
        private IEnumerator Run()
        {
            try
            {
                int count = Mathf.Max(4, samplesPerCase), warmup = Mathf.Max(1, warmupOperations);
                var report = new Report { utc = DateTime.UtcNow.ToString("O"), unity = Application.unityVersion,
                    platform = Application.platform.ToString(), processor = SystemInfo.processorType, editor = Application.isEditor,
                    externalProfilerEnabled = UnityEngine.Profiling.Profiler.enabled, screenWidth = Screen.width, screenHeight = Screen.height,
                    nodes = lab.LayoutRoot.GetComponentsInChildren<LyruisLayout>().Length, samplesPerCase = count,
                    warmupOperations = warmup, stopwatchFrequency = Stopwatch.Frequency };
                lab.ShowMeasurementStatus("AUTO REPORT / warming up; please keep the Game View resolution stable. Click AUTO again to stop.");
                for (int i = 0; i < Mathf.Max(1, settleFrames); i++) yield return null;
                foreach (LyruisProfilerLab.Workload mode in Enum.GetValues(typeof(LyruisProfilerLab.Workload)))
                for (int pass = 0; pass < 2; pass++)
                {
                    bool diagnostic = pass == 1;
                    lab.PrepareMeasurement(mode);
                    lab.ShowMeasurementStatus("AUTO REPORT / " + mode + (diagnostic ? " / phase diagnostics" : " / timing only") + " / " + count + " samples");
                    for (int i = 0; i < warmup; i++) { yield return null; Step(mode); }
                    var item = new Case { workload = mode.ToString(), diagnostic = diagnostic, milliseconds = new double[count], allocatedBytes = new long[count] };
                    var sums = new LyruisDiagnostics.PhaseResult[(int)LyruisDiagnostics.Phase.Count];
                    long rebuilds = 0;
                    for (int i = 0; i < count; i++)
                    {
                        yield return null;
                        if (diagnostic) LyruisDiagnostics.BeginMeasurement();
                        int before = lab.CompletedRebuilds;
                        long memory = GC.GetAllocatedBytesForCurrentThread();
                        long start = Stopwatch.GetTimestamp();
                        try { Step(mode); }
                        finally
                        {
                            item.milliseconds[i] = (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;
                            item.allocatedBytes[i] = GC.GetAllocatedBytesForCurrentThread() - memory;
                            if (diagnostic)
                            {
                                var phases = LyruisDiagnostics.EndMeasurement();
                                for (int p = 0; p < phases.Length; p++)
                                {
                                    sums[p].name = phases[p].name; sums[p].calls += phases[p].calls;
                                    sums[p].inclusiveMs += phases[p].inclusiveMs; sums[p].selfMs += phases[p].selfMs;
                                }
                            }
                        }
                        rebuilds += lab.CompletedRebuilds - before;
                    }
                    CheckGeometry();
                    item.meanRebuilds = rebuilds / (double)count;
                    var sorted = (double[])item.milliseconds.Clone(); Array.Sort(sorted);
                    item.medianMs = Median(sorted); item.p95Ms = sorted[Mathf.CeilToInt(count * .95f) - 1];
                    var allocations = (long[])item.allocatedBytes.Clone(); Array.Sort(allocations);
                    item.medianAllocatedBytes = count % 2 == 0 ? (allocations[count / 2 - 1] + (double)allocations[count / 2]) / 2 : allocations[count / 2];
                    if (diagnostic) item.phases.AddRange(sums);
                    report.cases.Add(item);
                }
                Analyze(report);
                string directory;
#if UNITY_EDITOR
                directory = Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkResults", "AutoMeasure");
#else
                directory = Path.Combine(Application.persistentDataPath, "LyruisAutoMeasure");
#endif
                Directory.CreateDirectory(directory);
                string stem = "measurement-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
                string json = Path.Combine(directory, stem + ".json"), markdown = Path.Combine(directory, stem + ".md");
                File.WriteAllText(json, JsonUtility.ToJson(report, true)); File.WriteAllText(markdown, Markdown(report));
                LastReportPath = markdown;
                Debug.Log("Lyruis automatic measurement complete. Report: " + markdown + "\nRaw JSON: " + json);
                Restore();
                lab.ShowMeasurementStatus("AUTO REPORT complete / " + Path.GetFileName(markdown) + " / full path in Console");
            }
            finally { Restore(); }
        }
        private void Step(LyruisProfilerLab.Workload mode)
        {
            // Idle has no explicit rebuild. Other cases do one operation, independent of the manual interval.
            if (mode != LyruisProfilerLab.Workload.Idle) lab.TickOnce();
            Canvas.ForceUpdateCanvases();
        }
        private void CheckGeometry()
        {
            foreach (var node in lab.LayoutRoot.GetComponentsInChildren<LyruisLayout>())
            {
                var r = node.RectTransform;
                if (!float.IsFinite(r.sizeDelta.x) || !float.IsFinite(r.sizeDelta.y) || r.sizeDelta.x < 0 || r.sizeDelta.y < 0 ||
                    !float.IsFinite(r.anchoredPosition.x) || !float.IsFinite(r.anchoredPosition.y))
                    throw new InvalidOperationException("Invalid measured geometry at " + node.name);
            }
        }
        private static double Median(double[] sorted) => sorted.Length % 2 == 0 ?
            (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2 : sorted[sorted.Length / 2];
        private static void Analyze(Report report)
        {
            foreach (var item in report.cases)
            {
                if (!item.diagnostic)
                {
                    if (item.workload == "Idle" && item.meanRebuilds > 0)
                        report.findings.Add("Idle unexpectedly rebuilt the root; inspect pending requests or external dimension changes.");
                    else if (item.workload == "Idle")
                        report.findings.Add("Idle produced no root completions; its nonzero operation time includes the explicit Canvas flush, not a Lyruis rebuild.");
                    else if (item.workload != "Idle" && item.meanRebuilds > 1.05)
                        report.findings.Add(item.workload + ": more than one root completion per operation; inspect duplicate invalidation and queued work.");
                    else if (item.workload != "Idle" && item.meanRebuilds < .95)
                        report.findings.Add(item.workload + ": expected one root completion per operation but observed fewer; check scheduling before trusting performance conclusions.");
                    if (item.medianAllocatedBytes > 0)
                        report.findings.Add(item.workload + ": main-thread allocations observed inside the operation/Canvas flush; capture allocation stacks to distinguish Lyruis, Text and uGUI.");
                    continue;
                }
                Case timing = report.cases.Find(x => !x.diagnostic && x.workload == item.workload);
                if (item.workload == "SingleLeaf")
                {
                    double leaves = 0, containers = 0;
                    foreach (var phase in item.phases)
                    {
                        if (phase.name == "LeafIntrinsicSize") leaves = phase.calls / (double)report.samplesPerCase;
                        if (phase.name == "MeasureContainer") containers = phase.calls / (double)report.samplesPerCase;
                    }
                    if (leaves > 1)
                        report.findings.Add(string.Format(CultureInfo.InvariantCulture,
                            "SingleLeaf: one edit visits {0:F0} leaf measurement scopes (including Fixed/Keep) and {1:F0} container measurement scopes; investigate dirty-subtree reuse rather than only arithmetic micro-optimizations.", leaves, containers));
                }
                if (timing != null && timing.medianMs > 0 && item.medianMs / timing.medianMs > 1.2)
                    report.findings.Add(item.workload + ": phase instrumentation increases median time by >20%; use timing-only results for total-cost comparisons.");
                var ranked = new List<LyruisDiagnostics.PhaseResult>(item.phases);
                ranked.Sort((a, b) => b.selfMs.CompareTo(a.selfMs));
                if (item.workload != "Idle" && ranked.Count > 0 && ranked[0].selfMs > 0)
                    report.findings.Add(item.workload + ": largest recorded SELF cost is " + ranked[0].name + ". " + Suggest(ranked[0].name));
            }
            report.findings.Add("These findings are rule-based leads, not proof of a bottleneck. Inclusive phase times overlap; never sum them. Rendering outside the explicit Canvas flush is not timed.");
        }
        private static string Suggest(string name)
        {
            switch (name)
            {
                case "RootLookup": return "Inspect cached parent/root links and lookups caused by dimension callbacks.";
                case "LeafIntrinsicSize": return "Inspect intrinsic Text/LayoutUtility costs and content/width-version caching.";
                case "ApplyChildRects": return "Inspect unchanged transform writes and dimension-change feedback.";
                case "ManualBounds": return "Inspect repeated descendant bounds queries.";
                case "ClearDrivenTracker": return "Inspect rebuilding driven registrations without topology/control changes.";
                case "MeasureContainer": case "ArrangeGeometry": return "Inspect dirty subtree and geometry reuse opportunities.";
                default: return "Inspect unmarked bookkeeping and child scopes before changing algorithms.";
            }
        }
        private static string Markdown(Report report)
        {
            var b = new StringBuilder("# Lyruis automatic measurement / 自动测量\n\n");
            b.AppendLine(report.utc + " | " + report.unity + " | " + report.platform + " | " + report.processor);
            b.AppendLine("Nodes: " + report.nodes + "; samples/case: " + report.samplesPerCase + "; warmups: " + report.warmupOperations + "; external Profiler: " + report.externalProfilerEnabled + ".\n");
            b.AppendLine("Timing-only disables detailed markers and phase counters. Diagnostics uses Stopwatch scopes independent of the Profiler window. No phase-overhead subtraction is applied.\n计时窗口为一次操作加显式 Canvas 刷新，不是整帧、渲染或完整 UI 耗时。GC 为当前主线程窗口内分配量，不能直接归因于 Lyruis。诊断计时有额外开销，不能与无细分计时混作同一基线。\n");
            b.AppendLine("| Workload | Pass | Median ms | P95 ms | Alloc median bytes | Root completions/op |\n| --- | --- | ---: | ---: | ---: | ---: |");
            foreach (var c in report.cases) b.AppendLine(string.Format(CultureInfo.InvariantCulture, "| {0} | {1} | {2:F4} | {3:F4} | {4:F0} | {5:F2} |", c.workload, c.diagnostic ? "Diagnostics" : "Timing only", c.medianMs, c.p95Ms, c.medianAllocatedBytes, c.meanRebuilds));
            foreach (var c in report.cases)
            {
                if (!c.diagnostic) continue;
                b.AppendLine("\n## " + c.workload + " — phase averages per operation\n");
                b.AppendLine("| Phase | Calls/op | Inclusive ms/op | Self ms/op |\n| --- | ---: | ---: | ---: |");
                var ranked = new List<LyruisDiagnostics.PhaseResult>(c.phases); ranked.Sort((a, d) => d.selfMs.CompareTo(a.selfMs));
                foreach (var p in ranked) if (p.calls > 0) b.AppendLine(string.Format(CultureInfo.InvariantCulture, "| {0} | {1:F2} | {2:F4} | {3:F4} |", p.name, p.calls / (double)report.samplesPerCase, p.inclusiveMs / report.samplesPerCase, p.selfMs / report.samplesPerCase));
            }
            b.AppendLine("\n## Findings / 自动提示\n"); foreach (var note in report.findings) b.AppendLine("- " + note);
            b.AppendLine("\nReports retain raw sample arrays in adjacent JSON. Geometry is checked for finite nonnegative sizes, not equivalence against another implementation. Pass order is fixed (timing then diagnostics); repeat runs to assess noise and order effects. Scene state is restored on completion/cancellation.");
            return b.ToString();
        }
    }
}
