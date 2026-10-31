# Peak sweep comparison / 峰值扫描与优化

Unity 2022.3.62f3; AMD Ryzen 9 9950X 16-Core Processor ; 1366x768; 40 samples/config; 6 warmups; external Profiler False.

Operations include one explicit Canvas flush. Root4 issues four synchronous rebuilds inside the same operation; other workloads issue one mutation/rebuild. These are operation costs, not measured FPS or guaranteed hardware limits. Max is the largest observed sample. Timing excludes detailed diagnostic counters; separate phase totals are retained in JSON.

优化保持尺寸和排布公式：合并并跳过无变化的 RectTransform 写入；按拓扑版本缓存根节点，在层级、启停及编辑器验证时保守失效。未引入跨帧尺寸缓存或局部重建，因此原有显式 Refresh 语义保持不变。

| Cards | Nodes | Workload | Before median | After median | Reduction | Before P95 | After P95 | Before max | After max | Geometry error |
| ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 12 | 637 | SingleLeaf | 3.573 | 2.886 | 19.2% | 4.150 | 3.397 | 4.413 | 3.577 | 0.000031 |
| 12 | 637 | Batch32 | 4.671 | 3.647 | 21.9% | 5.684 | 4.126 | 6.182 | 4.309 | 0.000031 |
| 12 | 637 | BatchAll | 6.329 | 5.754 | 9.1% | 7.186 | 6.456 | 7.805 | 6.656 | 0.000031 |
| 12 | 637 | Root1 | 3.124 | 2.582 | 17.4% | 4.123 | 3.001 | 5.058 | 3.218 | 0.000031 |
| 12 | 637 | Root4 | 9.783 | 8.139 | 16.8% | 10.143 | 8.516 | 10.317 | 9.985 | 0.000031 |
| 12 | 637 | Resize | 3.017 | 2.555 | 15.3% | 3.204 | 2.744 | 3.293 | 2.903 | 0.000031 |
| 24 | 1273 | SingleLeaf | 6.910 | 5.937 | 14.1% | 7.435 | 6.330 | 7.454 | 9.643 | 0.000031 |
| 24 | 1273 | Batch32 | 7.142 | 6.303 | 11.7% | 8.544 | 6.950 | 8.822 | 7.525 | 0.000031 |
| 24 | 1273 | BatchAll | 11.709 | 8.943 | 23.6% | 12.900 | 9.827 | 18.396 | 10.427 | 0.000031 |
| 24 | 1273 | Root1 | 6.155 | 5.914 | 3.9% | 7.041 | 6.414 | 7.120 | 6.561 | 0.000031 |
| 24 | 1273 | Root4 | 20.078 | 16.727 | 16.7% | 21.336 | 19.273 | 22.846 | 20.311 | 0.000031 |
| 24 | 1273 | Resize | 6.129 | 5.860 | 4.4% | 6.454 | 7.102 | 6.788 | 8.058 | 0.000031 |
| 48 | 2545 | SingleLeaf | 14.276 | 12.456 | 12.7% | 15.156 | 14.324 | 19.154 | 14.805 | 0.000031 |
| 48 | 2545 | Batch32 | 14.585 | 12.768 | 12.5% | 16.358 | 13.996 | 18.357 | 17.573 | 0.000031 |
| 48 | 2545 | BatchAll | 21.693 | 16.974 | 21.8% | 24.687 | 20.040 | 26.165 | 22.729 | 0.000031 |
| 48 | 2545 | Root1 | 13.881 | 12.202 | 12.1% | 14.988 | 13.089 | 15.110 | 13.476 | 0.000031 |
| 48 | 2545 | Root4 | 46.687 | 38.294 | 18.0% | 51.175 | 45.736 | 60.074 | 47.076 | 0.000031 |
| 48 | 2545 | Resize | 13.740 | 11.842 | 13.8% | 14.044 | 12.287 | 14.679 | 12.805 | 0.000031 |
| 80 | 4241 | SingleLeaf | 23.648 | 20.415 | 13.7% | 24.635 | 23.127 | 25.471 | 24.768 | 0.000031 |
| 80 | 4241 | Batch32 | 24.425 | 20.346 | 16.7% | 25.485 | 21.606 | 25.637 | 24.231 | 0.000031 |
| 80 | 4241 | BatchAll | 35.155 | 27.214 | 22.6% | 37.133 | 28.158 | 43.842 | 31.820 | 0.000031 |
| 80 | 4241 | Root1 | 23.841 | 21.926 | 8.0% | 25.698 | 22.452 | 29.146 | 22.730 | 0.000031 |
| 80 | 4241 | Root4 | 81.653 | 70.860 | 13.2% | 85.576 | 74.634 | 94.485 | 78.142 | 0.000031 |
| 80 | 4241 | Resize | 23.118 | 19.937 | 13.8% | 25.642 | 21.147 | 28.141 | 21.378 | 0.000031 |

