---
name: initialize-ai-navigation
description: Sets up and configures Unity AI Navigation, including NavMesh surfaces, agents, obstacles, and links. Use when the user asks about navigation meshes, pathfinding, patrol routes, or obstacle avoidance.
---

Determine what the user needs and guide them through navigation setup. See [navigation-system.md](references/navigation-system.md) for expanded component details, API notes, code recipes, and troubleshooting.

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Before making changes, establish a baseline of the current behavior so you can tell whether a later result is actually caused by your change.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask for. See [Final step](#final-step).
- Avoid long explanations.

## Full topic map

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

The docs pages are local markdown conversions of the Unity Manual, stored at `<project-folder>/Library/PackageCache/com.unity.ai.navigation@*/`. Read only the .md files in that specific `PackageCache` subfolder (they're in its `Documentation~/` folder). You must not look at non-.md files, or look outside at other Library or PackageCache folders, otherwise it takes too much time. If that folder has no `.md` files, read the same pages online at `https://docs.unity3d.com/Packages/com.unity.ai.navigation@<VERSION>/manual/<page>.html`, because some package versions don't ship their docs.

Prefer `WebFetch` over `WebSearch` — faster and lands on the exact reference. Only fetch what you need. Replace `<VERSION>` with the following before fetching:

- **Package on docs.unity3d.com**: The package version, eg `@1.1` or `@2.0`. Check the package version if unsure.
- **docs.unity.com**: The product version, eg `7000.0`

Read the Unity version from `ProjectSettings/ProjectVersion.txt`. For the package docs, use the major.minor version of `com.unity.ai.navigation` from `Packages/packages-lock.json`.

- **Install AI Navigation** — `InstallAINav.md`: how to add the package to a project.
- **Inner workings of the navigation system** — `NavInnerWorkings.md`: pathfinding, steering, avoidance and voxel bake quality.
- **About agents** — `AboutAgents.md`; **About obstacles** — `AboutObstacles.md`: agent cylinders, and when an obstacle should carve or only obstruct.
- **Navigation areas and costs** — `AreasAndCosts.md`: built-in and custom area types, path cost, agent area masks.
- **Create a NavMesh / agent / obstacle / link** — `CreateNavMesh.md`, `CreateNavMeshAgent.md`, `CreateNavMeshObstacle.md`, `CreateNavMeshLink.md`: setup procedure for each component.
- **Use NavMesh agents with other components** — `MixingComponents.md`: combining agents with Rigidbody, Animator and NavMesh Obstacle.
- **Navigation window** — `NavigationWindow.md`: agent type and area type settings.
- **Component references** — `NavMeshSurface.md`, `NavMeshAgent.md`, `NavMeshObstacle.md`, `NavMeshLink.md`, `NavMeshModifier.md`, `NavMeshModifierVolume.md`: every Inspector property and its default.
- **OffMesh Link component (deprecated)** — `OffMeshLink.md`: the legacy link component to migrate away from.
- **Move to a destination / clicked position / patrol** — `NavMoveToDestination.md`, `NavMoveToClickPoint.md`, `NavAgentPatrol.md`: movement scripts to save into the project.
- **Couple animation and navigation** — `CouplingAnimationAndNavigation.md`: Animator setup, blend tree and locomotion scripts.

### API reference

The package API is `Unity.AI.Navigation` (`NavMeshSurface`, `NavMeshLink`, `NavMeshModifier`, `NavMeshModifierVolume`), part of the `com.unity.ai.navigation` package. It is versioned by package version, not Unity version (e.g. `@1.1`, `@2.0`), so check the project's installed package version before relying on a specific class or method.

If the package is installed, grep the `.cs` files in `<project-folder>/Library/PackageCache/com.unity.ai.navigation@*/` for the class or member and read its `///` XML doc comments, because they hold the API reference text for the exact installed version. This is the only exception to reading `.md` files only in that folder. Use the following steps only when the package isn't installed yet, for example to decide whether the API fits the user's scenario:

1. Find the major.minor of the version you'd install, as described in [Package Installation Check](#0-package-installation-check).
2. Fetch and cache `https://docs.unity3d.com/Packages/com.unity.ai.navigation@<version>/xrefmap.yml` (replace `<version>` with that major.minor version, e.g. `2.0`). This file maps every public API member's fully qualified name to its docs page `href` (e.g. `href: api/Unity.AI.Navigation.NavMeshSurface.html` => `https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshSurface.html`).
3. Grep the yml file locally for a class or method instead of refetching per lookup. Don't read the whole file.

The agent and query API is `UnityEngine.AI` (`NavMeshAgent`, `NavMesh`, `NavMeshPath`, `NavMeshObstacle`), part of the Unity Engine. It is versioned by Unity version, so check the project's Unity version before relying on a specific class or method.

1. Use the `unity-cli` skill to check the project's Unity version.
2. If the Editor install folder has a `Data/Documentation` folder (e.g. `.../Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), grep the html files in the `en/ScriptReference` folder inside, and skip the remaining steps.
3. Otherwise, fetch and cache `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/xrefmap.yml` (replace `<VERSION>` with the Unity version, e.g. `6000.3`). This file lists every public API member's fully qualified name as a `uid` (e.g. `uid: 'UnityEngine.AI.NavMeshAgent.SetDestination(UnityEngine.Vector3)'`).
4. Grep the yml file locally for a class or method instead of refetching per lookup. The file is large (about 6 MB), so don't read the whole file.
5. Convert the `uid` to a markdown page: remove the parameters in brackets, lowercase it, replace `.` with `/`, and add `.md`. Then fetch `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/<converted uid>`. For example, `UnityEngine.AI.NavMeshAgent.SetDestination(UnityEngine.Vector3)` => `https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/ai/navmeshagent/setdestination.md`. All overloads are on the same page.
   - Constructors use `ctor` (e.g. `unityengine/ai/navmeshpath/ctor.md`), and indexers use `item`.
   - For operators, fetch the class page (e.g. `unityengine/ai/navmeshagent.md`) and follow its link.
   - Links inside the pages start with `/engine/`. Add `https://docs.unity.com/en-us` before them.
   - If the page doesn't exist for that Unity version, use the `href` in the `xrefmap.yml` entry instead (e.g. `href: 'AI.NavMeshAgent.SetDestination.html'` => `https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AI.NavMeshAgent.SetDestination.html`).

Always use the docs for the installed package and Unity version rather than relying on training knowledge, since APIs are frequently marked obsolete between versions (e.g. the `OffMeshLink` component is deprecated in favour of `NavMeshLink`).

### Passing C# to `eval`

`script eval` compiles a **statement block, not a file**, so these are compile errors rather than warnings:

| Error | Cause | Fix |
|---|---|---|
| `CS0210` | A `using` directive, such as `using UnityEngine;`, read as a resource-disposal statement | Remove every `using` directive |
| `CS0246` / `CS0103` | A bare type name, such as `AssetDatabase` | Fully qualify the type, such as `UnityEditor.AssetDatabase` |
| `CS0104` | A bare `Object`, which is ambiguous with `object` | Write `UnityEngine.Object` |

Where a snippet below is written as a file — with usings, for readability, or because it is
meant to be saved into the project — qualify the types before passing it to `script eval`.

## Routing Logic

| User Says | Interpretation |
|-----------|---------------|
| "add navigation" / "set up nav" | Full setup: NavMeshSurface + bake + NavMeshAgent |
| "make this character navigate" | Add NavMeshAgent, ensure NavMesh exists |
| "add pathfinding" | NavMeshAgent + movement script |
| "agent won't move" / "path not found" | Troubleshoot — see Troubleshooting Decision Tree in reference |
| "avoid obstacles" | NavMeshObstacle with carving or avoidance |
| "connect two areas" / "jump across" | NavMeshLink between areas |
| "patrol between points" | NavMeshAgent + patrol script |
| "click to move" | NavMeshAgent + raycast click-to-move script |
| "animate the character while navigating" | Couple Animator with NavMeshAgent |
| "different agent sizes" | Configure agent types in Navigation window |
| "areas and costs" / "restrict areas" | NavMesh area types, modifiers, and agent area masks |

## Workflow

### 0. Package Installation Check
Before doing anything else, verify that `com.unity.ai.navigation` is installed. If it's missing,
add it to `Packages/manifest.json` under `dependencies` — Unity resolves it when the Editor next
regains focus, and this needs no Editor connection:

```json
"com.unity.ai.navigation": "<current 2.x version>"
```

Don't invent the version string, and don't copy another package's version, because package
versions are independent. You must read the latest 2.x version from the Unity registry:
`https://packages.unity.com/com.unity.ai.navigation` lists every published version. Use version 2.0.16 when in doubt.

Proceed only once it's confirmed installed. If you have a live Editor to run C# in, see
[navigation-system.md](references/navigation-system.md) for the `Client.Add` equivalent.

### 1. Pre-Flight: Assess Current Navigation Setup
Before making changes, inspect what already exists:
1. Find the existing navigation components. With a connected Editor, query the live scene for
   `NavMeshSurface`, `NavMeshAgent`, `NavMeshObstacle`, `NavMeshLink` and `NavMeshModifier` — see
   the `unity-cli` skill for driving a running Editor. Without one, search the scene and prefab
   files for those component names.
2. List the configured agent types. With a connected Editor, loop over
   `UnityEngine.AI.NavMesh.GetSettingsCount()` and `GetSettingsByIndex(i)`, and name each with
   `GetSettingsNameFromID(settings.agentTypeID)`. Without one, read the `m_SettingNames` section in `ProjectSettings/NavMeshAreas.asset`.
3. Summarize ALL detected navigation components before proposing changes.

### 2. Gather Missing Information
Before creating components, ensure the user has specified: walkable surfaces, agent type/size, agent behavior, obstacles, links, and area/cost requirements. Ask if anything is unclear. See the Information Gathering Checklist in the reference for full details.

### 3. Planning & Execution
Follow this general order. See the Component Setup Guide in the reference for detailed step-by-step instructions per component:
1. **NavMesh Surface** — create the walkable mesh (bake it)
2. **NavMesh Agent** — add pathfinding characters
3. **NavMesh Obstacle** — add dynamic obstacles
4. **NavMesh Link** — connect disconnected NavMesh areas
5. **NavMesh Modifier / Modifier Volume** — fine-tune area types
6. **Scripts** — movement, patrol, click-to-move, animation coupling (see Common Recipes in reference)

### 4. Validation
After setup, read these values with `eval` (fully qualified) rather than checking the Scene view by eye, because a NavMesh that looks right can still be missing or mismatched:

1. Each `NavMeshSurface` has baked data: `navMeshData` isn't null.
2. Each `NavMeshSurface.agentTypeID` matches the `NavMeshAgent.agentTypeID` of the agents that walk on it.
3. Each agent can reach its destination: `agent.CalculatePath` to the target returns `true`, and the path's `status` is `PathComplete`. Use `NavMesh.CalculatePath(Vector3, Vector3, NavMeshQueryFilter, NavMeshPath)` if you check paths when a NavMeshAgent is not available.
4. `NavMeshObstacle.carving` is on for NavMeshObstacle components that stay still in the scene or are expected to move slowly, and off for those expected to move fast.
5. Each `NavMeshLink` has `activated` set to `true`, and `NavMesh.SamplePosition` finds the NavMesh within a short distance of both ends.
6. Each agent's `areaMask` includes every area it must cross.
7. No conflicting components (see Mixing Components Guide in reference).
8. If a NavMeshAgent's GameObject has a Rigidbody, `isKinematic` is `true`.

If a check fails, fix it and run the checks again before the Final step.

## Final step

Do the following checks:

1. Check the project has zero console errors. Use `console_status` for counts.
2. Stop there unless the user asked you to prove it works or you suspect a specific fault.
3. If you do enter Play mode, do it once only to chase a fault you already suspect. Read the world through `eval` rather than adding logging, for example each agent's `pathStatus` and `remainingDistance` to see whether it reaches its destination. Take no screenshots as they take too much time.

If a check fails, go back and reread the [docs pages](#full-topic-map) to find out what you missed.

## Final report

Summarize what was created or changed:
- NavMesh Surfaces: which GameObject, agent type, geometry mode, bake status
- NavMesh Agent(s): which GameObject, speed, stopping distance, area mask
- NavMesh Obstacle(s): which GameObject, shape, carve on/off
- NavMesh Link(s): start/end, bidirectional, area type
- Scripts: which scripts attached to which GameObjects
- Any manual steps required (adjust waypoints, re-bake after scene changes, etc.)
