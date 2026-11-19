# IMGUI guide

Maintain and extend IMGUI (immediate mode) code: EditorWindows and custom Inspectors that draw in `OnGUI()` or `OnInspectorGUI()`, PropertyDrawers that draw in `OnGUI(Rect, SerializedProperty, GUIContent)`, and runtime debug overlays in `MonoBehaviour.OnGUI()`.

Use this guide when the code you're changing already uses IMGUI, the user asks for IMGUI, or the project's editor tools use IMGUI exclusively. New editor windows and Inspectors in other projects use UI Toolkit, because it's Unity's recommended system for new Editor UI. IMGUI isn't for runtime game UI; use UI Toolkit or uGUI for that.

## Contents

- Docs to read first
- Conventions
- Rules
- Console errors
- Checks

## Docs to read first

Replace `<VERSION>` in these URLs with the project's Editor version, for example `6000.7`. Open the page for what you're building before writing code, because it has the current structure, a complete example, and the rules for styles, serialized data, undo, and layout:

| Building | Read |
|---|---|
| EditorWindow | [Editor Windows](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/ui-imgui/extending-the-editor/editor-editor-windows.md) |
| Custom Inspector | [Custom Editors](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/ui-imgui/extending-the-editor/editor-custom-editors.md) |
| PropertyDrawer | [Property Drawers](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/ui-imgui/extending-the-editor/editor-property-drawers.md) |
| Controls (buttons, fields, sliders) | [IMGUI controls](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/ui-imgui/gui-controls.md) |
| Layout (horizontal, vertical, scroll, areas) | [IMGUI layout modes](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/ui-imgui/gui-layout.md) |
| Styles and skins | [Customization](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/ui-imgui/gui-customization.md) |

## Conventions

Follow the project's existing editor scripts first. Otherwise:

| Type | Convention | Example |
|------|------------|---------|
| Script names | PascalCase | `MyToolWindow.cs` |
| EditorWindow | `[Name]Window.cs` | `LevelEditorWindow.cs` |
| Custom Editor | `[Type]Editor.cs` | `EnemyEditor.cs` |
| PropertyDrawer | `[Type]Drawer.cs` | `RangeDrawer.cs` |

**Location:** put scripts that use `UnityEditor` in an `Editor` folder or an Editor-only assembly definition, because player builds can't compile `UnityEditor` code. Keep runtime debug overlays (`MonoBehaviour.OnGUI`) out of Editor folders, because they must run in the player.

## Rules

The doc pages cover the general rules. These are the ones that cause the errors in the next table, or that the pages don't spell out:

- **Styles:** create each `GUIStyle` lazily on the first `OnGUI` call and cache it, because `EditorStyles` and `GUI.skin` aren't ready earlier.
- **PropertyDrawers:** edit the `SerializedProperty` that `OnGUI` receives, and don't call `Update()` or `ApplyModifiedProperties()` from the drawer, because that can discard changes the Inspector hasn't applied yet.
- **Begin/End pairs:** close every `Begin*` call on every code path, including early returns, or use the `using (new EditorGUILayout.HorizontalScope())` forms.
- **Same controls per event:** draw the same controls during the Layout and Repaint events of one frame. Change conditions that add or remove controls outside `OnGUI`, or at the start of the Layout event.

## Console errors

| Console message | Cause | Fix |
|---|---|---|
| `EndLayoutGroup: BeginLayoutGroup must be called first.` | An `End*` call without its `Begin*`, often after an early return | Match the pairs on every path, or use the `Scope` forms |
| `GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.` | A `BeginScrollView`, `BeginArea`, or `BeginClip` without its `End*` | Close every scope |
| `ArgumentException: Getting control 1's position in a group with only 1 controls when doing repaint` | Controls drawn in Layout differ from those drawn in Repaint | Apply the "same controls per event" rule |

## Checks

1. The Console has no errors after the scripts compile.
2. Open the window or select an object that uses the Inspector, and check that the Console stays free of the errors in the table above.
3. For runtime overlays, enter Play mode, check through `script eval` that the component that draws the overlay is active and enabled, and check that the Console stays free of errors. Don't add logging to the script.

If a check fails, reread the doc page for that script type before changing the code again.
