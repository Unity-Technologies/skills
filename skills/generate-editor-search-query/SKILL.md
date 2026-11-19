---
name: generate-editor-search-query
description: Generates Unity Search queries and opens the Search window to find assets or scene objects. Use when the user asks to find, filter, or list assets, GameObjects, components, or references in the Editor, not on disk.
---

Translate natural-language Unity Editor search requests into useful Unity Search queries, explain them briefly, and open the Unity Search window with the query when appropriate.

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Avoid long explanations.

## Default Behavior

If the prompt explicitly says Unity Search, Quick Search, Search window, open Search, or asks for a Unity Search query, handle the search request with this skill even when the target belongs to another domain such as lighting, UI, physics, audio, or animation.

For requests such as "find", "search", "locate", "list", "filter", "look up", "where is", "which assets use", or "what references" concrete Unity assets or scene objects:

1. Determine whether the request is primarily about project assets, scene objects, or both.
2. Build one concise Unity Search query.
3. Show the query in the response before or while opening Search.
4. Open Unity Search by running the Editor-side snippet unless the user explicitly asks for query text only.
5. If opening Search fails, return the query and tell the user to paste it into Unity Search manually.

Do not open Search when the user says "do not open Search", "query only", "what query should I use", "just give me the query", or similar, because the user has asked for the text only.

If the user asks for a general explanation, project overview, folder-structure summary, architecture walkthrough, gameplay-system summary, or "how do I" programming answer, do not use this skill unless the prompt explicitly asks for Unity Search, a Search query, or opening the Search window, because this skill only builds queries and opens the Search window.

If the user refers to "this", "selected", or "current" without an attached asset, scene object, visible name, or path, ask for the name/path instead of inventing one, because a guessed name produces a query that finds the wrong thing.

## Scope

Scope read-only Unity Search / Quick Search queries to:

- project assets such as materials, textures, prefabs, scenes, scripts, shaders, audio clips, sprites, meshes, models, animations, fonts, render textures, and ScriptableObject assets
- scene objects and components such as Cameras, Lights, Rigidbodies, Colliders, Renderers, Canvas objects, UI components, ParticleSystems, AudioSources, Animators, Terrain objects, and named GameObjects
- path, label, type, filename, keyword, and reference-oriented asset searches
- selected or named assets when the name/path is available from the conversation or attachment

Do not install packages, run menu commands, edit assets, modify scenes, search external documentation, search repository text, inspect build logs, delete results, fix search results, or perform dependency graph analysis, because this skill is read-only and only opens the Search window. If the user asks to act on results, first open Search or provide the query, then ask for confirmation before any separate modifying skill or workflow, because acting on results changes the project.

## Full topic map

Before generating non-trivial queries, read [references/query-patterns.md](references/query-patterns.md).

Before opening the Search window, read [references/open-search-window.md](references/open-search-window.md).

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

Prefer `WebFetch` over `WebSearch` — faster and lands on the exact reference. Only fetch what you need. Replace `<VERSION>` with the following before fetching:

- **Package on docs.unity3d.com**: The package version, eg `@6.6` or `@2.0`. Check the package version if unsure.
- **docs.unity.com**: The product version, eg `7000.0`

For the Unity version, use the major and minor version from `m_EditorVersion` in `ProjectSettings/ProjectVersion.txt`, eg `6000.0` for `6000.0.47f1`, because the docs site has no pages under a full release number.

