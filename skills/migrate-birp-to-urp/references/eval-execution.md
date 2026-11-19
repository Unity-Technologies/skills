# Running C# with `script eval`

### Passing C# to `script eval`

`script eval` compiles a **statement block, not a file**. Three consequences, all of which cause a compile
error rather than a warning:

- **No `using` directives.** The compiler reads `using UnityEngine;` as a resource-disposal
  statement and rejects it (`CS0210`).
- **Types must be fully qualified.** A bare `GraphicsSettings` or `Volume` does not resolve
  (`CS0246` / `CS0103`), and a bare `Object` is ambiguous with `object` (`CS0104`).
- **Extension methods are unavailable**, because they resolve through `using`. Two that this skill
  would otherwise reach for: `camera.GetUniversalAdditionalCameraData()` becomes
  `camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()`, and LINQ
  calls must be written statically — `System.Linq.Enumerable.FirstOrDefault(sequence, predicate)`
  rather than `sequence.FirstOrDefault(predicate)`.

### Two execution modes — pick by the snippet's shape

The references carry both shapes, and they are not interchangeable:

- **Statement-shaped** (a bare sequence of statements, like the detection snippets above) — pass
  straight to `script eval`, fully qualified, with no `using` lines.
- **Class- or method-shaped** (anything declaring a `class`, a `static` method, or a `[MenuItem]`,
  such as the material-snapshot pattern) — these are **project files, not `script eval` input**. A class
  declaration cannot be flattened into a statement block. Save the snippet under
  `Assets/Editor/`, let Unity compile it, then invoke its entry point through a one-line `script eval`
  call. Keep the `using` directives in that file; they are correct there.

For a multi-step migration the script route is the more reliable one anyway: it survives the domain
reloads that URP installation and material conversion trigger, whereas a long `script eval` payload does
not.

### Detecting the active render pipeline

```csharp
var rp = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
var qrp = UnityEngine.QualitySettings.renderPipeline;
return $"graphics={(rp == null ? "NULL (Built-in)" : rp.GetType().Name + ":" + rp.name)}, "
     + $"activeQualityLevel={(qrp == null ? "inherits Graphics" : qrp.GetType().Name + ":" + qrp.name)}";
```

A `UniversalRenderPipelineAsset` means URP; `HDRenderPipelineAsset` means HDRP; `NULL` on both
means Built-in. **Check the per-quality-level assignment too** — a project can be switched in
Graphics settings while a Quality level still points at a different asset, or at none:

```csharp
var names = UnityEngine.QualitySettings.names;
var rows = new System.Collections.Generic.List<string>();
for (int i = 0; i < names.Length; i++)
{
    var a = UnityEngine.QualitySettings.GetRenderPipelineAssetAt(i);
    rows.Add($"{i}:{names[i]}={(a == null ? "inherits Graphics" : a.name)}");
}
return string.Join(", ", rows);
```

If `GetRenderPipelineAssetAt` is unavailable in the project's Unity version, read
`ProjectSettings/QualitySettings.asset` instead rather than switching levels at runtime —
`SetQualityLevel` mutates project state.
