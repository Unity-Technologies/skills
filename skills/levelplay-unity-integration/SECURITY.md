# Security notes: levelplay-unity-integration

This skill does two things that automated skill scanners flag as powerful. When the user chooses it, the agent calls the LevelPlay Management API to find or create the user's LevelPlay app and ad units (`references/management-api.md`). In its final step, when the agent suspects a fault, it can also run read-only C# in the user's open Unity Editor through `unity command script eval`. Both are intentional, and the safeguards below limit them. The only package the skill has the user install is Ads Mediation (`com.unity.services.levelplay`), through **Package Manager**.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| Authenticated network calls | Sends HTTPS requests to `platform.ironsrc.com` with a secret key and refresh token read from the environment variables `LEVELPLAY_SECRET_KEY` and `LEVELPLAY_REFRESH_TOKEN` | The user sets the variables themselves and chooses this path over the dashboard. The calls go only to the LevelPlay platform host that issued the credentials, and only to the documented auth, applications and ad units endpoints. |
| Resources that cannot be deleted through the API | Creates apps and ad units on the user's LevelPlay account | The API has no delete call, so the agent lists what exists first, reuses it, and creates only what is missing, after the user confirms a table of exactly what will be created. |
| `SEC_POWER_CAP` | Runs C# in the user's open Unity Editor through `unity command script eval`, only in the final step, to read Play mode state when you suspect a fault | It reaches only the Editor the user already has open, on their own machine, so it grants nothing the user can't do themselves. The skill uses it to read state, not to change the project; project changes go through **Package Manager** and file edits the user approves step by step. |

## Mitigations

- **Credentials from the environment only.** The agent never asks the user to paste the secret key or refresh token, references them only as `$LEVELPLAY_SECRET_KEY` and `$LEVELPLAY_REFRESH_TOKEN` in commands, and checks them with `test -n` rather than printing them.
- **Never echoed, logged or written.** The credentials and the bearer token are never printed, never sent with verbose or tracing flags, and never written to a project file, an `.env` inside the project, a commit or generated C#.
- **Explicit confirmation before any create call.** The agent shows one table of every app and ad unit it will create, states that they cannot be deleted through the API, and proceeds only on an explicit yes.
- **Reuse before create.** Existing apps are matched by name and platform, and existing ad units by format, so a repeated run creates nothing new.
- **Project names never enter a shell command.** The app and ad unit names come from the project's files. The agent writes each create call's JSON body to a temporary file outside the project with its file-writing tool, checks it with `python3 -m json.tool`, sends it with `curl --data-binary @file`, and deletes the file, so a quote or other shell character in a name cannot end the command early or run anything.
- **Documented endpoints only.** The agent calls only the auth, applications and ad units endpoints described in the LevelPlay API documentation. It does not enable networks, change billing or take an app live.
- **Read-only Editor checks.** `script eval` snippets only read and return values. They don't edit assets, settings, or scenes, and they run only during the one Play mode session the final step allows.
- **No third-party hosts.** No request goes anywhere but `platform.ironsrc.com`, and no response is executed.
