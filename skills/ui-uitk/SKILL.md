---
name: ui-uitk
description: Unity UI Toolkit expert for Unity 6.0+. Understands, edits, and generates UXML and USS files with flex-based layouts. Use for requests involving .uxml, .uss, UI Toolkit, UIElements, UIDocument, UI runtime binding, Custom UI Elements, Manipulators or PanelSettings.
allowed-tools:
  - Read
  - Write
  - Edit
  - Glob
  - Grep
---

Understand existing Unity UI Toolkit code, make targeted edits, generate new UXML/USS files, Manipulators, and handle UI runtime binding.

## References

Read these as needed:
- `references/uss-guide.md` — USS patterns and examples
- `references/svg-icons.md` — SVG icon generation (only when generating icons)
- `references/common-issues.md` — Common mistakes to avoid
- `references/ui-runtime-binding.md` — Patterns and guide to bind data to UI at runtime (only when requested or when bindings are involved)
- `references/painter2d.md` — Painter2D API for custom visuals: gradients, shapes, arcs, procedural drawing (read this whenever gradients, custom shapes, progress rings, procedural drawing, or any visual beyond what USS can express is needed)
- `references/pointermanipulator-guide.md` — Patterns and guide to create and use Manipulators (only when requested or when manipulators are involved). This helps with setting up drag and drop features or simple event handling for a Visual Element.
- `references/custom-elements.md` — Custom UI Element patterns and guide to create reusable components with UXML, USS, and C#. This helps with creating complex UI components for reuse across the project.

Paths are relative to this skill's folder — read `references/uss-guide.md` directly.

## Understanding

When explaining UI structure, use this format:
```
[ElementType] name="elementName" class="class1 class2"
├── [ChildType] name="childName"
│   └── [GrandchildType]
└── [ChildType] class="another-class"
```

## Editing

**Common edit requests:**

| Request | Action |
|---------|--------|
| "Change button color" | Edit USS selector for that button |
| "Add a label here" | Add element to UXML at specified location |
| "Make this bigger" | Edit width/height in USS |
| "Hide this element" | Add `display: none` to USS or remove from UXML |
| "Rename this element" | Update `name` attribute in UXML |

**Don't over-edit:**
- Change only what's requested
- Preserve formatting and structure
- Don't "improve" unrelated code
- Don't add comments unless asked
- For targeted changes, prefer modifying specific elements or selectors over rewriting entire files — but use judgment; if a change touches most of the file, a rewrite may be cleaner
- Be careful not to accidentally drop existing elements, styles, or references when making edits
- **When editing USS**, focus on the properties and selectors relevant to the request — avoid unnecessary reorganization, but restructure if the change genuinely requires it

## Validation

There is no way to validate UXML or USS from outside the Editor — Unity parses these
files on import and reports problems in the Console. Write the files, then have the
user check the result.

**Workflow:**

1. Write the complete file to its target path in the project. Do not write partial or
   draft content — a half-written UXML file is a parse error the moment the Editor
   picks it up.
2. Ask the user to focus the Unity Editor. That triggers a reimport of the changed
   assets.
3. Ask them to report anything in the Console. UXML parse errors name the file and
   line; USS problems appear as warnings about unknown properties or selectors.
4. Fix what they report and repeat from step 1.

**Because feedback costs a user round-trip, get it right the first time:**

- Finish all files before asking the user to check, so one reimport covers everything
  rather than one per file.
- Re-read `references/uss-guide.md` and `references/common-issues.md` before writing,
  rather than after an error comes back.
- Watch for the mistakes that survive a parse but render wrong — those will not appear
  in the Console at all, so the user has to eyeball the UI. `references/common-issues.md`
  lists them.

## Generation

When creating new UI:

**Generate only what is requested:**

| Request | Output |
|---------|--------|
| USS only | `.uss` file only |
| UXML only | `.uxml` file only |
| UI screen / menu / panel | `.uss` + `.uxml` only |
| "with code" / "with logic" / "functional" | `.uss` + `.uxml` + `.cs` |

**These do NOT imply C#:**
- "proper buttons" → well-styled Button elements
- "currency display" → a Label element
- "working UI" → valid UXML/USS that renders
- "inventory screen" → visual layout only
- "inventory system" / "equipment system" / "crafting system" → Ask: "Should items be draggable?" If yes, see `references/pointermanipulator-guide.md` for patterns

**Generation workflow:**
1. **Analyze** — Determine exactly what files are needed. No extras.
2. **Search** — Find existing USS, UXML, assets. Don't assume paths.
3. **Follow project patterns** — Match folder structure and naming conventions.
4. **Reuse** — Check for shared stylesheets. Reuse if appropriate.
5. **Write USS first** — Verify against restrictions below.
6. **Write UXML** — Reference the USS, verify structure.
7. **Write the files out complete** — never partial content; see Validation above for
   how errors come back and why one round of files beats several.
8. **Scene setup** — Assign PanelSettings if adding UI to scene.
9. **Data binding** — If requested, add C# script with runtime data binding patterns (see `references/ui-runtime-binding.md`). Generate the scriptable object asset if needed. Assign the asset to the UI element root in UXML or via datasource in C#.

