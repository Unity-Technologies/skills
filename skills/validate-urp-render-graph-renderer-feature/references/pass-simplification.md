# Pass simplification checks

Checks 5 and 6 of the validation checklist: when a custom raster pass should be a built-in copy or blit helper.

## Contents

- [5. Manual Copy Pass Simplification](#5-manual-copy-pass-simplification)
- [6. Manual Blit Pass Simplification](#6-manual-blit-pass-simplification)

## 5. Manual Copy Pass Simplification
Check that:
- simple texture copy operations are not implemented as full custom raster passes when a built-in Render Graph helper is sufficient
- passes that only read one texture and write it unchanged to another target are simplified where appropriate
- the implementation prefers the most appropriate built-in helper for the target API/platform context
- `AddCopyPass(...)` is not recommended by default if a more compatible `AddBlitPass(...)` overload should be preferred in the current environment

Flag as an issue when:
- a raster pass exists only to copy one texture into another
- the pass uses no material and no custom processing
- the render function only performs a simple blit/copy equivalent
- the implementation uses a full custom raster pass where a built-in copy/blit helper would express the same behavior more directly

Prefer:
- the appropriate `AddBlitPass(...)` overload for straightforward copy-like operations when that is the recommended and more compatible path
- `AddCopyPass(...)` only when it is explicitly appropriate and supported for the target API/platform context
- a custom raster pass only when the copy requires additional logic or non-trivial behavior

## 6. Manual Blit Pass Simplification
Check that:
- straightforward fullscreen material blits are not implemented as custom raster passes when `renderGraph.AddBlitPass(...)` would express the same behavior more directly
- custom raster passes are only used for blits when additional logic or non-trivial behavior is actually required
- simple source-to-destination material blits use the most direct render graph helper available

Flag as an issue when:
- a raster pass reads one source texture and writes one destination texture
- the pass uses a material but no additional custom pass logic
- the render function only performs a simple fullscreen blit
- `renderGraph.AddBlitPass(...)` would provide an equivalent result more clearly

Prefer:
- `renderGraph.AddBlitPass(...)` for straightforward fullscreen material blits
- a custom raster pass only when extra logic, multiple operations, conditional behavior, or special setup is actually required
