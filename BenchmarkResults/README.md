# Benchmark / 性能对比

Unity 2022.3.62f3, Windows Editor (Play mode), AMD Ryzen 9 9950X. Recorded 2026-10-31 (Asia/Hong_Kong).

Measures LayoutRebuilder.ForceRebuildLayoutImmediate on separate flat trees, with identical 40x24 LayoutElement children. Root 800x600, top-left, no padding, gap 4. Grid: 10 columns. Native linear groups control both dimensions without force-expand; min equals preferred so neither baseline shrinks. Both trees have no Graphics or Canvas. Setup, geometry check and destruction are excluded.

20 warmup rebuilds each; 40 paired samples, alternating execution order; 10 rebuilds per sample. Values are milliseconds per rebuild. Median averages sorted entries 20 and 21; P95 is nearest-rank entry 38. maxGeometryError compares child size and anchored position before timing. This does not measure rendering, allocations, nested UI, dirty scheduling or player FPS. Run on target hardware for decisions.

测试为同步强制重建的平铺树；不包含创建、销毁、渲染、嵌套布局或正常脏标记调度。预热20次，40组交替测量，每组10次重建。中位数与P95单位均为毫秒。几何误差对比子节点尺寸与锚点位置。不是完整应用帧率测试。

| Mode | Children | Lyruis median | Native median | Lyruis P95 | Native P95 | Geometry error |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Horizontal | 10 | 0.1186 | 0.1171 | 0.1278 | 0.1187 | 0 |
| Horizontal | 100 | 1.1322 | 1.1395 | 1.1484 | 1.1582 | 0 |
| Horizontal | 1000 | 11.3604 | 12.2483 | 11.4914 | 12.3875 | 0 |
| Vertical | 10 | 0.1187 | 0.1158 | 0.1231 | 0.1193 | 0 |
| Vertical | 100 | 1.1449 | 1.1359 | 1.1543 | 1.1482 | 0 |
| Vertical | 1000 | 11.5492 | 12.3283 | 12.2912 | 13.4171 | 0 |
| Grid | 10 | 0.0449 | 0.0456 | 0.0479 | 0.0462 | 0 |
| Grid | 100 | 0.4023 | 0.4221 | 0.4169 | 0.4386 | 0 |
| Grid | 1000 | 4.0257 | 4.2286 | 4.0981 | 4.2749 | 0 |

All nine geometry checks matched. Timing differences are small in several cases and depend on the environment; this single run is not statistical evidence of a universal speedup. No allocation or zero-GC claim is made.

全部9组几何一致。部分耗时差异很小，单次运行不能证明普遍性能优势；未测量GC或分配率。

Reproduce: import Benchmark sample and enter Play mode; JSON is logged under Application.persistentDataPath. In this development project use Tools > Lyruis > Run Benchmark And Save Report. Raw data: [editor-2022.3.json](editor-2022.3.json).
