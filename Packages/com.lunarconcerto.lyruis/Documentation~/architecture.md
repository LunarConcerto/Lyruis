# Architecture / 架构与语义保留

This revision replaces the previous uGUI LayoutGroup adapter with an independent component tree. The archived implementation source was reviewed; its computational model and intended layout rules are retained with a new implementation. The previous statement that only the usage document was reviewed no longer applies.

本次直接阅读了归档实现，按原有计算模型和语义重新组织代码；不再声称只阅读了使用文档，也不使用 LayoutGroup 作为核心执行器。

## Responsibilities

- LyruisLayout: serialized settings, hierarchy membership, invalidation events and public API.
- LayoutScheduler: deduplicate deferred requests by their current connected root; flush through Canvas rendering or editor delay calls. It does not delegate to LayoutRebuilder.
- LayoutTree: child-first recursion, resolved size caching for this rebuild, leaf intrinsic size lookup, direct RectTransform application, completion callbacks.
- LayoutEngine: sum/max dimensions, manual-bounds mode (handled in LayoutTree), grid footprint with independent item size, Flow line wrapping and per-line alignment.

## Preserved semantics / 保留规则

Only enabled direct Lyruis children participate, in sibling order. Keep reads sizeDelta. Content containers use children, not their own Image/Text. Content leaves query LayoutUtility. None preserves manual placement and measures subtree bounds including the origin. Grid uses slots but does not stretch item sizes; zero count falls back to two, zero cell initializes from the first child. Flow uses the configured main-axis limit, aligns each line and aligns items within it. Content main axes and nonpositive inner limits do not wrap. Linear/grid centering preserves the earlier outer-center padding convention.

## Deliberate fixes / 明确修正

Fixed leaf sizes also apply when the leaf is a root (the earlier leaf refresh could leave the authored transform unchanged). Grid defaults resolve before measurement so the first rebuild is consistent. Child sizes are cached during the postorder pass instead of recursively recalculating sums from getters. There is no company-specific delay helper or logger; requests use a package-owned queue. Completion events run after geometry settles, child before parent; internal layout writes cannot recursively restart the rebuild.

这些是对旧实现边界问题的修正，并未照搬原代码中的重复递归计算、首帧默认值时序和项目依赖。

## Compatibility and limits

Overlay remains as a separate convenience mode; None is restored. No automatic old-component migration is provided. Lyruis is not a native LayoutGroup and does not implement the native layout-controller interfaces; avoid combining native groups/fitters on its controlled nodes. External Text/LayoutElement changes require Refresh. Arbitrary two-way constraints, percentage sizes and incremental dirty-subtree solving are not implemented. This revision restores the original postorder model rather than introducing a new constraint solver.

The source was reviewed to preserve algorithms; new code and structure do not themselves establish rights clearance. Archived code/assets and company dependencies are not distributed.
