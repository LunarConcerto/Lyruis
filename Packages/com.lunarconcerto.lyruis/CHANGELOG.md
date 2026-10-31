# Changelog

## Unreleased

- Redesign LyruisLayout Inspector with English/Chinese controls, mode-specific fields, alignment matrix, linked padding, sizing shortcuts and layout context.
- Support mixed multi-object edits, native Prefab override menus and undoable missing-child registration; add Flow and native-controller guidance.
- Verify Inspector interactions through Windows computer-use in Unity 2022.3.62f3; runtime calculation remains unchanged.

## 0.1.0 — 2026-10-31

- Add bounded peak sweeps, raw tail samples, geometry snapshots and comparison tooling.
- Cache connected roots with topology invalidation; combine child rectangle updates and skip unchanged setters without changing layout rules.

- Add window-independent phase timing, exclusive/inclusive accounting, automated two-pass measurement, cancellation/state restoration and raw JSON/Markdown reports.

- Add a serialized interactive Profiler Lab scene (1,273 layout nodes), controlled mutation workloads, and optional detailed execution markers.

- Replace the initial LayoutGroup adapter with an independent postorder tree, preserving the archived computational model in new code.
- Restore registered children, sizeDelta Keep, manual None bounds, independent Grid child sizes and per-line Flow alignment.
- Split component, scheduler, tree execution and geometry; regenerate samples, update tests and rerun both benchmark suites. Historical adapter results are retained separately.

- Add complex nested benchmarks: depth and branching sweeps, static and leaf-resize rebuilds, recursive geometry validation, and recorded results.
- Initial independent implementation with Overlay, Horizontal, Vertical, Grid and Flow.
- Keep, Fixed and Content sizing in a single uGUI component.
- Dedicated geometry engine, Editor Inspector, EditMode tests, importable gallery and benchmark.
- English and Simplified Chinese documentation.
