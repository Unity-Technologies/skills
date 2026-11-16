# Security notes: build-multiplayer-game

When testing host and client behaviour, this skill has the agent run C# inside the user's open Unity Editor, and inside Multiplayer Play Mode clone Editors of the same project, through `unity command script eval`. Automated skill scanners flag that as a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's open Editor and its Multiplayer Play Mode clones through `unity command script eval`, and triggers script recompiles and asset reimports through `unity command` | It only reaches Editors of the user's own project that are already open on their own machine, as that user, so it grants nothing they couldn't do themselves. `eval` sits behind the Pipeline capability gate. No code fetched from a remote source is run. Where a named `unity command` covers a step, the skill uses that instead of `eval`. |

## What it changes

The skill writes to the user's project and machine, not only reads. It asks the user before each of the larger changes below.

- **Packages:** adds the packages the chosen stack needs to `Packages/manifest.json`: `com.unity.services.multiplayer`, `com.unity.netcode.gameobjects` or `com.unity.netcode`, `com.unity.dedicated-server` for a dedicated server, and `com.unity.pipeline` (through the `unity-cli` skill) when the Editor commands are missing.
- **Editor modules:** installs a dedicated server module (`linux-server`, `windows-server` or `mac-server`) with `unity install-modules`, a 0.5–2 GB download.
- **Cloud project:** links the project to a Unity Cloud project with `unity projects link cloud`, which can create a new project in the user's organization. The skill reuses an existing project unless the user agrees to a new one, and asks the COPPA question before creating one.
- **Project files and settings:** creates scripts, prefabs and Play Mode Scenario assets, turns on Multiplayer Roles, sets which role keeps each GameObject and component (what a client or server strips), sets a scenario player's Multiplayer Role, turns on the project's Run in Background setting, and changes the netcode bootstrap setting.

## Mitigations

- **Only the user's own Editors.** Commands go to the Editor open on this machine and to clones of that same project. They can't reach another machine or another user's Editor.
- **Capability gate.** `eval` is only available when the project's Pipeline package provides it.
- **No remote code.** The agent runs C# it writes for the user's task, such as test-only host and join hooks. Nothing downloaded from outside is executed.
- **Clones drive the game, not the project.** The skill tells the agent to use clone Editors only to run and read the game, never to change project assets.
- **Named commands first.** When a dedicated `unity command` covers a step, the skill uses it instead of `eval`.
