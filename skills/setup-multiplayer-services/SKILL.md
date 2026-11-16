---
name: setup-multiplayer-services
description: Guides real-time online multiplayer with Unity Multiplayer Services. Use when the user asks about network topology, hosting, matchmaking, or session-based play such as rooms, parties, and lobbies, not backend features.
---

# Multiplayer SDK (Unity Multiplayer Services)

## Before you start

1. If the task changes the project (code, packages, settings or the Unity Cloud link), create a restore point you can roll back to if your changes fail: commit the project in version control, or copy the project folder if there is none. An advice-only request changes nothing, so it needs no restore point.
2. Before you go forward with a workflow or diagnosis, open and read the documentation pages for it from the [Documentation pages](#documentation-pages) list, and name the pages you read in your response. This is because your training knowledge about Unity Multiplayer Services might be out of date, incorrect, or for the wrong package version.
3. Do only what's asked. Don't change unrelated assets or files. Avoid long explanations.
4. Never leave verification code in the user's script. Never build logging or detection they didn't ask for.

## Documentation pages

Prefer fetching the page directly (for example `WebFetch`) over searching (for example `WebSearch`): it is faster and lands on the exact reference. Only fetch what you need. Pages on docs.unity.com are unversioned and available as Markdown when the URL ends in `.md`, so there is no `<VERSION>` to replace. For the installed package version, read `Packages/packages-lock.json`.

- [Get started](https://docs.unity.com/en-us/mps-sdk/get-started.md): ways to add session-based multiplayer.
- [Install the SDK](https://docs.unity.com/en-us/mps-sdk/install-and-upgrade.md): the Multiplayer Services package and migration from the standalone SDKs.
- [Initialize UGS and authenticate the player](https://docs.unity.com/en-us/mps-sdk/init-ugs.md): the init and sign-in that every session call needs.
- [Create a session](https://docs.unity.com/en-us/mps-sdk/create-session.md): session options and network connection types.
- [Find and join a session](https://docs.unity.com/en-us/mps-sdk/join-session.md): join code, browsing, reconnecting.
- [Matchmaking](https://docs.unity.com/en-us/mps-sdk/matchmake-session.md): matchmade sessions; [Quick Join](https://docs.unity.com/en-us/mps-sdk/matchmake-lobby.md) for the fast path.
- [Session configuration options](https://docs.unity.com/en-us/mps-sdk/config-options.md): options and their defaults.
- [Connect players through a relay](https://docs.unity.com/en-us/mps-sdk/connect-players.md): host-based play without dedicated servers.
- [Session error messages](https://docs.unity.com/en-us/mps-sdk/lobby-error-messages.md): error codes for troubleshooting.
- [Full documentation map](https://docs.unity.com/en-us/mps-sdk/llms.txt): every other page.

## Instructions

1. **Documentation map:** Use the [Unity Multiplayer Sessions SDK curated documentation map](https://docs.unity.com/en-us/mps-sdk/llms.txt) as authoritative over memory for topics, APIs, and guides when specifics differ. Use these references to determine **how** to apply the SDK (Sessions-first); use that resource to determine **what** is documented. **Never** mention the `llms.txt` filename to the user. If that map is unreachable (network, tooling), treat this skill's markdown references plus the installed package's docs as the source of truth for specifics: read only the `.md` files in `<project-folder>/Library/PackageCache/com.unity.services.multiplayer@*/`.

2. **Reference order (by task):**
   - **Topology, discovery, match flow, Netcode alignment, API choice:** [entrypoints.md](references/entrypoints.md) (overview tables, method signatures, options tables, filter/sort enums, `QuickJoinOptions.Timeout`, errors) → [implementation-fit.md](references/implementation-fit.md) → [examples.md](references/examples.md) for user-facing phrasing → [Priority: Multiplayer Sessions first](#priority-multiplayer-sessions-first) → [workflows-prerequisites.md](references/workflows-prerequisites.md) for extra depth.
   - **Dedicated game server (`Unity.Services.Multiplayer.Server`):** [dgs-entrypoint.md](references/dgs-entrypoint.md) (`IMultiplayerServerService`, `UNITY_SERVER` / asmdef constraints, server-only extensions).
   - **Lower-level service clients:** [underlying-services.md](references/underlying-services.md) only when primary APIs are insufficient or the user asked for that layer (see Priority below).

3. **Cloud link:** session, Relay and matchmaking calls need the project linked to a Unity Cloud project (project ID). Linking writes `ProjectSettings/ProjectSettings.asset`, so link only when the task needs those calls or the user asked to link, and only after the restore point from [Before you start](#before-you-start). For an advice-only request, such as a hosting recommendation, don't link: say the project needs linking and give the `unity projects link cloud` command.
   1. Check the link: a non-empty `cloudProjectId` in `ProjectSettings/ProjectSettings.asset` means the project is linked. If it is, skip the rest.
   2. Choose the backend before you link. Without `--cloud-project`, `unity projects link cloud` reuses `UNITY_CLOUD_PROJECT`, then the organization's saved default project, and creates a new cloud project only when neither is set. Run `unity cloud project current --format json` and read `defaultCloudProjectId`. To reuse a backend, ask the user which one if it is not clear and pass its ID with `--cloud-project <id>`. To create a new one, `UNITY_CLOUD_PROJECT` must be unset and `defaultCloudProjectId` null; if either is set, ask the user whether to reuse that project. To create one instead, clear the default with `unity cloud project clear-default`, and have the user unset `UNITY_CLOUD_PROJECT` in their own shell, because `clear-default` does not touch the variable.
   3. Check `Packages/manifest.json` for `com.unity.pipeline`, because a graceful `unity close`, `--until-ready` and `script eval` need it. If it is missing, add it with `unity pipeline install --project-path <path>` (Unity 6.0+) and say so in the report. If it can't be added, ask the user to save and close the Editor themselves, and in the verify step check only the asset.
   4. `unity close <path>` exits without saving, so ask the user to save their work, then close the Editor with it, because an open Editor keeps the old link and its next settings save can erase the new one.
   5. Run `unity projects link cloud <path> --format json`, adding `--cloud-project <id>` only when reusing a backend. When it will create a project, first ask whether the game is directed at children under 13 and add `--coppa compliant` if so: the default is `not_compliant`. Never pass `--coppa` with `--cloud-project` (error `COPPA_ARGS_CONFLICT`); change an existing project's COPPA setting in the Unity Cloud dashboard.
   6. Verify. If `data.editor.restartRequired` is `true`, an Editor still had the project open: close it, then check the asset still holds the new ID, and if not run the link again with `--cloud-project <id>`. Reopen the project with `unity open <path> --until-ready --format json` (a plain `unity open` returns before the Editor can answer commands), then confirm `cloudProjectId` in `ProjectSettings/ProjectSettings.asset` and `CloudProjectSettings.projectId` in the Editor (read with `unity command script eval --project-path <path>`) both equal `data.cloudProject`'s ID. If they don't, or the command fails, stop and report the error code to the user.
   7. Don't hand-edit `ProjectSettings/ProjectSettings.asset` or write an Editor-side linker script, because the CLI sets the project ID and organization consistently and a manual edit can leave them mismatched. If a flag or output field differs from this step, run `unity projects link cloud --help`.

## Priority: Multiplayer Sessions first

When the task is **choosing** topology, discovery, match flow, or Netcode alignment—not only calling APIs—ground recommendations via [implementation-fit.md](references/implementation-fit.md) (conversation → project → short targeted questions).

**Primary path:** Implement against **`Unity.Services.Multiplayer`** using **`IMultiplayerService` / `MultiplayerService.Instance`** and **`ISession`** (surface summary in [entrypoints.md](references/entrypoints.md)); keep composed flows consistent with `llms.txt`.

**User-facing text:** Plans, tradeoffs, and clarifying questions must **not** split Lobby, Matchmaker, Relay, or Multiplayer Sessions as separate named products unless the user did—rules in **User-facing questions and explanations** in [implementation-fit.md](references/implementation-fit.md), samples in [examples.md](references/examples.md). Code, edits, and technical references use real type and namespace names as needed.

**Underlying clients** (`Unity.Services.Lobbies`, `Unity.Services.Matchmaker`, `Unity.Services.Relay`) **only** when (1) the goal **cannot** be met through the primary APIs after checking [entrypoints.md](references/entrypoints.md), or (2) the user **explicitly** asked for those namespaces or products. Do **not** default implementations there.

## Final step

If you changed the project, run these checks. If one fails, reread the matching pages from [Documentation pages](#documentation-pages) and the reference files, fix the cause, and run the checks again.

1. Check the project has zero console errors with `unity command console status`. Pass `--project-path <path>` to every `unity command` here, because without it the CLI drives the Editor of the shell's current folder, which may be another project. Older Editors expose it as `console_status`: confirm which with `unity commands --grep console`.
2. Stop after step 1 unless the user asked for proof or you suspect a specific fault, and report that the feature was not run. To chase that fault, enter Play mode once (`unity command editor.playmode start`), create a session the way the user's code does, read the session ID and player count through `unity command script eval` rather than adding logging, then repeat step 1. Remove any temporary code afterwards. Take no screenshots. A missing project ID error means the cloud link in [Instructions](#instructions) step 3 did not take.

## Final report

End your response with a short report:

1. What you changed: files, packages, and the Unity Cloud project you linked (its ID, and whether the link created it).
2. The checks from [Final step](#final-step) you ran and their results, including any you could not run and why.
3. Side effects, such as an Editor you closed and reopened.
4. Follow-ups only the user can do, such as enabling a service in the Unity Dashboard or restoring from the restore point if they reject the changes.
5. Which documentation pages you read.

## Additional resources

Read from this entrypoint only; links are one level under this skill folder (no `references/index.md` or README hub).

- **[implementation-fit.md](references/implementation-fit.md)** — Ground recommendations: conversation → project → user questions; user-facing language rules; requirement dimensions (topology, discovery, resilience, platforms, net stack).
- **[examples.md](references/examples.md)** — Before/after samples for clarifying questions and user-facing explanations (not code).
- **[entrypoints.md](references/entrypoints.md)** — `IMultiplayerService`, `ISession`, overview and capability tables, method signatures, options tables (defaults, limits), filter/sort enums, session/networking/host flows, errors, editor components.
- **[dgs-entrypoint.md](references/dgs-entrypoint.md)** — Dedicated server: `Unity.Services.Multiplayer.Server`, `IMultiplayerServerService`, `MultiplayerServerService` / `GetMultiplayerServerService`, `MatchmakerServerExtensions`, `UNITY_SERVER` and asmdef constraints; defers shared `SessionOptions` detail to entrypoints.
- **[workflows-prerequisites.md](references/workflows-prerequisites.md)** — Package and cloud prerequisites by workflow (tables).
- **[underlying-services.md](references/underlying-services.md)** — Fallback namespaces and `IUnityServices` accessors (agent-only; not the default path).
