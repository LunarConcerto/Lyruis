# Benchmark results / 性能测量

Latest capture: **2026-11-01 (Asia/Hong_Kong)**. This directory separates current results from earlier implementations and captures. No benchmarks were rerun during documentation consolidation.

最新数据为 **2026-11-01（香港时间）**。本次仅整理报告，没有重新测量或修改原始数据。

| Document / 文档 | Contents / 内容 |
| --- | --- |
| This page / 本页 | Latest flat/nested results, methodology, validation and evidence index / 最新平铺与嵌套结果、方法、验证及数据索引 |
| [Automatic measurement / 自动测量](AutoMeasure/README.md) | Latest timing/diagnostic passes and phase costs / 最新双轮计时与阶段开销 |
| [Peak sweep / 峰值扫描](PeakSweep/README.md) | Optimization comparison and same-runtime retest, all 24 configurations / 优化前后及同版本复测，完整24组配置 |
| [Historical baselines / 历史基线](history.md) | Earlier flat/nested and repeated lab captures, separate from current results / 早期平铺、嵌套及重复采样 |

## Latest retest / 最新复测

Runtime: `54b5e16564081a8263340e72c95c38266b050036`; no runtime or harness changes during this retest. Unity 2022.3.62f3 / Windows Editor / AMD Ryzen 9 9950X. Profiler recording disabled.

23 tests passed (21 EditMode + 2 PlayMode). Flat and nested suites ran in Edit mode, with 20 warmups, 40 paired samples and 10 synchronous rebuilds/sample. All 17 geometry comparisons returned 0px error. These suites measure explicit layout rebuilds without Canvas rendering; do not directly compare their numbers with the mixed Text/Canvas lab operation measurements below.

### Flat / 平铺

| Mode | Items | Lyruis median ms | Native median ms | Lyruis P95 ms | Native P95 ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| Horizontal | 10 | 0.0704 | 0.1945 | 0.0717 | 0.1967 |
| Horizontal | 100 | 0.6679 | 1.8481 | 0.7058 | 1.9258 |
| Horizontal | 1000 | 4.9301 | 14.3602 | 6.5799 | 17.5223 |
| Vertical | 10 | 0.0485 | 0.1168 | 0.0731 | 0.2022 |
| Vertical | 100 | 0.4631 | 1.2627 | 0.5779 | 1.5101 |
| Vertical | 1000 | 5.0761 | 14.4933 | 5.8482 | 17.6537 |
| Grid | 10 | 0.0475 | 0.0710 | 0.0740 | 0.0902 |
| Grid | 100 | 0.6077 | 0.6753 | 0.7281 | 0.9040 |
| Grid | 1000 | 5.6509 | 6.2072 | 7.5011 | 8.2498 |

### Nested / 嵌套

| Depth | Branches | Nodes | Resize leaf | Lyruis median ms | Native median ms | Native / Lyruis |
| ---: | ---: | ---: | --- | ---: | ---: | ---: |
| 3 | 2 | 15 | False | 0.0586 | 0.2210 | 3.77x |
| 3 | 2 | 15 | True | 0.0670 | 0.2377 | 3.55x |
| 5 | 2 | 63 | False | 0.2599 | 0.9566 | 3.68x |
| 5 | 2 | 63 | True | 0.2603 | 0.9745 | 3.74x |
| 7 | 2 | 255 | False | 1.1487 | 3.9838 | 3.47x |
| 7 | 2 | 255 | True | 1.1162 | 3.9850 | 3.57x |
| 3 | 4 | 85 | False | 0.4030 | 1.3213 | 3.28x |
| 3 | 4 | 85 | True | 0.4162 | 1.3470 | 3.24x |

[Flat raw JSON](Retest-2026-11-01/flat.json) · [Nested raw JSON](Retest-2026-11-01/nested.json)

Timing ratios are environment-specific, not universal speedup guarantees. No Player build or full-frame FPS measurement was performed.

