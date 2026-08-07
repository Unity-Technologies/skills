# Runtime Data Binding Reference

## Table of Contents

- [Core Concepts](#core-concepts)
- [Data Source Setup](#data-source-setup)
- [CRITICAL: Always Use nameof()](#critical-always-use-nameof)
- [Explicit Binding Element](#explicit-binding-element)
- [C# SetBinding (Programmatic)](#c-setbinding-programmatic)
- [PanelRenderer with Bindings](#panelrenderer-with-bindings)

Unity UI Toolkit runtime data binding for efficient UI updates.

## Core Concepts

**Use binding when:**
- Connecting game state to UI (health, score, ammo)
- Multiple UI elements display the same data
- Data changes frequently but predictably

**Avoid binding when:**
- One-time UI updates
- Per-frame updates (use direct property sets)
- Simple direct property assignment is clearer

## Data Source Setup

### Required: [CreateProperty] Attribute

```csharp
using Unity.Properties;
using UnityEngine;

public class PlayerData
{
    [CreateProperty]
    public int Health { get; set; }

    // the private serialized field here helps the user see the property in the editor but do not create the property on the private member unless requested
    [SerializeField, DontCreateProperty]
    private float m_Speed;

    // the public member gets a property created out of it so we bind to this property
    [CreateProperty]
    public float Speed
    {
        get => m_Speed;
        set => m_Speed = value;
    }
}
```


## CRITICAL: Always Use nameof()
```csharp
element.SetBinding("value", new DataBinding
{
    dataSourcePath = new PropertyPath(nameof(HealthData.HealthPercentage))
});
```

**Why nameof():**
- Compile-time safety (no typos)
- Refactoring support
- IntelliSense/autocomplete
- No capitalization errors

**Nested properties:**
```csharp
dataSourcePath = new PropertyPath($"{nameof(PlayerData)}.{nameof(PlayerData.Health)}.{nameof(HealthData.Current)}")
```

### Explicit Binding Element

```xml
<UXML xmlns:ui="UnityEngine.UIElements" xmlns:engine="UnityEngine.UIElements">
    <Slider name="volume-slider">
        <Bindings>
            <engine:DataBinding
                property="value"
                data-source-path="MasterVolume"
                binding-mode="TwoWay"/>
        </Bindings>
    </Slider>
</UXML>
```

The datasource can be set in the root VisualElement like:
```xml
    <ui:VisualElement name="root" data-source="project://database/Assets/Scripts/PlayerData.asset?fileID=11400000&amp;guid=976f7b99fc1424923aee5b5657723366&amp;type=2#PlayerData" class="root">
```
This is so that UIBuilder can also read the datasource and preview the binding and control.

```csharp
private void OnEnable()
{
    var root = GetComponent<UIDocument>().rootVisualElement;
    root.dataSource = m_Settings;
}
```

## C# SetBinding (Programmatic)

### Basic Pattern

```csharp
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HealthBarController : MonoBehaviour
{
    [SerializeField] private HealthData m_HealthData;
    private Label m_HealthLabel;
    private ProgressBar m_HealthBar;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        m_HealthLabel = root.Q<Label>("health-label");
        m_HealthBar = root.Q<ProgressBar>("health-bar");

        m_HealthLabel.SetBinding("text", new DataBinding
        {
            dataSourcePath = new PropertyPath(nameof(HealthData.HealthText))
        });

        m_HealthBar.SetBinding("value", new DataBinding
        {
            dataSourcePath = new PropertyPath(nameof(HealthData.HealthPercentage))
        });

        m_HealthLabel.dataSource = m_HealthData;
        m_HealthBar.dataSource = m_HealthData;
    }

    private void OnDisable()
    {
        if (m_HealthLabel?.HasBinding("text") == true)
            m_HealthLabel.ClearBinding("text");
        if (m_HealthBar?.HasBinding("value") == true)
            m_HealthBar.ClearBinding("value");
    }
}
```

### Binding Modes

```csharp
element.SetBinding("text", new DataBinding
{
    dataSourcePath = new PropertyPath(nameof(Data.Score)),
    bindingMode = BindingMode.ToTarget
});

slider.SetBinding("value", new DataBinding
{
    dataSourcePath = new PropertyPath(nameof(Settings.MasterVolume)),
    bindingMode = BindingMode.TwoWay
});
```

**Modes:**
- `ToTarget` - Data → UI (read-only, default)
- `ToSource` - UI → Data (write-only, rare)
- `TwoWay` - Data ↔ UI (input fields)

### Runtime-Created Elements

```csharp
var label = new Label();
label.SetBinding("text", new DataBinding
{
    dataSourcePath = new PropertyPath(nameof(PlayerData.Name))
});
label.dataSource = m_PlayerData;
parentElement.Add(label);
```

## PanelRenderer with Bindings

**Unity 6.6+ only** - `PanelRenderer` is a new UI Toolkit runtime component.

**Use `PanelRenderer` instead of `UIDocument` for runtime UI with bindings.**

`PanelRenderer` provides `RegisterUIReloadCallback` which ensures bindings are properly re-established if the UI reloads.

### PanelRenderer Controller

```csharp
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

public class StatsController : MonoBehaviour
{
    [SerializeField] private MyStats m_Stats;
    private ScriptableObject m_LoadedData;

    private void Awake()
    {
        m_LoadedData = ScriptableObject.Instantiate(m_Stats);
    }

    private void OnEnable()
    {
        GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
    }

    private void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement)
    {
        var hpLabel = rootElement.Q("hpLabel");
        var mpLabel = rootElement.Q("mpLabel");

        rootElement.dataSource = m_LoadedData;

        hpLabel.SetBinding("text", new DataBinding
        {
            dataSourcePath = new PropertyPath(nameof(MyStats.HP))
        });

        mpLabel.SetBinding("text", new DataBinding
        {
            dataSourcePath = new PropertyPath(nameof(MyStats.MP))
        });
    }
}
```

**Key benefits:**
- Callback handles UI reload events automatically
- Bindings re-established if UI is reloaded
- Cleaner than manual OnEnable setup
- Works with ScriptableObject data sources
