# UI Toolkit Manipulator Reference

Pointer Manipulators handle pointer interactions like drag and drop, click, hover, and gestures in Unity UI Toolkit.

## What is a Manipulator?

A `Manipulator` is an event handler class attached to `VisualElement`s to add interactive behavior. They encapsulate interaction logic and can be reused across multiple elements.

## Base Pattern

Pointer Manipulators inherit from `PointerManipulator` and override:
- `RegisterCallbacksOnTarget()` — Subscribe to events when attached
- `UnregisterCallbacksFromTarget()` — Unsubscribe when detached

Attach with: `element.AddManipulator(new YourManipulator())`

## Drag and Drop Approach

### Drag and Drop Code Design
1. **PointerDown** — Capture pointer, store pointer start position, store dragged element reference, mark as dragging, set to Aboslute positioning, and BringToFront

```csharp
_isDragging = true;
target.style.position = Position.Absolute;
target.BringToFront();
target.usageHints = UsageHints.DynamicTransform;
```

2. **PointerMove** — Update dragged element position relative to the pointer, check if pointer is over valid drop target. ALWAYS use the StyleTranslate API for drag and drop and updating element position unless otherwise specified.

Example of using StyleTranslate API:
```csharp
target.style.translate = new StyleTranslate(newWorldPosition);
```

3. **PointerUp** — If over drop target or find nearest overlapping target, execute drop logic (move element, trigger callback), release pointer
4. **PointerCaptureOut** — Finalize drop and raise event

### Drop Target Detection
Use `VisualElement.panel.Pick(position)` or check bounds with `worldBound.Contains(position)` to find elements under the pointer.

### Visual Feedback
Add/remove USS classes on drag start/end for hover states, drag shadows, or drop zone highlights:
```csharp
target.AddToClassList("dragging");
dropZone.AddToClassList("drop-zone-active");
```

## Best Practices
- Use USS classes for visual states instead of inline `element.style.*` properties
- Validate drop targets before executing drop logic
- Use `evt.StopPropagation();` to prevent dragged items from behaving like buttons or interacting unexpectedly
- To ensure dragged item is always on top use `BringToFront()`
- Decide the `pickingMode` for the dragged item to ensure the slot underneath is detected and revert to proper state after dropping

## Common Enhancements

- **Constrain to bounds** — Clamp position to parent or screen bounds
- **StyleTranslation API** - Use `target.style.translate = new StyleTranslate(newWorldPosition)` to avoid style pass updates
- **Performance** - Use Usage Hints and `UsageHints.DynamicTransform;` for better performance
- **Drop validation** — Check if drop target accepts this element type
- **Revert on invalid drop** — Animate back to start position if dropped outside valid zone

## Inventory and Crafting Systems

When users request inventory or crafting systems, first determine requirements:

### Ask Users First
- "Should players be able to drag and drop items between slots?"
- "Should items snap to grid positions or specific equipment slots?"
- "Do items stack? Do they have quantities?"

If drag-and-drop is NOT needed, create static visual layout only (UXML/USS).

### When to Use Drag-Drop

**Inventory systems need drag-drop when:**
- Players move items between slots (inventory, equipment, hotbar)
- Items can be equipped to specific slots (head, chest, weapon)
- Players organize or sort their inventory

**Crafting systems need drag-drop when:**
- Players place ingredients into crafting slots
- Players combine items by dragging them together
- Recipe slots require specific item types

**Implementation Checklist**
When implementing drag-drop for inventory/crafting:
- Created manipulator and callback methods for draggable UI element
- Created UI element or slots as a drop zone
- Added USS classes for dragging and drop states
- Stored item data separately from visual elements (use data binding or C# dictionaries)
- Validated drop targets before executing drop
- Added visual feedback: drag shadows, slot highlighting, hover states
