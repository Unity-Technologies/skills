# Unity Skills

A collection of reusable AI agent skills for Unity workflows. Compatible with Claude Code, GitHub Copilot, Cursor, Cline, and [50+ other agents](https://skills.sh).

## Install

```bash
npx skills add Unity-Technologies/skills
```

## Available skills

| Skill | Description |
|---|---|
| `new-unity-project` | Guided flow from an idea to a running, version-controlled project — gathers concept, platforms, and monetization, then delegates setup to the skills below |
| `unity-cli` | Interact with the Unity CLI — bootstrap a new project from scratch, install editors, manage projects, run builds, check auth, and more |
| `unity-package-management` | Add, remove, upgrade, or discover Unity (UPM) packages programmatically — headless/CI installs via the C# PackageManager Client API, and choosing packages by genre/platform/monetization |
| `build-live-game` | Build and operate a live game with Unity Gaming Services — auth, cloud save, cloud code, economy, remote config, leaderboards, and more |
| `implement-in-app-purchases` | Implement, configure, and debug Unity In-App Purchases (IAP) |
| `levelplay-unity-integration` | Integrate LevelPlay (IronSource) ad mediation — rewarded, interstitial, and banner ads |
| `ui` | Router for Unity UI work — detects the project's UI system and routes to `ui-uitk`, `ui-ugui`, or `ui-imgui` |
| `ui-uitk` | UI Toolkit (Unity 6.0+) — author UXML/USS, flex layout, custom elements, Painter2D, runtime binding |
| `ui-ugui` | uGUI — Canvas hierarchies, RectTransform anchoring, Layout Groups, prefab UI |
| `ui-imgui` | IMGUI editor tooling — EditorWindows, custom Inspectors, PropertyDrawers |
| `optimize-text-mesh-pro` | TextMeshPro font stacks, dynamic atlases, SDF quality, CJK fallback, and text memory |
| `localization` | Unity Localization — locales, String and Asset Tables, CJK fonts, Addressables |

## Usage

Once installed, your agent will automatically use the relevant skill when you ask it to perform Unity CLI operations. For example:

> "Install Unity 6000.0.47f1 with the Android module"
> "List my registered projects"
> "Run the project in headless mode"

## Contributing

Want to add a skill? See [CONTRIBUTING.md](CONTRIBUTING.md) for the folder layout, `SKILL.md` format, and PR flow.

## Issues and feedback

Found a bug or have a suggestion? Post in the [Unity Discussions forum](https://discussions.unity.com/).
