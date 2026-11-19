# Security notes: localize-unity-project

This skill can have the agent run C# in the user's live Unity Editor with `unity command script eval`, to read project and Play mode state while it localizes or checks the localization of the project. Automated skill scanners flag running code in the Editor as a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's live Editor through `unity command script eval` | It only reaches the Editor the user already has open, on their own machine, as that user, so it grants nothing they couldn't do themselves. It also changes the project: it installs the Localization package (`com.unity.localization`) when it is missing, and switches the active locale while it checks translations. |

## Mitigations

- **Only the user's own Editor.** The code runs in the Editor open on this machine. It can't reach another machine or another user's Editor.
- **Nothing left behind.** The skill tells the agent never to leave verification code in the user's scripts, and to create a restore point before changes.
- **No remote code.** Nothing downloaded from outside is executed.
