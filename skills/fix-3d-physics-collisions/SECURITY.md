# Security notes: fix-3d-physics-collisions

This skill can have the agent copy `scripts/CollisionDebugger.cs` into the user's project and call it with `unity command script eval` in the user's live Unity Editor, to record which contacts the physics engine reports. Automated skill scanners flag running code in the Editor as a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Adds an Editor script to the user's project and runs C# in the user's live Editor | It only reaches the Editor the user already has open, on their own machine, as that user, so it grants nothing they couldn't do themselves. The script ships with this skill and is readable before use. It only reads physics settings and contact data, and logs them to the Console. |

## Mitigations

- **Only the user's own Editor.** The code runs in the Editor open on this machine. It can't reach another machine or another user's Editor.
- **Nothing saved to the scene.** The script adds no components. It sets `Collider.providesContacts` on the tracked colliders only during Play mode, which Unity discards when Play mode ends.
- **Read-only project settings.** It reads `ProjectSettings/DynamicsManager.asset` and never writes to it.
- **Temporary.** The skill tells the agent to delete the script and its `.meta` file after the diagnosis.
- **No remote code.** Nothing downloaded from outside is executed.
