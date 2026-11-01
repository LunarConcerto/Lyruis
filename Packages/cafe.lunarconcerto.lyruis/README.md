<p align="center">
  <img src="Editor/Icons/Lyruis.png" width="128" alt="Lyruis logo">
</p>

# Lyruis

**Lyra + UI** — a Unity uGUI enhancement framework, currently centered on single-component layouts. The text-free lyre-and-star mark represents the wider framework rather than a specific layout mode.

Independent postorder UI layout tree in `LunarConcerto.Lyruis`. Add LyruisLayout to containers **and every participating child**. Supports None, Horizontal, Vertical, Grid, Flow and Overlay, with Keep / Fixed / Content sizing. Core execution does not use LayoutGroup/LayoutRebuilder; intrinsic Content leaf sizes may come from uGUI.

独立递归布局树；容器和参与子节点均需挂组件。保留旧库的子节点优先计算、Grid 子项自身尺寸、None 手动包围盒和 Flow 逐行对齐语义。

[English](https://github.com/LunarConcerto/Lyruis/blob/main/README.md) · [中文](https://github.com/LunarConcerto/Lyruis/blob/main/README.zh-CN.md) · [Architecture](Documentation~/architecture.md)

Import Gallery and Benchmark from Package Manager Samples. Unity 2022.3+, MIT, version 0.1.0 (development).

[Inspector guide / 检查器指南](Documentation~/inspector.md): bilingual grouped controls, sizing shortcuts, alignment picker, linked padding, multi-edit, Undo and Prefab overrides.
