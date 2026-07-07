# Choosing a Unity Editor version

Explain these trade-offs to the user before they pick, then show real current versions with
the CLI. When in doubt, **default to the latest LTS.**

## The three streams

| Stream | What it is | Support window | Use it when |
|---|---|---|---|
| **LTS** (Long-Term Support) | The most stable release of a major version. Only bug/security patches land after it ships — no new features. | Longest (roughly 2 years of patches). | You're shipping a real game, working toward a launch, or want maximum stability. **This is the default recommendation.** |
| **Supported / Tech stream** | The newest *stable* feature releases (e.g. the mid-cycle innovation releases). Newer features than LTS, still production-usable, but a shorter support window and more churn. | Shorter than LTS. | You need a feature that isn't in the current LTS yet, and can tolerate faster version churn. |
| **Pre-release** (alpha / beta) | Preview builds of an upcoming version. Unstable, APIs can change, not for production. | None (preview only). | You're evaluating what's coming, or preparing a project for a future version. Never for a game you intend to ship soon. |

**Rule of thumb:** ship on LTS; reach for a Supported/Tech release only for a specific feature;
use pre-releases only to evaluate. A deadline (game jam, commercial launch) always argues for LTS.

## Show the real options

Version numbers move constantly — don't guess them, list them:

```bash
# Latest LTS releases (recommended default)
unity releases --stream lts --limit 5 --format json

# Newer Supported / Tech-stream releases
unity releases --stream tech --limit 5 --format json

# Pre-releases (evaluation only)
unity releases --stream beta --limit 5 --format json
unity releases --stream alpha --limit 5 --format json

# What's already installed on this machine
unity editors --installed --format json
```

The CLI also accepts the aliases `lts` and `latest` wherever a version is expected
(`unity install lts`, `unity editors default lts`), so you can install the current LTS without
pinning an exact number.

## Install with the right modules

Install the chosen version together with the platform modules the target platforms (Step 2)
require — otherwise the project can't build for them:

```bash
# Latest LTS + mobile modules
unity install lts --module android --module ios --yes --accept-eula

# A specific version + WebGL
unity install 6000.0.47f1 --module webgl --yes --accept-eula
```

Common module names: `android`, `ios`, `webgl`, `windows-mono`, `mac-mono`, `linux-mono`.
List what a version offers with `unity install-modules --editor-version <version> --list`.

See the **`unity-cli`** skill for the full `install`, `releases`, and module command reference.
