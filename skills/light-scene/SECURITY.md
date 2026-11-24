# Security notes: light-scene

This skill has the agent run C# inside the user's open Unity Editor through `unity command script eval`. Automated skill scanners flag that as a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's open Editor through `unity command script eval` | It only reaches the Editor the user already has open, on their own machine, as that user, so it grants nothing they couldn't do themselves. `script eval` sits behind the Pipeline package's capability gate. No code fetched from a remote source is run. The C# changes the scene's lighting and the assets the user asked about, and each change is recorded with Undo. |

## Mitigations

- **Only the user's own Editor.** `unity command script eval` talks to the Editor open on this machine. It can't reach another machine or another user's Editor.
- **Capability gate.** `script eval` is only available when the project's Pipeline package provides it.
- **No remote code.** The agent runs C# it writes from this skill's own instructions. Documentation pages it fetches are read as reference and never executed.
- **Undo and a restore point.** The skill makes a restore point before changing the project and records each change with Undo, so the user can roll it back.
- **Shared assets named first.** Before it edits an asset other scenes use, such as a pipeline asset, a Volume profile or a skybox material, the skill names the asset and what else uses it.
- **Scope limits.** The skill does only what the user asked, and obeys any limit the user sets on the work.