**Color, visibility, and specification rules:**
- **Ensure text is readable by default:** When choosing colors, ensure text contrasts with its background — but respect intentional low-contrast uses (disabled states, placeholder text, decorative elements). When using design tokens, check that text and background variables provide adequate contrast.
- **Honor exact values:** User-specified hex colors, pixel dimensions, spacing — use exactly as given. Do not approximate or substitute.

**Styling / Theme**
When styling UI or adjusting theme make sure to not only apply to the elements directly in the current UXML but also to the core elements of UI Toolkit which are composed of several child elements usually.

## Conventions

**Follow project patterns first.** Search existing files before applying defaults.

| Type | Convention | Good | Bad |
|------|------------|------|-----|
| `name` attribute | camelCase | `submitButton` | `submit-button` |
| `class` attribute / USS | kebab-case | `.submit-button` | `.submitButton` |
| File paths | Feature folders | `Assets/UI/Inventory/` | `Assets/Scripts/UI/` |

**Output format:**
```uxml Filename.uxml
<ui:UXML>...</ui:UXML>
```
```uss Filename.uss
.class { ... }
```

## USS Restrictions

Unity's USS is a subset of CSS. These properties do NOT exist — NEVER use them:

| NEVER Use | Use Instead |
|-----------|-------------|
| `border` shorthand | `border-width`, `border-color` separately |
| `gap` | `margin` on children |
| `z-index` | DOM order or parent nesting |
| `pointer-events` | `picking-mode` UXML attribute |
| `filter` | Not supported |
| `outline` | `border-*` properties |
| `box-shadow` | Nested elements or background image |
| `:first-child`, `:last-child`, `:nth-child` | Explicit classes |
| `[attribute]` selectors | Explicit classes |
| `transition-property: <value>` | Omit entirely, or `none`/`initial`/`inherit` only |
| `linear-gradient()`, `radial-gradient()` | Custom `VisualElement` with Painter2D (see `references/painter2d.md`) |

**Inline styles:** NEVER use `style="..."` in UXML. All styling in USS only.

**External URLs:** NEVER use `url()` with external paths. Only `url("project://database/Assets/...")`.

**Prefer flexible layouts over hardcoded sizes:**
- Use `flex-grow`, `flex-shrink`, or `%` instead of fixed `width`/`height` values
- Let elements flow naturally and be constrained by their parent container
- Set explicit pixel sizes only on root containers or when a fixed size is truly required
- Child elements should adapt to available space rather than define their own dimensions

## USS Brevity

- No default values (`flex-direction: column` is default)
- No default fonts
- No redundant constraints (`width: 100px` doesn't need `min-width`/`max-width`)
- No overlapping properties (`flex: 1` already sets grow/shrink)
- Simplest selector that works
- Never duplicate selectors

## UXML

Every file must:
1. Declare namespace: `<ui:UXML xmlns:ui="UnityEngine.UIElements">`
2. Link stylesheet(s): `<ui:Style src="Screen.uss" />`
3. Have exactly one top-level container
4. **No `style="..."` attributes** — use USS only

```uxml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
  <ui:Style src="Panel.uss" />
  <ui:VisualElement name="root" class="panel">
    <!-- content -->
  </ui:VisualElement>
</ui:UXML>
```

## Events and Interactivity
- Use Pointer Manipulators for event handling and interactivity on a VisualElement (see `references/pointermanipulator-guide.md`)
- If drag and drop is requested then write a pointer Manipulator and attach it to the relevant Visual Element in UXML or via C#.
- For simple click events, you can use the `clickable` manipulator in UXML
- For more advanced interactions, create use more traditional event callbacks in C# and attach them to elements as needed

**For inventory and crafting systems:**
- When users request an "inventory system", "equipment system", or "crafting system", ask explicitly: "Should players be able to drag and drop items?"
- If yes, read `references/pointermanipulator-guide.md` for inventory/crafting-specific patterns
- If no or unclear, create static layout only

## Assets

**Do NOT reference `UnityDefaultRuntimeTheme.tss`** or Unity's built-in theme icons.

**Icon priority:**
1. Reuse existing project icons
2. Generate SVG (see `references/svg-icons.md`)
3. Image generators (last resort)

**Reference format:**
```uss
background-image: url("project://database/Assets/UI/Textures/icon.png");
```

## Scene Setup

**PanelSettings is required** — UI won't render without it.

1. Search for existing PanelSettings asset
2. If none, create generic: `Assets/UI/PanelSettings.asset`
3. Assign to UIDocument's `Panel Settings` field

Skip for Editor UI (EditorWindow, PropertyDrawer).

## C# (Only When Requested)

- Style via USS classes (`AddToClassList()`) — never use `element.style.*` as inline styles have higher specificity than USS selectors, making them impossible to override via stylesheets, and add per-element memory overhead
- UITK uses TextCore text assets — use `FontAsset`, `TextStyleSheet`, and `TextSettings`, not their TextMeshPro equivalents (`TMP_FontAsset`, etc.)
- Place scripts in same folder as UXML/USS
