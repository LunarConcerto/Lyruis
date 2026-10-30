# Lyruis

[English](README.md)

LunarConcerto 的单组件 uGUI 布局框架。**核心采用独立组件树、子节点优先的尺寸计算和自定义排布，不继承 LayoutGroup，也不通过 LayoutRebuilder 执行 Lyruis 布局。** 只有 Content 叶子节点使用 LayoutUtility 获取自身内容首选尺寸。

版本 0.1.0（开发中），沿用 MIT 许可证。已验证 Unity 2022.3.62f3 / Windows Editor；其他版本和目标 Player 尚未验证。

## 安装

Package Manager → Add package from git URL：

```text
https://github.com/LunarConcerto/Lyruis.git?path=/Packages/com.lunarconcerto.lyruis
```

追加 `#<提交 SHA>` 可锁定版本。要求 Unity 2022.3+，依赖 uGUI（包已声明）。仓库是开发工程，安装包子目录不会安装开发工程中的 MCP 等工具。

## 快速开始

1. 在面板和**每个参与布局的子节点**上添加 Layout > Lyruis Layout。只处理激活的直接子组件，按兄弟顺序排列；普通 RectTransform 和禁用组件不参与。
2. 面板选择 Vertical，WidthMode 设 Fixed，FixedSize X 设 400，HeightMode 设 Content。
3. 子节点使用 Fixed 固定尺寸、Keep 保留尺寸，或 Content 读取叶子上的 Text/Image/LayoutElement 首选尺寸。
4. 设置 padding、Spacing、childAlignment。配置属性 setter 自动请求根节点重建。修改外部内容如 Text.text、LayoutElement.preferredHeight 后，显式调用 Refresh()。

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
        // 每个参与节点也需挂 LyruisLayout。
        layout.Refresh();
    }
}
```

## 布局与尺寸规则

| 模式 | 语义 |
| --- | --- |
| None | 保留手动位置；Content 按参与子节点的相对包围盒计算，包含子树和原点，再加 padding。 |
| Horizontal / Vertical | 主轴取子节点尺寸之和加间距；交叉轴取最大值，再加 padding。 |
| Grid | CellSize 决定格子步进和容器尺寸，子节点保留自身尺寸；Axis 决定横向铺满列数或纵向铺满行数。GridCount 为0时回退为2；CellSize 为零时从首个子节点初始化。 |
| Flow | 按可用主轴空间换行／换列；每行分别对齐，行内子项也进行交叉轴对齐。主轴 Content 或可用空间为0时不换行；超大项可溢出。 |
| Overlay | 额外保留的便捷模式：共用内容框对齐；与 None 不同。 |

Keep 保留 sizeDelta，包括拉伸锚点的节点。Fixed 读取 FixedSize。Content 容器根据注册的子节点计算；Content 叶子读取自身其他组件提供的首选尺寸，不叠加容器 padding。叶子先应用配置宽度再测量首选高度；不求解任意双向尺寸依赖。

横竖排与网格沿用旧版边缘内边距规则：靠起点使用起点 padding，靠终点使用终点 padding，居中按外框中心计算。Flow 在扣除内边距后的空间中逐行对齐，并将富余空间限制为非负。Spacing 可以为负值。

不提供 flex-grow、百分比尺寸、虚拟化，也不自动混用同一控制子树上的原生布局组件。LayoutElement.ignoreLayout 不会排除已注册的 Lyruis 节点；需要排除时禁用它的 LyruisLayout。

## API 与刷新

命名空间：LunarConcerto.Lyruis。配置属性：Mode、Axis、WidthMode、HeightMode、FixedSize、Spacing、CellSize、GridCount、padding、childAlignment。SizeMode 包含 Keep、Fixed、Content；FlowAxis 包含 Horizontal、Vertical。

- Refresh() / RefreshRootLayout()：延迟并合并到激活的根节点。运行时由 Canvas.willRenderCanvases 刷新；编辑器另通过 delayCall 刷新。
- RebuildNow() / RefreshLayout()：立即重建当前子树。
- RebuildRootNow()：立即重建相连的根节点。
- OnLayoutRefreshed：重建结束后按子节点到父节点顺序通知。后续更新使用 Refresh，避免在事件中同步递归重建。
- RectTransform：缓存的变换访问属性。

缓存子节点列表，在层级和启停变化时失效。执行顺序为：递归刷新子节点、缓存结果、计算自身尺寸、排布子节点。内部写入尺寸时抑制反馈请求；根尺寸变化请求刷新。外部内容变化需显式 Refresh，不逐帧轮询。父节点驱动子节点锚点、尺寸和位置。禁用解除驱动标记，但不恢复历史变换。直接修改返回的 RectOffset 内部字段后，也需调用 Refresh。

## 示例、Benchmark 与开发

Package Manager 的 Samples 可导入 Gallery 与 Benchmark。开发工程已导入到 Assets/Samples/Lyruis/0.1.0。Gallery 包含横排、竖排、网格、Flow、Overlay 与嵌套布局。Benchmark 场景进入 Play 后运行平铺和嵌套测试，Console 输出 Application.persistentDataPath 下两个 JSON 的路径。

开发菜单：Tools > Lyruis > Generate Sample Scenes、Run Benchmark And Save Report、Run Nested Benchmark And Save Report。参见[平铺实测](BenchmarkResults/README.md)、[嵌套实测](BenchmarkResults/nested.md)和[验证记录](BenchmarkResults/validation.md)。Lyruis 调用自己的 RebuildNow，原生侧调用 LayoutRebuilder.ForceRebuildLayoutImmediate。测试构建几何等价的树，不代表完整应用 FPS 或 Canvas 自动调度。此前 LayoutGroup 适配版的数据保留为 historical JSON，不代表当前实现。

在 Test Runner 的 EditMode 中运行 LunarConcerto.Lyruis.Tests；开发工程通过 testables 启用包测试。覆盖子节点优先测量、注册节点、旧版 Grid 尺寸、None 包围盒、逐行对齐、Keep／拉伸、回调与层级变化。尚未验证 Player 构建与独立消费工程的样例导入。

## 架构、重写与迁移

Runtime 拆分为 LyruisLayout（组件入口和层级事件）、LayoutScheduler（根请求合并）、LayoutTree（递归尺寸计算与变换写入）、LayoutEngine（几何排布）。Inspector 位于独立 Editor 程序集。详见[架构与迁移](Packages/com.lunarconcerto.lyruis/Documentation~/architecture.md)。

本次阅读归档实现以保留计算模型与布局语义，再以新的结构和代码实现。未引入原命名空间、公司专用辅助工具、日志和资源。这不是人员隔离的正式 clean-room 流程，也不构成权属许可声明。

不提供旧组件和字段的自动迁移。请重建代表性 prefab 并验证外观。本次也修正了上一版 Lyruis 适配方案：需要显式注册子节点、Grid 不再拉伸子项、None 与 Overlay 分开、Keep 使用 sizeDelta。贡献时请提供 Unity 版本、最小复现和预期几何。参见[更新记录](Packages/com.lunarconcerto.lyruis/CHANGELOG.md)。

PlayMode tests: `LunarConcerto.Lyruis.PlayMode.Tests` (deferred sizing and root request coalescing / 延迟尺寸计算与根请求合并).

## Profiler 交互场景

导入 **Profiler Lab**，打开 ProfilerLab.unity 后进入 Play。包含24张卡片、1,273个布局节点、6层嵌套、168个可变文本叶子，混合横竖排、Grid、Flow、None。屏幕按钮可切换静止、单叶子变化、批量变化、逐帧强制重建和根宽度变化；渲染和细粒度标记可分别切换。详见[Profiler 使用说明](Packages/com.lunarconcerto.lyruis/Samples~/ProfilerLab/README.md)。开发菜单：Tools > Lyruis > Generate Profiler Lab Scene。
