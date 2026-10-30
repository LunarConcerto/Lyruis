# Complex nested benchmark / 复杂嵌套测试

Unity 2022.3.62f3, Windows Editor (Edit mode), AMD Ryzen 9 9950X; 2026-10-31 Asia/Hong_Kong. [Raw JSON](nested-editor-2022.3.json).

## Method / 方法

- Depth is the number of container levels; leaves are one level below. Each container has the configured branching count. Alternating Vertical and Horizontal groups use content sizing on both axes, 4px gaps, top-left alignment, and asymmetric level-dependent padding. Leaves have varied preferred sizes (40–61 by 24–33px), with min equal to preferred.
- Lyruis: one Content/Content LyruisLayout per container. Native: Horizontal/VerticalLayoutGroup per container, control both axes, no force-expand, plus ContentSizeFitter on the root only. Nested native groups receive preferred sizes from parents; adding fitters to them would be redundant. No Graphics, Canvas or rendering.
- Static: repeatedly force-rebuild the root. Resize: alternate one deepest leaf between 40x24 and 73x39 before each whole-root rebuild; includes the identical LayoutElement setters and synchronous invalidation overhead on both sides. It does not measure automatic Canvas scheduling or subtree-only rebuilds.
- 20 warmups each; 40 paired batches, alternating which implementation runs first; 10 rebuilds per batch. Setup, validation and destruction excluded. Median averages sorted entries 20/21; P95 is nearest-rank entry 38. Units: ms/rebuild.
- Validate root and all descendants: sizes and anchored positions, after warmup and after each timed batch; additionally validate both resize states before timing. Reject errors over 0.01px or non-finite sizes. All 8 cases passed with 0px error.

深度表示容器层数，叶子再下一层。容器横竖交替、双轴内容自适应，间距4像素，包含随层级变化的非对称内边距；叶子尺寸不统一。原生内部容器不添加冗余 ContentSizeFitter，仅根节点使用。

静态场景反复强制重建；动态场景每次修改一个最深叶子的首选尺寸和最小尺寸，再重建整树，计时包含属性修改及同步标脏开销。两侧执行相同操作，不测试 Canvas 自动脏传播或局部子树优化。检查全部节点几何，误差超标则拒绝报告。

## Results / 结果

| Depth | Branches | Containers | Leaves | Workload | Lyruis median | Native median | Lyruis / native | Lyruis P95 | Native P95 | Error px |
| ---: | ---: | ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 3 | 2 | 7 | 8 | Static rebuild | 0.1944 | 0.1772 | 1.097x | 0.2010 | 0.1822 | 0 |
| 3 | 2 | 7 | 8 | Leaf resize | 0.2078 | 0.1932 | 1.076x | 0.2088 | 0.2001 | 0 |
| 5 | 2 | 31 | 32 | Static rebuild | 0.8608 | 0.7706 | 1.117x | 0.8687 | 0.7799 | 0 |
| 5 | 2 | 31 | 32 | Leaf resize | 0.8809 | 0.7934 | 1.110x | 0.8919 | 0.8023 | 0 |
| 7 | 2 | 127 | 128 | Static rebuild | 3.5391 | 3.1508 | 1.123x | 3.6375 | 3.1980 | 0 |
| 7 | 2 | 127 | 128 | Leaf resize | 3.5887 | 3.2018 | 1.121x | 3.6345 | 3.2318 | 0 |
| 3 | 4 | 21 | 64 | Static rebuild | 1.1256 | 1.0556 | 1.066x | 1.1569 | 1.0646 | 0 |
| 3 | 4 | 21 | 64 | Leaf resize | 1.1401 | 1.0768 | 1.059x | 1.1498 | 1.0862 | 0 |

Ratios above 1 mean Lyruis took longer. This run does not show an advantage for the current Lyruis implementation in these nested workloads. Both use the native uGUI traversal; Lyruis still measures child preferred sizes and solves geometry in multiple callbacks. A single run cannot isolate the exact cost or establish a general performance claim; profiling is required before choosing an optimization.

倍率大于1表示 Lyruis 耗时更高。本次测试没有证明当前实现的复杂嵌套性能优势。两者都依赖原生布局遍历，Lyruis 仍在多个回调中查询子节点首选尺寸、计算几何；具体瓶颈需要 Profiler 定位，不能仅凭此表确定。

Not covered: wrapped text, grids/Flow, Canvas dirty scheduling, allocations, rendering, or target players. The earlier flat report ran in Play mode; this report ran in Edit mode and should be compared only within its own matched pairs.

未覆盖换行文本、网格／Flow、Canvas 调度、分配率、渲染及目标 Player。旧的平铺报告在 Play 模式测量，本报告在 Edit 模式测量，应只比较本报告内成对结果。

## Reproduce / 复现

Development project: Tools > Lyruis > Run Nested Benchmark And Save Report. The report overwrites BenchmarkResults/nested-editor-2022.3.json. Imported Benchmark.unity runs flat and nested suites on Play and logs both output paths under Application.persistentDataPath.
