# Security notes: build-gtk

This skill has the agent read state from the user's open Unity Editor through `unity command script eval` when it checks a suspected fault in Play mode. Automated skill scanners flag that as a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's open Editor through `unity command script eval` | It only reaches the Editor the user already has open, on their own machine, as that user, so it grants nothing they couldn't do themselves. `script eval` sits behind the Pipeline capability gate. No code fetched from a remote source is run. The skill uses it only to read state while chasing a suspected fault, and uses `unity command console status` for the console check. |

## Mitigations

- **Only the user's own Editor.** `unity command script eval` talks to the Editor open on this machine. It can't reach another machine or another user's Editor.
- **Capability gate.** `script eval` is only available when the project's Pipeline package provides it.
- **No remote code.** The agent runs C# it writes itself to read Editor state. Nothing downloaded from outside is executed.
- **Read only, once.** The skill enters Play mode at most once, to chase a fault it already suspects, and reads state instead of adding logging to the user's scripts.