### Automatic lab measurement / 自动阶段测量

1,273 nodes, 1366x768, Play mode, 40 samples/case, 10 warmups. Timing-only totals include the explicit Canvas flush; diagnostic scopes are reported separately in the linked report. Idle root completions = 0; active cases = 1 per operation.

| Workload | Median ms | P95 ms | Median thread allocations (bytes) |
| --- | ---: | ---: | ---: |
| Idle | 1.2855 | 1.9923 | 0 |
| SingleLeaf | 6.8450 | 7.6088 | 0 |
| BatchLeaves | 7.4703 | 8.7054 | 0 |
| ForceRootEveryFrame | 7.0141 | 8.0336 | 0 |
| ResizeRoot | 6.6108 | 7.8976 | 0 |

[Detailed report](AutoMeasure/README.md) / [raw JSON](AutoMeasure/measurement-20261031-161523-140.json). Names use UTC timestamps; this run took place on 2026-11-01 in Asia/Hong_Kong. Zero median allocations apply only to the measured main-thread window, not whole-frame GC.

### Peak sweep / 峰值扫描

24 configurations, 637–4,241 nodes, 40 samples/config, 6 warmups and 4 separate diagnostic operations. All stored sizes, positions, anchors and pivots match the previous optimized capture exactly (maximum absolute difference 0); expected root completion counts also match.

