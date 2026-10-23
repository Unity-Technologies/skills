# Security notes: migrate-birp-to-urp

This skill has the agent run C# inside the user's open Unity Editor through `unity command script eval`. Automated skill scanners flag that as a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's open Editor through `unity command script eval` | It only reaches the Editor the user already has open, on their own machine, as that user, so it grants nothing they couldn't do themselves. `script eval` sits behind the Pipeline capability gate. No code fetched from a remote source is run. Where a named `unity command` covers a step, the skill uses that instead of `script eval`. |

## Mitigations

- **Only the user's own Editor.** `unity command script eval` talks to the Editor open on this machine. It can't reach another machine or another user's Editor.
- **Capability gate.** `script eval` is only available when the project's Pipeline package provides it.
- **No remote code.** The agent runs C# it writes from this skill's own recipes. Nothing downloaded from outside is executed.
- **Named commands first.** When a dedicated `unity command` covers a step, the skill uses it instead of `script eval`.