## Sampled reference-budget crossings / 采样预算越界点

First sampled node count whose operation P95 exceeds 16.67ms; this is not a full-frame 60-FPS capacity claim. No interpolation is performed.

| Workload | Before | After |
| --- | --- | --- |
| SingleLeaf | 4241 | 4241 |
| Batch32 | 4241 | 4241 |
| BatchAll | 2545 | 2545 |
| Root1 | 4241 | 4241 |
| Root4 | 1273 | 1273 |
| Resize | 4241 | 4241 |

## Largest configuration: diagnostic SELF ms per operation

Diagnostic scopes perturb timings and are not summed with parent inclusive totals. LeafIntrinsicSize counts all leaf-resolution scopes, including Fixed/Keep.

| Workload | Phase | Before self ms/op | After self ms/op |
| --- | --- | ---: | ---: |
| BatchAll | ApplyChildRects | 5.315 | 3.204 |
| BatchAll | RootLookup | 5.380 | 0.208 |
| BatchAll | LeafIntrinsicSize | 6.640 | 6.929 |
| BatchAll | ClearDrivenTracker | 2.736 | 2.940 |
| Root1 | ApplyChildRects | 5.163 | 2.819 |
| Root1 | RootLookup | 0.007 | 0.001 |
| Root1 | LeafIntrinsicSize | 4.811 | 4.692 |
| Root1 | ClearDrivenTracker | 2.751 | 2.875 |
| Root4 | ApplyChildRects | 20.536 | 10.917 |
| Root4 | RootLookup | 0.022 | 0.002 |
| Root4 | LeafIntrinsicSize | 18.787 | 18.343 |
| Root4 | ClearDrivenTracker | 10.670 | 9.408 |

## Validation and limits / 验证与边界

- All 24 configurations matched root completion counts and all stored size/position/anchor/pivot coordinates within .01. Maximum absolute difference: 0.000031.
- Median regressions above 5%: none in this sweep.
- P95 increases above 5%: 1273/Resize.
- Observed Max increases above 5%: 1273/SingleLeaf, 1273/Resize.
- Sequential before/after Editor captures; not randomized A/B or statistical confidence intervals. Temperature, editor activity and GC can influence tails. Player builds and rendering outside explicit flush are not covered.
- Original scene is disabled during temporary test-lab execution and restored afterward. No source scene is overwritten.

- [Before raw JSON](peak-before-20261031-153602.json) / [geometry](peak-before-20261031-153602.geometry.bin)
- [After raw JSON](peak-after-20261031-153850.json) / [geometry](peak-after-20261031-153850.geometry.bin)

## Next optimization targets / 下一轮方向

1. Leaf measurement reuse and separate measure/arrange dirtiness. Even SingleLeaf still visits every leaf and container; at 4,241 nodes, leaf resolution remains a leading cost after transform/root caching. Preserve explicit Refresh semantics and width-dependent text height invalidation before caching values.
2. Driven tracker registration lifetime. ClearDrivenTracker remains roughly 2.9ms per largest single rebuild in diagnostic samples. Retaining registrations requires correct parent/child ownership, membership, mode changes, disable and reparent behavior; it is not safe to simply delete Clear/Add calls.
3. Batch synchronous requests at the caller when intermediate results are not needed. Root4 deliberately retains four immediate rebuilds; RebuildNow semantics were not silently changed to coalesce them.

本轮优化减少常数开销，并未改变采样中16.67ms首次越界的节点档位。下一步要显著改善单叶子变化，更值得降低实际重新测量和重新排布的范围；不要把平均耗时下降误认为已解决所有尾部峰值。
