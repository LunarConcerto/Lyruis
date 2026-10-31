# Same-runtime retest comparison / 同版本重测对比

Unity 2022.3.62f3; AMD Ryzen 9 9950X 16-Core Processor ; 1366x768; 40 samples/config; 6 warmups; external Profiler False.

Operations include one explicit Canvas flush. Root4 issues four synchronous rebuilds inside the same operation; other workloads issue one mutation/rebuild. These are operation costs, not measured FPS or guaranteed hardware limits. Max is the largest observed sample. Timing excludes detailed diagnostic counters; separate phase totals are retained in JSON.

本页对比同一运行实现的两次测量；没有新增性能代码修改。Reduction 为跨运行差值，并非本轮优化收益。两次采样环境元数据一致，但不能排除编辑器状态、系统负载或采样噪声影响。

| Cards | Nodes | Workload | Before median | After median | Reduction | Before P95 | After P95 | Before max | After max | Geometry error |
| ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 12 | 637 | SingleLeaf | 2.886 | 3.085 | -6.9% | 3.397 | 3.693 | 3.577 | 4.879 | 0.000000 |
| 12 | 637 | Batch32 | 3.647 | 3.882 | -6.4% | 4.126 | 4.527 | 4.309 | 5.786 | 0.000000 |
| 12 | 637 | BatchAll | 5.754 | 5.736 | 0.3% | 6.456 | 6.710 | 6.656 | 7.301 | 0.000000 |
| 12 | 637 | Root1 | 2.582 | 3.143 | -21.7% | 3.001 | 4.175 | 3.218 | 4.416 | 0.000000 |
| 12 | 637 | Root4 | 8.139 | 8.643 | -6.2% | 8.516 | 9.344 | 9.985 | 9.717 | 0.000000 |
| 12 | 637 | Resize | 2.555 | 2.714 | -6.2% | 2.744 | 2.936 | 2.903 | 3.132 | 0.000000 |
| 24 | 1273 | SingleLeaf | 5.937 | 6.558 | -10.5% | 6.330 | 7.183 | 9.643 | 7.726 | 0.000000 |
| 24 | 1273 | Batch32 | 6.303 | 6.979 | -10.7% | 6.950 | 7.760 | 7.525 | 7.886 | 0.000000 |
| 24 | 1273 | BatchAll | 8.943 | 9.908 | -10.8% | 9.827 | 11.063 | 10.427 | 11.129 | 0.000000 |
| 24 | 1273 | Root1 | 5.914 | 6.132 | -3.7% | 6.414 | 6.942 | 6.561 | 6.961 | 0.000000 |
| 24 | 1273 | Root4 | 16.727 | 19.061 | -14.0% | 19.273 | 23.824 | 20.311 | 30.041 | 0.000000 |
| 24 | 1273 | Resize | 5.860 | 6.222 | -6.2% | 7.102 | 6.984 | 8.058 | 7.147 | 0.000000 |
| 48 | 2545 | SingleLeaf | 12.456 | 14.425 | -15.8% | 14.324 | 15.242 | 14.805 | 16.152 | 0.000000 |
| 48 | 2545 | Batch32 | 12.768 | 14.674 | -14.9% | 13.996 | 16.768 | 17.573 | 19.494 | 0.000000 |
| 48 | 2545 | BatchAll | 16.974 | 19.127 | -12.7% | 20.040 | 20.669 | 22.729 | 21.344 | 0.000000 |
| 48 | 2545 | Root1 | 12.202 | 13.947 | -14.3% | 13.089 | 14.730 | 13.476 | 19.107 | 0.000000 |
| 48 | 2545 | Root4 | 38.294 | 46.796 | -22.2% | 45.736 | 48.916 | 47.076 | 49.201 | 0.000000 |
| 48 | 2545 | Resize | 11.842 | 13.077 | -10.4% | 12.287 | 14.091 | 12.805 | 15.518 | 0.000000 |
| 80 | 4241 | SingleLeaf | 20.415 | 23.637 | -15.8% | 23.127 | 24.250 | 24.768 | 24.455 | 0.000000 |
| 80 | 4241 | Batch32 | 20.346 | 24.466 | -20.2% | 21.606 | 25.724 | 24.231 | 28.069 | 0.000000 |
| 80 | 4241 | BatchAll | 27.214 | 31.957 | -17.4% | 28.158 | 33.203 | 31.820 | 39.792 | 0.000000 |
| 80 | 4241 | Root1 | 21.926 | 24.312 | -10.9% | 22.452 | 25.238 | 22.730 | 26.095 | 0.000000 |
| 80 | 4241 | Root4 | 70.860 | 78.945 | -11.4% | 74.634 | 82.425 | 78.142 | 86.505 | 0.000000 |
| 80 | 4241 | Resize | 19.937 | 23.390 | -17.3% | 21.147 | 25.782 | 21.378 | 26.233 | 0.000000 |

