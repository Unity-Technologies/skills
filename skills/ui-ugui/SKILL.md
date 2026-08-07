---
name: ui-ugui
description: Unity uGUI (Canvas-based) UI expert. Understands, edits, and generates Canvas hierarchies, RectTransforms, Layout Groups, and prefab UI. Use for requests involving Canvas, uGUI, RectTransform, or .prefab UI files.
allowed-tools:
  - Read
  - Write
  - Edit
  - Glob
  - Grep
---

Understand existing Unity uGUI, make targeted edits, and generate new Canvas-based hierarchies.

When working with ScrollRect/ScrollView, read the reference file:
- `references/scrollview-setup.md` — Required hierarchy, setup rules, and common failures

## Scope

Determine what the user is asking for:

| Request Type | Action |
|--------------|--------|
| Question about UI | **Understand** — analyze hierarchy, explain structure |
| Change specific element | **Edit** — targeted modification only |
| Create new UI | **Generate** — create new hierarchy |
| Fix/improve existing UI | **Edit** — modify existing, don't rebuild |

**Generate only what is requested:**

| Request | Output |
|---------|--------|
| UI layout | Prefab or scene hierarchy only |
| "with code" / "with logic" / "functional" | Hierarchy + scripts |

**These do NOT imply scripts:**
- "proper buttons" → well-configured Button components
- "working UI" → valid hierarchy that renders
- "menu screen" → visual layout only

## Critical Rules

**Namespace disambiguation:**
- Always use fully qualified type names when creating or referencing UI components
- `UnityEngine.UI.Image`, not `Image`
- `UnityEngine.UI.Button`, not `Button`
- Other namespaces in the project can cause ambiguous type errors

**Verify before modifying:**
- Always check what currently exists before making changes
- Confirm parent objects exist before adding children
- Verify components are present before modifying properties
- Never assume hierarchy state — query it first

**Incremental fixes over rebuilds:**
- When fixing issues, make targeted corrections
- Never destroy and recreate entire hierarchies to fix problems — destroyed objects cause null reference cascades
- Prefer identifying the specific broken property and fixing only that over rewriting large sections
- **When a fix fails, revert the change** before trying an alternative approach

**One change at a time:**
- Make a single change, then verify the result
- Do not batch multiple unrelated modifications
- Be careful not to inadvertently modify or remove adjacent elements when editing a specific one
- If something fails, understand why before trying alternatives
- Avoid "shotgun debugging" with multiple simultaneous changes

**Color and visibility:**
- **Ensure text is readable by default:** When choosing colors, ensure text contrasts with its background — but respect intentional low-contrast uses (disabled states, placeholder text, decorative elements)
- **Check visibility for new elements:** After creating UI elements, verify they have non-zero size and are within parent bounds. Elements intentionally created hidden (for later toggling, animation, etc.) are fine

**Specification adherence:**
- **Honor user specifications exactly:** When the user provides pixel dimensions, hex colors, positions, spacing, or other exact values, apply them precisely — do not approximate or substitute
- **Minimize unrelated changes:** When editing, avoid changing properties the user didn't ask about unless a related adjustment is necessary for the fix to work

## Conventions

**Follow project patterns first.** Search existing files before applying defaults.

| Type | Convention | Good | Bad |
|------|------------|------|-----|
| GameObject names | PascalCase | `SubmitButton` | `submit-button` |
| Prefab paths | Feature folders | `Assets/UI/Inventory/` | `Assets/Prefabs/UI/` |

## Workflow

1. **Verify state** — Check what exists in the scene/hierarchy before any action.
2. **Analyze** — Determine exactly what's needed. No extras.
3. **Search** — Find existing prefabs, canvases, assets. Don't assume paths.
4. **Follow project patterns** — Match folder structure and naming.
5. **Create or edit** — Build structure with proper anchoring, or make targeted edits.
6. **Confirm result** — Verify the change worked before moving on.

## Canvas Setup

Every UI needs a Canvas:

```
Canvas (Screen Space - Overlay or Camera)
├── CanvasScaler (Scale With Screen Size recommended)
├── GraphicRaycaster
└── [UI Content]
```

**CanvasScaler settings:**
- Default to UI Scale Mode "Scale With Screen Size" unless the project has a specific reason for "Constant Pixel Size" (e.g., pixel-art, fixed-resolution targets)
- Reference Resolution: Match project standards (e.g., 1920x1080)
- When creating a Canvas with Screen Space - Camera, **read the camera's reference resolution** first
- Match Width Or Height: 0.5 (balanced)
- If an existing Canvas uses "Constant Pixel Size", flag it and ask the user before changing
- Prefer anchors and Layout Groups over absolute pixel positions for layout

## Layout Components

**Layout Groups control child sizing:**
- When a parent has a Layout Group, it manages child RectTransforms
- Children's anchors and sizeDelta may be overridden by the parent
- Understand whether the parent or child controls size before setting values

**Vertical/Horizontal Layout Groups:**
- Control Child Size: determines if parent sets child dimensions
- Child Force Expand: determines if children stretch to fill space
- If Control Child Size is off, children must have explicit sizes

**Avoiding layout conflicts:**
- Do not manually set child anchors/size when parent controls them
- Do not add Layout Group to an element that should have fixed size
- Nested Layout Groups require careful configuration of each level
- When layout is wrong, check parent settings before modifying child
- ContentSizeFitter on the **same** object as a Layout Group that has Control Child Size enabled = conflict
- ContentSizeFitter on a child whose parent has Control Child Size enabled = ContentSizeFitter is overridden (wasted)
- When using ContentSizeFitter with a Layout Group parent, disable Control Child Size on the parent for the relevant axis
- Common pattern: ScrollView Content should have ContentSizeFitter + VerticalLayoutGroup where VLG controls children but ContentSizeFitter sizes the Content itself

