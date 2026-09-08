# Late-Binding via Addressables (On-Demand Loading)

This approach creates SpriteAtlases that are NOT included in the build and are instead loaded on-demand via the Addressables system.

## When to Use

- DLC content, optional features, large assets, downloadable content
- Level-specific sprites (100+ levels)
- Localized UI sprites (multiple languages)
- Character skins or cosmetics
- Any content that can be loaded asynchronously

## Prerequisites

1. Install Addressables package: `com.unity.addressables`
2. Initialize Addressables in your project (Window > Asset Management > Addressables > Groups)

## 🚨 REQUIRED: Three Scripts for Addressables Delivery

When using Addressables, you MUST create all three scripts:

1. **Prebuild Script** - Generates atlases and creates Addressables entries (IPreprocessBuildWithReport)
2. **Build Addressables Script** - Builds Addressables content as build step (IPostprocessBuildWithReport)
3. **Late-Binding Runtime Loader** - Loads atlases at runtime via SpriteAtlasManager

## Script 1: Prebuild Atlas Generator with Addressables

See [resources/spriteatlasprebuildgenerator.cs](../resources/spriteatlasprebuildgenerator.cs).

## Script 2: Build Addressables Content (REQUIRED)

This script runs AFTER atlas generation to build the Addressables content bundle.

See [resources/buildaddressablespostprocess.cs](../resources/buildaddressablespostprocess.cs).

## Script 3: Late-Binding Runtime Loader (REQUIRED)

This script handles automatic on-demand loading of atlases via SpriteAtlasManager callbacks.

See [resources/spriteatlaslatebinding.cs](../resources/spriteatlaslatebinding.cs).

## Setup Instructions

1. **Create all three scripts** in your Editor folder (Scripts 1 & 2) and runtime folder (Script 3)
2. **Add SpriteAtlasLateBinding to scene**: Attach to a persistent GameObject in your first scene
3. **Build your project**: Atlases will be generated, addressables will be built automatically
4. **Runtime**: Sprites automatically trigger atlas loading on first access

## Key Points

1. **🚨 All three scripts are REQUIRED** - Missing any will break Addressables delivery
2. **includeInBuild = false**: This is critical for Addressables delivery
3. **Automatic Addressables setup**: The prebuild script creates addressable entries automatically
4. **Build Addressables as postprocess step**: Script 2 ensures content bundles are built
5. **Late-binding via SpriteAtlasManager**: Script 3 handles on-demand loading automatically
6. **Address naming**: Uses the atlas filename as the address (e.g., "UI_Atlas")
7. **Async loading**: Runtime loading is asynchronous and automatic
8. **Resource management**: Handles automatically release all atlases on destroy
