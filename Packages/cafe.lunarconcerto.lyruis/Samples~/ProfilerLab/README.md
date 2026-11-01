# Profiler Lab / 复杂嵌套分析场景

Open `ProfilerLab.unity` and enter Play. Default workload is **Idle**. All content is serialized, so the hierarchy can be inspected before Play. The first 60 Update frames are reserved for startup/font/layout warmup; discard startup frames when profiling. The lab enables background execution while playing and restores the previous setting on disable.

开发工程场景：`Assets/Samples/Lyruis/0.1.0/ProfilerLab/ProfilerLab.unity`。直接打开并进入 Play；独立使用包时先从 Samples 导入 **Profiler Lab**。初始为静止模式，前60个 Update 帧不执行连续负载，用于排除字体和布局初始化。场景可在编辑模式展开查看。

## Contents / 场景结构

- 24 content-sized cards, **1,273 Lyruis nodes**, maximum depth **6** (including root), **168 mutable text leaves**.
- Flow root → Vertical card → Grid metrics → Vertical metric → Horizontal value row → intrinsic Text leaf.
- Each card also contains a Horizontal header/footer, wrapped description, Flow tags, and a manually positioned None/bounds badge.
- Root width 1840: four cards per line. Width 1380: three cards per line. Card descriptions can change height and propagate into the root's line measurements.
- Workload and controls use separate Canvases. Scroll the workload viewport; controls remain available when workload rendering is disabled. This is not virtualized: offscreen nodes still participate in layout.

混合宽高自适应、固定尺寸、换行文本、Grid、Flow、None 手动包围盒。静态指标叶子变化主要影响局部宽度，描述文本变化还会影响卡片高度；便于后续验证局部更新机制。滚动裁剪不会减少布局节点数。

## Controls / 负载控制

| Button / key | Workload | What to inspect |
| --- | --- | --- |
| 1 Idle | No continuous mutations or explicit rebuilds after warmup | Baseline; look for unexpected repeated `Lyruis.Rebuild` |
| 2 Single leaf | Rotate through mutable leaves; change one cached text string each interval and call its Refresh | Normal deferred root scheduling from a local mutation |
| 3 Batch leaves | Change up to Batch Size leaves within one Update | Root request coalescing and repeated RootLookup costs |
| 4 Force root/frame | RebuildNow on the root every Update, without changing data | Full-rebuild steady-state cost and redundant transform work |
| 5 Resize + reflow | Alternate root width between 1840 and 1380 | Four/three-column reflow and affected text/geometry work |
| Single step / Space | Perform one tick of the selected workload; Idle performs a synchronous root rebuild | Capture a controlled single event after pausing continuous loads |
| Draw UI: toggle | Show/hide the workload through CanvasGroup alpha; controls remain rendered | Contrast workload drawing/mesh costs with layout costs |
| Detail: toggle | Enable/disable fine-grained Lyruis markers | Locate costs, then assess marker overhead |

键盘快捷键要求 Legacy Input Manager；本开发工程已启用。屏幕按钮通过内置 StandaloneInputModule 工作。若消费工程仅启用新 Input System，请将场景 EventSystem 的输入模块换成对应模块；也可以在 Inspector 选择负载，或使用组件的 Tick Once 菜单。

在 `Profiler Lab - select for workload settings` 对象的 Inspector 调整 Workload、Change Interval（默认0.1秒）、Batch Size（默认32）、Warmup Frames、Detailed Markers、Draw Workload Canvas。修改 Card Count 不会自动重建已序列化场景；修改生成配置后重新调用开发生成方法，或在新的空对象上调用 Build。

Text strings are prepared during scene creation, rather than formatted on every tick. The lab does not refresh status text every frame; startup and button changes do allocate. Text mesh/intrinsic processing can allocate and is part of the workload. `CompletedRebuilds`, `MutationTicks`, `LayoutRoot`, and `TargetCount` are available as public diagnostic properties.

## Capture / 建议操作

