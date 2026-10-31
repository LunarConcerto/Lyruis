# Validation — 2026-10-31

Unity 2022.3.62f3 / Windows Editor. Runtime now uses an independent postorder tree, not LayoutGroup.

- EditMode: 16 passed, 0 failed. Behavior tests cover registered membership, child-first sizing, manual None bounds, independent grid sizes/defaults, Flow line alignment and wrapping, Keep/sizeDelta, callbacks and hierarchy changes.
- PlayMode: 2 passed, 0 failed (deferred provider sizing and root request coalescing).
- Flat benchmark: all 9 geometry comparisons match (0px error).
- Nested benchmark: all 8 recursive geometry comparisons match (0px error), including both leaf resize states.
- Current raw reports were generated in Edit mode. Historical adapter reports are retained under historical-ugui-adapter-* filenames.
- Gallery regenerated for explicit child registration; package sample directories/scenes verified and synchronized. Play-mode screenshot confirms all six panels render after automatic startup layout.
- Scene initialization requeues roots after all intrinsic providers activate, avoiding premature editor requests leaving zero-sized items.

Not verified: other Unity versions, player builds, GC rates, separate consumer-project sample import, automatic Canvas scheduling performance, and exhaustive equivalence of every archived edge case. The original source was inspected to preserve the computational model; archived source was not executed as a second test assembly.

## Profiler Lab — 2026-10-31

- Instrumented runtime compiles; existing 16 EditMode and 2 PlayMode tests pass.
- Serialized ProfilerLab scene: 24 cards, 1,273 layout nodes, depth 6, 168 mutable text leaves.
- Play-mode checks: automatic startup layout, button wiring, single-leaf/batch/forced rebuilds, and 1840/1380px root reflow. All nodes retain finite nonnegative dimensions.
- Single and batch mutation ticks each produce one root completion when flushed.
- Visibility uses CanvasGroup alpha; a Play-mode comparison of all 1,273 node rectangles found zero changed rectangles while hidden, preserving active Canvas/Text measurement context; it is not a complete UI CPU isolation switch. Detail toggle changes LyruisDiagnostics.DetailedProfiling.
- Unity Profiler CPU frame data contains LyruisLab.WorkloadTick and Lyruis markers for rebuild, root lookup, child collection, tracker clearing, intrinsic measurement, container measurement, arrangement, rectangle writes, manual bounds and completion.
- Game View inspected; 18 existing tests passed. No performance speedup claim is made for this instrumented revision, and no target-player build was run.

## Automated phase measurement — 2026-10-31

- 18 EditMode tests passed, including phase-accounting/reset/concurrent-session checks; 2 PlayMode scheduling tests passed.
- Two complete automated captures ran with Profiler recording disabled, each with 10 cases and 40 raw samples per case. Idle had 0 root completions/op; every active case had 1/op.
- Phase calls match tree structure: 1,273 tracker scopes, 720 leaf scopes and 529 container-measurement scopes per root rebuild.
- Cancellation was verified after a real text mutation: all workload text strings and root size restored, automation and counters inactive afterward.
- Reports preserve raw evidence and distinguish instrumented phase data from timing-only totals; current-thread allocations are not claimed as whole-frame GC statistics.

## Peak sweep and conservative runtime optimization — 2026-10-31

- 21 EditMode tests passed, including arbitrary-pivot/external-RectTransform-edit equivalence and cached root invalidation on reparenting, parent addition and enable/disable. 2 PlayMode scheduling tests passed (23 total tests).
- Before and after: 24 matching configurations, 40 timing samples each, 6 warmups and 4 separate diagnostic samples. 637/1273/2545/4241 nodes, six workloads.
- Binary geometry comparison checks all nodes' dimensions, positions, anchors and pivots across all configurations; maximum absolute coordinate difference .000031, below .01. Root completion counts match expected 1 or 4.
- Reports preserve median, P95, observed Max and regressions rather than asserting a universal speedup. Existing algorithm/Flow/Grid/None semantics remain unchanged.
- Source scenes were not regenerated or overwritten by the sweep. Editor-only captures; no target-player validation or full-frame FPS/capacity claim.

## Full retest — 2026-11-01 (Asia/Hong_Kong)

Runtime 54b5e16; no code changes. 21 EditMode and 2 PlayMode tests passed. Nine flat and eight nested geometry comparisons have zero error; ten automatic two-pass cases completed; all 24 peak configurations have identical stored geometry to the previous optimized capture and expected root completion counts. Profiler recording was disabled. Raw evidence and hashes: [retest manifest](Retest-2026-11-01/manifest.json).