**Grid Layout Group:**
- For inventory grids, card layouts
- Cell Size must be set explicitly — children are sized to match
- Constraint controls row/column limits

**Content Size Fitter:**
- Horizontal/Vertical Fit: Preferred Size
- Use on containers that should size to their content
- Requires a layout element or text component to provide preferred size

## RectTransform Anchoring

**Elements must have non-zero size to be visible:**
- Set explicit width/height via sizeDelta, or
- Use stretch anchors with proper offsets, or
- Let a parent Layout Group control size (with Control Child Size enabled)

**Anchor configuration order:**
1. Set anchor preset first (corner, edge, or stretch)
2. Then set position/offset values
3. Verify the resulting size is non-zero

**Common patterns:**
- **Stretch anchors** — for responsive elements that fill available space
- **Corner anchors** — for fixed-position, fixed-size elements
- **Edge anchors** — for elements that stretch in one direction only

**Positioning from natural language descriptions:**
When the user describes a position (e.g., "top right", "bottom bar", "left side"):
1. Determine if it's a **corner** (fixed point), an **edge** (stretch along one axis), or **fill** (stretch both axes)
2. Set anchor min and anchor max — for corners these are the same point; for edges/fill they span a range
3. **Set pivot to match the anchor point** — pivot must align with where the element is anchored, not left at the default (0.5, 0.5). A "top right" element needs pivot at the top-right corner; a "top bar" needs pivot at the top edge center
4. Set position/offset values **after** anchors and pivot are configured

**Visibility checklist:**
- Width and height are both greater than zero
- Element is within parent bounds
- Element is not obscured by siblings (check hierarchy order)
- Image component has a sprite or color with alpha > 0

## Common Components

| Component | Use Case |
|-----------|----------|
| `Image` | Backgrounds, icons |
| `RawImage` | Render textures, videos |
| `Text (TMP)` | All text (use TextMeshPro) |
| `Button` | Clickable elements |
| `Toggle` | Checkboxes, radio buttons |
| `Slider` | Value ranges |
| `ScrollRect` | Scrollable content |
| `InputField (TMP)` | Text input |

## Best Practices

- Use TextMeshPro for all text (not legacy Text)
- WorldSpace UI that have text should also use Text Mesh Pro, be sure to review the project and import the TMP essentials if they are not present in the project
- If the TextMeshPro Essentials were imported be sure to close the TMP Importer Windows and the Import Unity Package Window once the assets are imported
- Organize hierarchy logically (Header, Content, Footer)
- Use Layout Groups instead of manual positioning where possible
- Set Raycast Target = false on non-interactive images
- Use sprite atlases for performance

**Never use** `EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources")`, unless the user asks for an interactive TMP Essentials installation. It opens a modal dialog that blocks whatever invoked it until a human dismisses it.

**Do not use** `AssetDatabase.ImportPackage()` for TMP resources, instead `TMP_PackageResourceImporter.ImportResources()` is the canonical non-interactive API.

## Interaction Readiness

Before completing any UI that contains interactive elements, verify:

1. **EventSystem** must exist in the scene (exactly one)
2. **GraphicRaycaster** must be on the Canvas
3. **Raycast Target = true** on interactive elements (and false on non-interactive ones to avoid blocking)
4. **Button.onClick should be wired** (via inspector or script) — only when scripts or logic were requested

If the first three are missing, interactive elements will exist visually but fail silently.

## Understanding

When the user asks questions about existing UI:

**Read the hierarchy first.** Don't assume — always inspect the scene or prefab before answering.

**Analyze structure:**
- Identify the Canvas and its render mode
- Map the parent-child relationships
- Identify which Layout Groups control which children
- Check RectTransform anchor configurations

**Answer questions about:**
- "What does this button do?" → Explain component, hierarchy position, event wiring
- "How is this laid out?" → Describe Layout Groups, anchoring, hierarchy
- "Why is this invisible?" → Check size, anchors, parent bounds, component state
- "What controls this element's size?" → Trace Layout Group settings or anchors

## Editing

For targeted changes to existing UI:

**Read before editing.** Always inspect the current state first.

**Edit workflow:**
1. Verify the target object exists
2. Identify the specific property or component to change
3. Make the minimal change required
4. Verify the result before proceeding

**Never destroy to fix:**
- Destroying objects cascades to null references elsewhere
- Fix properties in place rather than recreating
- If an element must be removed, update all references first

## C# (Only When Requested)

- Use fully qualified UI types to avoid namespace conflicts
- Use `[SerializeField]` for inspector references
- Cache component references in Awake()
- Use events/delegates for button callbacks
- Place scripts in same folder as prefabs (follow project patterns)

**Component references:**
- Verify referenced objects exist before accessing them
- Handle cases where serialized references may be null
- When wiring up references, confirm the target component is present

## Error Recovery

When something goes wrong:

**Stop and diagnose:**
- Identify the exact error or symptom
- Determine the root cause before attempting fixes
- Do not make speculative changes

**Fix incrementally:**
- Address one issue at a time
- Verify each fix before moving to the next
- Keep track of what was changed

**Avoid destructive patterns:**
- "Start fresh" strategies destroy working elements along with broken ones
- Rebuilding entire hierarchies creates more problems than it solves
- Prefer surgical fixes to wholesale replacements

**When stuck:**
- Re-verify the current state of the hierarchy
- Check if previous changes were actually applied
- Consider if the approach itself is wrong rather than the implementation
