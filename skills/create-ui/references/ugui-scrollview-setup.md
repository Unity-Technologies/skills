# uGUI ScrollView setup

Read `Library/PackageCache/com.unity.ugui@*/Documentation~/script-ScrollRect.md` first. The path resolves to the Unity project folder. Build this hierarchy, because it's the one where a missing or misordered part is easiest to spot when items duplicate, jitter, or don't scroll:

```
ScrollView (ScrollRect)
├── Viewport (RectTransform + Image + Mask, or RectTransform + Image + RectMask2D)
│   └── Content (RectTransform + VerticalLayoutGroup + ContentSizeFitter)
│       └── [Children, static or added at runtime]
└── Scrollbar (optional)
```

- **ScrollRect** goes on the root object. Assign its Content and Viewport fields, and the Scrollbar if there is one.
- **Viewport** clips the content and receives the drag. Give it an Image, because the ScrollRect only gets drags over a raycast target. Add a RectMask2D for rectangular viewports, because it's cheaper than a Mask and lets the Image be fully transparent. Use a Mask only for non-rectangular shapes; it clips to the Image's shape, so keep the Image visible to the Mask and turn off Show Mask Graphic to hide it.
- **Content** stretches across the top of the Viewport (anchors top-left to top-right, pivot y = 1), so it grows downward. It has a ContentSizeFitter with Vertical Fit set to Preferred Size on the same object as its VerticalLayoutGroup, so the scroll area grows with its children.
- **Dynamic items:** destroy the existing children of Content before adding new ones, because panels that repopulate on open otherwise add a second copy each time.

| Symptom | Cause | Fix |
|---|---|---|
| Items duplicated each time the panel opens | Content isn't cleared before repopulating | Destroy Content's children before adding items |
| Content doesn't scroll | Content isn't taller than the Viewport, or the ScrollRect's Content field is empty | Add the ContentSizeFitter to Content, and assign the Content field |
| Dragging in empty space doesn't scroll | The Viewport has no Image to receive the drag | Add an Image to the Viewport |
| Items show outside the scroll area | The Viewport has no Mask or RectMask2D | Add one to the Viewport |
| Jitter or flicker | A ContentSizeFitter on an item whose parent layout group also sizes it | Remove the item's ContentSizeFitter, and size items with a Layout Element |
