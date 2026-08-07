# Common USS/UXML Issues

## Transitions on :hover

Transitions must be defined on the base class, not the hover state:

```uss
/* WRONG - won't animate on hover-out */
.button:hover {
  background-color: blue;
  transition-duration: 0.2s;
}

/* CORRECT */
.button {
  background-color: white;
  transition-duration: 0.2s;
}
.button:hover {
  background-color: blue;
}
```

## Hardcoded Percentage Widths

Avoid hardcoded percentages for flexible layouts:

```uss
/* AVOID */
.column {
  width: 33%;
}

/* PREFER */
.column {
  flex-grow: 1;
}
```

## Unclosed Brackets

Always ensure brackets are properly closed:

```uss
/* WRONG - missing closing bracket */
.panel {
  padding: 16px;

.button {
  color: white;
}

/* CORRECT */
.panel {
  padding: 16px;
}

.button {
  color: white;
}
```

## Referencing Unity Theme

**Do NOT reference `UnityDefaultRuntimeTheme.tss`** or built-in theme icons.

Create custom styles or reuse project assets instead.

## Performance Issues

- **Inline styles** cause per-element memory overhead
- **`:hover` on parents** with many children invalidates entire hierarchies
- **Many classes per element** decreases selector performance linearly
- **Large hierarchies** are the main performance factor
