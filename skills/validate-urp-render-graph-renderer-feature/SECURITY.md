# Security notes: validate-urp-render-graph-renderer-feature

This skill has the agent read state from the user's open Unity Editor through `unity command script eval` when it checks a suspected fault in Play mode while reviewing a Render Graph renderer feature. Automated skill scanners flag running code in the Editor as a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's open Editor through `unity command script eval` | It only reaches the Editor the user already has open, on their own machine, as that user, so it grants nothing they couldn't do themselves. `script eval` sits behind the Pipeline capability gate. No code fetched from a remote source is run. |

## Mitigations

- **Only the user's own Editor.** `unity command script eval` talks to the Editor open on this machine. It can't reach another machine or another user's Editor.
- **Capability gate.** `script eval` is only available when the project's Pipeline package provides it.
- **No remote code.** The agent runs C# it writes itself to read Editor state. Nothing downloaded from outside is executed.
- **Review only.** The skill doesn't edit the user's files unless they ask for the fixes, enters Play mode at most once to chase a fault it already suspects, and reads state instead of adding logging or verification code. It creates a restore point just before its first edit.
