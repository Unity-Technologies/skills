# Custom VisualElements in UI Toolkit

Guide for creating custom VisualElements using Unity 6+ `[UxmlElement]` and `[UxmlAttribute]` attributes.

## Table of Contents

- [Overview](#overview)
- [Requirements](#requirements)
- [Supported Property Types](#supported-property-types)
- [Advanced Patterns](#advanced-patterns)
- [Best Practices](#best-practices)
- [UXML Namespace Declaration](#uxml-namespace-declaration)
- [UI Builder Integration](#ui-builder-integration)
- [Summary](#summary)

## Overview

Unity 6+ uses attribute-based custom elements. The old factory pattern (`IUxmlFactory`, `UxmlTraits`) is **deprecated**.

### ⚠️ CRITICAL: Namespace Declaration

**Never include assembly names in UXML namespace declarations:**

```xml
❌ WRONG: xmlns:custom="Game.UI.Custom, Assembly-CSharp"
✅ CORRECT: xmlns:custom="Game.UI.Custom"
```

**Format**: `xmlns:prefix="Namespace.Path"` (namespace only, no assembly)

### Basic Pattern

```csharp
[UxmlElement]
public partial class MyElement : VisualElement
{
    [UxmlAttribute]
    public string myValue { get; set; }
}
```

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements"
         xmlns:custom="Game.UI.Custom">
    <custom:MyElement my-value="Hello" />
</ui:UXML>
```

## Requirements

1. **[UxmlElement]** attribute on the class
2. **partial** keyword required
3. Inherit from **VisualElement** or subclass
4. **[UxmlAttribute]** on exposed properties

### Property Naming

C# camelCase → UXML kebab-case:
- `myStringValue` → `my-string-value`
- `maxHealth` → `max-health`

### Example

```csharp
[UxmlElement]
public partial class CustomLabel : Label
{
    [UxmlAttribute]
    public Color textColor { get; set; } = Color.white;

    [UxmlAttribute]
    public int fontSize { get; set; } = 14;

    public CustomLabel()
    {
        RegisterCallback<GeometryChangedEvent>(evt => {
            style.color = textColor;
            style.fontSize = fontSize;
        });
    }
}
```

```xml
<custom:CustomLabel text="Hello" text-color="rgb(255,215,0)" font-size="24" />
```

## Supported Property Types

**Basic**: `string`, `int`, `float`, `bool`, `Color`
**Unity**: `Texture2D`, `Sprite`, `Font`
**Enums**: Any enum type
**Collections**: `List<T>`, `T[]`

### Image Handling

**Static images** (don't change) → Use USS:
```css
.icon { background-image: url('project://Assets/UI/Icons/fireball.png'); }
```

**Dynamic images** (change per instance) → Use `[UxmlAttribute]`:
```csharp
[UxmlAttribute]
public Sprite portrait { get; set; }  // Different per character
```

## Advanced Patterns

### Validation Attributes

```csharp
[UxmlAttribute]
[Range(0, 100)]
[Tooltip("Current health value")]
public float health { get; set; } = 100f;
```

Improves UI Builder inspector experience.

### Custom Attribute Names

```csharp
[UxmlAttribute("hp")]
public float health { get; set; }  // Use "hp" in UXML
```

### Custom Type Converters

Use `UxmlAttributeConverter<T>` when a property's type is not natively supported by UXML (e.g. structs, complex data objects). An example below implements `FromString` to parse the UXML attribute string into the specified type, then register it with `[UxmlAttributeConverter]` on the property.

```csharp
public class HealthDataConverter : UxmlAttributeConverter<HealthData>
{
    public override HealthData FromString(string value)
    {
        var parts = value.Split(',');
        return new HealthData { current = float.Parse(parts[0]), max = float.Parse(parts[1]) };
    }
}

[UxmlAttribute]
[UxmlAttributeConverter(typeof(HealthDataConverter))]
public HealthData healthData { get; set; }
```

### Custom Property Drawers

Create custom UI Builder inspector controls for your attributes:

```csharp
// 1. Custom attribute
public class SliderDrawerAttribute : PropertyAttribute { }

// 2. Property drawer
[CustomPropertyDrawer(typeof(SliderDrawerAttribute))]
public class SliderDrawerPropertyDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var field = new SliderInt(0, 100) { label = property.displayName };
        field.BindProperty(property);
        return field;
    }
}

// 3. Usage
[UxmlElement]
public partial class StyledButton : Button
{
    [UxmlAttribute]
    [SliderDrawer]
    public int intensity { get; set; } = 50;
}
```

**Override existing properties** with custom drawers:

```csharp
[UxmlElement]
public partial class CustomIntField : IntegerField
{
    // Override base 'value' property to use slider drawer in UI Builder
    [UxmlAttribute("value"), SliderDrawer]
    internal int myValue
    {
        get => this.value;
        set => this.value = value;
    }
}
```

This customizes how properties appear in the UI Builder inspector.


## Best Practices

1. **Always use `partial`** - Required for [UxmlElement]
2. **Provide defaults** - `public float radius { get; set; } = 30f;`
3. **Update on changes** - Use property setters to call `UpdateVisuals()` or `MarkDirtyRepaint()`
4. **Use backing fields** - When validation or change detection needed
5. **USS for styling** - Add class names and style via USS, not inline styles
6. **Static images → USS, Dynamic → C#** - Use USS `background-image` for fixed images
7. **Document elements** - Add XML comments for better developer experience

## UXML Namespace Declaration

### Rules

1. **Namespace only** - No assembly name, no commas
2. **Exact match** - Must match C# namespace exactly (case-sensitive)
3. **Format**: `xmlns:prefix="Namespace.Path"`

### Examples

```xml
<!-- ✅ Single namespace -->
<ui:UXML xmlns:ui="UnityEngine.UIElements"
         xmlns:custom="Game.UI.Custom">
    <custom:RadialProgress />
</ui:UXML>

<!-- ✅ Multiple namespaces -->
<ui:UXML xmlns:ui="UnityEngine.UIElements"
         xmlns:hud="Game.UI.HUD"
         xmlns:menu="Game.UI.Menu">
    <hud:HealthBar />
    <menu:SettingsPanel />
</ui:UXML>
```

### Common Mistakes

```xml
❌ xmlns:custom="Game.UI.Custom, Assembly-CSharp"  (assembly name)
❌ xmlns:custom="game.ui.custom"                    (wrong case)
❌ xmlns:custom="Custom"                            (incomplete)
❌ xmlns:custom="Game.UI.Custom.RadialProgress"     (class name)

✅ xmlns:custom="Game.UI.Custom"                    (correct)
```
