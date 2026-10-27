# Security notes: craft-game-floors

This skill has the agent run C# inside the user's open Unity Editor (applying the craft floors:
materials, lighting, UI, audio, captures) and, for the live floor, wire Unity Gaming Services
calls in the user's own project. Automated skill scanners flag these as powerful capabilities.
They are intentional, and limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's open Editor | It only reaches the Editor the user already has open, on their own machine, as that user, behind the Pipeline capability gate. No code fetched from a remote source is run — the agent runs C# written from this skill's own references, which also require a restore point before project changes. |
| `SEC_CLOUD` | Wires Cloud Save / Leaderboards / Remote Config client calls (the live floor) | Only against the project's own cloud id, only when a live identity probe succeeds, and declining any floor is one word. Provisioning that needs credentials or the Editor UI is named as a human step, never automated. |

## Mitigations

- **Restore point first.** The skill requires a rollback point (a clean commit, or one it creates)
  before changing the project.
- **Floors are declinable.** Every floor reverses on a word; declines are recorded, not argued.
- **Only the user's own Editor and project.** Everything runs on this machine as this user.
- **No remote code.** All executed C# comes from this skill's references.
- **Mechanics route to their owners.** Mixer routing, UI construction, backend integration, and
  import settings go to their dedicated skills rather than being improvised here.
