# ScrollView Setup

ScrollRect requires a specific hierarchy to function correctly. Missing or misordered components cause jitter, duplication, or non-scrollable content.

**Required hierarchy:**
```
ScrollView (ScrollRect)
├── Viewport (RectTransform + Mask + Image)
│   └── Content (RectTransform + VerticalLayoutGroup + ContentSizeFitter)
│       └── [Children — static or added at runtime]
└── Scrollbar (optional)
```

**Setup rules:**
- **ScrollRect** component goes on the root ScrollView object
- **Viewport** must have a `Mask` component and an `Image` component (Mask requires Image)
- **Content** must have `ContentSizeFitter` with Vertical Fit set to "Preferred Size" so the scroll area grows with children
- ScrollRect references: assign Content to `Content`, Viewport to `Viewport`, and optionally assign a Scrollbar
- Content can contain static children (settings lists, about pages) or be populated dynamically at runtime

**Dynamic population (runtime items):**
- **Clear existing children** before populating to prevent duplication on panel re-open
- Instantiate item prefabs as children of the Content object
- The ContentSizeFitter + VerticalLayoutGroup on Content will automatically size the scrollable area

**Common failures:**
- Items duplicated on every open → Content not cleared before populating
- Content not scrollable → missing ContentSizeFitter on Content, or Viewport missing Mask
- Jitter/flickering → ContentSizeFitter conflict with parent Layout Group (see "Avoiding layout conflicts" in the main skill)
