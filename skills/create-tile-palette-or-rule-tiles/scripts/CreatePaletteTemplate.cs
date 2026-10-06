// Code template for creating Tile Palettes using GridPaletteUtility.CreateNewPalette
// Reference: Packages/com.unity.2d.tilemap/Editor/GridPaletteUtility.cs

using UnityEngine;
using UnityEditor.Tilemaps;

namespace TilePaletteCreation
{
    public static class CreatePaletteTemplate
    {
        // CreateNewPalette signature:
        // public static GameObject CreateNewPalette(
        //     string folderPath,
        //     string name,
        //     GridLayout.CellLayout layout,
        //     GridPalette.CellSizing cellSizing,
        //     Vector3 cellSize,
        //     GridLayout.CellSwizzle swizzle,
        //     TransparencySortMode sortMode,
        //     Vector3 sortAxis)

        /// <summary>
        /// Creates a rectangular tile palette with automatic cell sizing.
        /// </summary>
        public static GameObject CreateRectangularPalette(string folderPath, string paletteName)
        {
            return GridPaletteUtility.CreateNewPalette(
                folderPath,
                paletteName,
                GridLayout.CellLayout.Rectangle,
                GridPalette.CellSizing.Automatic,
                new Vector3(1f, 1f, 0f),
                GridLayout.CellSwizzle.XYZ,
                TransparencySortMode.Default,
                new Vector3(0f, 0f, 1f));
        }

        /// <summary>
        /// Creates a hexagonal tile palette (point-top orientation).
        /// Uses the standard hexagonal cell size.
        /// </summary>
        public static GameObject CreateHexagonalPalette(string folderPath, string paletteName)
        {
            return GridPaletteUtility.CreateNewPalette(
                folderPath,
                paletteName,
                GridLayout.CellLayout.Hexagon,
                GridPalette.CellSizing.Manual,
                new Vector3(0.8659766f, 1f, 1f),
                GridLayout.CellSwizzle.XYZ,
                TransparencySortMode.Default,
                new Vector3(0f, 0f, 1f));
        }

        /// <summary>
        /// Creates an isometric tile palette with custom sort axis.
        /// </summary>
        public static GameObject CreateIsometricPalette(string folderPath, string paletteName)
        {
            return GridPaletteUtility.CreateNewPalette(
                folderPath,
                paletteName,
                GridLayout.CellLayout.Isometric,
                GridPalette.CellSizing.Manual,
                new Vector3(1f, 0.5f, 1f),
                GridLayout.CellSwizzle.XYZ,
                TransparencySortMode.CustomAxis,
                new Vector3(0f, 0f, 1f));
        }

        /// <summary>
        /// Creates an isometric Z as Y tile palette.
        /// Used when Z position should affect Y sorting.
        /// </summary>
        public static GameObject CreateIsometricZAsYPalette(string folderPath, string paletteName)
        {
            return GridPaletteUtility.CreateNewPalette(
                folderPath,
                paletteName,
                GridLayout.CellLayout.IsometricZAsY,
                GridPalette.CellSizing.Manual,
                new Vector3(1f, 0.5f, 1f),
                GridLayout.CellSwizzle.XYZ,
                TransparencySortMode.CustomAxis,
                new Vector3(0f, 0f, 1f));
        }

        /// <summary>
        /// Creates a tile palette with fully customizable parameters.
        /// </summary>
        /// <param name="folderPath">Project-relative folder path (e.g., "Assets/Palettes")</param>
        /// <param name="paletteName">Name of the palette asset</param>
        /// <param name="layout">Grid cell layout type</param>
        /// <param name="cellSizing">Automatic or Manual cell sizing</param>
        /// <param name="cellSize">Size of each cell in the grid</param>
        /// <param name="swizzle">Cell coordinate swizzle</param>
        /// <param name="sortMode">Transparency sort mode for rendering</param>
        /// <param name="sortAxis">Custom sort axis (used when sortMode is CustomAxis)</param>
        /// <returns>The created palette GameObject, or null if creation failed</returns>
        public static GameObject CreateCustomPalette(
            string folderPath,
            string paletteName,
            GridLayout.CellLayout layout,
            GridPalette.CellSizing cellSizing,
            Vector3 cellSize,
            GridLayout.CellSwizzle swizzle,
            TransparencySortMode sortMode,
            Vector3 sortAxis)
        {
            return GridPaletteUtility.CreateNewPalette(
                folderPath,
                paletteName,
                layout,
                cellSizing,
                cellSize,
                swizzle,
                sortMode,
                sortAxis);
        }
    }
}

// Parameter Reference:
//
// GridLayout.CellLayout:
//   - Rectangle: Standard rectangular grid
//   - Hexagon: Hexagonal grid
//   - Isometric: Isometric grid
//   - IsometricZAsY: Isometric grid where Z position affects Y sorting
//
// GridPalette.CellSizing:
//   - Automatic: Cell size determined automatically from sprites
//   - Manual: Cell size specified explicitly
//
// GridLayout.CellSwizzle:
//   - XYZ: Standard coordinate system
//   - XZY, YXZ, YZX, ZXY, ZYX: Various coordinate swizzles
//
// TransparencySortMode:
//   - Default: Use default sorting
//   - Perspective: Sort by perspective
//   - Orthographic: Sort orthographically
//   - CustomAxis: Sort along a custom axis
//
// Recommended Cell Sizes:
//   - Rectangular: (1, 1, 0) with Automatic sizing
//   - Hexagonal: (0.8659766, 1, 1)
//   - Isometric: (1, 0.5, 1)
