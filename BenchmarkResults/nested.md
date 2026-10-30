# Nested benchmark / 复杂嵌套测试

Measured on Unity 2022.3.62f3 / Windows Editor, Edit mode, Ryzen 9 9950X, 2026-10-31 (Asia/Hong_Kong). Lyruis uses its independent postorder tree; native uGUI uses LayoutRebuilder. No Canvas or Graphics in the test trees. Setup, warmup and validation are excluded from timing. 20 warmups, 40 paired batches alternating execution order, 10 rebuilds per batch. Median averages entries 20/21; P95 uses nearest-rank entry 38. Units: ms/rebuild. These measure explicit full rebuilds, not FPS, rendering, GC or normal dirty scheduling. Player results remain unverified.

当前数据来自独立树执行模型，不是此前 LayoutGroup 适配版。Lyruis 一侧的每个参与节点都挂组件；容器直接读取子节点计算结果，只有 Content 叶子查询 LayoutUtility。原生侧使用原生布局和 LayoutElement。两侧先验证几何，再比较耗时。历史 JSON 保留用于追溯，不应混作同一实现的结果。

Depth counts container levels, followed by one leaf level. Containers alternate Vertical/Horizontal, both axes Content, asymmetric per-level padding and gap 4. Leaves vary from 40–61 by 24–33, min equals preferred. Native groups control both axes without force-expand, with ContentSizeFitter on the root only. Lyruis leaves use Content plus the same LayoutElement data, so intrinsic-size costs are included. Static workloads repeatedly rebuild the root; resize workloads alternate one deepest leaf between 40x24 and 73x39 and rebuild the root, including setter/invalidation costs. Geometry validation checks every node after warmup and every timed batch, plus both resize states beforehand, rejecting errors above .01px or non-finite sizes.

深度表示容器层数；叶子再下一层。横竖交替，双轴内容自适应，有非对称内边距。原生仅根节点使用 ContentSizeFitter，避免冗余组件。动态场景改变最深叶子的首选／最小尺寸后整树重建；两侧执行同样的属性修改。检查所有节点与根节点的尺寸和锚点位置。

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

[Raw JSON](nested-editor-2022.3.json). [Historical adapter JSON](historical-ugui-adapter-nested-editor-2022.3.json). Reproduce: Tools > Lyruis > Run Nested Benchmark And Save Report; imported Benchmark.unity runs both suites and logs paths under Application.persistentDataPath.
