---
name: generate-editor-search-query
description: Generates Unity Search / Quick Search queries and opens the Unity Search window for read-only Unity Editor asset or scene-object lookup requests. Always use when the user asks to find, search, show, locate, filter, look up, query, or list concrete assets or scene objects in the current project or scene, even if Unity Search is not named. Covers materials, textures, prefabs, scenes, scripts, shaders, GameObjects, components, Lights, Cameras, UI objects, labels, paths, references, selected or named assets, and asset types. Also use when the user explicitly mentions Unity Search, Quick Search, Search window, open Search, or asks what Unity Search query to use. Do not use for general project overview, project structure, folder-purpose summaries, gameplay/system explanations, how-to programming questions, web search, repository text search, build logs, package installation, menu or settings search, modifying results, or non-Unity filesystem search unless the user explicitly asks to use Unity Search.
enabled: true
modes: [agent, ask]
---

Translate natural-language Unity Editor search requests into useful Unity Search queries, explain them briefly, and open the Unity Search window with the query when appropriate.

## Default Behavior

If the prompt explicitly says Unity Search, Quick Search, Search window, open Search, or asks for a Unity Search query, handle the search request with this skill even when the target belongs to another domain such as lighting, UI, physics, audio, or animation.

For requests such as "find", "search", "locate", "list", "filter", "look up", "where is", "which assets use", or "what references" concrete Unity assets or scene objects:

1. Determine whether the request is primarily about project assets, scene objects, or both.
2. Build one concise Unity Search query.
3. Show the query in the response before or while opening Search.
4. Open Unity Search by running the Editor-side snippet unless the user explicitly asks for query text only.
5. If opening Search fails, return the query and tell the user to paste it into Unity Search manually.

Do not open Search when the user says "do not open Search", "query only", "what query should I use", "just give me the query", or similar.

If the user asks for a general explanation, project overview, folder-structure summary, architecture walkthrough, gameplay-system summary, or "how do I" programming answer, do not use this skill unless the prompt explicitly asks for Unity Search, a Search query, or opening the Search window.

If the user refers to "this", "selected", or "current" without an attached asset, scene object, visible name, or path, ask for the name/path instead of inventing one.

## Scope

Scope read-only Unity Search / Quick Search queries to:

- project assets such as materials, textures, prefabs, scenes, scripts, shaders, audio clips, sprites, meshes, models, animations, fonts, render textures, and ScriptableObject assets
- scene objects and components such as Cameras, Lights, Rigidbodies, Colliders, Renderers, Canvas objects, UI components, ParticleSystems, AudioSources, Animators, Terrain objects, and named GameObjects
- path, label, type, filename, keyword, and reference-oriented asset searches
- selected or named assets when the name/path is available from the conversation or attachment

Do not install packages, run menu commands, edit assets, modify scenes, search external documentation, search repository text, inspect build logs, delete results, fix search results, or perform dependency graph analysis. If the user asks to act on results, first open Search or provide the query, then ask for confirmation before any separate modifying skill or workflow.

## References

Before generating non-trivial queries, read [references/query-patterns.md](references/query-patterns.md).

Before opening the Search window, read [references/open-search-window.md](references/open-search-window.md).

Official English references:

- [Unity Search](https://docs.unity3d.com/Manual/search-overview.html)
- [Search expressions](https://docs.unity3d.com/Manual/search-expressions.html)
- [SearchService.ShowWindow](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Search.SearchService.ShowWindow.html)

## Passing C# to `eval`

`eval` compiles a **statement block, not a file**. Two consequences, both compile errors:

- **No `using` directives.** The compiler reads `using UnityEditor;` as a resource-disposal
  statement and rejects it (`CS0210`).
- **Types must be fully qualified.** A bare `SearchService` does not resolve (`CS0246`), and a
  bare `Object` is ambiguous with `object` (`CS0104`).

The `unity-cli` skill owns the prerequisites — installing the CLI, confirming a connected Editor,
adding the project's `com.unity.pipeline` package, and discovering the command catalog. You need
`eval` in particular; if it is absent, generate the query text and say the window can't be opened.

## Query Generation Rules

Use the simplest query that is likely to produce the requested result.

- Prefer type filters for asset and component requests, such as `t:material`, `t:texture`, `t:prefab`, `t:scene`, `t:script`, `t:shader`, `t:Light`, or `t:Rigidbody`.
- Preserve user-provided names and keywords as plain query terms unless they need quoting.
- Use `dir:` when the user gives a folder or says "in Assets/..." or "under ...".
- Use `l:` when the user asks for a Unity asset label.
- Use `ref=` when the user asks what references, uses, depends on, contains, or is connected to an asset.
- Use Search expressions only when they clearly improve the query, such as `t:prefab ref={t:texture}` or `t:scene ref={t:prefab}`.
- For selected/current wording, use the known selected asset name or path only if it is present in the chat context; otherwise ask for it.
- For broad relationship requests such as "where is Rigidbody used", use a simple keyword or component query first, then mention that a scripted audit is separate if the user needs exhaustive code/property analysis.
- For requests that mix Search with repair work, generate and open the query first; do not modify results unless the user explicitly confirms a separate follow-up action.
- Do not invent unsupported filters for uncertain requests. If a request cannot be expressed reliably with Unity Search syntax, open the closest safe query and state the limitation.

## Opening Unity Search

Run C# in the Editor only to open the Search window. This is an Editor UI action and must not change project assets or scene contents.

When opening Search:

1. Escape the query safely in the generated C# string.
2. Prefer asset and scene providers for this v1 skill.
3. Fall back to active providers if a provider is unavailable.
4. Log the query that was opened.
5. Never claim Search opened if the command failed.

## Response Format

Keep the user-facing response short:

```text
Query: `t:material`
Opened Unity Search with that query.
```

If not opening Search:

```text
Query: `t:prefab ref={t:texture}`
Paste this into Unity Search.
```

If the request is ambiguous but still searchable, choose the most likely query and mention the assumption. Ask a question only when the search target cannot be inferred, such as "find the thing" with no asset, scene, type, name, or context.

## Validation Checklist

Before reporting success:

- the generated query is shown to the user
- the query targets assets, scene objects, or both
- Search was opened only when the user did not opt out
- any fallback or uncertainty is stated plainly
- no asset, scene, package, project setting, or search result was modified
