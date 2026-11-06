# Tilemap Palette Creation

Requires package: `com.unity.2d.tilemap` >=1.0.0

## Workflow

### Step 1: Parameter Gathering
WAIT for the user to specify the following parameters if not already provided, because the name sets the asset file name and the grid type sets the palette's cell layout and cell size:
- **Palette Name**: The name of the asset.
- **Grid Type**: Rectangular, Hexagonal, Isometric, or Isometric Z As Y.
- **Cell Size**: Optional (defaults based on Grid Type).
- **Sort Axis**: Required for Isometric palettes.

### Step 2: Asset Creation
Utilise `UnityEditor.Tilemaps.GridPaletteUtility.CreateNewPalette` to create the Tile Palette asset.

## Branching Logic (Grid Types)

### Path A: Rectangular
- Use **Automatic** cell sizing with a `cellSize` of `(1, 1, 0)`, as `CreatePaletteTemplate.cs` does, because square tiles then fill one cell each.

### Path B: Hexagonal
- Use cell size: `(0.8659766, 1, 1)`, because a hexagon's width is about 0.866 of its height, so neighbouring hex tiles meet without gaps.

### Path C: Isometric
- Use cell size: `(1, 0.5, 1)`, because isometric tiles are diamonds twice as wide as they are tall.
- Use a **Custom Sort Axis** with value `(0, 0, 1)`, because overlapping isometric tiles must draw in a fixed order or the palette shows them in front of each other wrongly.

### Path D: Isometric Z As Y
- Use cell size: `(1, 0.5, 1)`, for the same reason as Path C.
- Use a **Custom Sort Axis** with value `(0, 0, 1)`, for the same reason as Path C.

## Post-Creation
Ensure that there is a `GridPalette` as a sub-asset of the Tile Palette asset, because the Tile Palette window can't use the palette without that sub-asset. Check with `AssetDatabase.LoadAllAssetsAtPath`.

## References

Code Template: "../scripts/CreatePaletteTemplate.cs"