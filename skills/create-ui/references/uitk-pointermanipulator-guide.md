# Drag and drop with pointer manipulators

Replace `<VERSION>` in these URLs with the project's Editor version, for example `6000.7`. Read them before writing a manipulator. They cover the `PointerManipulator` base class, attaching it with `AddManipulator`, pointer capture, and the PointerDown, PointerMove, PointerUp, and PointerCaptureOut lifecycle:

- [Create a drag-and-drop UI](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-events/uie-event-examples/uie-create-drag-and-drop-ui.md) (an Editor window example; the manipulator works the same at runtime)
- [Move elements at runtime](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-support-for-runtime-ui/uie-runtime-examples/uie-move-elements-at-runtime.md)
- [Manipulators](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-events/uie-manipulators.md)
- [Capture the pointer](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-events/uie-capture-the-pointer.md)

This file adds what inventory and crafting screens need on top of those pages.

## Moving the dragged item

1. On PointerDown, capture the pointer, store the pointer position, set `usageHints = UsageHints.DynamicTransform`, and call `BringToFront()` so the item draws above its siblings.
2. On PointerMove, set `style.translate = new Translate(delta.x, delta.y)`, where `delta` is the pointer movement since PointerDown. `translate` is an offset from the element's layout position in its parent's space, not a world position, and changing it skips the layout pass, so the other slots stay still.
3. If the item must draw above other slots (other parents), move it into a top-level drag layer first. Convert its current `worldBound` position with the layer's `WorldToLocal`, set `left` and `top` from the result, and only then set `position: absolute`. Switching to absolute without `left` and `top` makes it jump to the layer's top-left corner.
4. Find the slot under the pointer by testing each slot's `worldBound.Contains(evt.position)`. The dragged item and its children sit on top of the slot, so `panel.Pick` returns them instead.
5. On PointerUp, drop into a valid slot or animate back to the start position. Also finish the drop on PointerCaptureOut, because capture can end without a PointerUp (a window switch, for example) and the item would stay stuck mid-drag.
6. Call `evt.StopPropagation()` in the pointer callbacks so the item doesn't also act as a button.

Show drag state with USS classes rather than inline styles, so the look stays in the stylesheet:

```csharp
target.AddToClassList("dragging");
slot.AddToClassList("drop-target");
```

## Inventory and crafting defaults

When the user asks for an inventory, equipment, or crafting screen, build these unless the request describes a static display:

- Items drag between slots: inventory, equipment, and hotbar.
- Items snap to grid positions or equipment slots, and equipment slots accept only matching item types (head, chest, weapon).
- Stackable items show a quantity.
- Crafting slots accept ingredients, and recipe slots accept only the item types the recipe uses.

Checklist:

- A manipulator class with its four pointer callbacks, attached to each item
- Slots that act as drop targets, each with a rule for what it accepts
- USS classes for the dragging, valid-target, and invalid-target states
- Item data stored separately from the visual elements (a C# list or dictionary, or data binding), so moving an element and moving an item are the same operation
- Visual feedback: a drag shadow, slot highlighting, and hover states
