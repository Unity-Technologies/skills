# IMGUI Elements and Layout

## Common GUI Elements

| Method | Use Case |
|--------|----------|
| `GUILayout.Label()` | Text display |
| `GUILayout.Button()` | Clickable button |
| `GUILayout.TextField()` | Text input |
| `GUILayout.TextArea()` | Multi-line text input |
| `GUILayout.Toggle()` | Checkbox |
| `GUILayout.Slider()` | Value slider |
| `GUILayout.SelectionGrid()` | Button grid selection |
| `GUILayout.Toolbar()` | Toolbar buttons |

## Editor-Specific Elements

| Method | Use Case |
|--------|----------|
| `EditorGUILayout.PropertyField()` | Serialized property (auto) |
| `EditorGUILayout.ObjectField()` | Object reference |
| `EditorGUILayout.IntField()` | Integer input |
| `EditorGUILayout.FloatField()` | Float input |
| `EditorGUILayout.Vector3Field()` | Vector3 input |
| `EditorGUILayout.ColorField()` | Color picker |
| `EditorGUILayout.CurveField()` | Animation curve |
| `EditorGUILayout.EnumPopup()` | Enum dropdown |
| `EditorGUILayout.Popup()` | String dropdown |
| `EditorGUILayout.Foldout()` | Collapsible section |
| `EditorGUILayout.HelpBox()` | Info/warning/error box |

## Layout Groups

```csharp
// Horizontal
GUILayout.BeginHorizontal();
GUILayout.Button("Left");
GUILayout.Button("Right");
GUILayout.EndHorizontal();

// Vertical
GUILayout.BeginVertical();
GUILayout.Button("Top");
GUILayout.Button("Bottom");
GUILayout.EndVertical();

// Scroll View
scrollPos = GUILayout.BeginScrollView(scrollPos);
// ... content
GUILayout.EndScrollView();

// Area (absolute positioning)
GUILayout.BeginArea(new Rect(10, 10, 200, 100));
// ... content
GUILayout.EndArea();
```

## Editor Layout Groups

```csharp
// Foldout section
showSection = EditorGUILayout.Foldout(showSection, "Section");
if (showSection)
{
    EditorGUI.indentLevel++;
    // ... content
    EditorGUI.indentLevel--;
}

// Horizontal with flex space
EditorGUILayout.BeginHorizontal();
GUILayout.Label("Label");
GUILayout.FlexibleSpace();
GUILayout.Button("Action");
EditorGUILayout.EndHorizontal();

// Box group
EditorGUILayout.BeginVertical("box");
// ... content
EditorGUILayout.EndVertical();
```

## Layout Options

```csharp
// Fixed width
GUILayout.Button("Wide", GUILayout.Width(200));

// Fixed height
GUILayout.Button("Tall", GUILayout.Height(50));

// Min/Max
GUILayout.Button("Flex", GUILayout.MinWidth(100), GUILayout.MaxWidth(300));

// Expand
GUILayout.Button("Fill", GUILayout.ExpandWidth(true));
```

## Styling

```csharp
// Built-in styles
GUILayout.Label("Bold", EditorStyles.boldLabel);
GUILayout.Label("Large", EditorStyles.largeLabel);
GUILayout.Label("Mini", EditorStyles.miniLabel);
GUILayout.Label("Centered", EditorStyles.centeredGreyMiniLabel);

// Custom style (cache this, don't create in OnGUI)
private GUIStyle _headerStyle;
private GUIStyle HeaderStyle => _headerStyle ??= new GUIStyle(EditorStyles.boldLabel)
{
    fontSize = 16,
    alignment = TextAnchor.MiddleCenter
};
```

## Spacing and Separators

```csharp
// Fixed space
GUILayout.Space(10);

// Flexible space (pushes elements apart)
GUILayout.FlexibleSpace();

// Horizontal line
EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

// Editor separator
EditorGUILayout.Separator();
```

## Disabled Groups

```csharp
EditorGUI.BeginDisabledGroup(someCondition);
// ... disabled elements
EditorGUI.EndDisabledGroup();

// Or with using
using (new EditorGUI.DisabledGroupScope(someCondition))
{
    // ... disabled elements
}
```

## Change Check

```csharp
EditorGUI.BeginChangeCheck();
value = EditorGUILayout.IntField("Value", value);
if (EditorGUI.EndChangeCheck())
{
    // Value changed, do something
}
```
