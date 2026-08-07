# Custom Visuals with Painter2D

## Table of Contents

- [The Pattern](#the-pattern)
- [Canvas-to-Painter2D Mapping](#canvas-to-painter2d-mapping)
- [Gradient Fills](#gradient-fills)
- [GradientElement — Full Example](#gradientelement--full-example)
- [Other Use Cases](#other-use-cases)
- [Rules](#rules)

USS cannot draw gradients, arbitrary shapes, arcs, or procedural patterns. For these, use the **Painter2D** API via the `generateVisualContent` callback.

Painter2D is modeled on the **HTML Canvas 2D** context — `BeginPath`, `MoveTo`, `LineTo`, `Arc`, `BezierCurveTo`, `Fill`, `Stroke` all map directly. Key differences from Canvas: text is drawn via `ctx.DrawText()` on the `MeshGenerationContext` (not on Painter2D itself), no `drawImage()` (use `fillTexture` or child elements with USS `background-image`), angles use `Angle.Degrees()` / `Angle.Turns()` structs, arc direction is an enum (`ArcDirection.Clockwise` / `.CounterClockwise`), and coordinates are local to the element's content rect.

**Drawing text:** Use `ctx.DrawText(string text, Vector2 pos, float fontSize, Color color, FontAsset font)` on the `MeshGenerationContext` directly. Pass `null` for `font` to use the element's USS font. This is useful when text must be positioned precisely within custom-drawn visuals — for simpler cases, child `Label` elements are easier.

## The Pattern

Every custom-drawn element: extend `VisualElement` directly (never `Label`, `Button`, etc. — Painter2D won't render correctly on those), subscribe to `generateVisualContent`, draw with `ctx.painter2D`, call `MarkDirtyRepaint()` when properties change. For animations, call `MarkDirtyRepaint()` every frame from an update loop.

```csharp
[UxmlElement]
public partial class MyCustomVisual : VisualElement
{
    float m_Value = 0.5f;

    [UxmlAttribute]
    public float Value
    {
        get => m_Value;
        set { m_Value = value; MarkDirtyRepaint(); }
    }

    public MyCustomVisual()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        float w = contentRect.width;
        float h = contentRect.height;
        if (w < 1f || h < 1f) return;

        var painter = ctx.painter2D;
        // ... drawing commands
    }
}
```

Name classes to match their purpose — `GradientCard`, `RadialProgress`, `WaveformDisplay`, etc.

## Canvas-to-Painter2D Mapping

| HTML Canvas 2D | Unity Painter2D |
|----------------|-----------------|
| `beginPath()` | `BeginPath()` |
| `moveTo(x, y)` | `MoveTo(new Vector2(x, y))` |
| `lineTo(x, y)` | `LineTo(new Vector2(x, y))` |
| `arc(cx, cy, r, start, end)` | `Arc(Vector2 center, float radius, Angle start, Angle end, ArcDirection dir)` |
| `arcTo(x1, y1, x2, y2, r)` | `ArcTo(Vector2 p1, Vector2 p2, float radius)` |
| `bezierCurveTo(...)` | `BezierCurveTo(Vector2 ctrl1, Vector2 ctrl2, Vector2 end)` |
| `quadraticCurveTo(...)` | `QuadraticCurveTo(Vector2 ctrl, Vector2 end)` |
| `closePath()` | `ClosePath()` |
| `rect(x, y, w, h)` | **No equivalent** — trace manually with `MoveTo`/`LineTo`/`ClosePath` |
| `fill()` | `Fill(FillRule rule = NonZero)` — use `OddEven` for holes/cutouts |
| `stroke()` | `Stroke()` |
| `lineWidth` | `lineWidth` |
| `strokeStyle` | `strokeColor` / `strokeGradient` / `strokeFillGradient` |
| `fillStyle` | `fillColor` / `fillGradient` / `fillTexture` |
| `lineCap` | `lineCap` — `LineCap.Butt` (default), `.Round`, `.Square` |
| `lineJoin` | `lineJoin` — `LineJoin.Miter` (default), `.Bevel`, `.Round` |
| `setLineDash([...])` | `SetDashPattern(float[])` |
| `lineDashOffset` | `dashOffset` |

Both `Fill()` and `Stroke()` can be called on the same path.

Angle helpers: `Angle.Degrees(float)`, `Angle.Radians(float)`, `Angle.Turns(float)`.

## Gradient Fills

USS has no `linear-gradient()` or `radial-gradient()`. Use `FillGradient`:

```csharp
// Linear — two-color shorthand
FillGradient.MakeLinearGradient(Color startColor, Color endColor, Vector2 start, Vector2 end, AddressMode mode)
// Linear — multi-stop via Gradient object
FillGradient.MakeLinearGradient(Gradient gradient, Vector2 start, Vector2 end, AddressMode mode)

// Radial — two-color shorthand
FillGradient.MakeRadialGradient(Color startColor, Color endColor, Vector2 center, float radius, Vector2 focus, AddressMode mode)
// Radial — multi-stop via Gradient object
FillGradient.MakeRadialGradient(Gradient gradient, Vector2 center, float radius, Vector2 focus, AddressMode mode)
```

`AddressMode`: `Clamp` (extend edge color), `Repeat` (tile), `Mirror` (reflect).

**Linear gradient direction** — controlled by start/end points:

| Direction | Start | End |
|-----------|-------|-----|
| Top → Bottom | `(0, 0)` | `(0, height)` |
| Left → Right | `(0, 0)` | `(width, 0)` |
| Diagonal | `(0, 0)` | `(width, height)` |

**Radial gradient** — set `focus` off-center to shift the bright spot.

## GradientElement — Full Example

A custom element rendering a linear gradient with rounded corners and optional border stroke. All properties exposed as UXML attributes.

```csharp
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class GradientElement : VisualElement
{
    Color m_StartColor = new Color(0.13f, 0.59f, 0.95f);
    Color m_EndColor = new Color(0.61f, 0.15f, 0.69f);
    Color m_BorderColor = Color.white;
    float m_BorderWidth = 2f;
    float m_CornerRadius = 8f;
    float m_GradientAlpha = 1f;

    [UxmlAttribute]
    public Color StartColor
    {
        get => m_StartColor;
        set { m_StartColor = value; MarkDirtyRepaint(); }
    }

    [UxmlAttribute]
    public Color EndColor
    {
        get => m_EndColor;
        set { m_EndColor = value; MarkDirtyRepaint(); }
    }

    [UxmlAttribute]
    public Color BorderColor
    {
        get => m_BorderColor;
        set { m_BorderColor = value; MarkDirtyRepaint(); }
    }

    [UxmlAttribute]
    public float BorderWidth
    {
        get => m_BorderWidth;
        set { m_BorderWidth = value; MarkDirtyRepaint(); }
    }

    [UxmlAttribute]
    public float CornerRadius
    {
        get => m_CornerRadius;
        set { m_CornerRadius = value; MarkDirtyRepaint(); }
    }

    [UxmlAttribute]
    public float GradientAlpha
    {
        get => m_GradientAlpha;
        set { m_GradientAlpha = Mathf.Clamp01(value); MarkDirtyRepaint(); }
    }

    public GradientElement()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        float w = contentRect.width;
        float h = contentRect.height;
        if (w < 1f || h < 1f)
            return;

        DrawGradientBackground(
            ctx.painter2D, w, h,
            m_StartColor, m_EndColor, m_GradientAlpha,
            m_CornerRadius,
            m_BorderColor, m_BorderWidth);
    }

    static void DrawGradientBackground(
        Painter2D painter,
        float width, float height,
        Color startColor, Color endColor, float alpha,
        float cornerRadius,
        Color borderColor, float borderWidth)
    {
        float r = Mathf.Min(cornerRadius, Mathf.Min(width, height) * 0.5f);

        var start = startColor;
        var end = endColor;
        start.a *= alpha;
        end.a *= alpha;

        painter.fillGradient = FillGradient.MakeLinearGradient(
            BuildGradient(start, end),
            new Vector2(0f, 0f),
            new Vector2(0f, height),
            AddressMode.Clamp);

        painter.BeginPath();
        TraceRoundedRect(painter, 0f, 0f, width, height, r);
        painter.Fill();

        if (borderWidth <= 0f)
            return;

        float half = borderWidth * 0.5f;
        painter.strokeColor = borderColor;
        painter.lineWidth = borderWidth;
        painter.lineJoin = LineJoin.Round;

        painter.BeginPath();
        TraceRoundedRect(
            painter, half, half,
            width - borderWidth, height - borderWidth,
            Mathf.Max(0f, r - half));
        painter.Stroke();
    }

    static Gradient BuildGradient(Color start, Color end)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(end.a, 1f) });
        return gradient;
    }

    static void TraceRoundedRect(Painter2D p, float x, float y, float w, float h, float r)
    {
        p.MoveTo(new Vector2(x + r, y));
        p.LineTo(new Vector2(x + w - r, y));
        p.ArcTo(new Vector2(x + w, y), new Vector2(x + w, y + r), r);
        p.LineTo(new Vector2(x + w, y + h - r));
        p.ArcTo(new Vector2(x + w, y + h), new Vector2(x + w - r, y + h), r);
        p.LineTo(new Vector2(x + r, y + h));
        p.ArcTo(new Vector2(x, y + h), new Vector2(x, y + h - r), r);
        p.LineTo(new Vector2(x, y + r));
        p.ArcTo(new Vector2(x, y), new Vector2(x + r, y), r);
        p.ClosePath();
    }
}
```

### Usage in UXML

```uxml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
  <ui:Style src="Screen.uss" />
  <GradientElement class="gradient-card"
      start-color="#2196F3" end-color="#9C27B0"
      gradient-alpha="0.9" corner-radius="12"
      border-color="#FFFFFF" border-width="1">
    <ui:Label text="Card Title" class="card-title" />
    <ui:Label text="Description text goes here" class="card-desc" />
  </GradientElement>
</ui:UXML>
```

The element participates in flexbox, accepts children, and can be styled with USS for sizing, padding, and margin. The gradient draws behind child content.

## Other Use Cases

Painter2D handles any visual USS cannot express — progress rings (`Arc()` with dynamic `endAngle`), custom shapes (polygons, stars, badges), charts (bar fills, pie segments, sparklines), decorative elements (wave patterns, bezier flourishes), and animated visuals (drive properties from C#, call `MarkDirtyRepaint()` each frame).

## Rules

- **Extend `VisualElement` directly** — never `Label`, `Button`, etc.
- **Guard zero dimensions** — `if (contentRect.width < 1f || contentRect.height < 1f) return;`
- **`BeginPath()` before every path** — omitting it causes silent failures
- **No `Rect()` method** — Painter2D has no rectangle convenience method. Trace rectangles manually with `MoveTo`/`LineTo`/`ClosePath` (see `TraceRoundedRect` in the gradient example)
- **Set style properties before `BeginPath()`** — `lineWidth`, `strokeColor`, `fillColor`, etc.
- **Never mutate the element inside `generateVisualContent`** — no style changes, no adding children, no `MarkDirtyRepaint()` from within the callback
- **`LineCap.Butt` for precise arc endpoints** — `Round` extends past the endpoint by half the line width
