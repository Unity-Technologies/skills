# Blueprints, Building Blocks and Sample Projects

## Table of Contents

- [Choosing a Starting Point](#choosing-a-starting-point)
- [Cloud Code in Building Blocks and Samples](#cloud-code-in-building-blocks-and-samples)
- [Feature Blueprints](#feature-blueprints)
- [Asset Store Building Blocks](#asset-store-building-blocks)
- [Related Sample Projects](#related-sample-projects)

---

## Choosing a Starting Point

1. For a battle pass, achievements or player accounts, start from this skill's feature blueprint, because the blueprints use `.ccmu` modules and generated client bindings. These need Unity 6000.5+; on an older Editor, put the blueprint's module code in a `.ccmr` module (see the Legacy Modules section of `references/cloud-code-modules.md`) and call it with `CallModuleEndpointAsync`.
2. For UI, player flows and cloud resource files (`.ac`, `.rc`, `.lb`), reuse the matching Building Block or sample project, because those parts are ready to import.
3. For a feature with neither, follow the service references and the architecture patterns in the skill.

---

## Cloud Code in Building Blocks and Samples

The Building Blocks and sample projects predate native module authoring. Their Cloud Code is legacy `.ccmr` modules (a `.sln` and `.csproj` built outside Unity) or JavaScript scripts.

- On Unity 6000.5+, do not copy their solution scaffolding, `.ccmr` files or raw `CallModuleEndpointAsync` calls into new work. On an older Editor, which cannot author `.ccmu`, `.ccmr` is the supported path.
- On Unity 6000.5+, to reuse a block's server logic, move each `[CloudCodeFunction]` class into a new `.ccmu` module and call it through the module's generated client. The Creating a Cloud Code Module and Migrating a `.ccmr` to `.ccmu` sections of `references/cloud-code-modules.md` cover both steps.
- An imported block's `.ccmr` still deploys and runs. Maintain it as legacy until you port it, rather than rewriting it unasked.

---

## Feature Blueprints

Implementation-ready blueprints in this skill. Each includes data models, client code, a `.ccmu` Cloud Code module and cloud resource definitions.

| Feature | Services | Blueprint |
|---|---|---|
| **Battle Pass** | Remote Config (pass definitions), Cloud Save Protected (progress), Cloud Code (XP awards, reward claims, premium purchase stub) | `references/battlepass.md` |
| **Achievements** | Remote Config (definitions), Cloud Save (player records), Cloud Code (server-authoritative unlocks), Access Control | `references/achievements.md` |
| **Player Account** | Authentication (anonymous, Unity browser, username/password), Cloud Save (Default, Public and Protected player data) | `references/player-account.md` |

---

## Asset Store Building Blocks

Free Asset Store packages, each a `.unitypackage` with working UI, runtime code, Cloud Code and cloud resource files for one feature. They are also bundled in the `com.unity.starter-kits` package.

| Building Block | Type | Key Dependencies | Source |
|---|---|---|---|
| **Achievements** | LiveOps | `cloudsave`, `remote-config`, `cloudcode`, `tooling`, `deployment`, `analytics`, `authentication` | [Asset Store](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-achievements-341918) |
| **Leaderboards** | LiveOps | `leaderboards`, `cloudcode`, `tooling`, `deployment`, `authentication` | [Asset Store](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-leaderboards-341926) |
| **Player Account** | LiveOps | `authentication`, `cloudsave`, `cloudcode`, `deployment` | [Asset Store](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-player-account-341928), or `com.unity.starter-kits` |
| **Multiplayer Session** | Multiplayer | `multiplayer` | [Asset Store](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-multiplayer-session-341930) |
| **Matchmaker Session** | Multiplayer | `multiplayer`, `deployment` | [Asset Store](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-matchmaker-session-341932) |
| **Server Session** | Multiplayer | `multiplayer`, `apis`, `deployment` | `com.unity.starter-kits` |
| **Vivox** | Multiplayer | `vivox`, `authentication` | `com.unity.starter-kits` |
| **Platformer Starter Kit** | Gameplay | `multiplayer`, `netcode.gameobjects`, URP, Cinemachine, Input System | `com.unity.starter-kits` |

What the LiveOps blocks demonstrate:

- **Achievements:** achievement records in Cloud Save Protected, written through Cloud Code, with an Access Control policy denying direct player writes.
- **Leaderboards:** score handling and leaderboard admin functions in Cloud Code, with player score data in Cloud Save.
- **Player Account:** sign-in UI (anonymous, Unity browser, username/password), and profile data in the Default and Public access classes written directly from the client.

---

## Related Sample Projects

| Project | Description | Source |
|---|---|---|
| **Use Case Samples** | Battle Pass, Virtual Shop, Daily Rewards, Starter Pack, Cloud AI Mini Game, A/B testing | [GitHub](https://github.com/Unity-Technologies/com.unity.services.samples.use-cases) |
| **UGS Samples** | Authentication flows, Remote Config, Cloud Code integration | [GitHub](https://github.com/Unity-Technologies/com.unity.services.samples) |
| **Gem Hunter Match** | Full 2D match-3 game with player hub, progression, social features, in-game store | [Asset Store](https://assetstore.unity.com/packages/essentials/tutorial-projects/gem-hunter-match-2d-sample-project-278941) |
| **Boss Room** | 8-player co-op RPG using Netcode for GameObjects, Authentication, Multiplayer Services | [GitHub](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop) |

Treat the samples' Cloud Code as legacy (see [Cloud Code in Building Blocks and Samples](#cloud-code-in-building-blocks-and-samples)).
