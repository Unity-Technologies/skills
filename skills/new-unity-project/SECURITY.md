# Security notes: new-unity-project

This skill has the agent install Unity Editors and packages, create a cloud-linked project, run C#
inside the user's Editor (the visual baseline), write one small bootstrap script into the project,
and append one line to the project's CLAUDE.md or AGENTS.md. Automated skill scanners flag several
of these as powerful capabilities. They are intentional, and limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's Editor | It only reaches the Editor the user already has open, on their own machine, as that user; the commands sit behind the Pipeline capability gate. No code fetched from a remote source is run — the agent runs C# it writes from this skill's and its delegated skills' own recipes. |
| `SEC_FILE_WRITE` | Writes one bootstrap script (Step 6) and appends one line to CLAUDE.md/AGENTS.md (Step 9) | Both writes land inside the project the skill just created and both are surfaced before they run: in paved mode as notice with an opt-out on the assumptions manifest ("say the word to change any of these"), in guided mode as per-step asks that are true consent. Both are reversible on a word, and the scope section caps the skill at exactly one bootstrap script. |
| `SEC_INSTALL` | Installs Editors, modules, packages, and (with the user's yes) agent skills | Installs go through the Unity CLI's own commands and the PackageManager API — never `curl \| sh`, never hand-edited manifests. Skill installs always ask first (Before Step 1). Versions and the package list are declared before installing. |
| `SEC_CLOUD` | Creates a cloud-linked Unity project with diagnostics per the CLI's defaults | The link is `projects create`'s own default behavior, narrated to the user from the create result. Sign-in is never automated — it is a human gate. `--bare` and the link opt-outs are honored on the user's word. |

## Mitigations

- **The manifest is the notice-and-opt-out surface** in paved mode: every assumption — the file
  writes and the cloud-link narration included — prints in one block before the create launches,
  and any line reverses on a word. Consent in the strict sense comes from guided mode's per-step
  asks; paved mode's bar is that nothing runs unannounced and nothing is irreversible.
- **Human gates are never consumed.** Sign-in and publishing always ask the user.
- **Only the user's own Editor and account.** Everything runs on this machine as this user.
- **No remote code.** All executed C# comes from this skill's text and its delegated skills.
- **Named commands first.** Where a dedicated `unity` command covers a step, the skill uses it.
