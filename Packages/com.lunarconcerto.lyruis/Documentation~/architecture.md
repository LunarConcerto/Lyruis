# Architecture / 架构

`LyruisLayout` is a LayoutGroup adapter and self size controller. Native uGUI traverses horizontal measurement, horizontal control, vertical measurement, vertical control. This permits width-dependent text height to be measured after horizontal constraints are applied. Lists are reused; content changes do not create a second scheduling system.

`LayoutEngine` receives measured child sizes and emits top-left rectangles. Its single pass handles axis transposition, rows and columns, with no scene traversal or RectTransform writes. `LyruisLayoutEditor` owns the Inspector. These responsibilities have separate files and assembly boundaries.

LyruisLayout 负责接入原生布局遍历和自身尺寸驱动；LayoutEngine 只计算矩形；Inspector 位于独立 Editor 程序集。每个容器仍然只需一个组件，不引入第二套逐帧调度器。

## Migration / 迁移

| Earlier concept / 旧概念 | New choice / 新配置 |
| --- | --- |
| None / manual bounds | Overlay; behavior differs, manual bounds are not preserved / 不保留手动包围盒 |
| Flex | Flow with Axis; fixed main axis for wrapping / 固定主轴实现换行 |
| auto | Content |
| default | Keep uses rect size including stretch, rather than sizeDelta / 使用实际 rect 尺寸 |
| fixed pixels | Fixed + FixedSize |
| Child component required | Native preferred-size providers accepted / 原生尺寸提供者可直接参与 |
| Grid child size independent of cell | Grid fills cells / 子节点填满格子 |
| Refresh root helper | Refresh for deferred invalidation; RebuildNow on chosen root for synchronous work |

No old namespace aliases, field serializers, helper implementations or archived assets are included. Only the archived usage document was read to establish the functional scope. This is a fresh implementation after reviewing that document, not a formal independently separated clean-room process. No rights clearance is asserted.

未包含旧命名空间别名、序列化字段转换、旧辅助工具实现或归档资源。仅阅读归档使用文档确定功能范围；这是阅读功能说明后的新实现，并非具有人员隔离的正式 clean-room 流程，也不构成权属许可声明。
