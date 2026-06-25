# Tooling Reference

## Table of Contents

- [Overview](#overview)
- [Access Control](#access-control)
  - [`.ac` File Format](#ac-file-format)
  - [URN Pattern Format](#urn-pattern-format)
  - [URN Reference by Service](#urn-reference-by-service)
  - [Recommended Pattern: Deny-All-Then-Allow](#recommended-pattern-deny-all-then-allow)
- [Game Overrides](#game-overrides)
  - [`.ugo` File Format](#ugo-file-format)
- [Editor API Models](#editor-api-models)

Editor-only package. Registers **Access Control** (`.ac`) and **Game Overrides** (`.ugo`) file types with the Deployment Window (`com.unity.services.deployment`).

- **Package:** `com.unity.services.tooling`
- **Namespace:** `Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Model` / `Unity.Services.Tooling.Editor.GameOverrides.Authoring.Core.Model`

---

## Overview

Tooling provides two cloud resource file types deployed through the Deployment Window:

| File Type | Extension | Purpose |
|---|---|---|
| Access Control | `.ac` | Permit or deny player/service-account access to UGS services on a URN basis |
| Game Overrides | `.ugo` | A/B testing and audience targeting by overriding Remote Config values for player segments |

Both file types are created via right-click in the Project window → **Create > Unity Gaming Services** and deployed via **Services > Deployment**.

---

## Access Control

Access Control policies permit or deny access to UGS services on a **URN basis**. Each statement targets a resource URN pattern and applies a `Read`, `Write`, or `*` action for a principal (`Player` or `ServiceAccount`). **Deny takes precedence over Allow.**

| Concept | Description |
|---|---|
| **Effect** | `"Allow"` or `"Deny"` — Deny takes precedence over Allow |
| **Action** | `"Read"`, `"Write"`, or `"*"` (both) |
| **Principal** | `"Player"` (end user) or `"ServiceAccount"` (server/admin) |
| **Resource** | URN pattern — `urn:ugs:<service>:/<path>` with `*` and `**` wildcards |

Common use cases:
- Deny direct player writes to Cloud Save (force writes through Cloud Code)
- Deny player currency/inventory writes in Economy (only allow purchases)
- Restrict Cloud Code module/script invocation to specific endpoints
- Deny player account deletion or identity unlinking

### `.ac` File Format

**Create via:** Right-click in Project window → **Create > Unity Gaming Services > Access Control**

```json
{
  "$schema": "https://ugs-config-schemas.unity3d.com/v1/project-access-policy.schema.json",
  "statements": [
    {
      "Sid": "DenyPlayerCloudSaveWrites",
      "Effect": "Deny",
      "Action": ["Write"],
      "Principal": "Player",
      "Resource": "urn:ugs:cloud-save:/**"
    }
  ]
}
```

| Field | Type | Description |
|---|---|---|
| `Sid` | `string` | Unique statement ID within the file |
| `Effect` | `string` | `"Allow"` or `"Deny"` — Deny wins over Allow |
| `Action` | `string` or `string[]` | `"Read"`, `"Write"`, or `"*"` (both) |
| `Principal` | `string` or `string[]` | `"Player"` (end user) or `"ServiceAccount"` (server/admin) |
| `Resource` | `string` or `string[]` | URN pattern — `urn:ugs:<service>:/<path>` |

### URN Pattern Format

```
urn:ugs:<service>:/<path>
```

- `*` matches a single path segment
- `**` matches zero or more path segments (including nested)
- `/**/` is commonly used to skip environment/version segments in the URL

### URN Reference by Service

#### Authentication (`player-auth`)

| URN Pattern | Description |
|---|---|
| `urn:ugs:player-auth:/*/authentication/anonymous**` | Anonymous sign-up |
| `urn:ugs:player-auth:/*/authentication/external-token**` | External token sign-in (social, platform) |
| `urn:ugs:player-auth:/*/authentication/session-token**` | Session token sign-in |
| `urn:ugs:player-auth:/*/authentication/link/**` | Link external identity |
| `urn:ugs:player-auth:/*/authentication/unlink/**` | Unlink external identity |
| `urn:ugs:player-auth:/*/users**` | Player info (Read) / Delete player (Write) |
| `urn:ugs:player-auth:/.well-known/**` | JWKS public keys |

#### Cloud Save (`cloud-save`)

| URN Pattern | Description |
|---|---|
| `urn:ugs:cloud-save:/**` | All Cloud Save (blanket) |
| `urn:ugs:cloud-save:/**/players/*/keys**` | List player data keys |
| `urn:ugs:cloud-save:/**/players/*/items**` | Read/write player data items (own data only) |
| `urn:ugs:cloud-save:/**/players/*/item-batch**` | Batch write player data items |
| `urn:ugs:cloud-save:/**/players/query**` | Query Default player data |
| `urn:ugs:cloud-save:/**/players/*/public/keys**` | Read another player's public keys |
| `urn:ugs:cloud-save:/**/players/*/public/items**` | Read another player's public items |
| `urn:ugs:cloud-save:/**/players/public/query**` | Query Public player data |
| `urn:ugs:cloud-save:/**/custom/*/items**` | Read game-wide (Custom) data items |

#### Economy (`economy`)

| URN Pattern | Description |
|---|---|
| `urn:ugs:economy:/**/players/*/config**` | Read player economy configuration |
| `urn:ugs:economy:/**/currencies**` | Player currencies (Read/Write) |
| `urn:ugs:economy:/**/inventory**` | Player inventory (Read/Write) |
| `urn:ugs:economy:/**/purchases/virtual**` | Virtual purchases |
| `urn:ugs:economy:/**/purchases/googleplaystore**` | Google Play Store purchases |
| `urn:ugs:economy:/**/purchases/appleappstore**` | Apple App Store purchases |

#### Leaderboards (`leaderboards`)

| URN Pattern | Description |
|---|---|
| `urn:ugs:leaderboards:/**/leaderboards/**` | All leaderboard operations |

#### Cloud Code (`cloud-code`)

| URN Pattern | Description |
|---|---|
| `urn:ugs:cloud-code:/**/modules/**` | Cloud Code module endpoints |
| `urn:ugs:cloud-code:/**/scripts/**` | Cloud Code scripts (legacy JS) |
| `urn:ugs:cloud-code:/**/subscriptions/tokens/**` | Subscription tokens |

### Recommended Pattern: Deny-All-Then-Allow

Start with a blanket deny, then explicitly allow only what the player needs. This ensures new services are locked down by default.

```json
{
  "$schema": "https://ugs-config-schemas.unity3d.com/v1/project-access-policy.schema.json",
  "statements": [
    {
      "Sid": "Deny-all-ugs-access",
      "Effect": "Deny",
      "Action": ["*"],
      "Principal": "Player",
      "Resource": "urn:ugs:*:/**"
    },
    {
      "Sid": "Allow-Anonymous-SignUp",
      "Effect": "Allow",
      "Action": ["Write"],
      "Principal": "Player",
      "Resource": "urn:ugs:player-auth:/*/authentication/anonymous**"
    },
    {
      "Sid": "Allow-External-Token-SignIn",
      "Effect": "Allow",
      "Action": ["Write"],
      "Principal": "Player",
      "Resource": "urn:ugs:player-auth:/*/authentication/external-token**"
    },
    {
      "Sid": "Allow-Session-Token-SignIn",
      "Effect": "Allow",
      "Action": ["Write"],
      "Principal": "Player",
      "Resource": "urn:ugs:player-auth:/*/authentication/session-token**"
    },
    {
      "Sid": "Allow-Link-External-Id",
      "Effect": "Allow",
      "Action": ["Write"],
      "Principal": "Player",
      "Resource": "urn:ugs:player-auth:/*/authentication/link/**"
    },
    {
      "Sid": "Allow-Get-PlayerInfo",
      "Effect": "Allow",
      "Action": ["Read"],
      "Principal": "Player",
      "Resource": "urn:ugs:player-auth:/*/users**"
    },
    {
      "Sid": "Allow-Get-JWKS",
      "Effect": "Allow",
      "Action": ["Read"],
      "Principal": "Player",
      "Resource": "urn:ugs:player-auth:/.well-known/**"
    },
    {
      "Sid": "Allow-Read-Economy-Config",
      "Effect": "Allow",
      "Action": ["Read"],
      "Principal": "Player",
      "Resource": "urn:ugs:economy:/**/players/*/config**"
    },
    {
      "Sid": "Allow-Read-Currencies",
      "Effect": "Allow",
      "Action": ["Read"],
      "Principal": "Player",
      "Resource": "urn:ugs:economy:/**/currencies**"
    },
    {
      "Sid": "Allow-Read-Inventory",
      "Effect": "Allow",
      "Action": ["Read"],
      "Principal": "Player",
      "Resource": "urn:ugs:economy:/**/inventory**"
    },
    {
      "Sid": "Allow-Virtual-Purchases",
      "Effect": "Allow",
      "Action": ["Write"],
      "Principal": "Player",
      "Resource": "urn:ugs:economy:/**/purchases/virtual**"
    },
    {
      "Sid": "Allow-GooglePlay-Purchases",
      "Effect": "Allow",
      "Action": ["Write"],
      "Principal": "Player",
      "Resource": "urn:ugs:economy:/**/purchases/googleplaystore**"
    },
    {
      "Sid": "Allow-AppStore-Purchases",
      "Effect": "Allow",
      "Action": ["Write"],
      "Principal": "Player",
      "Resource": "urn:ugs:economy:/**/purchases/appleappstore**"
    },
    {
      "Sid": "Allow-Read-Leaderboards",
      "Effect": "Allow",
      "Action": ["Read"],
      "Principal": "Player",
      "Resource": "urn:ugs:leaderboards:/**/leaderboards/**"
    },
    {
      "Sid": "Allow-Read-CloudSave-PlayerKeys",
      "Effect": "Allow",
      "Action": ["Read"],
      "Principal": "Player",
      "Resource": "urn:ugs:cloud-save:/**/players/*/keys**"
    },
    {
      "Sid": "Allow-CloudSave-PlayerItems",
      "Effect": "Allow",
      "Action": ["*"],
      "Principal": "Player",
      "Resource": "urn:ugs:cloud-save:/**/players/*/items**"
    },
    {
      "Sid": "Allow-CloudSave-PlayerItemBatch",
      "Effect": "Allow",
      "Action": ["Write"],
      "Principal": "Player",
      "Resource": "urn:ugs:cloud-save:/**/players/*/item-batch**"
    },
    {
      "Sid": "Allow-Query-Default-PlayerData",
      "Effect": "Allow",
      "Action": ["Write"],
      "Principal": "Player",
      "Resource": "urn:ugs:cloud-save:/**/players/query**"
    },
    {
      "Sid": "Allow-Read-Public-PlayerKeys",
      "Effect": "Allow",
      "Action": ["Read"],
      "Principal": "Player",
      "Resource": "urn:ugs:cloud-save:/**/players/*/public/keys**"
    },
    {
      "Sid": "Allow-Read-Public-PlayerItems",
      "Effect": "Allow",
      "Action": ["Read"],
      "Principal": "Player",
      "Resource": "urn:ugs:cloud-save:/**/players/*/public/items**"
    },
    {
      "Sid": "Allow-Query-Public-PlayerData",
      "Effect": "Allow",
      "Action": ["Write"],
      "Principal": "Player",
      "Resource": "urn:ugs:cloud-save:/**/players/public/query**"
    },
    {
      "Sid": "Allow-Read-GameData",
      "Effect": "Allow",
      "Action": ["Read"],
      "Principal": "Player",
      "Resource": "urn:ugs:cloud-save:/**/custom/*/items**"
    },
    {
      "Sid": "Allow-CloudCode-Modules",
      "Effect": "Allow",
      "Action": ["*"],
      "Principal": "Player",
      "Resource": "urn:ugs:cloud-code:/**/modules/**"
    },
    {
      "Sid": "Allow-CloudCode-Scripts",
      "Effect": "Allow",
      "Action": ["*"],
      "Principal": "Player",
      "Resource": "urn:ugs:cloud-code:/**/scripts/**"
    },
    {
      "Sid": "Allow-Subscription-Tokens",
      "Effect": "Allow",
      "Action": ["*"],
      "Principal": "Player",
      "Resource": "urn:ugs:cloud-code:/**/subscriptions/tokens/**"
    }
  ]
}
```

This policy:
- **Denies** all UGS access by default
- **Allows** authentication (anonymous, external, session token sign-in; link external ID; read player info)
- **Denies** identity unlinking and player deletion (inherited from blanket deny)
- **Allows** economy reads (config, currencies, inventory) and purchases (virtual, Google Play, Apple App Store)
- **Denies** direct currency/inventory writes (inherited from blanket deny — use Cloud Code for server grants)
- **Allows** leaderboard reads
- **Allows** Cloud Save player data reads/writes for own data (Default items), public data reads, game-wide data reads
- **Denies** Cloud Save blanket writes (Protected data only writable via Cloud Code)
- **Allows** Cloud Code module, script, and subscription token access

---

## Game Overrides

Game Overrides are the A/B testing and audience targeting mechanism for UGS. They override **Remote Config** values for specific player segments (audiences). Remote Config itself does not provide A/B testing — Game Overrides layer on top of it.

Use cases:
- A/B test different XP multipliers for player segments
- Roll out features gradually to targeted audiences
- Run time-limited promotions with different reward values

### `.ugo` File Format

**Create via:** Right-click in Project window → **Create > Unity Gaming Services > Game Override**

```json
{
  "GameOverrides": [
    {
      "id": "double-xp-weekend",
      "name": "Double XP Weekend",
      "enabled": true,
      "audiences": ["high_engagement_players"],
      "overrides": [
        {
          "key": "xp_multiplier",
          "value": 2.0
        }
      ]
    }
  ]
}
```

| Field | Type | Description |
|---|---|---|
| `id` | `string` | Unique override ID |
| `name` | `string` | Human-readable name |
| `enabled` | `bool` | Whether this override is active |
| `audiences` | `string[]` | Segment IDs from Unity Segmentation |
| `overrides[].key` | `string` | Remote Config key to override |
| `overrides[].value` | `any` | Replacement value |

---

## Editor API Models

### Access Control

```csharp
namespace Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Model
{
    interface IProjectAccessFile
    {
        string Path { get; }
        string Name { get; }
        List<IAccessControlStatement> Statements { get; }
    }

    interface IAccessControlStatement
    {
        string Sid { get; }          // Statement ID — unique within the file
        string Effect { get; }       // "Allow" or "Deny" — Deny takes precedence
        List<string> Action { get; } // "Read", "Write", or "*" (both)
        List<string> Principal { get; } // "Player" or "ServiceAccount"
        List<string> Resource { get; }  // URN pattern(s) the rule applies to
    }
}
```

### Game Overrides

```csharp
namespace Unity.Services.Tooling.Editor.GameOverrides.Authoring.Core.Model
{
    interface IGameOverride
    {
        string Id { get; }
        string Name { get; }
        bool Enabled { get; }
        List<string> Audiences { get; }
        List<OverrideConfig> Overrides { get; }
    }

    class GameOverridesConfigFile
    {
        public List<IGameOverride> GameOverrides { get; }
    }
}
```

Both file types are discovered automatically by the Deployment Window when `com.unity.services.deployment` is installed.
