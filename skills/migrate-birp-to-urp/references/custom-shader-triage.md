# Custom Shader Triage

Use this triage for shader and material cases that the Render Pipeline Converter cannot solve automatically.

## Table of Contents

- [Official Reference](#official-reference)
- [Ground Truth](#ground-truth)
- [Triage Workflow](#triage-workflow)
- [Classification Guide](#classification-guide)
- [Manual Conversion Guidance](#manual-conversion-guidance)
- [Shader Graph Option](#shader-graph-option)
- [Validation Checklist](#validation-checklist)
- [Never Rules](#never-rules)

## Official Reference

- [Upgrade custom shaders for URP compatibility](https://docs.unity3d.com/Manual/urp/urp-shaders/birp-urp-custom-shader-upgrade-guide.html)
- [Upgrade material assets to URP or HDRP](https://docs.unity3d.com/Manual/upgrade-material.html)
- [Render pipeline feature comparison](https://docs.unity3d.com/Manual/render-pipelines-feature-comparison.html)

## Ground Truth

- Built-in custom shaders are not automatically upgraded by the Render Pipeline Converter.
- After URP migration, materials that still rely on unsupported shaders often appear magenta.
- The safe response is triage first, not blind bulk rewriting.
- Hand-written ShaderLab shaders can work in URP, but they often need deliberate porting.
- Shader Graph is often safer than hand-porting when the original visual behavior is conceptually simple.

## Triage Workflow

1. Identify which materials are magenta or failing.
2. Find the shader asset each failing material uses.
3. Determine how widely each shader is used before changing it.
4. Inspect Console and Inspector errors before editing shader code.
5. Separate the cases into:
   - Unity built-in shaders the converter should have handled
   - particle, VFX, fog, smoke, steam, decal, or transparent effect materials that need URP particle/effect shader mapping
   - simple custom shaders that may be rewritten safely
   - complex or package-owned shaders that need scoped manual work
6. Prefer the smallest safe fix that restores rendering for the user.
7. Validate representative materials before scaling a fix across the project.

## Classification Guide

### Lower-Risk Cases

These are better candidates for a scoped manual conversion:

- simple unlit custom shaders
- legacy particle/fog/smoke materials that can map cleanly to URP particle shaders without changing the intended blend mode
- very small single-pass shaders
- effect shaders where the visual intent is narrow and obvious
- custom shaders used by only one or two materials

### Medium-Risk Cases

These often need a representative port and careful validation:

- shaders with multiple exposed material properties
- hand-written lit shaders that expect Built-in lighting helpers
- shaders used across many materials or prefabs
- shaders that must preserve batching or performance characteristics

### Higher-Risk Cases

These should be treated as deliberate migration tasks, not quick fixes:

- surface shaders
- `GrabPass`-based shaders
- large multi-pass shaders
- package-owned shaders
- water, foliage, toon, outline, x-ray, or dissolve frameworks
- shaders tied to custom lighting models, replacement shaders, fullscreen image effects, or camera callbacks
- shaders that assume specific Built-in forward/deferred behavior
- shaders with platform-specific or XR-specific paths
- vegetation shaders that rely on cutout alpha, two-sided leaf cards, billboards, terrain detail rendering, wind animation, or SpeedTree-style behavior

For advanced cases, continue with [complex-shader-situations.md](complex-shader-situations.md).

## Manual Conversion Guidance

The official URP guide shows a typical direction for simple shader rewrites:

- move from `CGPROGRAM` to `HLSLPROGRAM`
- use URP shader library includes such as `Core.hlsl`
- add the URP render pipeline tag
- rewrite incompatible Built-in pipeline code instead of expecting compatibility

Do not assume this is enough for lit, multi-pass, or effect-heavy shaders.

Do not generalize one sample shader rewrite across an entire project.

## Shader Graph Option

If the original shader behavior is conceptually simple, recreating it in Shader Graph can be safer than hand-porting a complex Built-in shader.

Good Shader Graph candidates include:

- simple unlit or fresnel-based materials
- color-mask, dissolve, or rim-light effects
- sprite and VFX-style materials with limited lighting requirements
- simple distortion or scene-color-driven effects that have a URP-compatible replacement path

## Validation Checklist

After triaging or porting a shader:

1. Validate at least one representative material in-scene.
2. Check Console and Inspector for shader warnings and errors.
3. Check whether the shader still supports the required rendering path and passes.
4. Check whether batching or performance changed significantly.
5. For particle and VFX materials, confirm blend mode, softness, alpha clipping, and sorting still look correct.
6. For foliage and vegetation materials, confirm grass/tree cards are not solid opaque quads, cutout thresholds are preserved, leaves render from the expected sides, texture/tint/normal data survived, and wind or billboard behavior is either still working or explicitly listed as manual follow-up.
7. Only then consider applying the same approach to more materials.

## Never Rules

- NEVER claim that Render Pipeline Converter handles custom shaders automatically.
- NEVER bulk search-and-replace shader code across a project without a verified mapping.
- NEVER promise perfect visual parity after a shader port without validation.
- NEVER continue editing shader files when Console errors show the approach is diverging. Stop, summarize the blocker, and propose the next safe step instead.
- NEVER treat package-owned shaders as safe to rewrite blindly. Check whether the package already has URP support first.