If the Editor install folder has a `Data/Documentation` folder (eg `...Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), read the html files from the `en/Manual` folder (`<editor-install>/Editor/Data/Documentation/en/Manual/<page>.html`).

Otherwise the docs pages are online at `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity-editor/editor-navigating-managing/editor-searching/`.

- **Unity Search** — local `search-overview.html`, online `search-overview.md`. Providers, filters, and opening the Search window.
  - Search expressions — local `search-expressions.html`, online `search-overview/search-textual-query/search-expressions.md`. Nested queries such as `ref={t:texture}`.

### API reference

The API is `UnityEditor.Search.SearchService` (mainly `SearchService.ShowWindow`), part of the Unity Engine. It is versioned by Unity version, so check the project's Unity version before relying on a specific class or method.

1. Use the `unity-cli` skill to check the project's Unity version.
2. If the Editor install folder has a `Data/Documentation` folder, grep the html files in `<editor-install>/Editor/Data/Documentation/en/ScriptReference` (eg `Search.SearchService.ShowWindow.html`), and skip the remaining steps.
3. Otherwise, fetch and cache `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/xrefmap.yml`. This file lists every public API member's fully qualified name as a `uid` (eg `uid: 'UnityEditor.Search.SearchService.ShowWindow(UnityEditor.Search.SearchViewState)'`).
4. Grep the yml file locally for a class or method instead of refetching per lookup. The file is large (about 6 MB), so don't read the whole file.
5. Convert the `uid` to a markdown page: remove the parameters in brackets, lowercase it, replace `.` with `/`, and add `.md`. Then fetch `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/<converted uid>`, eg `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityeditor/search/searchservice/showwindow.md`. All overloads are on the same page.
   - Links inside the pages start with `/engine/`. Add `https://docs.unity.com/en-us` before them.
   - If the page doesn't exist for that Unity version, use the `href` in the `xrefmap.yml` entry instead (eg `href: 'Search.SearchService.ShowWindow.html'` => `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/Search.SearchService.ShowWindow.html`).

Always use the docs for the project's Unity version rather than relying on training knowledge, since APIs are frequently marked obsolete between versions.

## Passing C# to `script eval`

`script eval` compiles a **statement block, not a file**, so write the code without `using` directives and with fully qualified types. If the code doesn't compile, look up the error here:

| Error | Cause | Fix |
| --- | --- | --- |
| `CS0210` | A `using` directive, such as `using UnityEditor;`, which the compiler reads as a resource-disposal statement | Remove the directive and qualify the types |
| `CS0246` | An unqualified type, such as `SearchService` | Use the full name, such as `UnityEditor.Search.SearchService` |
| `CS0104` | A bare `Object`, which is ambiguous with `object` | Use `UnityEngine.Object` |

The `unity-cli` skill owns the prerequisites — installing the CLI, confirming a connected Editor,
adding the project's `com.unity.pipeline` package, and discovering the command catalog. You need
`script eval` in particular; if it is absent, generate the query text and say the window can't be opened.

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
- Do not invent unsupported filters for uncertain requests, because an unsupported filter returns no results or the wrong results. If a request cannot be expressed reliably with Unity Search syntax, open the closest safe query and state the limitation.

## Opening Unity Search

Run C# in the Editor only to open the Search window. This is an Editor UI action and must not change project assets or scene contents.

When opening Search:

1. Base64-encode the query (UTF-8) and embed only the base64 text in the generated C# string, decoding it back to a string inside the snippet.
2. Prefer asset and scene providers for this v1 skill.
3. Fall back to active providers if a provider is unavailable.
4. Log the query that was opened.
5. Never claim Search opened if the command failed, because the user would look for a window that isn't there.

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

## Final step

If you ran the snippet, check it worked before you report, because `script eval` can fail without the window opening:

1. Read the `script eval` result. It succeeded only if it returns `Opened Unity Search with query: <query>` with the query you generated. If it returns a compile error, look the error up in the table under "Passing C# to `script eval`", fix the snippet, and run it again once.
2. Run `unity command console status` to check the console has zero errors, because a runtime exception in the snippet shows up there rather than in the result. If there are errors, run `unity command console get -- --tail 20 --level error` to read them. Confirm the command names with `unity command` first, because they differ between `com.unity.pipeline` versions.
3. If either check still fails, reread the [docs pages](#full-topic-map) for the project's Unity version to find what you missed. If you can't fix it, follow the fallback behavior in [references/open-search-window.md](references/open-search-window.md): don't claim Search opened, and give the user the query to paste in.

Take no screenshots, because they take more time and the result string already says whether the window opened.

Before reporting success:

- the generated query is shown to the user
- the query targets assets, scene objects, or both
- Search was opened only when the user did not opt out
- any fallback or uncertainty is stated plainly
- no asset, scene, package, project setting, or search result was modified

## Final report

End with a short report to the user, in the [response format](#response-format), so they know what ran and what is left for them:

1. The query you generated.
2. Whether Unity Search opened with it, or why it didn't (the user opted out, `script eval` is unavailable, or the command failed).
3. Any side effects or fallbacks: the temporary file you used to base64-encode the query and that you deleted it, or that Search fell back to active providers.
4. Any assumption you made about an ambiguous request, and any limitation of the query.
5. What the user needs to do next, such as paste the query into Unity Search manually, or confirm a separate follow-up if they asked to act on the results.
