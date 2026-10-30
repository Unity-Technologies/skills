# Skill setup — getting the delegated skills loaded

> Reference for the `new-unity-project` skill: the install routes for its delegated skills, and the
> `unity-pipeline` guide's location inside the Pipeline package. The body's "Before Step 1" check says
> *when* to use this; this file says *how*.

## Install routes, by client

If a delegated skill is missing, tell the user which one and what it does for them. Offer to install
the Unity skills before you start, and don't install without a yes, because it changes their agent
setup:

- **Claude Code, with none of the Unity skills installed:** `unity setup claude --format json`
  installs the Unity plugin, which carries all of them. It would duplicate any you already have. An
  older CLI doesn't know `unity setup`, so use the next route then. A fresh install loads in the next
  session, or after `/reload-plugins`. Until then, read the skills from the `skillsPath` it reports.
- **Any other case, including a partial set:** run `unity skill install <client>`, where
  `unity skill install --list` names the clients and where each one installs. It installs the Unity
  skills from the Unity-Technologies/skills repository at the commit this CLI pins, and checks each
  one against that pin before it writes anything. To add only the missing skills, name each with
  `--skill`, which repeats and takes a comma list (`--skill ui,localization`). The source is fixed in
  the CLI, so don't look for a flag to point it elsewhere. Offline, it installs only the two skills
  built into the CLI (`unity-cli` and `new-unity-project`), names the ones it couldn't install, and
  exits 0: run it again once the machine is online.
- **Codex, Grok Build or GitHub Copilot CLI whose plugin can't be installed:** `unity setup <agent>`
  offers the same repo skills instead when the agent's CLI is missing or its plugin install fails.
  With the user's yes, `unity setup <agent> --yes --format json` installs them without a prompt and
  reports `installedVia: "skills"`, the `skillsPath` and the `skillsToLoad` it installed. The same
  works for `unity setup claude`.
- **An older CLI** installs only `unity-cli` and `new-unity-project`. There, tell the user to copy
  each missing skill's folder from github.com/Unity-Technologies/skills (under `skills`) into their
  client's skills folder.

A newly installed skill may not load until the client restarts. If it still isn't listed, ask where
the installer put it and read the skill file from there.

## The `unity-pipeline` guide (read it once the Editor is ready)

It is the Pipeline package's own guide, and no client loads it from the package folder. If it is
missing from your skills, read its skill file directly. It sits under
`.claude/skills/unity-pipeline/` in the package: `Packages/com.unity.pipeline/` if the project embeds
the package, otherwise the `Library/PackageCache/com.unity.pipeline@*` folder. List
`Library/PackageCache` for the exact name, since the suffix is often a hash, not the version.

To have your client load it in later sessions, `unity skill install <client> --local` can mirror it,
but only for a client with a project scope (`unity skill install --list` shows which, and Claude
Desktop has none). It also writes a project copy of `unity-cli` beside it. The `unity-cli` skill
suggests running it once, but here, wait until the user asks, and run it from the project folder
after the open. In Claude Code with the Unity plugin installed it stops short, because the plugin
already carries `unity-cli`. There, with the user's yes, copy the `unity-pipeline` folder into the
project's `.claude/skills/` instead. Where no mirror is possible, read the package copy again in each
session.

## Pipeline failure codes and the stuck-Editor recovery

| Symptom | Cause | Resolution |
|---|---|---|
| `PIPELINE_MANIFEST_WRITE_FAILED` on `unity pipeline install` | the project is open in an Editor | ask the user to close the Editor, re-run the install |
| `STATUS_PIPELINE_LOAD_PENDING` right after an install against an open project | the open Editor loads the package when its window regains focus | ask the user to switch to the Editor window |
| `STATUS_PIPELINE_LOAD_PENDING` that never clears after an open | the Pipeline server can run while its port file never lands (package/editor pairing issues) — focus is not the cause | read `Logs/Editor.log` for `Pipeline:` lines to confirm, then recover below |

**Stuck-Editor recovery:** ask the user first — unsaved work is lost — then
`unity close <project> --force`, which terminates by PID and works without Pipeline connectivity,
and reopen the project.
