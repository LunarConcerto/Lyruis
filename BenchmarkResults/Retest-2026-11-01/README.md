# Benchmark retest / 重测 — 2026-11-01 (Asia/Hong_Kong)

Runtime: `54b5e16564081a8263340e72c95c38266b050036`; no runtime or harness changes during this retest. Unity 2022.3.62f3 / Windows Editor / AMD Ryzen 9 9950X. Profiler recording disabled. Existing local EditorSettings/IDE changes excluded from the retest commit.

23 tests passed (21 EditMode + 2 PlayMode). Flat and nested suites ran in Edit mode, with 20 warmups, 40 paired samples and 10 synchronous rebuilds/sample. All 17 geometry comparisons returned 0px error. These suites measure explicit layout rebuilds without Canvas rendering; do not directly compare their numbers with the mixed Text/Canvas lab operation measurements below.

## Flat / 平铺

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

## Nested / 嵌套

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

[Flat raw JSON](flat.json) · [Nested raw JSON](nested.json)

Timing ratios are environment-specific, not universal speedup guarantees. No Player build or full-frame FPS measurement was performed.

## Automatic lab measurement / 自动阶段测量

1,273 nodes, 1366x768, Play mode, 40 samples/case, 10 warmups. Timing-only totals include the explicit Canvas flush; diagnostic scopes are reported separately in the linked report. Idle root completions = 0; active cases = 1 per operation.

| Workload | Median ms | P95 ms | Median thread allocations (bytes) |
| --- | ---: | ---: | ---: |
| Idle | 1.2855 | 1.9923 | 0 |
| SingleLeaf | 6.8449 | 7.6088 | 0 |
| BatchLeaves | 7.4703 | 8.7054 | 0 |
| ForceRootEveryFrame | 7.0141 | 8.0336 | 0 |
| ResizeRoot | 6.6108 | 7.8976 | 0 |

[Detailed report](../AutoMeasure/measurement-20261031-161523-140.md) / [raw JSON](../AutoMeasure/measurement-20261031-161523-140.json). Names use UTC timestamps; this run took place on 2026-11-01 in Asia/Hong_Kong. Zero median allocations apply only to the measured main-thread window, not whole-frame GC.

## Peak sweep / 峰值扫描

24 configurations, 637–4,241 nodes, 40 samples/config, 6 warmups and 4 separate diagnostic operations. All stored sizes, positions, anchors and pivots match the previous optimized capture exactly (maximum absolute difference 0); expected root completion counts also match.

| Nodes | Workload | Median ms | P95 ms | Observed Max ms | Previous median ms |
| ---: | --- | ---: | ---: | ---: | ---: |
| 4241 | SingleLeaf | 23.637 | 24.250 | 24.455 | 20.415 |
| 4241 | Batch32 | 24.466 | 25.724 | 28.069 | 20.346 |
| 4241 | BatchAll | 31.957 | 33.203 | 39.792 | 27.214 |
| 4241 | Root1 | 24.312 | 25.238 | 26.095 | 21.926 |
| 4241 | Root4 | 78.945 | 82.425 | 86.505 | 70.860 |
| 4241 | Resize | 23.390 | 25.782 | 26.233 | 19.937 |

[Full sweep](../PeakSweep/peak-retest-20261101-20261031-161604.md) · [raw JSON](../PeakSweep/peak-retest-20261101-20261031-161604.json) · [same-runtime comparison](../PeakSweep/retest-2026-11-01-comparison.md).

No runtime/harness code changed. This run is generally slower than the previous optimized sweep despite matching configuration metadata. Sequential captures alone cannot attribute the difference to one cause; Editor state, system load and sampling noise remain possible influences. The comparison is a repeatability observation, not a new optimization or code-regression claim.

本轮未修改运行代码；部分跨运行差异明显，不能把单次测量当作固定性能保证。报告保留全部样本与尾部变化。操作包含显式Canvas刷新，不代表完整帧率；Root4有意在一次操作中连续同步重建4次。
