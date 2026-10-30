# Lyruis

[English](README.md)

LunarConcerto 的单组件 uGUI 布局包。一个 Inspector 同时配置容器排布与自身尺寸，内部将 Unity 生命周期和几何计算分开维护。

当前版本 **0.1.0**；验证环境为 **Unity 2022.3.62f3 / Windows Editor**。尚未验证其他 Unity 版本或 Player 性能。使用仓库已有的 MIT 许可证。

## 安装

Package Manager → **Add package from git URL**：

```text
https://github.com/LunarConcerto/Lyruis.git?path=/Packages/com.lunarconcerto.lyruis
```

需要锁定版本时追加 `#<提交 SHA>`。要求 Unity 2022.3+，声明依赖 `com.unity.ugui`。整个仓库是可打开的开发工程；使用者仅安装上面的包子目录，无需安装开发工程中的 MCP 等工具。

## 快速开始

1. 创建 UI Panel，添加 **Layout > Lyruis Layout**。
2. Mode 选择 Vertical；Width Mode 选择 Fixed，Fixed Size X 设为 400；Height Mode 选择 Content。
3. 添加 Text、Image 或设置了 LayoutElement 首选尺寸的子节点。只有需要自身排布或尺寸规则的子节点才需要再挂 LyruisLayout。
4. 设置 Padding、Spacing、Child Alignment。通过 uGUI 布局系统自动重建。

```csharp
using LunarConcerto.Lyruis;
using UnityEngine;

public class PanelSetup : MonoBehaviour
{
    void Start()
    {
        var layout = gameObject.AddComponent<LyruisLayout>();
        layout.Mode = LayoutMode.Flow;
        layout.WidthMode = SizeMode.Fixed;
        layout.FixedSize = new Vector2(400, 0);
        layout.HeightMode = SizeMode.Content;
        layout.Spacing = new Vector2(8, 8);
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.Refresh();
    }
}
```

## 排布与尺寸规则

| 配置 | 行为 |
| --- | --- |
| Horizontal / Vertical | 按兄弟顺序横排／竖排，交叉轴遵循 Child Alignment |
| Grid | 所有子节点填满 Cell Size；Axis 为 Horizontal 时 Grid Count 是列数，为 Vertical 时是行数 |
| Flow | 主轴空间不足时换行／换列；超大子项单独占一行并可能溢出 |
| Overlay | 子节点共用一个内容框并按 Child Alignment 对齐，不保留手动位置的包围盒 |
| Keep | 以当前 RectTransform.rect 尺寸作为首选尺寸；根节点不自行驱动此轴 |
| Fixed | 使用 Fixed Size，对负尺寸按 0 处理 |
| Content | 内容尺寸加 padding；叶子读取自身其他 ILayoutElement 的首选尺寸 |

测量时 Spacing 不小于 0，Grid Count 至少为 1。Flow 主轴使用 Content 时视为无限空间，不换行；该轴应使用 Fixed 或 Keep。Flow 的 Child Alignment 对齐整个内容块，每一行从主轴起点开始。当前没有 CSS flex-grow、百分比尺寸、margin、虚拟列表、RTL 或逐行两端对齐。

参与布局的是激活的直接 RectTransform 子节点，按兄弟顺序处理，并尊重 `ILayoutIgnorer.ignoreLayout`。非网格模式通过 LayoutUtility 读取首选尺寸。没有尺寸提供者的普通 RectTransform 会测量为 0；请添加 LayoutElement，或挂 LyruisLayout 并选择 Fixed/Keep。

父布局驱动子节点的锚点、位置和尺寸。避免在同一对象的同一轴上叠加其他 LayoutGroup 或 ContentSizeFitter。Lyruis 通过 LayoutGroup 接入 ILayoutElement、ILayoutController、ILayoutSelfController。

## API

命名空间：`LunarConcerto.Lyruis`。

| 成员 | 类型／用途 |
| --- | --- |
| Mode | LayoutMode：Overlay、Horizontal、Vertical、Grid、Flow |
| Axis | FlowAxis：Horizontal / Vertical，影响 Grid 和 Flow |
| WidthMode / HeightMode | SizeMode：Keep、Fixed、Content |
| FixedSize | Vector2，仅 Fixed 轴读取对应值 |
| Spacing | Vector2，水平／垂直间距 |
| CellSize / GridCount | Vector2 / int，仅 Grid 使用 |
| padding / childAlignment | 继承的 RectOffset / TextAnchor |
| Refresh() | 标记脏状态，批量修改外部内容后调用 |
| RebuildNow() | 立即重建当前子树；若祖先依赖其尺寸，请在布局根节点调用 |

公开配置属性的 setter 自动标脏。Inspector、层级、尺寸变化沿用 LayoutGroup 的失效机制。不要从布局回调中递归调用 RebuildNow。换行文本先确定宽度再测量内容高度。禁用组件会解除驱动标记，但不会恢复历史 RectTransform 值。

## 示例

在 Package Manager 的 Samples 中导入 **Layout Gallery** 和 **Benchmark**。Gallery.unity 包含横排、竖排、网格、流式、叠放和嵌套布局。内容超过容器时会溢出，布局本身不裁剪。

开发工程已导入到 `Assets/Samples/Lyruis/0.1.0`。菜单 **Tools > Lyruis > Generate Sample Scenes** 可重新生成。Benchmark.unity 进入 Play 后执行，将 JSON 写入 `Application.persistentDataPath/lyruis-benchmark.json`，Console 输出路径。样例脚本只有导入后才编译，不进入包的 Runtime 程序集。

## Benchmark

参见[方法与实测](BenchmarkResults/README.md)和[原始 JSON](BenchmarkResults/editor-2022.3.json)。对比 HorizontalLayoutGroup、VerticalLayoutGroup、GridLayoutGroup，分别测试 10／100／1000 个子节点。测量同步强制重建，不代表渲染、静止帧、分配率或完整应用 FPS。Flow 没有纳入原生性能对照；结果仅适用于对应测试环境，不承诺普遍加速。

## 目录与开发

```text
Packages/com.lunarconcerto.lyruis/
  Runtime/        组件、配置类型、几何引擎
  Editor/         按模式显示的 Inspector
  Tests/Editor/   Unity EditMode 行为测试
  Samples~/       可导入场景和 Benchmark
  Documentation~/ 架构与迁移说明
```

使用 Unity 2022.3.62f3 打开仓库。manifest 的 testables 已包含本包，在 Test Runner 的 EditMode 中运行 `LunarConcerto.Lyruis.Tests`。测试覆盖自适应、嵌套、忽略／失活节点、换行、网格和空容器 padding。

通过 **Tools > Lyruis > Run Benchmark And Save Report** 重现 Editor 测量；目标平台性能需将 Benchmark 场景构建为 Player 后测量。Unity 生成资源元数据，应将 `.meta` 与资源一起提交。

## 重写与迁移

本次根据功能描述重新实现，没有导入归档实现代码、注释或资源。新版本不提供二进制兼容：组件类型、命名空间、序列化字段和部分语义均已变化，没有自动迁移器。建议重建代表性的 prefab，核对尺寸和对齐。详见[架构与迁移说明](Packages/com.lunarconcerto.lyruis/Documentation~/architecture.md)。

沿用仓库已有 MIT 许可证。重新实现本身不能证明既往职务成果的权属已经解除；开发过程说明不代表法律审查结论。

贡献时请提供最小 UI 复现、Unity 版本和预期几何结果。几何改动放在引擎，Unity 生命周期改动放在组件，并为回归问题添加行为测试。参见[更新记录](Packages/com.lunarconcerto.lyruis/CHANGELOG.md)。
