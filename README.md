# Unity Skills

A collection of reusable AI agent skills for Unity workflows. Compatible with Claude Code, GitHub Copilot, Cursor, Cline, and [50+ other agents](https://skills.sh).

## Install

```bash
npx skills add Unity-Technologies/skills
```

### Using Claude Code or Codex?

Install the [official Unity plugin](https://github.com/Unity-Technologies/unity-agent-plugin) instead. It ships these same skills as one package, so they update together and appear in the agent's own plugin and slash-command menus. See the plugin's README for the two install commands.

## Usage

Once installed, your agent uses the relevant skill automatically when you ask it to do something in your Unity project. For example:

> "Add in-app purchases so players can buy a coin pack"
>
> "My pixel art looks blurry and jitters when the camera moves"
>
> "Create a hexagonal tile palette for my level"
>
> "Install Unity 6000.0.47f1 with the Android module"

## Works best with the Unity CLI

Many skills drive your open Unity Editor directly instead of hand-editing scene and asset files. They do this through the [Unity CLI](https://docs.unity.com/en-us/unity-cli), which also installs Editors, creates projects, and runs builds and tests. Your agent can install it when a task needs it, and the Unity Hub installs it automatically. To set it up yourself, see [Use the Unity CLI](https://docs.unity.com/en-us/unity-cli/use-unity-cli).

## Contributing

Want to add a skill? See [CONTRIBUTING.md](CONTRIBUTING.md) for the folder layout, `SKILL.md` format, and PR flow.

## Issues and feedback

Found a bug or have a suggestion? Post in the [Unity Discussions forum](https://discussions.unity.com/).
