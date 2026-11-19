# Security notes: configure-2d-pixel-perfect

This skill can have the agent run C# in the user's live Unity Editor with `unity command script eval`, to detect the render pipeline, configure the Pixel Perfect Camera and sprite import settings, and read Play mode state. Automated skill scanners flag running code in the Editor as a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's live Editor through `unity command script eval` | It only reaches the Editor the user already has open, on their own machine, as that user, so it grants nothing they couldn't do themselves. The skill uses it to set up the camera and sprite import settings the request needs, using the example code in `references/`, and to read state and `return` values. |

## Mitigations

- **Only the user's own Editor.** The code runs in the Editor open on this machine. It can't reach another machine or another user's Editor.
- **Capability gate.** `script eval` is only available when the project's Pipeline package provides it.
- **Changes only what the user asked for.** The skill tells the agent to do only what's asked and not to change unrelated assets or files. It reads state through `script eval` instead of adding logging or detection code to the user's scripts.
- **Nothing left behind.** The skill tells the agent never to leave verification code in the user's scripts, and to create a restore point before changes.
- **No remote code.** The agent runs C# it writes itself, or adapts from the example code in `references/`. The skill never tells it to download or run code from outside.
