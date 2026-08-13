# Unity Search Query Patterns

Use these patterns to translate common natural-language requests into Unity Search queries. Keep queries simple unless the user asks for a more complex relationship.

## Table of Contents

- [Asset Queries](#asset-queries)
- [Reference Queries](#reference-queries)
- [Scene Queries](#scene-queries)
- [Natural-Language Mapping](#natural-language-mapping)
- [Mixed Asset And Scene Queries](#mixed-asset-and-scene-queries)
- [Unsupported Or Risky Requests](#unsupported-or-risky-requests)
- [Query Quality Rules](#query-quality-rules)

## Asset Queries

| User intent | Query |
| --- | --- |
| Find all materials | `t:material` |
| Find all textures | `t:texture` |
| Find materials and textures | `t:[material, texture]` |
| Find prefabs | `t:prefab` |
| Find scenes | `t:scene` |
| Find scripts | `t:script` |
| Find shaders | `t:shader` |
| Find sprites | `t:sprite` |
| Find audio clips | `t:audioclip` |
| Find animation clips | `t:animationclip` |
| Find meshes/models | `t:mesh` |
| Find fonts | `t:font` |
| Find render textures | `t:rendertexture` |
| Find ScriptableObject assets | `t:ScriptableObject` |
| Find assets in a folder | `dir:Assets/FolderName` |
| Find prefabs in a folder | `t:prefab dir:Assets/FolderName` |
| Find materials in a folder | `t:material dir:Assets/FolderName` |
| Find assets by label | `l:LabelName` |
| Find assets by name or keyword | `keyword` |

If the user gives an asset extension or filename pattern, include it as a keyword or glob-style term when useful, such as `*.prefab`, `*.mat`, or `Player`.

## Reference Queries

Use `ref=` when the request is about usage, references, dependencies, or "what uses this".

| User intent | Query |
| --- | --- |
| Find prefabs that reference textures | `t:prefab ref={t:texture}` |
| Find scenes that reference prefabs | `t:scene ref={t:prefab}` |
| Find assets that reference a named asset | `ref=AssetName` |
| Find scenes referencing a specific prefab path | `t:scene ref="Assets/Path/Prefab.prefab"` |
| Find prefabs that use a material | `t:prefab ref=MaterialName` |
| Find materials that use a texture | `t:material ref=TextureName` |
| Find scenes that use a script or component name | `t:scene ScriptOrComponentName` |
| Find prefabs that mention a script or component name | `t:prefab ScriptOrComponentName` |

If the user gives a specific selected asset but the exact path is unknown, use the visible asset name first and say that a path-specific query will be more precise.

## Scene Queries

Scene-object searches should use component type names or object-name keywords. These queries are intended for the scene provider.

| User intent | Query |
| --- | --- |
| Find Lights | `t:Light` |
| Find Cameras | `t:Camera` |
| Find Rigidbodies | `t:Rigidbody` |
| Find Colliders | `t:Collider` |
| Find Canvas UI objects | `t:Canvas` |
| Find UI Buttons | `t:Button` |
| Find TextMeshPro UI text | `t:TextMeshProUGUI` |
| Find RectTransforms | `t:RectTransform` |
| Find Particle Systems | `t:ParticleSystem` |
| Find AudioSources | `t:AudioSource` |
| Find Animators | `t:Animator` |
| Find MeshRenderers | `t:MeshRenderer` |
| Find SkinnedMeshRenderers | `t:SkinnedMeshRenderer` |
| Find NavMeshAgents | `t:NavMeshAgent` |
| Find Terrain objects | `t:Terrain` |
| Find objects by name | `ObjectName` |

For scene object requests that mention both a name and a component, combine them, for example `Enemy t:Rigidbody`.

## Natural-Language Mapping

Map common phrases to stable query forms:

| User wording | Query strategy |
| --- | --- |
| "where is X" | Use `X` as a keyword query unless X is clearly a type. |
| "which prefabs use X" | Use `t:prefab ref=X` when X is an asset name; otherwise use `t:prefab X`. |
| "what references this texture" | Use `ref=TextureName`, or `ref="Assets/Path/Texture.png"` if a path is available. |
| "what references the selected material" | Use the selected asset name/path if provided; otherwise ask for the material name or path. |
| "show all X in Assets/UI" | Use `t:x dir:Assets/UI` when X maps to an asset type. |
| "find scene objects with X" | Use `t:X` when X is a component type; otherwise use `X`. |
| "find current scene objects named X" | Use `X` with scene providers; add a component type only when the user specifies one. |
| "query only" | Return the query and do not open Search. |

## Mixed Asset And Scene Queries

If the user asks broadly, such as "find everything related to Rigidbody", open Search with active asset and scene providers and use the plain keyword plus likely type:

- `Rigidbody`
- `t:Rigidbody`
- `Player Rigidbody`

For "materials using Standard shader" or other property-specific material questions, Unity Search may not expose every material shader property consistently across versions. Prefer a query such as `t:material Standard` and state that it narrows to likely matches; a scripted audit is a separate task.

## Unsupported Or Risky Requests

Do not pretend Unity Search can reliably express every inspection.

- Missing script detection might need a scripted scan. Use a broad query such as `missing script` or `t:prefab missing` only as a starting point and state the limitation.
- Deep dependency graph questions are not this skill's scope. Provide a Search reference query if possible, then suggest dependency analysis as a separate workflow.
- Repository text search, build log analysis, web documentation lookup, package installation, menu command search, settings search, and result actions are outside v1 scope.
- If the user asks to "find and fix" assets, open Search for the find step only. Ask for confirmation before any separate modifying workflow.
- If the user asks about a selected/current asset but no selection details are visible, ask for the asset name or path rather than guessing.

## Query Quality Rules

- Prefer lowercase asset type names such as `t:material`, `t:texture`, and `t:prefab`.
- Use Unity type names for scene components, such as `t:Light` and `t:Camera`.
- Quote paths with spaces, for example `ref="Assets/My Folder/Player.prefab"`.
- Do not combine many speculative terms. One good query is better than a long fragile query.
- If multiple plausible queries exist, choose one and mention alternatives only when helpful.
