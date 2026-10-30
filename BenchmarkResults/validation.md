# Validation — 2026-10-31

Unity 2022.3.62f3, Windows Editor.

- Package runtime and editor scripts compile without errors.
- `LunarConcerto.Lyruis.Tests`: 11 passed, 0 failed, 0 skipped (EditMode).
- Coverage: empty padding, vertical fit and position, horizontal and vertical Flow wrapping, unbounded Flow, grid partial rows, ignored/inactive children, nested content, intrinsic leaf size, bottom-right padding alignment, child removal.
- Gallery scene opened and inspected through a Game View screenshot in Play mode: six panels rendered, including nested content and wrapping.
- All nine benchmark configurations produced zero geometry difference from their native uGUI counterparts. Raw timing report is retained beside this file.

Not verified: other Unity versions, player builds, GC allocation rate, deeply nested performance, target-device rendering and automated sample import in a separate consumer project.
