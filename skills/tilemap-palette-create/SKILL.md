---
name: tilemap-palette-create
description: Creates a Tile Palette asset. Use when the user wants to organize tiles for 2D level design or create a new Tile Palette from scratch. The user can specify the Grid layout, eg. Rectangular, Hexagonal, Isometric.
required_packages:
  com.unity.2d.tilemap: ">=1.0.0"
---

# Tilemap Palette Creation

## Workflow

### Step 1: Parameter Gathering
WAIT for the user to specify the following parameters if not already provided:
- **Palette Name**: The name of the asset.
- **Grid Type**: Rectangular, Hexagonal, or Isometric.
- **Cell Size**: Optional (defaults based on Grid Type).
- **Sort Axis**: Required for Isometric palettes.

### Step 2: Asset Creation
Utilise `UnityEditor.Tilemaps.GridPaletteUtility.CreateNewPalette` to create the Tile Palette asset.

## Branching Logic (Grid Types)

### Path A: Rectangular
- Use **Automatic** cell sizing. Use cell size: `(1, 1, 0)` as a default.

### Path B: Hexagonal
- Use cell size: `(0.8659766, 1, 1)`.

### Path C: Isometric
- Use cell size: `(1, 0.5, 1)`.
- Use a **Custom Sort Axis** with value `(0, 0, 1)`.

### Path D: Isometric Z As Y
- Use cell size: `(1, 0.5, 1)`.
- Use a **Custom Sort Axis** with value `(0, 0, 1)`.

## Post-Creation
Ensure that there is a `GridPalette` as a sub-asset of the Tile Palette asset.

## References

Code Template: "scripts/CreatePaletteTemplate.cs"