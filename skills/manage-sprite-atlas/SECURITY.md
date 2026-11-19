# Security notes: manage-sprite-atlas

This skill has the agent run short C# snippets inside the user's live Unity Editor, to read back `EditorSettings.spritePackerMode` and to check that each atlas it created actually packs. It doesn't say which command runs the code; with the `unity` CLI that is `unity command script eval`. Automated skill scanners flag running generated code as a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs agent-written C# in the user's live Editor | It only reaches the Editor the user already has open, on their own machine, as that user, so it grants nothing they couldn't do themselves. The snippets only read atlas and Editor settings, and pack atlases into the Editor's packing cache. No code fetched from a remote source is run. |

## Mitigations

- **Only the user's own Editor.** The code runs in the Editor open on this machine. It can't reach another machine or another user's Editor.
- **Narrow purpose.** The snippets read `EditorSettings.spritePackerMode`, load the atlases the agent created, pack them with `SpriteAtlasUtility.PackAtlases`, and return `spriteCount` and `CanBindTo`. They don't change project assets.
- **No verification code left behind.** The skill tells the agent never to leave verification code in the user's scripts, so checks run only through `script eval`.
- **No remote code.** The agent runs C# it writes from this skill's own instructions. Nothing downloaded from outside is executed.