1. Open **Window > Analysis > Profiler**, select CPU Usage, disable **Deep Profile** initially, and enable recording. Keep Game View at a consistent resolution; 1920×1080 is the design reference.
2. Enter Play and wait past warmup. Record Idle first, then Single leaf, Batch leaves, Force root/frame and Resize. Record each separately so a mode change does not contaminate measurements.
3. In CPU Hierarchy/Timeline, search for `Lyruis.`. Use **Total** for inclusive cost, **Self** for the marker's own work; do not add a parent marker's Total to its child totals.
4. Mutations appear under `LyruisLab.WorkloadTick` during Update. Deferred layout appears later under `Lyruis.FlushRequests` in Canvas processing. A fast mutation tick alone does not mean layout was cheap. Force root/frame nests the rebuild inside the workload tick.
5. Compare Draw UI enabled/disabled, and Detail enabled/disabled. Hiding uses CanvasGroup alpha zero, retaining the active Canvas and Text components so intrinsic text sizing stays consistent. It reduces visible drawing but does not eliminate all UI processing. Controls, ScrollRect, scene scripts and editor costs remain; this is not a completely isolated layout microbenchmark.
6. Use a Development Player with Autoconnect Profiler for target-platform decisions. This sample has been checked in Windows Editor; player builds are not yet verified.

建议重点看：Idle 是否仍重建；批量变化是否合并；ApplyChildRects 中是否包含大量 DimensionChanged/RootLookup；LeafIntrinsicSize 是否占主导；None 的 ManualBounds 是否出现重复扫描。先定位，再关闭细粒度标记比较总耗时。

## Markers / 标记含义

| Marker | Meaning |
| --- | --- |
| Lyruis.Rebuild | One requested subtree rebuild, including completion callbacks |
| Lyruis.FlushRequests | Resolve and deduplicate queued roots, then execute deferred rebuilds |
| Lyruis.CompletionCallbacks | Child-first completion notification pass |
| Lyruis.RootLookup | Resolve root through active parent components |
| Lyruis.CollectChildren | Refresh/reuse the cached child list |
| Lyruis.ClearDrivenTracker | Clear driven-property registrations |
| Lyruis.LeafIntrinsicSize | Resolve leaf dimensions; Fixed/Keep leaves can also enter this scope |
| Lyruis.ManualBounds | None container's subtree bounds query |
| Lyruis.MeasureContainer | Sum/max, grid footprint or Flow line measurement |
| Lyruis.ArrangeGeometry | Produce child slot rectangles |
| Lyruis.ApplySelfSize | Register and apply this node's size |
| Lyruis.ApplyChildRects | Register and write child anchors/positions/sizes |
| Lyruis.DimensionChanged | Dimension-change callbacks, including nested root lookups |

Detailed markers are controlled globally by `LyruisDiagnostics.DetailedProfiling` (default false). The lab sets it from its Inspector and restores the previous value when disabled. `Rebuild`, `FlushRequests`, and `CompletionCallbacks` remain available with detail off. Fine-grained markers add overhead and should not be treated as an uninstrumented performance baseline. No existing benchmark timings were reissued as measurements of this instrumented revision.

## Regenerate / 重新生成

Development project: **Tools > Lyruis > Generate Profiler Lab Scene**. This recreates the sample scene asset. An open, unmodified Profiler Lab is replaced with the generated scene; regeneration rejects an open Lab with unsaved changes. Importable copies live under `Packages/cafe.lunarconcerto.lyruis/Samples~/ProfilerLab` and must be synchronized when regenerating for distribution.

Visibility uses CanvasGroup instead of disabling Canvas: disabling Canvas can change the Canvas context used by legacy Text intrinsic sizing. 可见性通过 CanvasGroup.alpha 切换，避免禁用 Canvas 改变文本测量上下文；这不等同于关闭全部 UI CPU 开销。


## One-click automated measurement / 一键自动测量

**Development project:** choose **Tools > Lyruis > Run Automated Measurement** with this scene open. It enters Play automatically, or unpauses an existing Play session. **Imported sample:** enter Play and press **AUTO REPORT / Stop**. Press it again to cancel. No Profiler recording, CPU Hierarchy search, Deep Profile or ProfilerRecorder availability is required.

自动测量会暂时接管场景负载并禁用其余负载按钮；完成或取消时恢复原来的文本、尺寸、显示、负载配置及计数器。默认执行5种负载 × 2轮 × 40个样本，每轮10次预热；开始前等待30帧。Manual Change Interval 不参与此测试，每个样本执行一次操作，跨帧采样。

