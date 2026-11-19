# Custom visuals with Painter2D

Use Painter2D for what USS backgrounds can't draw: arcs and progress rings, pie and line charts, polygons, stars, badges, and wave patterns. For a rectangular gradient fill, use a USS `linear-gradient()` background first if the [Set background images](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-uss/properties/uib-styling-ui-backgrounds.md) page for `<VERSION>` lists it, because it needs no C#.

Replace `<VERSION>` in these URLs with the project's Editor version, for example `6000.7`. Read the first page before writing the element, and the second for rings and arcs:

- [Generate 2D visual content](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-ui-renderer/uie-generate-2d-visual-content.md): the `generateVisualContent` pattern and the Painter2D drawing model
- [Create a radial progress indicator](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-ui-renderer/uie-radial-progress-use-vector-api.md): a complete ring example
- [Painter2D](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/uielements/painter2d.md) and [FillGradient](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/uielements/fillgradient.md): member names and signatures

## Names

Painter2D uses the HTML Canvas 2D method names in PascalCase, but angles, arc direction, fill and stroke styles, line caps, and dashes take Unity types instead of Canvas values. Check each member in the Painter2D and FillGradient reference pages before writing it, because a Canvas name that looks right often doesn't compile.

## Rules

The pages above show the element pattern and repainting. These are the mistakes they don't warn about:

- **Element type:** draw on a plain `VisualElement` subclass, not a built-in control, because built-in controls already generate their own content and yours layers with theirs.
- **Padding:** drawing starts at the element's top-left corner, not inside its padding. Offset every point by `contentRect.x` and `contentRect.y` to respect the USS padding.
- **Zero size:** return early when the width or height is below 1, because an element with no size yet gives arcs and gradients degenerate geometry.
- **Paths:** call `BeginPath()` before every path, or `Fill()` and `Stroke()` redraw the earlier sub-paths too.
- **Arc ends:** use `LineCap.Butt` for progress rings, because `Round` extends each end past its angle by half the line width.
- **Read-only callback:** change styles, add children, and call `MarkDirtyRepaint()` outside `generateVisualContent`, because doing it inside dirties the element again.
- **Expose settings:** mark the values designers tune (colors, thickness, progress) with `[UxmlAttribute]`.