[Full sweep and same-runtime comparison](PeakSweep/README.md#same-runtime-retest) · [raw JSON](PeakSweep/peak-retest-20261101-20261031-161604.json).

No runtime/harness code changed. This run is generally slower than the previous optimized sweep despite matching configuration metadata. Sequential captures alone cannot attribute the difference to one cause; Editor state, system load and sampling noise remain possible influences. The comparison is a repeatability observation, not a new optimization or code-regression claim.

本轮未修改运行代码；部分跨运行差异明显，不能把单次测量当作固定性能保证。报告保留全部样本与尾部变化。操作包含显式Canvas刷新，不代表完整帧率；Root4有意在一次操作中连续同步重建4次。

## Methodology / 测量方法

Unity 2022.3.62f3 / Windows Editor / Ryzen 9 9950X. All timings are milliseconds per rebuild or operation as stated; no Player or whole-frame FPS claim is made. Editor/system activity can change results between captures. Lyruis uses its independent postorder component tree, not LayoutGroup execution; only intrinsic Content leaves query LayoutUtility. The native comparison uses LayoutRebuilder and native layout components.

### Flat and nested / 平铺与嵌套

- Edit mode, no Canvas or Graphics in the test trees. Setup, validation and 20 warmups are excluded. 40 paired batches alternate execution order, with 10 synchronous rebuilds per batch. Median averages entries 20/21; P95 uses nearest-rank entry 38.
- Flat: root 800x600; item preferred/minimum size 40x24, gap 4; Grid uses 40x24 slots and 10 columns. Linear native groups control both axes without force-expand. Lyruis Grid children keep their independent sizes, chosen to match the native baseline.
- Nested: depth counts container levels followed by one leaf level. Containers alternate Vertical/Horizontal with both axes Content, asymmetric per-level padding and gap 4. Leaves range from 40–61 by 24–33, minimum equals preferred. Native uses ContentSizeFitter only on the root. Lyruis Content leaves read the same LayoutElement data.
- Resize alternates a deepest leaf between 40x24 and 73x39, including property modification/invalidation costs before the full root rebuild. Geometry validation covers every node after warmup and every timed batch, plus both resize states beforehand; reject non-finite sizes or errors above .01px.

平铺与嵌套比较的是几何等价树的显式完整重建。两侧均包含 Content 叶子的首选尺寸读取；动态用例包含相同的属性修改。它们不衡量渲染、GC、自动脏调度，也不能与 Text/Canvas 实验室操作耗时直接比较。嵌套用例不覆盖 Flow、Grid 或换行文本。

### Lab operations / 实验室操作

Play mode, 1366x768, Profiler recording disabled. Automatic measurement uses 1,273 nodes, five workloads, 40 samples/pass and 10 warmups. Peak sweep uses 637/1,273/2,545/4,241 nodes, six workloads, 40 timing samples, 6 warmups and 4 separate diagnostic operations per configuration. Operations include one explicit Canvas flush; Root4 deliberately performs four immediate rebuilds. Diagnostic instrumentation is not subtracted from timings; inclusive phases overlap and must not be summed. Allocation counts cover only the measured main-thread window. Observed Max and 16.67ms crossings are sample observations, not guaranteed limits or FPS capacity.

## Validation / 验证

- Latest runtime `54b5e16`: **21 EditMode + 2 PlayMode tests passed**. Coverage includes membership, child-first sizing, None bounds, Grid semantics, Flow alignment/wrapping, Keep/sizeDelta, callbacks, deferred providers/request coalescing, phase accounting and topology cache invalidation.
- Flat/nested: all **17 geometry cases match exactly**. Peak retest: all **24 configurations** match previous optimized geometry and expected root completion counts; max coordinate difference **0**. Optimization before/after max difference was **.000031**, below .01.
- Profiler Lab: 24 cards, 1,273 layout nodes, depth 6, 168 mutable text leaves. Startup, single/batch mutations and 1840/1380px root reflow were checked; dimensions remained finite and nonnegative. Single/batch operations produced one root completion. Idle produced none.
- CanvasGroup alpha preserved all 1,273 node rectangles while hidden; it is not complete UI CPU isolation. CPU captures exposed Lyruis rebuild, lookup, collection, tracker, measurement, geometry, rectangle-write and completion markers.
- Automation cancellation after a real text edit restored workload text/root size and disabled counters. Temporary sweep scenes restored the original scene state without overwriting source scenes.
- Gallery was regenerated for explicit registration and visually inspected in Play mode. Earlier test counts (16, then 18 EditMode cases) are superseded by the latest 21.

Not verified: other Unity versions, target-player builds, whole-frame GC/FPS, separate consumer-project sample import, automatic Canvas scheduling performance, or exhaustive equivalence to the archived source. Archived source informed the computational model but was not executed as a second test assembly.

已验证本次记录中的测试、几何一致性及场景恢复；不将编辑器采样扩展为全平台性能保证，也不将旧适配器或归档源码视为当前运行实现。

## Raw evidence and reproduction / 原始数据与复现

- [Retest manifest and SHA-256 hashes](Retest-2026-11-01/manifest.json): latest flat, nested, auto and peak evidence. Paths remain unchanged so the manifest still verifies.
- `Retest-2026-11-01/`: latest flat/nested JSON and manifest.
- `AutoMeasure/`: two earlier independent captures plus the latest capture; JSON contains raw samples, diagnostic phases and findings.
- `PeakSweep/`: before/after/retest JSON and matching `.geometry.bin` snapshots. Geometry stores sizeDelta, anchoredPosition, anchorMin, anchorMax and pivot (10 Float32 values/node); JSON contains offsets.
- Root `editor-2022.3.json` and `nested-editor-2022.3.json`: earlier independent-tree baselines and the existing development menu output paths. `historical-ugui-adapter-*.json`: obsolete adapter implementation, retained only for provenance.

Reproduce with **Tools > Lyruis > Run Benchmark And Save Report**, **Run Nested Benchmark And Save Report**, **Run Automated Measurement**, or **Run Peak Sweep (Play Mode)**. The imported Benchmark scene runs both microbenchmarks; ProfilerLab provides **AUTO REPORT / Stop**. Runtime report outputs use Application.persistentDataPath; development Editor outputs use this directory. Generated Markdown may be consolidated again after a new capture.

所有 JSON 与几何快照均保留原路径及字节内容；重复 Markdown 表格已合并。UTC 文件名可能对应香港时间次日。新一轮结果应标明运行代码版本、环境和计时边界，再更新本页入口。
