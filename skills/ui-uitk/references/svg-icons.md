# SVG Icon Generation (Unity 6.3+)

## Table of Contents

- [When to Use SVG](#when-to-use-svg)
- [Priority Order](#priority-order)
- [SVG Format](#svg-format)
- [Common Icon Examples](#common-icon-examples)
- [Usage in Unity](#usage-in-unity)
- [Tips](#tips)

## When to Use SVG

**Prefer SVG for:**
- Simple icons (arrows, chevrons, checkmarks)
- Geometric shapes
- UI symbols (close, menu, settings)
- Any icon that can be drawn with paths

**Use image generators for:**
- Complex illustrations
- Photorealistic content
- Detailed artwork
- Gradients with many stops

## Priority Order

1. **Reuse existing project icons** — always search first
2. **Generate SVG** — fast, resolution-independent, low cost
3. **Image generators** — last resort, slower and higher cost

## SVG Format

Unity imports SVG as VectorImage assets. Use standard SVG markup:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
  <path d="..." stroke="currentColor" stroke-width="2" fill="none"/>
</svg>
```

## Common Icon Examples

### Arrow Right
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
  <path d="M8 4l8 8-8 8" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round"/>
</svg>
```

### Arrow Left
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
  <path d="M16 4l-8 8 8 8" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round"/>
</svg>
```

### Chevron Down
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
  <path d="M4 8l8 8 8-8" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round"/>
</svg>
```

### Checkmark
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
  <path d="M4 12l6 6L20 6" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round"/>
</svg>
```

### Close (X)
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
  <path d="M6 6l12 12M18 6L6 18" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round"/>
</svg>
```

### Plus
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
  <path d="M12 4v16M4 12h16" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round"/>
</svg>
```

### Minus
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
  <path d="M4 12h16" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round"/>
</svg>
```

### Menu (Hamburger)
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
  <path d="M4 6h16M4 12h16M4 18h16" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round"/>
</svg>
```

### Settings (Gear)
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
  <circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="2" fill="none"/>
  <path d="M12 1v4M12 19v4M4.22 4.22l2.83 2.83M16.95 16.95l2.83 2.83M1 12h4M19 12h4M4.22 19.78l2.83-2.83M16.95 7.05l2.83-2.83" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round"/>
</svg>
```

### Search (Magnifying Glass)
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
  <circle cx="10" cy="10" r="6" stroke="currentColor" stroke-width="2" fill="none"/>
  <path d="M14.5 14.5L20 20" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round"/>
</svg>
```

## Usage in Unity

1. Save SVG file to project (e.g., `Assets/UI/Icons/arrow-right.svg`)
2. Unity auto-imports as VectorImage
3. Set "Generated Asset Type" to "UI Toolkit Vector Image" in Inspector
4. Reference in USS:

```uss
.icon-arrow {
  background-image: url("project://database/Assets/UI/Icons/arrow-right.svg");
  width: 24px;
  height: 24px;
}
```

## Tips

- Use `viewBox="0 0 24 24"` for consistent sizing
- Use `stroke="currentColor"` for tintable icons
- Use `fill="none"` for outline-style icons
- Keep paths simple — complex SVGs may not render correctly
