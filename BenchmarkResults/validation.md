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
