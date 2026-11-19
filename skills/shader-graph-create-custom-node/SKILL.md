---
name: shader-graph-create-custom-node
description: Generates custom Shader Graph nodes from HLSL code by reflection. Use when asked for a new Shader Graph node, or to make existing HLSL work as a reflected function node.
required_packages:
  com.unity.shadergraph: ">=17.5.0"
---
Help the user turn an HLSL function into a custom Shader Graph node by reflection.

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Run C# with `unity command script eval --code '<snippet>'`. Don't use `using` directives or unqualified types in the snippet. The compiler reads using `UnityEngine` and a bare AssetDatabase, Volume, or Object causes errors (CS0246 / CS0103 / CS0104). `return` what you want to read; it arrives at `data.result.result`.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask for. See [Final step](#final-step).
- Avoid long explanations.

## Step 1

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

## Full topic map

The Shader Graph manual is package documentation, not part of the Unity Manual, so don't look for it in the Unity Editor's `Data/Documentation` folder.

The docs pages are local markdown conversions of the Unity Manual, stored at `<project-folder>/Library/PackageCache/com.unity.shadergraph@*/`. Read only the .md files in that specific `PackageCache` subfolder. You must not look at non-.md files, or look outside at other Library or PackageCache folders, otherwise it takes too much time. The pages are in its `Documentation~` folder. If that folder has no `.md` files, use the online pages at `https://docs.unity3d.com/Packages/com.unity.shadergraph@<VERSION>/manual/` in the following list instead, because some package versions don't ship their docs.

Prefer fetching the page directly (for example `WebFetch`) over searching (for example `WebSearch`) — faster and lands on the exact reference. Only fetch what you need, and only search if you don't have the exact URL. Replace `<VERSION>` with the following before fetching:

- **docs.unity3d.com**: The Unity version, eg `6000.6`
- **Package on docs.unity3d.com**: The package version, eg `@6.6` or `@2.0`. Check the package version if unsure.
- **Engine on docs.unity.com**: The Unity version, eg `6000.3`
- **docs.unity.com**: The product version, eg `7000.0`

Read the Shader Graph major.minor from `com.unity.shadergraph` in `Packages/packages-lock.json` (for example `17.5` from `17.5.0`), because the full version number gives a 404.

- [Introduction to HLSL in Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@<VERSION>/manual/Custom-nodes-hlsl-introduction.html) — local `Custom-nodes-hlsl-introduction.md`: Custom Function nodes compared with nodes reflected from HLSL.
- [Create a custom node by reflection from HLSL](https://docs.unity3d.com/Packages/com.unity.shadergraph@<VERSION>/manual/Custom-nodes-hlsl-create-node-by-reflection.html) — local `Custom-nodes-hlsl-create-node-by-reflection.md`: the include, `UNITY_EXPORT_REFLECTION`, and adding function and parameter hints.
- [Reflected function hints reference](https://docs.unity3d.com/Packages/com.unity.shadergraph@<VERSION>/manual/Custom-nodes-hlsl-reflection-hints-reference.html) — local `Custom-nodes-hlsl-reflection-hints-reference.md`: every `funchints` and `paramhints` tag and its values.
- [Reflected Function node](https://docs.unity3d.com/Packages/com.unity.shadergraph@<VERSION>/manual/Reflected-Function-Node.html) — local `Reflected-Function-Node.md`: the node Shader Graph creates from a reflected function.

### API reference

The API is `UnityEditor.ShaderGraph`, part of the `com.unity.shadergraph` package. It is versioned by package version, not Unity version (e.g. `@17.5`, `@17.6`), so check the project's installed package version before relying on a specific class or method.

1. Use the `unity-cli` skill to check the installed package version.
2. Fetch and cache `https://docs.unity3d.com/Packages/com.unity.shadergraph@<VERSION>/xrefmap.yml` (replace `<VERSION>` with the installed package version, e.g. `17.5`). This file maps every public API member's fully qualified name to its docs page `href` (e.g. `href: api/UnityEditor.ShaderGraph.html` => `https://docs.unity3d.com/Packages/com.unity.shadergraph@<VERSION>/api/UnityEditor.ShaderGraph.html`).
3. Grep the yml file locally for a class or method instead of refetching per lookup. Don't read the whole file.

Always fetch the version matching the installed package rather than relying on training knowledge, since APIs are frequently marked obsolete between major versions.

## Procedure

1. Get the Shader Graph version from `com.unity.shadergraph` in `Packages/packages-lock.json`. If you can't find it, assume Shader Graph 17.5, name the assumption in your report, and carry on. If there's no project at all, give the user the HLSL code and the file to save it in, and say that you didn't write the file. If the user gives no HLSL function and doesn't describe the node they want, ask for it.
2. Read the doc pages from the [Full topic map](#full-topic-map) that cover what you're about to do. If you can't reach the docs, carry on from this skill and say so in your report.
3. Follow [Step 2](#step-2-generate-an-hlsl-function-definition), [Step 3](#step-3-decorate-with-shader-graph-hint-tags) and [Step 4](#step-4-write-the-code-to-an-asset) in order.
4. Check the Console for HLSL compile errors from the `.hlsl` asset, and check that the node appears in the Shader Graph Create Node menu under its `sg:SearchCategory`. If it doesn't, compare the code with [references/all_hints.hlsl](references/all_hints.hlsl) and fix it. Then run the [Final step](#final-step).
5. Report back as described in [Final report](#final-report), including whether the Final step checks passed.

## Step 2: Generate an HLSL function definition
- Precede the function with the preprocessor define `UNITY_EXPORT_REFLECTION`

## Step 3: Decorate with Shader Graph hint tags
- Refer to [references/all_hints.hlsl](references/all_hints.hlsl) for all valid hint tags and usage patterns
- You can use C#-style documentation tags outside of `funchints` or `paramhints` blocks
- Add these required function hints:
  - `sg:ProviderKey`
  - `sg:SearchCategory`
  - `sg:SearchTerms`
  - `sg:DisplayName`

## Step 4: Write the code to an asset
- Search the project for a `ShaderInclude` asset (`.hlsl`) that already contains custom Shader Graph nodes
- If a matching asset exists, show the user its current contents and ask for confirmation before appending the new code
- If no matching asset exists, create a new `.hlsl` asset
- Ensure the file begins with `#include "ShaderApiReflectionSupport.hlsl"`

## Final step

Do the following checks:

1. Check the project has zero console errors. Use `console status` for counts.  
2. Stop there unless the user asked you to prove it works or you suspect a specific fault.  
3. If you do enter Play mode, do it once only to chase a fault you already suspect. Read the world through `script eval` rather than adding logging. Take no screenshots as they take too much time.

If a check fails, go back and reread the [docs pages](#full-topic-map) to find out what you missed.

## Final report

Give the user a concise but not dense checklist of what you did and why, including any incidental side effects (for example, settings you toggled to test something, or temporary files you created). If the user needs to follow up with more steps or decisions, list them. For example:

```
I made the following changes:

[✓] Added a reflected `BlendOverlay` function to `Assets/Shaders/CustomNodes.hlsl`.

Follow these steps to use the node:

1. Open a Shader Graph asset.
2. Open the Create Node menu and find **Blend Overlay** under **Custom/Blend**.
```
