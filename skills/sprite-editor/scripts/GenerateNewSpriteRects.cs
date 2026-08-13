using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Editor
{
    static public partial class SpriteEditorUtility
    {
        public enum AddNewSpriteMethod
        {
            DeleteAll,  // Remove all existing sprites and create new ones
            Smart,      // Update overlapping sprites, add non-overlapping ones
            Safe        // Only add sprites that don't overlap with existing ones
        }

        /// <summary>
        /// Generates new sprite rectangles from a collection of rects, handling existing sprites based on the specified method.
        /// Automatically assigns unique names using the provided name generator function.
        /// </summary>
        /// <param name="spriteDataProvider">The sprite data provider containing existing sprites.</param>
        /// <param name="rects">Collection of rectangles to create sprites from.</param>
        /// <param name="addNewSpriteMethod">Strategy for handling existing sprites.</param>
        /// <param name="nameGenerator">Function that takes an index and returns a sprite name.</param>
        /// <param name="kOverlapTolerance">Minimum overlap area ratio to consider sprites overlapping.</param>
        /// <param name="kBestFitTolerance">Maximum overlap ratio difference for best-fit matching.</param>
        /// <param name="bestFit">If true, finds the best matching existing sprite; if false, uses first match.</param>
        /// <returns>List of sprite rectangles ready to be set on the data provider.</returns>
        public static List<SpriteRect> GenerateNewSpriteRects(ISpriteEditorDataProvider spriteDataProvider, IEnumerable<Rect> rects, AddNewSpriteMethod addNewSpriteMethod, Func<int, string> nameGenerator,
            float kOverlapTolerance= 0.00001f, float kBestFitTolerance = 0.5f, bool bestFit = false)
        {
            const int k_NameFindBreakLimit = 1000000;
            var existingSpriteRects = spriteDataProvider.GetSpriteRects();
            List<SpriteRect> newRects = new List<SpriteRect>();
            HashSet<string> existingNames = new HashSet<string>();

            Func<Rect, SpriteRect> newRectLambda = (frame) =>
            {
                int nameIndex = existingNames.Count;
                var spriteName = "";
                while (nameIndex < k_NameFindBreakLimit)
                {
                    spriteName = nameGenerator(nameIndex++);
                    if (!existingNames.Contains(spriteName))
                        break;
                }

                if(nameIndex >= k_NameFindBreakLimit)
                {
                    Debug.LogError("Failed to generate unique sprite name for automatic slicing. Please check the name generator function.");
                    return null;
                }

                existingNames.Add(spriteName);
                return new SpriteRect()
                {
                    name = spriteName,
                    alignment = SpriteAlignment.Center,
                    rect = frame,
                };
            };

            Action<Rect> deleteAllSliceMethodLambda = (frame) =>
            {
                var newRect = newRectLambda(frame);
                if (newRect != null)
                {
                    newRects.Add(newRect);
                }
            };

            Action<Rect> smartSliceMethodLambda = (frame) =>
            {
                var outSprite = GetExistingOverlappingSprite(spriteDataProvider, frame, kOverlapTolerance, kBestFitTolerance, bestFit);
                if (outSprite != -1)
                {
                    var existingRect = existingSpriteRects[outSprite];
                    existingRect.rect = frame;
                    if (existingNames.Contains(existingRect.name))
                    {
                        // Handle name conflict by renaming the previous sprite
                        var conflictRect = newRects.FindIndex(x => x.name == existingRect.name);
                        if (conflictRect != -1)
                        {
                            int nameIndex = existingNames.Count;
                            var spriteName = "";
                            while (nameIndex < k_NameFindBreakLimit)
                            {
                                spriteName = nameGenerator(nameIndex++);
                                if (!existingNames.Contains(spriteName))
                                    break;
                            }
                            if(nameIndex >= k_NameFindBreakLimit)
                            {
                                Debug.LogError("Failed to generate unique sprite name for automatic slicing. Removing conflicting sprite.");
                                newRects.RemoveAt(conflictRect);
                            }
                            else
                                newRects[conflictRect].name = spriteName;
                        }
                    }
                    else
                        existingNames.Add(existingRect.name);
                    newRects.Add(existingRect);
                }
                else
                {
                    var newRect = newRectLambda(frame);
                    if (newRect != null)
                    {
                        newRects.Add(newRect);
                    }
                }
            };

            Action<Rect> safeSliceMethodLambda = (frame) =>
            {
                var outSprite = GetExistingOverlappingSprite(spriteDataProvider, frame, kOverlapTolerance, kBestFitTolerance, bestFit);
                if (outSprite == -1)
                {
                    var newRect = newRectLambda(frame);
                    if (newRect != null)
                    {
                        newRects.Add(newRect);
                    }
                }
            };

            Action<Rect> sliceMethodLambda = safeSliceMethodLambda;
            switch (addNewSpriteMethod)
            {
                case AddNewSpriteMethod.DeleteAll:
                    sliceMethodLambda = deleteAllSliceMethodLambda;
                    break;
                case AddNewSpriteMethod.Smart:
                    sliceMethodLambda = smartSliceMethodLambda;
                    break;
                case AddNewSpriteMethod.Safe:
                    // Preserve all existing sprites
                    foreach(var existingRect in existingSpriteRects)
                    {
                        existingNames.Add(existingRect.name);
                    }
                    newRects.AddRange(existingSpriteRects);
                    break;
            }

            foreach (var frame in rects)
            {
                sliceMethodLambda(frame);
            }

            return newRects;
        }

        private static int GetExistingOverlappingSprite(ISpriteEditorDataProvider dataProvider, Rect rect, float kOverlapTolerance= 0.00001f, float kBestFitTolerance = 0.5f, bool bestFit = false)
        {
            var spriteRects = dataProvider.GetSpriteRects();
            var count = spriteRects.Length;
            var bestRect = -1;
            var rectArea = rect.width * rect.height;
            if (rectArea < kOverlapTolerance)
                return bestRect;

            var bestRatio = float.MaxValue;
            var bestArea = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Rect existingRect = spriteRects[i].rect;
                if (existingRect.Overlaps(rect))
                {
                    if (bestFit)
                    {
                        var dx = Math.Min(rect.xMax, existingRect.xMax) - Math.Max(rect.xMin, existingRect.xMin);
                        var dy = Math.Min(rect.yMax, existingRect.yMax) - Math.Max(rect.yMin, existingRect.yMin);
                        var overlapArea = dx * dy;
                        var overlapRatio = Math.Abs((overlapArea / rectArea) - 1.0f);
                        var existingArea = existingRect.width * existingRect.height;
                        if (overlapRatio < bestRatio || (overlapRatio < kOverlapTolerance && existingArea < bestArea))
                        {
                            bestRatio = overlapRatio;
                            if (overlapRatio < kOverlapTolerance)
                                bestArea = existingArea;
                            bestRect = i;
                        }
                    }
                    else
                    {
                        bestRect = i;
                        break;
                    }
                }
            }
            if (bestFit && bestRatio > kBestFitTolerance)
                return -1;
            return bestRect;
        }
    }
}