- **Timing only:** detailed markers and phase timers are off. Records operation-plus-explicit-Canvas-flush Median/P95, current-thread allocated bytes, and root completion counts.
- **Diagnostics:** resets the same text/size state, repeats the workload, and records per-phase calls, inclusive time and self time with Stopwatch scopes. Self excludes other instrumented child scopes. Inclusive phases overlap and must not be summed. Existing coarse markers remain available, but window recording can stay off.
- **Idle:** no tick/rebuild is explicitly requested; still times Canvas.ForceUpdateCanvases. This baseline is intentionally not zero and is not silently subtracted from other cases.
- **Raw evidence:** JSON retains all sample times and allocated byte counts plus phase totals. Markdown provides summaries and rule-based findings. Findings are optimization leads, not automatic proof of a bottleneck.

Editor reports: `BenchmarkResults/AutoMeasure/measurement-<UTC timestamp>.md` and `.json`. Player reports: `Application.persistentDataPath/LyruisAutoMeasure`. Full paths are logged to Console; the scene status shows the completed report filename. Select the `LyruisAutoMeasure` component to change Samples Per Case, Warmup Operations and Settle Frames.

报告会提示：Idle 意外重建、一次操作多次根完成、窗口内分配、细分计时导致的明显开销增长，以及各负载中 Self 耗时最高的阶段。提示依据是明确规则，不会自动修改布局代码。GC列仅表示当前主线程计时窗口内分配；报告数组、状态文字和导出本身在窗口外仍会分配，不能据此声称整帧零GC。

These are synchronous operation measurements, not complete PlayerLoop frame time, GPU time, or delayed rendering/mesh updates outside the explicit flush. Per-phase timings are instrumented and can perturb the workload; use timing-only totals for comparisons. Pass order is fixed (timing then diagnostics), so repeat captures to assess order/noise effects. Geometry checks reject non-finite/negative sizes, but do not establish equivalence against another implementation. Scene initialization and export are excluded. Player builds remain unverified.

### Why manual Profiler entries can be absent / 为什么手动查不到

Idle does not continuously rebuild. If recording is off, Play is paused, or the selected frame is from Idle, there may be no Lyruis samples in that frame. Fine-grained markers also require Detailed Markers enabled. The automatic report avoids these UI/selected-frame dependencies and explicitly states what it measured.

### Window-independent phase API

`LyruisDiagnostics.BeginMeasurement()` resets and enables main-thread counters. Run the operation, then call `EndMeasurement()` outside all scopes to receive `PhaseResult[]` (calls, inclusiveMs, selfMs). It works with `DetailedProfiling = false`. Nested/concurrent measurement sessions are rejected. This API is for diagnostic runs; keep it off for ordinary runtime operation.


## Peak sweep / 峰值与扩展性扫描

Development project: enter Play in ProfilerLab, then **Tools > Lyruis > Run Peak Sweep (Play Mode)**. The current lab is temporarily disabled, test labs are generated at runtime, and the original lab is restored afterward. Source scenes are not overwritten. Imported sample: attach `LyruisPeakSweep` to a **separate GameObject**, then use its Run Current Peak Sweep context menu in Play. The controller must remain active while the visible lab is disabled.

默认规模：12/24/48/80张卡片，分别为637/1273/2545/4241个布局节点。每个规模测试单叶子变化、32叶子批量变化、全部可变叶子变化、一次根重建、同一操作连续4次根重建、根宽变化。每组40个跨帧样本，6次预热，另采4个细分诊断样本；诊断不混入总耗时统计。

Reports: `BenchmarkResults/PeakSweep/peak-<label>-<UTC timestamp>.json/.md/.geometry.bin`. JSON preserves raw timing/allocation samples, P95, observed Max, root completion counts and diagnostic phase totals. Geometry snapshots store every node's sizeDelta, anchoredPosition, anchorMin, anchorMax and pivot (10 Float32 values/node) so optimizations can be compared without relying only on hashes.

此扫描用于寻找本机、当前配置下的操作成本越界点，不是理论性能上限。一次操作包含显式 Canvas 刷新，尚不等于完整帧；Root4 是人为放大的同步调用压力。16.67ms仅作参考预算线，不能据此宣称实际60FPS容量。有限样本的Max是观测极值，不是最坏情况保证。未验证Player。

Compare matching captures with repository script:

```text
python scripts/compare_peak_sweeps.py before.json after.json comparison.md
```

The comparator rejects mismatched environment/configurations, unexpected root completion counts, or any stored geometry difference above .01; it reports median/P95/observed-Max changes including regressions. The retained before capture used runtime commit `4d3d3d7` with this same stress harness; the after capture includes root caching and conditional combined transform writes. Sequential captures are not randomized A/B trials or confidence intervals.
