# Flat benchmark / 平铺测试

Measured on Unity 2022.3.62f3 / Windows Editor, Edit mode, Ryzen 9 9950X, 2026-10-31 (Asia/Hong_Kong). Lyruis uses its independent postorder tree; native uGUI uses LayoutRebuilder. No Canvas or Graphics in the test trees. Setup, warmup and validation are excluded from timing. 20 warmups, 40 paired batches alternating execution order, 10 rebuilds per batch. Median averages entries 20/21; P95 uses nearest-rank entry 38. Units: ms/rebuild. These measure explicit full rebuilds, not FPS, rendering, GC or normal dirty scheduling. Player results remain unverified.

当前数据来自独立树执行模型，不是此前 LayoutGroup 适配版。Lyruis 一侧的每个参与节点都挂组件；容器直接读取子节点计算结果，只有 Content 叶子查询 LayoutUtility。原生侧使用原生布局和 LayoutElement。两侧先验证几何，再比较耗时。历史 JSON 保留用于追溯，不应混作同一实现的结果。

Root 800x600; item preferred/min 40x24; gaps 4; grid cell 40x24, 10 columns. Lyruis item Content sizes remain independent of grid cells; chosen sizes match the native grid baseline. Linear native groups control both axes without force-expand.

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

[Raw JSON](editor-2022.3.json). [Historical adapter JSON](historical-ugui-adapter-editor-2022.3.json). [Nested results](nested.md). Reproduce: Tools > Lyruis > Run Benchmark And Save Report, or Play the imported Benchmark scene.
