---
name: validate-urp-render-graph-renderer-feature
description: Reviews a Unity 6 URP ScriptableRendererFeature that uses the Render Graph API, checking resource wiring, material binding, execution structure, and best practices. Use when asked to review a URP Render Graph renderer feature.
---
# Skill: Validate a Unity URP Render Graph Renderer Feature

## Purpose
Review a Unity 6+ URP Render Graph `ScriptableRendererFeature` and its associated pass implementation for correctness issues related to material binding, resource wiring, render graph texture creation, static render function structure, global resource exposure, and API/version usage.

## Important
- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Run C# with `unity command script eval --code '<snippet>'`. Don't use `using` directives or unqualified types in the snippet, because the compiler only reads `using UnityEngine`, and a bare `AssetDatabase` or `Object` causes errors (CS0246 / CS0103 / CS0104). `return` what you want to read; it arrives at `data.result.result`.
- Before making changes, establish a baseline of the current behavior so you can tell whether a later result is actually caused by your change.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask for. See [Final step](#final-step).
- Avoid long explanations.
- This is a review. Don't edit the user's files unless they ask you to apply the fixes, and create the restore point just before your first edit.

## Full topic map
Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

If the Editor install folder has a `Data/Documentation` folder (eg `...Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), read the html files from the `en/Manual` folder.

Otherwise the docs pages are online as markdown at `https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/customizing-urp/`. Prefer `WebFetch` over `WebSearch` — faster and lands on the exact reference. Only fetch what you need. Replace `<VERSION>` with the following before fetching:

- **docs.unity3d.com**: The Unity version, eg `6000.6`
- **Package on docs.unity3d.com**: The package version, eg `@6.6` or `@2.0`. Check the package version if unsure.
- **docs.unity.com**: The Unity version, eg `6000.0`

For these Engine manual pages, `<VERSION>` is the Unity version. Get it from `ProjectSettings/ProjectVersion.txt`, using only the major and minor parts. For example, get `6000.0` from `6000.0.58f1`.

Local file names below are relative to `en/Manual/urp/`.

- [Write a render pass using the render graph system in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/customizing-urp/render-graph/write-render-pass.md) — local `render-graph-write-render-pass.html`: pass setup, `PassData`, and the render function.
- [Blit using the render graph system in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/customizing-urp/render-graph/blit.md) — local `render-graph-blit.html`: `AddBlitPass` and blit passes.
- [Create a temporary texture for a single frame in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/customizing-urp/render-graph/working-with-textures/create-a-texture.md) — local `render-graph-create-a-texture.html`: creating a texture from a graph-derived descriptor.
- [Create a texture as a global texture in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/customizing-urp/render-graph/working-with-textures/pass-textures-between-passes/create-global-texture.md) — local `render-graph-create-global-texture.html`: making a texture global with `SetGlobalTextureAfterPass`.
- [Inject a render pass with a Scriptable Renderer Feature in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/customizing-urp/inject-a-render-pass/inject-a-pass-using-a-scriptable-renderer-feature.md) — local `renderer-features/scriptable-renderer-features/inject-a-pass-using-a-scriptable-renderer-feature.html`: adding a pass from a `ScriptableRendererFeature`.

### API reference

Before relying on a specific URP, Render Graph, or Engine class or method, follow the lookup steps in [API reference](references/api-reference.md) for the project's installed versions.

## Procedure
1. Get the Unity version from `ProjectSettings/ProjectVersion.txt` and the URP version from `Packages/packages-lock.json` (or `Packages/manifest.json` if there's no lock file), or from the user. For each value you can't find, assume Unity 6000.0 or URP 17.0, name the assumption in your report, and carry on.
2. Read the code the user gave you: the `ScriptableRendererFeature`, its pass, and any shader the pass binds. If there's no code, ask for it.
3. Read the documentation pages that cover the APIs the code uses.
4. Run sections 1–8 of the [Validation Checklist](#validation-checklist) against the code.
5. Report back in the [Output Format](#output-format).
6. If the user asks you to apply the fixes, apply them, run the [Final step](#final-step), then tell the user what you changed and whether the checks passed.

## When to Use
Use this skill when:
- reviewing AI-generated Unity URP Render Graph renderer feature code
- validating a custom `ScriptableRendererFeature` before integrating it
- checking for common Render Graph resource wiring and pass setup mistakes
- diagnosing suspicious but plausible Render Graph pass implementations
- reviewing whether a custom raster/blit/copy pass uses the most appropriate Render Graph helper APIs

## Inputs
Expect:
- **Unity version**
- **URP version**
- **Render Graph renderer feature code to review**
  - `ScriptableRendererFeature`
  - associated pass code
  - or both
- **Intended behavior**
- **Expected inputs/outputs**, if known
- **Optional constraints**
  - project conventions
  - required pass type
  - known resources
  - required material/shader properties

## Output Format
Return results in the following structure:

### 1. Validation Summary
A short overall assessment of the implementation.

### 2. Confirmed Issues
Concrete issues directly supported by the provided code.

### 3. Likely Issues / Risky Assumptions
Potential issues that depend on missing context or incomplete information.

### 4. Recommended Fixes
Minimal targeted fixes for each issue.

### 5. Corrected Snippets
Small corrected code snippets where useful.

### 6. Missing Information
Any information needed to validate the code with higher confidence.

## Validation Checklist

### 1. Material Binding
Check that:
- materials are declared clearly
- serialized materials are exposed correctly when needed
- materials are passed into the pass correctly
- null handling exists where required
- declared materials are actually used
- **all required material inputs are bound before execution**
- **the primary input texture is explicitly bound when the shader expects one**, because otherwise the shader samples a default or stale texture without any error
- auxiliary textures and parameter textures are also bound explicitly
- texture/property binding matches the shader’s expected property names, because a misspelled name sets a property the shader never reads

Flag as an issue when:
- a material is declared or passed but not actually used
- only secondary textures or parameters are bound while the main input texture is omitted
- the pass binds a mask/noise/auxiliary texture but fails to bind the primary color/input texture
- required shader properties are assumed to exist without being set
- property names are inconsistent or ambiguous
- the code relies on implicit main texture binding when explicit binding is required by the pass pattern

Prefer patterns where:
1. the material is created or assigned clearly
2. the primary source texture is bound explicitly
3. all auxiliary textures are bound explicitly
4. property names are consistent and intentional
5. null or missing-resource cases are handled or reported

### 2. Texture Resource Wiring
Check that:
- sampled/read textures, write targets, and auxiliary textures are clearly distinguished
- textures used with `UseTexture(...)` are appropriate for read access
- textures used with `SetRenderAttachment(...)` are appropriate as write targets, because Render Graph uses these read/write declarations to order passes and allocate resources
- source, destination, and auxiliary resources are not confused
- all expected textures are explicitly wired, because Render Graph throws an error when a render function accesses a texture the pass didn't declare, and it can't track dependencies or lifetimes for it.
- texture property names are explicit and consistent when materials are involved
- the implementation does not invent texture availability
- multi-texture resource usage is handled explicitly rather than implicitly

Flag as an issue when:
- a texture intended as an input/read resource is instead used as a write target without justification
- a destination/write target is confused with a sampled input
- auxiliary textures are used without being clearly sourced or wired
- a required texture resource is missing from the pass setup
- read/write resource roles are ambiguous or inconsistent

### 3. Execution Structure / Static Render Function
Check that:
- the render function is declared as `static`, because a static function can't capture instance state, so it reads only the `PassData` recorded for this pass
- the execution structure matches the target pass type and API style
- pass data is wired correctly into execution
- resources are accessed in the correct stage
- execution logic is consistent with the intended render pass behavior
- every `PassData` field used by the render function is explicitly assigned during pass setup for the current recording
- the implementation does not rely on default values or previously assigned `PassData` state, because Render Graph pools `PassData` objects and reuses them across passes and frames
- resource handles stored in `PassData` are assigned fresh for the current frame/pass recording and are not left dangling from prior usage

Flag as an issue when:
- the render function is not `static`
- the implementation uses an instance method where the API pattern expects a static render function
- pass data or required resources are accessed through instance state instead of the pass data/context provided to the static function
- execution flow does not match the expected render graph or pass execution pattern
- a `PassData` field is read in the render function but is not clearly assigned during pass setup
- only some `PassData` fields are reassigned while others may retain stale values from previous pooled usage
- a resource handle stored in `PassData` may survive from a previous frame or pass due to incomplete reassignment
- the implementation risks using a dangling or stale handle because pass data is not fully initialized each time it is recorded

Prefer:
- explicitly assigning every `PassData` field used by the pass during each `RecordRenderGraph(...)` call
- treating `PassData` as transient per-recording data, not persistent state
- avoiding partial initialization of pooled pass data objects

### 4. Render Graph Descriptor Validation
Check that:
- the implementation does **not** create a new `TextureDesc` by default when an appropriate graph-derived descriptor can be used directly
- when a texture should match the active render target, the descriptor is sourced from the relevant render graph resource first, such as:
  - `resourceData.activeColorTexture.GetDescriptor(renderGraph)`
  - or another appropriate existing graph-backed resource
- only the fields that actually need to differ are modified after sourcing the descriptor
- descriptor fields such as name, depth bits, format, and MSAA are intentionally preserved or intentionally overridden
- any manual reconstruction of descriptor data is justified by a specific requirement

Flag as an issue when:
- the code creates a fresh `TextureDesc` without first attempting to reuse a graph-derived descriptor
- width and height are manually copied into a new descriptor structure by default
- `cameraTargetDescriptor` is used as the primary source when a render-graph-derived descriptor is available and more accurate
- important properties such as MSAA, graphics format, or compatibility with the active render target are dropped accidentally
- descriptor reconstruction is used as a convenience shortcut rather than a necessary divergence from the source resource

Preferred rule:
- **Do not create a new `TextureDesc` unless a graph-derived descriptor cannot be used directly or the texture must intentionally diverge from the source resource.**

Preferred pattern:
1. get the descriptor from the relevant render graph resource
2. modify only the fields that must change
3. create the texture from that derived descriptor whenever possible

Examples:

#### Preferred
```csharp
RenderTextureDescriptor desc = resourceData.activeColorTexture.GetDescriptor(renderGraph);
desc.depthBufferBits = 0;
desc.name = "New name";
// other desired parameters
passData.targetTexture = renderGraph.CreateTexture(desc);
```

### 5. Manual Copy Pass Simplification
Flag a custom raster pass that only copies one texture to another with no material or custom logic. Read the checks in [references/pass-simplification.md](references/pass-simplification.md#5-manual-copy-pass-simplification), because the right helper (`AddBlitPass` or `AddCopyPass`) depends on the target API and platform.

### 6. Manual Blit Pass Simplification
Flag a custom raster pass that only does a fullscreen material blit from one source to one destination. Read the checks in [references/pass-simplification.md](references/pass-simplification.md#6-manual-blit-pass-simplification), because `renderGraph.AddBlitPass(...)` usually expresses it more directly.

### 7. Global Resource Exposure
Check that:
- textures and buffers are not exposed globally unless explicitly requested or clearly required by a downstream consumer
- when global exposure is required in a render graph pass, the implementation uses the appropriate render graph publication mechanism
- direct command buffer global state mutation is not used as a substitute for render graph resource publication
- global exposure is not extending resource lifetime unnecessarily or reducing aliasing opportunities without justification

Flag as an issue when:
- `SetGlobalTextureAfterPass` is used without a clear consumer
- `context.cmd.SetGlobalTexture(...)` is used inside a render graph pass where render graph resource publication is the appropriate mechanism
- global exposure is used as a convenience shortcut instead of explicit pass-to-pass wiring
- hidden coupling is introduced unnecessarily
- a globally exposed texture may be kept alive longer than necessary due to downstream `UseGlobalTexture(...)` or `UseAllGlobalTextures()` usage, increasing memory pressure or reducing aliasing opportunities

Prefer:
- no global exposure by default
- explicit resource wiring where possible
- `builder.SetGlobalTextureAfterPass(...)` only when global publication is truly required in render graph
- resource lifetimes that remain as local and short-lived as possible

### 8. Renderer Feature Input Declaration
Check that:
- the `ScriptableRendererFeature` and its associated pass declare required pipeline inputs using `ConfigureInput(...)` when needed
- the requested input flags match the feature’s intended behavior and visible resource usage
- the pass does not rely on pipeline-provided inputs without declaring them when required by the target API pattern
- unnecessary input requests are not declared by default, especially when they may introduce extra copies, intermediate resources, or avoidable pipeline work

Flag as an issue when:
- the feature’s pass uses or is clearly intended to use a pipeline-provided input but does not declare it with `ConfigureInput(...)`
- `ConfigureInput(...)` requests inputs that the feature/pass does not appear to use
- the declared input flags do not match the intended effect behavior
- the effect description, code, and declared inputs imply conflicting requirements
- unnecessary declared inputs may force extra copies, extra pass work, or other avoidable performance costs

Classification guidance:
- mark as a **confirmed issue** when the code clearly shows a required input is used but not declared
- mark as a **likely issue** when the intended effect implies a required input but the visible code does not fully prove shader/resource usage
- treat unnecessary input declarations as higher severity when they are likely to introduce additional copies or other measurable runtime cost

## Guardrails
Always:
- avoid inventing unsupported APIs, because a fix that calls an API that doesn't exist won't compile
- distinguish **confirmed** issues from **likely** issues, because likely issues depend on code or context you haven't seen
- prefer minimal targeted fixes over broad rewrites, because rewrites are harder to review and can break behavior the user relies on
- explain why each issue matters, so the user can judge whether to fix it
- flag hidden coupling and unnecessary global state, because they keep resources alive longer and make passes harder to change
- state when the provided code is insufficient for full certainty, so the user knows what to send next

## Non-Goals
- guarantee runtime correctness
- rewrite the entire renderer feature unless necessary
- validate unrelated gameplay logic
- validate shader internals unless directly relevant to pass wiring or binding

## Final step
Do the following checks:

1. Check the project has zero console errors. Use `console status` for counts.
2. Stop there unless the user asked you to prove it works or you suspect a specific fault.
3. If you do enter Play mode, do it once only to chase a fault you already suspect. Read the world through `script eval` rather than adding logging. Take no screenshots as they take too much time.

If a check fails, go back and reread the [docs pages](#full-topic-map) to find out what you missed.

## Final report
Give the user a concise checklist of what you did and why. If you applied fixes, list the files you changed and any side effects, such as the restore point you created. List any follow-up steps or decisions the user needs, such as the [Missing Information](#6-missing-information) you still need.
