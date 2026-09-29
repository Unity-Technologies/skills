# Security notes: asset-transformer-toolkit

This skill has the agent write C# and run it inside the user's live Unity Editor, to create and edit Asset Transformer RuleSets. It doesn't say which command runs the code; with the `unity` CLI that is `unity command eval`. Automated skill scanners flag running generated code as a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs agent-written C# in the user's live Editor | It only reaches the Editor the user already has open, on their own machine, as that user, so it grants nothing they couldn't do themselves. The code only edits RuleSet assets in the user's own project. No code fetched from a remote source is run. |

## Mitigations

- **Only the user's own Editor.** The code runs in the Editor open on this machine. It can't reach another machine or another user's Editor.
- **Narrow purpose.** The scripts create or modify RuleSet assets through the documented Asset Transformer API, then validate them against the checklist in `references/`.
- **No remote code.** The agent runs C# it writes from this skill's own recipes. Nothing downloaded from outside is executed.
