# API reference

The API is `UnityEngine.Rendering.Universal` (`ScriptableRendererFeature`, `ScriptableRenderPass`, `UniversalResourceData`), part of the `com.unity.render-pipelines.universal` package, and `UnityEngine.Rendering.RenderGraphModule` (`RenderGraph`, `TextureHandle`, builders, `AddBlitPass`), part of the `com.unity.render-pipelines.core` package. Both are versioned by package version, not Unity version (e.g. `@17.0`, `@17.3`), so check the project's installed URP version before relying on a specific class or method.

1. Use the `unity-cli` skill, or `Packages/packages-lock.json`, to check the installed URP version. The core package has the same version.
2. Fetch and cache `https://docs.unity3d.com/Packages/<package-name>@<version>/xrefmap.yml` for each package (replace `<version>` with the installed package version, e.g. `17.3`). This file maps every public API member's fully qualified name to its docs page `href` (e.g. `href: api/UnityEngine.Rendering.Universal.ScriptableRendererFeature.html` => `https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.3/api/UnityEngine.Rendering.Universal.ScriptableRendererFeature.html`).
3. Grep the yml files locally for a class or method instead of refetching per lookup. Don't read the whole file.

Always fetch the version matching the installed package rather than relying on training knowledge, since APIs are frequently marked obsolete between major versions.

Engine types the code uses, such as `Material` and `RenderTextureDescriptor`, are in `UnityEngine`, versioned by Unity version:

1. If the Editor install folder has a `Data/Documentation` folder, grep the html files in `<editor-install>/Editor/Data/Documentation/en/ScriptReference/`, and skip the remaining steps.
2. Otherwise, fetch and cache `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/xrefmap.yml` (replace `<VERSION>` with the Unity version, e.g. `6000.3`). This file lists every public API member's fully qualified name as a `uid` (e.g. `uid: 'UnityEngine.Material.SetTexture(System.String,UnityEngine.Texture)'`).
3. Grep the yml file locally for a class or method instead of refetching per lookup. The file is large (about 6 MB), so don't read the whole file.
4. Convert the `uid` to a markdown page: remove the parameters in brackets, lowercase it, replace `.` with `/`, and add `.md`. Then fetch `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/<converted uid>`. For example, `UnityEngine.Material.SetTexture(System.String,UnityEngine.Texture)` => `https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/material/settexture.md`. All overloads are on the same page.
   - Constructors use `ctor`, and indexers use `item`. For operators, fetch the class page and follow its link.
   - Links inside the pages start with `/engine/`. Add `https://docs.unity.com/en-us` before them.
   - If the page doesn't exist for that Unity version, use the `href` in the `xrefmap.yml` entry instead (e.g. `href: 'RenderTextureDescriptor.html'` => `https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RenderTextureDescriptor.html`).
