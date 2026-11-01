# Logo / 标志

Lyruis combines **Lyra + UI**. The mark pairs an abstract lyre with one four-point star: a reference to Lyra and a small accent of enhancement. Broad rounded shapes and open gaps keep it recognizable at script-icon sizes. The violet mark has no text or layout-grid motif, leaving room for capabilities beyond the current layout system.

名称来自 **Lyra + UI**。抽象竖琴呼应 Lyra，单个星芒表达增强能力；圆润轮廓与留白适合小尺寸图标。标志没有文字和布局网格，面向综合 UI 能力增强框架，而非绑定某一种布局功能。

## Asset and integration / 资源与接入

- [Canonical transparent PNG](../Editor/Icons/Lyruis.png): 1,254 × 1,254 RGBA. Repository and package READMEs reference this same file.
- Unity imports it at a maximum of 128px, with alpha transparency, no mipmaps, no compression, bilinear filtering and clamp wrapping. The high-resolution source remains intact.
- Core Runtime and Editor `.cs.meta` files store persistent `MonoImporter` icon references. Consumers receive the icons with the package; no initialization hook or per-frame code is needed. New component scripts can reuse this icon through their MonoImporter settings.
- The image lives in the package's Editor directory and is used only for editor presentation; it is not a runtime UI texture dependency.

仓库、包内 README 与 Unity 脚本图标共用一份 PNG。Unity 仅按小图标尺寸导入，保留原始透明图片；脚本 `.meta` 持久化引用，无需额外初始化脚本。

Verified in Unity 2022.3.62f3: all seven core script importers and their visible icons resolve to this asset. A temporary preview was visually checked at 16, 32, 64 and 128px on dark and light backgrounds, then removed. Console contained no errors.

## Generation record / 生成记录

Created with the built-in **imagegen** tool on 2026-11-01. No raster post-processing was applied; the selected generated output was copied into the package. Other generated candidates are not distributed.

Initial prompt:

> A simple text-free emblem for Lyruis (Lyra + UI), a broad Unity UI enhancement framework. An abstract modern lyre with a compact curved frame, three stout inner strings and a single upper-right four-point star. Bold shapes, open gaps, a restrained violet/periwinkle palette, centered square composition and genuine transparent alpha. Legible at 16–32px. No UI grid, wordmark, mockup or background.

Final refinement prompt:

> Polish this exact logo into a clean flat production mark. Preserve its abstract lyre silhouette, three rounded vertical strings, single upper-right four-point star, composition and transparent background. Simplify all surfaces into one solid mid-light periwinkle purple, no gradients or shaded facets. Remove every stray speck/pixel and all irregular blue fringes from the transparent gaps and outer edges. Crisp smooth antialiased edges and generous clean transparent gaps. Keep the bold recognizable mark legible at 16-32 pixels. No text, no background, no added elements. Return one square image with real transparent alpha.
