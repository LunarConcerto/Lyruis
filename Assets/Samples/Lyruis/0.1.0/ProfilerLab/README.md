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

Development project: **Tools > Lyruis > Generate Profiler Lab Scene**. This recreates the sample scene asset. An open, unmodified Profiler Lab is replaced with the generated scene; regeneration rejects an open Lab with unsaved changes. Importable copies live under `Packages/com.lunarconcerto.lyruis/Samples~/ProfilerLab` and must be synchronized when regenerating for distribution.

Visibility uses CanvasGroup instead of disabling Canvas: disabling Canvas can change the Canvas context used by legacy Text intrinsic sizing. 可见性通过 CanvasGroup.alpha 切换，避免禁用 Canvas 改变文本测量上下文；这不等同于关闭全部 UI CPU 开销。
