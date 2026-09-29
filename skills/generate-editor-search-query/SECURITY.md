# Security notes: generate-editor-search-query

This skill has the agent run C# inside the user's open Unity Editor through `unity command eval`. Automated skill scanners flag that as a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's open Editor through `unity command eval` | It only reaches the Editor the user already has open, on their own machine, as that user, so it grants nothing they couldn't do themselves. `eval` sits behind the Pipeline capability gate. No code fetched from a remote source is run. The skill only opens the Search window with a query; it does not act on results. |

## Mitigations

- **Only the user's own Editor.** `unity command eval` talks to the Editor open on this machine. It can't reach another machine or another user's Editor.
- **Capability gate.** `eval` is only available when the project's Pipeline package provides it.
- **No remote code.** The agent runs C# it writes from this skill's own recipes. Nothing downloaded from outside is executed.
- **Query passed base64-encoded, not interpolated as text.** The generated Unity Search query is base64-encoded before it is embedded in the C# snippet, and decoded back to a string inside the snippet. A query containing `"` can't break out of the C# string, and a query containing `'` can't break the shell's single-quoting around `--code` — base64's alphabet (`A-Z a-z 0-9 + / =`) contains neither character, so the query text can never change the code that runs.
- **Read-only.** The snippet only opens the Search window; it does not select, rename, delete, move, import, edit, or save anything.

## Background

Pending owner review.
