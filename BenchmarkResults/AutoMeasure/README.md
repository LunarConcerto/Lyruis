# Automated measurements / 自动测量报告

Windows Editor, Unity 2022.3.62f3. Both recorded with Profiler window recording disabled. Each run uses five workloads, two passes, 40 samples per pass, and 10 warmup operations.

- [measurement-20261031-054359-442](measurement-20261031-054359-442.md) ([raw JSON](measurement-20261031-054359-442.json))
- [measurement-20261031-054729-522](measurement-20261031-054729-522.md) ([raw JSON](measurement-20261031-054729-522.json))

The later run includes the expanded rule-based findings. Repeated measurements show run-to-run noise; do not subtract diagnostic instrumentation overhead or compare these mixed Text/Canvas operation windows directly with the earlier flat/nested microbenchmarks. Read each report for its exact boundaries.

实测确认：Idle 无根完成；单叶子、批量、强制重建和根宽变化每个操作均为一次根完成。单叶子变化仍经过720个叶子测量作用域、529个容器测量作用域、1273次 tracker 清理。调用次数是标记作用域次数，不是对应底层 API 的调用次数。

## Reproduce / 复现

Open ProfilerLab.unity and use Tools > Lyruis > Run Automated Measurement, or enter Play and press AUTO REPORT / Stop. Reports are timestamped; these two raw captures are retained as evidence rather than overwritten.