## Sampled reference-budget crossings / 采样预算越界点

First sampled node count whose operation P95 exceeds 16.67ms; this is not a full-frame 60-FPS capacity claim. No interpolation is performed.

| Workload | Before | After |
| --- | --- | --- |
| SingleLeaf | 4241 | 4241 |
| Batch32 | 4241 | 2545 |
| BatchAll | 2545 | 2545 |
| Root1 | 4241 | 4241 |
| Root4 | 1273 | 1273 |
| Resize | 4241 | 4241 |

## Largest configuration: diagnostic SELF ms per operation

Diagnostic scopes perturb timings and are not summed with parent inclusive totals. LeafIntrinsicSize counts all leaf-resolution scopes, including Fixed/Keep.

| Workload | Phase | Before self ms/op | After self ms/op |
| --- | --- | ---: | ---: |
| BatchAll | ApplyChildRects | 3.204 | 3.187 |
| BatchAll | RootLookup | 0.208 | 0.250 |
| BatchAll | LeafIntrinsicSize | 6.929 | 7.359 |
| BatchAll | ClearDrivenTracker | 2.940 | 3.553 |
| Root1 | ApplyChildRects | 2.819 | 2.960 |
| Root1 | RootLookup | 0.001 | 0.001 |
| Root1 | LeafIntrinsicSize | 4.692 | 5.059 |
| Root1 | ClearDrivenTracker | 2.875 | 3.435 |
| Root4 | ApplyChildRects | 10.917 | 11.707 |
| Root4 | RootLookup | 0.002 | 0.001 |
| Root4 | LeafIntrinsicSize | 18.343 | 19.665 |
| Root4 | ClearDrivenTracker | 9.408 | 13.069 |

## Validation and limits / 验证与边界

- All 24 configurations matched root completion counts and all stored size/position/anchor/pivot coordinates within .01. Maximum absolute difference: 0.000000.
- Median regressions above 5%: 637/SingleLeaf, 637/Batch32, 637/Root1, 637/Root4, 637/Resize, 1273/SingleLeaf, 1273/Batch32, 1273/BatchAll, 1273/Root4, 1273/Resize, 2545/SingleLeaf, 2545/Batch32, 2545/BatchAll, 2545/Root1, 2545/Root4, 2545/Resize, 4241/SingleLeaf, 4241/Batch32, 4241/BatchAll, 4241/Root1, 4241/Root4, 4241/Resize.
- P95 increases above 5%: 637/SingleLeaf, 637/Batch32, 637/Root1, 637/Root4, 637/Resize, 1273/SingleLeaf, 1273/Batch32, 1273/BatchAll, 1273/Root1, 1273/Root4, 2545/SingleLeaf, 2545/Batch32, 2545/Root1, 2545/Root4, 2545/Resize, 4241/Batch32, 4241/BatchAll, 4241/Root1, 4241/Root4, 4241/Resize.
- Observed Max increases above 5%: 637/SingleLeaf, 637/Batch32, 637/BatchAll, 637/Root1, 637/Resize, 1273/BatchAll, 1273/Root1, 1273/Root4, 2545/SingleLeaf, 2545/Batch32, 2545/Root1, 2545/Resize, 4241/Batch32, 4241/BatchAll, 4241/Root1, 4241/Root4, 4241/Resize.
- Sequential before/after Editor captures; not randomized A/B or statistical confidence intervals. Temperature, editor activity and GC can influence tails. Player builds and rendering outside explicit flush are not covered.
- Original scene is disabled during temporary test-lab execution and restored afterward. No source scene is overwritten.

- [Before raw JSON](peak-after-20261031-153850.json) / [geometry](peak-after-20261031-153850.geometry.bin)
- [After raw JSON](peak-retest-20261101-20261031-161604.json) / [geometry](peak-retest-20261101-20261031-161604.geometry.bin)
