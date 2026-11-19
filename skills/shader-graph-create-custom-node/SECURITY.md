# Security notes: shader-graph-create-custom-node

This skill has the agent read state from the user's open Unity Editor through `unity command script eval` when it checks a suspected fault in Play mode after it writes a reflected HLSL function. Automated skill scanners flag running code in the Editor as a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's open Editor through `unity command script eval` | It only reaches the Editor the user already has open, on their own machine, as that user, so it grants nothing they couldn't do themselves. `script eval` sits behind the Pipeline capability gate. No code fetched from a remote source is run. |

## Mitigations

- **Only the user's own Editor.** `unity command script eval` talks to the Editor open on this machine. It can't reach another machine or another user's Editor.
- **Capability gate.** `script eval` is only available when the project's Pipeline package provides it.
- **No remote code.** The agent runs C# it writes itself to read Editor state. Nothing downloaded from outside is executed.
- **Read only, once.** The skill enters Play mode at most once, to chase a fault it already suspects, and reads state instead of adding logging or verification code to the user's scripts. It creates a restore point before it changes the project.
