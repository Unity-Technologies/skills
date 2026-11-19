# Manual Authoring (Advanced - Not Default)
Relative paths in this file resolve to the skill directory — the folder that holds the skill's main instructions.

> ⚠ **WARNING**: Manual authoring is NOT the default approach. Only use these patterns when the user EXPLICITLY requests manual control or editor preview during development.

> 🚨 **DEFAULT APPROACH**: Use prebuild generation with `IPreprocessBuildWithReport` instead. See the Procedure section of the parent skill.

## When to Use Manual Authoring

**Manual authoring is appropriate ONLY for:**
- Hand-optimized sprite layouts where exact positioning matters
- Custom sprite ordering requirements
- Editor preview during authoring workflow
- Explicitly requested by user

**DO NOT use manual authoring when:**
- User asks to "create sprite atlas" (use prebuild)
- User asks to "optimize sprites" (use prebuild)
- User asks for automated workflow (use prebuild)
- No specific manual control requirement mentioned

## Create and Configure Master Atlas (Manual Pattern)

> 🚨 **REMINDER**: Only use this if explicitly requested. Default is prebuild with `IPreprocessBuildWithReport`.

See [scripts/createandconfiguremasteratlas.cs](../scripts/createandconfiguremasteratlas.cs).

## Create Variant Atlas (Manual Pattern)

> 🚨 **REMINDER**: Only use this if explicitly requested. Default is prebuild with `IPreprocessBuildWithReport`.

See [scripts/variantatlas.cs](../scripts/variantatlas.cs).

## Runtime Dynamic Loading

See [scripts/runtimeatlasloader.cs](../scripts/runtimeatlasloader.cs).

## Runtime Sprite Access

See [scripts/runtimespriteaccess.cs](../scripts/runtimespriteaccess.cs).
