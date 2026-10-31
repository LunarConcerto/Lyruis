# Lyruis Layout Inspector / 检查器

Use the **EN / 中文** toolbar to choose the Inspector language. This preference and linked padding are stored locally in EditorPrefs, not in scenes or prefabs.

使用顶部 **EN / 中文** 切换语言。语言及内边距关联开关保存在本机 EditorPrefs，不写入场景或 Prefab。

## Layout and sizing / 布局与尺寸

- Mode changes arrangement without resetting size rules. Grid exposes fill direction, columns/rows and cell size; Flow exposes its main axis. Mixed selections retain their individual values until edited.
- Width and Height independently support Keep, Fixed and Content. Fixed enables the numeric field; other modes display the current `sizeDelta` read-only. A dash indicates mixed values.
- **Fit content** sets both axes to Content. For Flow, it fixes the main axis to each object's current size to preserve wrapping.
- **Freeze size** copies each selected object's current `sizeDelta` into Fixed sizing on both axes.

- 模式切换不重置尺寸规则。Grid 显示铺排方向、列数／行数和格子尺寸，Flow 显示主轴。多选时保留各对象已有配置，直到明确修改。
- 宽高独立选择保持、固定或内容。固定时可输入数值，其余模式只读显示当前 `sizeDelta`；混合值显示短横线。
- **适应内容**将两轴设为内容；Flow 会将主轴固定为各对象当前尺寸，保留换行约束。
- **固定当前尺寸**将各对象自己的当前 `sizeDelta` 写入固定宽高。

## Spacing and workflow / 间距与操作

- The nine-button matrix controls content alignment. Flow also aligns each line and its items.
- **Link** alone changes no padding. The next side edit copies that value to all four sides, including across a multi-selection.
- A Content main axis on Flow shows an explanation and a shortcut to fix that axis to the current size.
- **Register missing children** adds LyruisLayout with Keep sizing to direct RectTransform children missing the component. It does not recurse or enable existing disabled components. This action supports Undo and Prefab added-component overrides.
- **Rebuild root** synchronously rebuilds each distinct connected root. **Select root** navigates to the root. Layout context shows the root, layout size, rendered rect and child membership counts.
- Fields support Unity Undo, mixed values and native Prefab override menus. Conflicting enabled native layout controllers produce a warning.

- 九宫格控制内容对齐；Flow 同时对齐每行及行内子项。
- 单独切换**关联**不改变内边距；随后修改任意一边时，四边同步为该值，也适用于多选。
- Flow 主轴为内容尺寸时，显示说明及按当前尺寸固定主轴的快捷操作。
- **注册缺少组件的子节点**只为缺少组件的直接 RectTransform 子节点添加 Keep 模式组件，不递归，也不启用已有的禁用组件。支持 Undo 和 Prefab 新增组件覆盖。
- **刷新根布局**立即重建每个不同的相连根节点，**选择根节点**跳转到根节点。布局上下文显示根节点、布局尺寸、实际矩形及子节点参与情况。
- 字段支持 Unity Undo、混合值及原生 Prefab 覆盖菜单；存在可能竞争的已启用原生布局控制器时显示警告。

## Verification / 验证

Windows / Unity 2022.3.62f3, 2026-11-01: actual mouse/keyboard checks covered 320px and 400px Inspector windows in English and Chinese, mode-dependent Grid controls, linked padding, alignment, single/multi-object size shortcuts, Undo/Redo, Flow guidance, Prefab numeric/alignment overrides, native Revert and undoable child registration. Console contained no errors after these checks. Temporary scenes and prefab fixtures were removed; the user's previously dirty scene was preserved without saving. Runtime algorithms were unchanged; this revision does not claim new benchmark results.

在上述环境下通过真实鼠标键盘验证了中英文窄窗口、Grid 动态字段、关联内边距、对齐、单选／多选尺寸快捷操作、Undo/Redo、Flow 提示、Prefab 数值及对齐覆盖、原生 Revert 和可撤销的子节点注册。检查结束时 Console 无错误；临时场景及 Prefab 已清理，原有未保存场景保持未保存。本次未修改运行时算法，也未新增性能结论。
