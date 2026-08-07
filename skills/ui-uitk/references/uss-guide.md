# USS Patterns and Examples

## Table of Contents

- [Design Tokens](#design-tokens)
- [Transitions](#transitions)
- [Pseudo-State Tinting](#pseudo-state-tinting)
- [Text Wrapping](#text-wrapping)
- [9-Slice Backgrounds](#9-slice-backgrounds)
- [Child vs Descendant Selectors](#child-vs-descendant-selectors)
- [Specificity](#specificity)

## Design Tokens

Use `:root` variables for repeated values:

```uss
:root {
  --spacing-sm: 8px;
  --spacing-md: 16px;
  --spacing-lg: 24px;
  --color-primary: #4da3ff;
  --color-bg-dark: #1a1a1a;
  --color-text: #ffffff;
}

.container {
  padding: var(--spacing-md);
  background-color: var(--color-bg-dark);
  color: var(--color-text);
}

.button {
  padding: var(--spacing-sm) var(--spacing-md);
  background-color: var(--color-primary);
}
```

## Transitions

Define transition properties on the **base class**, not on `:hover`. Otherwise hover-out won't animate.

```uss
/* CORRECT */
.button {
  background-color: #4da3ff;
  transition-duration: 0.2s;
}
.button:hover {
  background-color: #6db3ff;
}

/* WRONG - transition on :hover won't animate out */
.button {
  background-color: #4da3ff;
}
.button:hover {
  background-color: #6db3ff;
  transition-duration: 0.2s;
}
```

## Pseudo-State Tinting

Prefer tinting one image instead of creating multiple image variants:

```uss
.button {
  background-image: url("project://database/Assets/UI/Textures/button-bg.png");
}

.button:hover {
  -unity-background-image-tint-color: rgba(255, 255, 255, 0.15);
}

.button:active {
  -unity-background-image-tint-color: rgba(0, 0, 0, 0.2);
}

.button:disabled {
  -unity-background-image-tint-color: rgba(128, 128, 128, 0.5);
}
```

## Text Wrapping

Labels don't wrap by default. Enable wrapping explicitly:

```uss
.description-text {
  white-space: normal;
  overflow: visible;
}
```

## 9-Slice Backgrounds

For scalable backgrounds that stretch without distorting edges:

```uss
.panel-background {
  background-image: url("project://database/Assets/UI/Textures/panel-bg.png");
  -unity-slice-left: 12;
  -unity-slice-top: 12;
  -unity-slice-right: 12;
  -unity-slice-bottom: 12;
  -unity-slice-scale: 1;
}
```

Slice values define the non-stretched border regions in pixels.

## Child vs Descendant Selectors

Prefer child selectors for performance:

```uss
/* BETTER - child selector */
.panel > .header > .title { }

/* AVOID - descendant selector (slower) */
.panel .header .title { }
```

## Specificity

More specific selectors override less specific ones. If your styles aren't applying, check for conflicting selectors:

```uss
/* Less specific */
.button { color: white; }

/* More specific - wins */
.panel .button { color: black; }

/* Even more specific - wins */
.panel > .content > .button { color: red; }
```
