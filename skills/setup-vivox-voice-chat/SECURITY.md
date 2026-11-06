# Security notes: setup-vivox-voice-chat

This skill can have the agent run C# in the user's live Unity Editor with `unity command script eval`, to read project and Play mode state while it sets up or checks Vivox voice and text chat. Automated skill scanners flag running code in the Editor as a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's live Editor through `unity command script eval` | It only reaches the Editor the user already has open, on their own machine, as that user, so it grants nothing they couldn't do themselves. The skill uses it to read state and `return` values, not to change the project. |

## Mitigations

- **Only the user's own Editor.** The code runs in the Editor open on this machine. It can't reach another machine or another user's Editor.
- **Reads, not writes.** The skill uses `script eval` to read values it returns, instead of adding logging or detection code to the user's scripts.
- **Nothing left behind.** The skill tells the agent never to leave verification code in the user's scripts, and to create a restore point before changes.
- **No remote code.** The skill fetches Vivox documentation to read it. Nothing downloaded from outside is executed.
