# Lyruis

[简体中文](README.zh-CN.md)

Single-component uGUI layout framework by LunarConcerto. **The core runs its own component tree, postorder sizing and geometry calculation. It does not inherit LayoutGroup or use LayoutRebuilder to execute Lyruis layouts.** Unity LayoutUtility is used only for intrinsic sizes of Content leaves.

Version 0.1.0 (development), MIT. Verified on Unity 2022.3.62f3 / Windows Editor; other Unity versions and target players remain unverified.

## Install

Package Manager → Add package from git URL:

```text
https://github.com/LunarConcerto/Lyruis.git?path=/Packages/com.lunarconcerto.lyruis
```

Append `#<commit SHA>` to pin a revision. Requires Unity 2022.3+ and uGUI (declared dependency). This repository is a development project; installing the package subdirectory does not install its MCP/editor development dependencies.

## Quick start

1. Add **Layout > Lyruis Layout** to the panel **and each participating child**. Active direct children with this component participate in sibling order; plain RectTransforms and disabled components are excluded.
2. Choose Vertical on the panel. Set WidthMode to Fixed, FixedSize X to 400, HeightMode to Content.
3. Set children to Fixed sizes, Keep authored sizes, or Content to measure Text/Image/LayoutElement on leaves.
4. Configure padding, spacing and alignment. Property setters request a deferred root rebuild. After external content changes such as `Text.text` or `LayoutElement.preferredHeight`, call `Refresh()` on the changed node or root.

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
        // Each participating child also needs LyruisLayout.
        layout.Refresh();
    }
}
```

## Rules

| Mode | Semantics |
| --- | --- |
| None | Preserve child positions. Content uses relative bounds of participating child subtrees, including the origin, plus padding. |
| Horizontal / Vertical | Sum sizes and gaps on the main axis; maximum child size on the cross axis; add padding. |
| Grid | Cell size determines slot increments and container extent; children retain their own calculated sizes. Axis selects columns or rows. Count 0 falls back to 2. Zero CellSize initializes from the first participating child. |
| Flow | Wrap on the configured main-axis limit. Align each line and items within each line. Oversized items can overflow. Content main axis, or a zero inner limit, disables wrapping. |
| Overlay | Additional convenience mode: align children in a common content box. This is distinct from None. |

Keep preserves `sizeDelta`, including on stretched RectTransforms. Fixed uses FixedSize. Content containers measure their registered children; Content leaves read intrinsic preferred sizes from other components on themselves, without container padding. The implementation applies a leaf's configured width before reading its preferred height. It does not solve arbitrary bidirectional size dependencies.

Linear and Grid alignment retain the earlier edge-padding rule: start uses start padding, end uses end padding, center uses the center of the outer rectangle. Flow uses inner-space alignment and clamps surplus space to zero. Spacing may be negative. There is no flex-grow, percentage sizing, virtualization or automatic interoperability with native layout groups on the same controlled subtree. `LayoutElement.ignoreLayout` does not exclude a registered Lyruis node; disable its Lyruis component to exclude it.

## API and execution

Namespace: `LunarConcerto.Lyruis`. Public configuration: `Mode`, `Axis`, `WidthMode`, `HeightMode`, `FixedSize`, `Spacing`, `CellSize`, `GridCount`, `padding`, `childAlignment`. SizeMode values: Keep, Fixed, Content. FlowAxis values: Horizontal, Vertical.

- `Refresh()` / `RefreshRootLayout()`: defer and coalesce requests at the active root. Canvas.willRenderCanvases flushes runtime requests; an editor delay call also flushes edit-mode requests.
- `RebuildNow()` / `RefreshLayout()`: synchronously rebuild the current subtree.
- `RebuildRootNow()`: synchronously rebuild the connected root.
- `OnLayoutRefreshed`: completion notification, children before parents after the rebuild. Call Refresh for follow-up work; do not synchronously rebuild from the event.
- `RectTransform`: cached transform accessor.

Child membership is cached and invalidated by hierarchy/enable/disable changes. The tree refreshes children first, stores their calculated dimensions, measures the parent, and places children. Requests during internal transform writes are suppressed to prevent feedback. Root dimension changes request a rebuild. External intrinsic changes require explicit Refresh; there is no per-frame intrinsic polling. Parent containers drive child anchors, size and position. Disabling releases driven flags, without restoring historical transforms. Call Refresh after mutating fields inside the returned RectOffset.

## Samples, benchmark and development

Import Gallery and Benchmark from Package Manager Samples. The development project already has them at `Assets/Samples/Lyruis/0.1.0`. Gallery contains linear, grid, flow, overlay and nested examples. Benchmark.unity runs flat and nested suites on Play; Console logs JSON output paths under Application.persistentDataPath.

Development menus: **Tools > Lyruis > Generate Sample Scenes**, **Run Benchmark And Save Report**, **Run Nested Benchmark And Save Report**. See [flat results](BenchmarkResults/README.md), [nested results](BenchmarkResults/nested.md), and [validation](BenchmarkResults/validation.md). Lyruis calls its own RebuildNow; native groups call LayoutRebuilder.ForceRebuildLayoutImmediate. Trees are configured for matching geometry. Results measure explicit rebuilds, not application FPS or automatic Canvas scheduling. The earlier LayoutGroup-adapter measurements are retained as historical JSON and do not describe this implementation.

Run `LunarConcerto.Lyruis.Tests` in Test Runner EditMode; the project manifest enables package tests via testables. Tests cover postorder content sizing, registered membership, legacy grid sizing, manual None bounds, line alignment, Keep/stretch, callbacks and hierarchy changes. Player builds and separate-project sample import remain unverified.

## Architecture and provenance

Runtime is split into `LyruisLayout` (single component/API and hierarchy events), `LayoutScheduler` (root request batching), `LayoutTree` (postorder size resolution and RectTransform writes), and `LayoutEngine` (geometry). Inspector is a separate Editor assembly. [Architecture and migration](Packages/com.lunarconcerto.lyruis/Documentation~/architecture.md).

The archived implementation was read to preserve its computational model and layout semantics, then implemented with new structure and code. Company-specific helpers, logging, assets and the old namespace are not included. This is not a formally isolated clean-room process or a claim of rights clearance. The repository's MIT license is retained.

There is no automatic migration of old components or serialized fields. Rebuild representative prefabs and validate their appearance. In particular, this revision corrects the earlier Lyruis adapter's behavior: register children explicitly, Grid no longer stretches them, None is distinct from Overlay, and Keep means sizeDelta. Contributions should include Unity version, a minimal reproduction and expected geometry. [Changelog](Packages/com.lunarconcerto.lyruis/CHANGELOG.md).

PlayMode tests: `LunarConcerto.Lyruis.PlayMode.Tests` (deferred sizing and root request coalescing / 延迟尺寸计算与根请求合并).

## Interactive Profiler Lab

Import **Profiler Lab**, open `ProfilerLab.unity`, and enter Play. The scene contains 24 cards, 1,273 layout nodes, depth 6, 168 mutable text leaves, and Horizontal/Vertical/Grid/Flow/None containers. Choose Idle, Single Leaf, Batch Leaves, Force Root Every Frame, or Resize Root from the on-screen buttons. Rendering and detailed markers can be toggled independently. See the [profiling guide](Packages/com.lunarconcerto.lyruis/Samples~/ProfilerLab/README.md). Development menu: **Tools > Lyruis > Generate Profiler Lab Scene**.

The Profiler Lab now includes **AUTO REPORT / Stop** and **Tools > Lyruis > Run Automated Measurement**: it runs five workloads in timing-only and phase-diagnostic passes, exports raw JSON plus Markdown findings, and requires no Profiler window recording. Reports are saved in `BenchmarkResults/AutoMeasure` in the Editor. See the [automatic measurement guide](Packages/com.lunarconcerto.lyruis/Samples~/ProfilerLab/README.md#one-click-automated-measurement--一键自动测量).
