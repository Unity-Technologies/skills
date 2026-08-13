---
name: validate-urp-render-graph-renderer-feature
description: Use when the user wants to review or validate a Unity 6+ URP ScriptableRendererFeature that uses the Render Graph API. Checks for correctness issues - resource wiring, material binding, execution structure, descriptor usage, global resource exposure, and Render Graph best practices.
---
# Skill: Validate a Unity URP Render Graph Renderer Feature

## Purpose
Review a Unity 6+ URP Render Graph `ScriptableRendererFeature` and its associated pass implementation for correctness issues related to material binding, resource wiring, render graph texture creation, static render function structure, global resource exposure, and API/version usage.

## When to Use
Use this skill when:
- reviewing AI-generated Unity URP Render Graph renderer feature code
- validating a custom `ScriptableRendererFeature` before integrating it
- checking for common Render Graph resource wiring and pass setup mistakes
- diagnosing suspicious but plausible Render Graph pass implementations
- reviewing whether a custom raster/blit/copy pass uses the most appropriate Render Graph helper APIs

## Inputs
The skill should expect:
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
The skill must return results in the following structure:

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
- **the primary input texture is explicitly bound when the shader expects one**
- auxiliary textures and parameter textures are also bound explicitly
- texture/property binding matches the shader’s expected property names

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
- textures used with `SetRenderAttachment(...)` are appropriate as write targets
- source, destination, and auxiliary resources are not confused
- all expected textures are explicitly wired
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
- the render function is declared as `static`
- the execution structure matches the target pass type and API style
- pass data is wired correctly into execution
- resources are accessed in the correct stage
- execution logic is consistent with the intended render pass behavior
- every `PassData` field used by the render function is explicitly assigned during pass setup for the current recording
- the implementation does not rely on default values or previously assigned `PassData` state
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
// other desired paramters
passData.targetTexture = renderGraph.CreateTexture(desc);
```

### 5. Manual Copy Pass Simplification
Check that:
- simple texture copy operations are not implemented as full custom raster passes when a built-in Render Graph helper is sufficient
- passes that only read one texture and write it unchanged to another target are simplified where appropriate
- the implementation prefers the most appropriate built-in helper for the target API/platform context
- `AddCopyPass(...)` is not recommended by default if a more compatible `AddBlitPass(...)` overload should be preferred in the current environment

Flag as an issue when:
- a raster pass exists only to copy one texture into another
- the pass uses no material and no custom processing
- the render function only performs a simple blit/copy equivalent
- the implementation uses a full custom raster pass where a built-in copy/blit helper would express the same behavior more directly

Prefer:
- the appropriate `AddBlitPass(...)` overload for straightforward copy-like operations when that is the recommended and more compatible path
- `AddCopyPass(...)` only when it is explicitly appropriate and supported for the target API/platform context
- a custom raster pass only when the copy requires additional logic or non-trivial behavior

### 6. Manual Blit Pass Simplification
Check that:
- straightforward fullscreen material blits are not implemented as custom raster passes when `renderGraph.AddBlitPass(...)` would express the same behavior more directly
- custom raster passes are only used for blits when additional logic or non-trivial behavior is actually required
- simple source-to-destination material blits use the most direct render graph helper available

Flag as an issue when:
- a raster pass reads one source texture and writes one destination texture
- the pass uses a material but no additional custom pass logic
- the render function only performs a simple fullscreen blit
- `renderGraph.AddBlitPass(...)` would provide an equivalent result more clearly

Prefer:
- `renderGraph.AddBlitPass(...)` for straightforward fullscreen material blits
- a custom raster pass only when extra logic, multiple operations, conditional behavior, or special setup is actually required

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
The skill must:
- avoid inventing unsupported APIs
- distinguish **confirmed** issues from **likely** issues
- prefer minimal targeted fixes over broad rewrites
- explain why each issue matters
- flag hidden coupling and unnecessary global state
- state when the provided code is insufficient for full certainty

## Non-Goals
- guarantee runtime correctness
- rewrite the entire renderer feature unless necessary
- validate unrelated gameplay logic
- validate shader internals unless directly relevant to pass wiring or binding

## Evaluation Criteria
A successful validation should:
- catch real wiring and API issues
- identify suspicious but plausible mistakes
- provide actionable fixes
- avoid false certainty
- improve trust in generated render pass code

## Notes for Future Expansion
As new recurring issues are discovered, extend this checklist with additional rules such as:
- pass ordering / injection point validation
- resource lifetime and cleanup checks
- read/write hazard detection
- unnecessary copies or allocations
- camera depth/color dependency validation
- multi-pass dependency validation
- override material correctness
- pass configuration
