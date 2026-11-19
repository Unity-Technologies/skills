# uGUI guide

Understand existing uGUI (Canvas-based Unity UI), make targeted edits, and generate new Canvas hierarchies.

Paths in this file that start with `references/`, such as `references/ugui-scrollview-setup.md`, resolve to the skill folder, not to this file's folder. Paths that start with `Library/` or `Assets/` resolve to the Unity project folder.

## Contents

- Docs to read first
- Critical rules
- Generation
- Canvas setup
- Layout
- RectTransform anchoring
- Text
- Interaction readiness
- Understanding and editing
- C#
- Error recovery
- Checks

## Docs to read first

The uGUI documentation ships with the package in the project: `Library/PackageCache/com.unity.ugui@*/Documentation~/`, where `@*` is the installed version's folder. Read only the `.md` files there. Open the page for what you're about to build before changing the hierarchy, because uGUI's layout components interact in ways that are easy to get wrong from memory.

| Building | Read in `Documentation~/` |
|---|---|
| Canvas and render modes | `UICanvas.md` |
| Scaling to screen sizes | `HOWTO-UIMultiResolution.md`, `script-CanvasScaler.md` |
| Anchors and pivots | `UIBasicLayout.md`, `class-RectTransform.md` |
| Layout groups | `UIAutoLayout.md`, `script-HorizontalLayoutGroup.md`, `script-VerticalLayoutGroup.md`, `script-GridLayoutGroup.md` |
| Sizing to content | `HOWTO-UIFitContentSize.md`, `script-ContentSizeFitter.md`, `script-LayoutElement.md` |
| Scroll views | `script-ScrollRect.md`, then `references/ugui-scrollview-setup.md` |
| Input and events | `EventSystem.md`, `InputModules.md`, `script-GraphicRaycaster.md` |
| Text | `TextMeshPro/index.md` |
| Phone notches and cutouts | `script-SafeArea.md`, if present in the installed version |

## Critical rules

- **Fully qualified types:** write `UnityEngine.UI.Image` and `UnityEngine.UI.Button`, not `Image` and `Button`, because `UnityEngine.UIElements` and project namespaces have types with the same names, and the ambiguity breaks compilation.
- **Verify before modifying:** query the hierarchy before each change. Confirm that parents exist before adding children, and that components exist before setting their properties, because scripts that assume state fail partway and leave half-built UI.
- **Fix in place:** correct the specific broken property instead of destroying and recreating the hierarchy, because destroyed objects break every reference to them. If a fix fails, revert it before trying another.
- **One change at a time:** make one change, check the result, then make the next. Keep adjacent elements untouched when editing one.
- **Readable text:** text contrasts with its background, except for intentional low-contrast uses (disabled states, placeholder text, decoration).
- **Exact values:** apply pixel sizes, hex colors, positions, and spacing the user gives exactly.

## Generation

| Request | Output |
|---------|--------|
| A screen, menu, panel, or HUD | Canvas hierarchy (in the scene or a prefab) + a controller script for live values and interactions, in a saved scene |
| "Static layout" or "layout to wire up later" | Canvas hierarchy only |

Steps:

1. Search for existing Canvases, prefabs, sprites, and fonts, and use their actual paths.
2. Design the palette, type scale, spacing, and states before building (`references/ui-visual-design.md`).
3. If the open scene is untitled, save it with an explicit path, for example `EditorSceneManager.SaveScene(scene, "Assets/Scenes/MenuDemo.unity")`, because saving an untitled scene without a path opens a modal Save As dialog that blocks the Editor.
4. Build the Canvas (Canvas setup), then the content with anchors and layout groups.
5. Write the controller script, and wire each `Button.onClick`.
6. Save the scene, then run the checks at the end of this file.

Name GameObjects in PascalCase (`SubmitButton`), and keep prefabs in feature folders (`Assets/UI/Inventory/`), unless the project uses other conventions.

## Canvas setup

`UICanvas.md` and `script-CanvasScaler.md` describe the render modes and scale modes. Defaults for a new Canvas:

- **Scaler:** Scale With Screen Size, reference resolution 1920×1080 (or the project's standard), Match 0.5.
- **Existing Constant Pixel Size Canvas:** ask the user before changing it, because pixel-art and fixed-resolution projects use it on purpose.

## Layout

Read `UIAutoLayout.md` and `HOWTO-UIFitContentSize.md` before nesting layout groups or adding a Content Size Fitter. The mistake to avoid: a Content Size Fitter on a child of a layout group, where the group and the fitter both set the child's size. Put the fitter on the object that has the layout group instead, as ScrollView Content does.

## RectTransform anchoring

`UIBasicLayout.md` and `class-RectTransform.md` cover anchors and pivots. When placing an element from a description ("top right", "bottom bar"), set the anchors first, then the pivot to the same corner or edge, then position and size, because position values are relative to the anchors and the pivot.

## Text

- Use TextMesh Pro for all text, including world-space text, because legacy `Text` renders blurry when scaled.
- If `Assets/TextMesh Pro/` doesn't exist, import the TMP Essentials with `TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false)` (essentials, no examples, not interactive). The Window > TextMeshPro menu item opens a modal dialog that blocks the Editor until a human dismisses it.

## Interaction readiness

Interactive UI fails silently without these:

1. **One EventSystem** in the scene.
2. **The right input module:** if the project's Active Input Handling is the Input System package only, the EventSystem needs `InputSystemUIInputModule`, because `StandaloneInputModule` throws errors every frame without the old Input Manager.
3. **A GraphicRaycaster** on the Canvas.
4. **Raycast Target:** on for interactive graphics. Turn it off on decorative images and text, because a raycast target drawn over a button blocks its clicks unless it's the button's child, and every target adds raycast cost.

## Understanding and editing

Inspect the scene or prefab before answering or editing.

- To explain structure: identify the Canvas and its render mode, map the parent-child relationships, say which layout groups control which children, and give the anchor setup.
- For "Why is this invisible?": check the size, anchors, parent bounds, sibling order, the Image's sprite or color alpha, and the CanvasGroup alpha.
- For edits: confirm the target exists, change the one property the request covers, and check the result.

## C#

- Use `[SerializeField]` fields for references, cache components in `Awake()`, and check serialized references for null before using them.
- Wire buttons in the controller script with `button.onClick.AddListener(...)`, so the wiring is in code you can read back and check.
- Place scripts next to their prefabs, unless the project keeps scripts elsewhere.

## Error recovery

1. Identify the exact error or symptom, and find its cause before changing anything.
2. Fix one issue at a time and check each fix.
3. If you're stuck, re-read the hierarchy and confirm your earlier changes were applied. Then consider whether the approach is wrong, rather than the implementation.

## Checks

1. The Console has no errors.
2. Enter Play mode, and through `script eval` call `Canvas.ForceUpdateCanvases()`, then read the `RectTransform.rect.size` of each key element, because layout groups only resize their children when the canvas updates. Check that every size is above zero. Don't add logging to the controller script.
3. The scene has exactly one EventSystem, with the input module that matches the project's input handling.
4. Capture the Game view and review it against `references/ui-visual-design.md`. Save Game view captures with `unity command capture game_view --source screen --output-path Temp/UIReview/<name>.png`. The path is relative to the project folder, and Unity doesn't import `Temp/`.

If a check fails, reread the page for that component from the table at the top before changing the hierarchy again.
