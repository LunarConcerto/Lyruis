# Historical baselines / 历史基线

[Current results and methodology / 当前结果与方法](README.md)

These are earlier captures, not the latest measurements. Environment: Unity 2022.3.62f3 / Windows Editor / Ryzen 9 9950X, 2026-10-31 Asia/Hong_Kong. Flat/nested use the independent tree before the later conservative runtime optimization; do not mix them with the obsolete LayoutGroup adapter or interpret cross-run differences as isolated optimization gains.

这些结果用于追溯；最新结果请看总览。旧 LayoutGroup 适配器与独立树不是同一实现。

## Flat / 平铺

| Mode | Items | Lyruis median | Native median | Lyruis P95 | Native P95 | Error |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Horizontal | 10 | 0.0479 | 0.1282 | 0.0484 | 0.1294 | 0.0 |
| Horizontal | 100 | 0.4664 | 1.2510 | 0.4768 | 1.2734 | 0.0 |
| Horizontal | 1000 | 4.6919 | 13.3516 | 4.8096 | 13.8041 | 0.0 |
| Vertical | 10 | 0.0486 | 0.1292 | 0.0501 | 0.1345 | 0.0 |
| Vertical | 100 | 0.4680 | 1.2536 | 0.4813 | 1.2726 | 0.0 |
| Vertical | 1000 | 4.7125 | 13.3320 | 4.7843 | 13.5589 | 0.0 |
| Grid | 10 | 0.0474 | 0.0508 | 0.0486 | 0.0553 | 0.0 |
| Grid | 100 | 0.4596 | 0.4689 | 0.4846 | 0.5453 | 0.0 |
| Grid | 1000 | 4.6435 | 4.6932 | 4.7703 | 4.7922 | 0.0 |

[Raw JSON](editor-2022.3.json) · [obsolete adapter JSON](historical-ugui-adapter-editor-2022.3.json)

## Nested / 嵌套

| Depth | Branches | Total nodes | Resize | Lyruis median | Native median | Native / Lyruis | Lyruis P95 | Native P95 | Error |
| ---: | ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 3 | 2 | 15 | False | 0.0558 | 0.1773 | 3.18x | 0.0642 | 0.1851 | 0.0 |
| 3 | 2 | 15 | True | 0.0669 | 0.1930 | 2.89x | 0.0735 | 0.1959 | 0.0 |
| 5 | 2 | 63 | False | 0.2414 | 0.7721 | 3.20x | 0.2509 | 0.7855 | 0.0 |
| 5 | 2 | 63 | True | 0.2602 | 0.7953 | 3.06x | 0.2692 | 0.8040 | 0.0 |
| 7 | 2 | 255 | False | 1.0150 | 3.1989 | 3.15x | 1.0358 | 3.2744 | 0.0 |
| 7 | 2 | 255 | True | 1.0443 | 3.2129 | 3.08x | 1.0654 | 3.2365 | 0.0 |
| 3 | 4 | 85 | False | 0.3634 | 1.0656 | 2.93x | 0.3696 | 1.0800 | 0.0 |
| 3 | 4 | 85 | True | 0.3796 | 1.0982 | 2.89x | 0.4070 | 1.1339 | 0.0 |

All eight geometry comparisons match. This run shows lower rebuild costs for the restored independent tree, but does not establish a universal advantage or performance of the archived implementation itself. Wrapped text, grids/Flow, Canvas scheduling and target-player performance are outside this nested test.

8组几何一致。本次独立树实现的耗时低于原生对照，不代表所有场景，也不是归档旧实现本身的性能测量。未覆盖文本、Flow、Canvas 调度或 Player。

[Raw JSON](nested-editor-2022.3.json) · [obsolete adapter JSON](historical-ugui-adapter-nested-editor-2022.3.json)

## Earlier automatic captures / 早期自动采样

Both used 1,273 nodes, five workloads, timing/diagnostic passes, 40 samples/pass and 10 warmups, with Profiler recording off. Each is an independent capture, not a duplicate file. Detailed phase samples and findings remain in JSON.

两次采样均保留原始 JSON。Idle 根完成次数为0，活动操作为1；每次根重建经过720个叶子、529个容器测量作用域及1,273次 tracker 清理。作用域次数不等于底层 API 调用次数。早期主要 Self 成本为变换写入或叶子测量；后续优化与当前结果见另外两份专题报告。

### measurement-20261031-054359-442 (UTC)

| Workload | Pass | Median ms | P95 ms | Alloc median bytes | Root completions/op |
| --- | --- | ---: | ---: | ---: | ---: |
| Idle | Timing only | 1.5115 | 2.2360 | 0 | 0.00 |
| Idle | Diagnostics | 1.3462 | 2.0546 | 0 | 0.00 |
| SingleLeaf | Timing only | 8.7227 | 10.0076 | 0 | 1.00 |
| SingleLeaf | Diagnostics | 9.5168 | 12.2595 | 0 | 1.00 |
| BatchLeaves | Timing only | 10.1411 | 12.8452 | 0 | 1.00 |
| BatchLeaves | Diagnostics | 10.0515 | 11.3363 | 0 | 1.00 |
| ForceRootEveryFrame | Timing only | 8.3225 | 9.2904 | 0 | 1.00 |
| ForceRootEveryFrame | Diagnostics | 8.4328 | 9.6704 | 0 | 1.00 |
| ResizeRoot | Timing only | 8.3693 | 10.2816 | 0 | 1.00 |
| ResizeRoot | Diagnostics | 9.2045 | 9.8974 | 0 | 1.00 |

[Raw JSON](AutoMeasure/measurement-20261031-054359-442.json)

### measurement-20261031-054729-522 (UTC)

| Workload | Pass | Median ms | P95 ms | Alloc median bytes | Root completions/op |
| --- | --- | ---: | ---: | ---: | ---: |
| Idle | Timing only | 1.3396 | 2.0853 | 0 | 0.00 |
| Idle | Diagnostics | 1.2941 | 1.7751 | 0 | 0.00 |
| SingleLeaf | Timing only | 9.0273 | 11.7714 | 0 | 1.00 |
| SingleLeaf | Diagnostics | 9.6551 | 11.3063 | 0 | 1.00 |
| BatchLeaves | Timing only | 10.5895 | 13.0252 | 0 | 1.00 |
| BatchLeaves | Diagnostics | 11.5160 | 13.9059 | 0 | 1.00 |
| ForceRootEveryFrame | Timing only | 8.7780 | 10.5720 | 0 | 1.00 |
| ForceRootEveryFrame | Diagnostics | 8.8548 | 11.0654 | 0 | 1.00 |
| ResizeRoot | Timing only | 8.4872 | 9.6422 | 0 | 1.00 |
| ResizeRoot | Diagnostics | 8.6345 | 10.4589 | 0 | 1.00 |

[Raw JSON](AutoMeasure/measurement-20261031-054729-522.json)
