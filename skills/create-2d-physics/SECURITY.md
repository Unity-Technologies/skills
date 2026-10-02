# Security notes: create-2d-physics

This skill has the agent run C# inside the user's open Unity Editor through `unity command eval`. Automated skill scanners flag that as a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's open Editor through `unity command eval` | It only reaches the Editor the user already has open, on their own machine, as that user, so it grants nothing they couldn't do themselves. `eval` sits behind the Pipeline capability gate. No code fetched from a remote source is run. Where a named `unity command` covers a step, the skill uses that instead of `eval`. |

## Mitigations

- **Only the user's own Editor.** `unity command eval` talks to the Editor open on this machine. It can't reach another machine or another user's Editor.
- **Capability gate.** `eval` is only available when the project's Pipeline package provides it.
- **No remote code.** The agent runs C# it writes from this skill's own recipes. Nothing downloaded from outside is executed.
- **Named commands first.** When a dedicated `unity command` covers a step, the skill uses it instead of `eval`.

## Background

This skill was authored directly against `unity command eval` (Unity-Technologies/skills#84, Melvyn May), unlike the other `unity command eval` skills in this repo whose SECURITY.md records a retrofit from the AI Assistant's `RunCommand` tool. This file was added while moving the skill from the public repo into unity/skills, to satisfy this repo's `SEC_POWER_CAP` gate, and it mirrors the risk-acceptance template already applied to `optimize-web`, `sprite-editor`, `audio-setup-mixers`, `migrate-birp-to-urp`, `urp-postprocessing`, and `optimize-audio`. It has not yet had its own explicit sign-off from a security reviewer for this skill specifically — flagged in the PR for one.
