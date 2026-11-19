# SVG icons

## Contents

- When to use SVG
- Import and use
- Drawing icons

## When to use SVG

Use SVG for simple icons that can be drawn with paths: arrows, chevrons, checkmarks, close, menu, plus and minus, and geometric symbols. Use an image generator for illustrations, photorealistic content, and detailed artwork, because the importer drops the SVG features it doesn't support and those icons lose parts.

## Import and use

Replace `<VERSION>` in this URL with the project's Editor version, for example `6000.7`, and read the page before importing: [Work with vector graphics](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-ui-renderer/work-with-vector-graphics.md). It lists the importer settings and the SVG features the importer supports.

1. Save each icon as its own file in the project, for example `Assets/UI/Icons/chevron-right.svg`.
2. An SVG imports as a UI Toolkit Vector Image by default, but cropped to the shape it draws, so icons drawn on one 24×24 `viewBox` come out at different sizes and a chevron stretches to fill its element. Set the importer's viewport option to Preserve Viewport through `SerializedObject` (`m_ViewportOptions` = `1`), then call `SaveAndReimport()`, because setting the importer's C# property from a script doesn't save.
3. Reference the icon from USS:

   ```uss
   .icon-chevron-right {
     background-image: url("project://database/Assets/UI/Icons/chevron-right.svg");
     width: 24px;
     height: 24px;
   }
   ```

4. Color the icon from USS with `-unity-background-image-tint-color`. Tint multiplies the icon's colors, so author icons in white (`#FFFFFF`). A white icon takes the tint color exactly.

## Drawing icons

Draw every icon of a screen on a 24×24 `viewBox` with white strokes, `stroke-width="2"`, `fill="none"`, and round caps and joins, so the icons share one size grid and one style. For example, a chevron:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
  <path d="M8 4l8 8-8 8" stroke="#FFFFFF" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round"/>
</svg>
```
