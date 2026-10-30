# Lyruis

[简体中文](README.zh-CN.md)

A single-component uGUI layout package by LunarConcerto. Configure containers and their own sizes in one Inspector, with geometry isolated from Unity lifecycle code.

Version **0.1.0**, tested with **Unity 2022.3.62f3 / Windows Editor**. Other Unity versions and player performance have not yet been verified. MIT licensed.

## Install

In Package Manager, choose **Add package from git URL**:

```text
https://github.com/LunarConcerto/Lyruis.git?path=/Packages/com.lunarconcerto.lyruis
```

For reproducible dependencies, append `#<commit SHA>`. Requires Unity 2022.3+ and `com.unity.ugui` (declared dependency). The repository itself is a runnable Unity development project; consumers only install the package subdirectory. MCP and the other development project dependencies are not runtime package dependencies.

## Quick start

1. Create a UI panel and add **Layout > Lyruis Layout**.
2. Choose Vertical, set Width Mode to Fixed, Fixed Size X to 400, and Height Mode to Content.
3. Add Text, Image, or children with LayoutElement preferred sizes. Children do not need LyruisLayout unless they need their own container/sizing behavior.
4. Adjust Padding, Spacing and Child Alignment. Changes rebuild through uGUI's layout system.

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

## Layout and sizing

| Setting | Behavior |
| --- | --- |
| Horizontal / Vertical | Sibling order along one axis; cross-axis alignment follows Child Alignment |
| Grid | Uniform Cell Size; Grid Count specifies columns for Horizontal axis or rows for Vertical axis; children fill cells |
| Flow | Wraps at available main-axis space; Axis selects rows or columns; oversized items occupy their own line and may overflow |
| Overlay | Children share a content box and use Child Alignment; does not preserve manually positioned child bounds |
| Keep | Reports current RectTransform rect size as preferred size; root size is not self-driven |
| Fixed | Uses nonnegative Fixed Size for that axis |
| Content | Uses measured content plus padding; a leaf reads other ILayoutElement providers on itself |

Spacing is clamped to zero or above during measurement. Grid Count is at least 1. Content on the Flow main axis means unbounded space, so no wrapping. Use Fixed or Keep on that axis. Child Alignment positions the overall Flow block; individual lines start at the beginning of the main axis. There is no CSS flex-grow, percentage size, margin, virtualization, RTL or per-line justification.

Children are active direct RectTransforms, in sibling order; `ILayoutIgnorer.ignoreLayout` is respected. Non-grid children use LayoutUtility preferred sizes. A plain RectTransform without a preferred-size provider measures as zero; add LayoutElement or a LyruisLayout with Fixed/Keep sizing. A parent layout drives participating children's anchors, size and position. Avoid another LayoutGroup or ContentSizeFitter controlling the same axes on the same object. Lyruis implements ILayoutElement, ILayoutController and ILayoutSelfController through LayoutGroup.

## API

Namespace: `LunarConcerto.Lyruis`.

| Member | Type / purpose |
| --- | --- |
| Mode | LayoutMode: Overlay, Horizontal, Vertical, Grid, Flow |
| Axis | FlowAxis: Horizontal or Vertical (Grid and Flow) |
| WidthMode / HeightMode | SizeMode: Keep, Fixed, Content |
| FixedSize | Vector2; values used only on Fixed axes |
| Spacing | Vector2; horizontal and vertical gaps |
| CellSize / GridCount | Vector2 / int; Grid only |
| padding / childAlignment | Inherited RectOffset / TextAnchor |
| Refresh() | Marks layout dirty; use after batching external content changes |
| RebuildNow() | Immediately rebuilds this subtree; call on its root when ancestors depend on it |

Public setting setters mark dirty. Inspector edits, hierarchy changes and dimension changes follow LayoutGroup's invalidation. Do not call RebuildNow recursively from layout callbacks. For wrapped text, constrain width before content height is measured. Disabling the component releases driven properties; it does not restore earlier authored transforms.

## Samples

Import **Layout Gallery** and **Benchmark** from this package's Samples section in Package Manager. Open Gallery.unity for horizontal, vertical, grid, flow, overlay and nested containers. Panels intentionally show overflow where items exceed available space; layout does not clip. Benchmark.unity runs on entering Play mode and writes `lyruis-benchmark.json` under `Application.persistentDataPath` (path logged to Console).

In this repository, imported samples already live under `Assets/Samples/Lyruis/0.1.0`. **Tools > Lyruis > Generate Sample Scenes** regenerates them. Sample scripts are optional and excluded from runtime package compilation until imported.

## Benchmark

See [method and recorded results](BenchmarkResults/README.md) and [raw JSON](BenchmarkResults/editor-2022.3.json). HorizontalLayoutGroup, VerticalLayoutGroup and GridLayoutGroup are compared against equivalent Lyruis settings at 10/100/1000 children. It measures synchronous forced layout rebuilds, not rendering, idle frames, allocation rates or a full application's FPS. Flow has no equivalent native baseline here. Results are environment-specific; no universal speedup is claimed.

The sample also runs a [complex nested benchmark](BenchmarkResults/nested.md): alternating horizontal/vertical content-sized containers at 3/5/7 levels, plus a wider tree. Each shape measures repeated rebuilds and leaf resizing with whole-tree rebuilds. It checks every descendant and the root before and after timed batches, rejecting geometry mismatches over 0.01 pixels. Use **Tools > Lyruis > Run Nested Benchmark And Save Report** in the development project; the imported sample writes `lyruis-nested-benchmark.json` beside the flat report. This measures explicit rebuilds, not automatic Canvas dirty propagation.

## Structure and development

```text
Packages/com.lunarconcerto.lyruis/
  Runtime/       Component, settings and geometry engine
  Editor/        Conditional Inspector
  Tests/Editor/  Unity EditMode behavior tests
  Samples~/      Importable scenes and benchmark runner
  Documentation~/ Architecture and migration notes
```

Open the repository in Unity 2022.3.62f3. Its manifest lists the package in `testables`; run `LunarConcerto.Lyruis.Tests` in Test Runner's EditMode tab. Tests cover fit, nesting, ignored/inactive children, wrapping, grid and empty padding. Use **Tools > Lyruis > Run Benchmark And Save Report** to reproduce the Editor report. Run the imported benchmark in a target player for platform-specific measurements. Unity generates project/asset metadata; commit package `.meta` files along with assets.

## Design and migration

This implementation was authored anew from a functional description; archived implementation source, comments and assets were not imported. It is not a binary-compatible continuation: component type, namespace, serialized fields and some layout semantics differ. There is no automatic old-component migration. Rebuild representative prefabs using Lyruis and check their sizing and alignment. See [architecture and migration](Packages/com.lunarconcerto.lyruis/Documentation~/architecture.md).

The repository's existing MIT license is retained. A new implementation does not itself settle ownership of earlier employment-related work; provenance notes describe the implementation process, not legal clearance.

Contributions: include a minimal UI reproduction, Unity version and expected geometry. Keep geometry changes in the engine and Unity lifecycle changes in the component; add behavior tests for regressions. See [changelog](Packages/com.lunarconcerto.lyruis/CHANGELOG.md).
