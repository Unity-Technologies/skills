# UniPay IAP — Assessment and Guidance

## Table of Contents

- [Trigger Phrases](#trigger-phrases)
- [Overview](#overview)
- [Step 1 — Detect UniPay Version and Unity IAP Version](#step-1--detect-unipay-version-and-unity-iap-version)
- [Step 2 — Detect Platform Targets](#step-2--detect-platform-targets)
- [Step 3 — Route Based on Findings](#step-3--route-based-on-findings)

Use this reference when the project has UniPay (FLOBUK) installed and the user asks to migrate, convert, or upgrade IAP. This path does **not** perform a conversion — it assesses the project state and either confirms no action is needed, stops with upgrade advice, or stops due to unsupported platforms.

---

## Trigger Phrases

- "Migrate from UniPay"
- "Convert UniPay to Unity IAP"
- "Remove UniPay"
- "Upgrade UniPay"
- "Replace UniPay with Unity IAP"

---

## Overview

UniPay wraps `com.unity.purchasing` (Unity IAP) as its underlying billing layer for Apple App Store, Google Play, and Amazon Appstore. It also ships its own separate plugins for Steam, Meta Quest/Oculus, PayPal, and Facebook Instant Games — these platforms have **no Unity IAP equivalent** and cannot be migrated.

Because UniPay already uses Unity IAP under the hood, there is no "conversion" to perform for Apple/Google/Amazon billing. The assessment stops at one of three outcomes:

| Outcome | Condition |
|---|---|
| **Unsupported platform — stop** | Project targets Steam, Meta Quest, PayPal, or Facebook Instant Games |
| **Upgrade required — stop** | UniPay is present with Unity IAP v4 |
| **No action needed** | UniPay 6.2.0+ with Unity IAP v5 already installed |

---

## Step 1 — Detect UniPay Version and Unity IAP Version

### 1a — Locate UniPay version

Search for UniPay's version in this order:

1. `Assets/Plugins/UniPay/package.json` — read the `"version"` field.
2. `Assets/Plugins/SIS/package.json` — legacy path (UniPay was formerly "Simple IAP System").
3. `Assets/Plugins/UniPay/Scripts/IAPManager.cs` or any file containing a `VersionNumber` or `version` constant string.
4. `ProjectSettings/ProjectSettings.asset` — search for `UniPay` or `SIS` version keys.

If the version cannot be determined from any of the above, report:

> "UniPay is installed but its version could not be determined. Please check **Edit > Project Settings > UniPay In-App Purchasing** for the installed version and confirm whether it is 6.2.0 or later."

Do not stop — proceed to Step 1b with the version marked as unknown.

### 1b — Check Unity IAP version

Read `Packages/manifest.json` and find the `com.unity.purchasing` entry.

| Result | Note |
|---|---|
| Version matches `4\.` | Unity IAP v4 — see routing in Step 3 |
| Version matches `5\.` | Unity IAP v5 — see routing in Step 3 |
| Absent | Unexpected — UniPay requires Unity IAP; report the missing dependency and stop |

---

## Step 2 — Detect Platform Targets

Search `ProjectSettings/ProjectSettings.asset` for the enabled build targets. Also search `Assets/**/*.cs` and `Assets/Plugins/` for integration signals:

| Signal to search for | Platform implied |
|---|---|
| `Steamworks|SteamManager|SteamAPI|com\.rlabrecque\.steamworks` | Steam (Standalone PC/Mac) |
| `OculusSDK|OVRManager|com\.oculus|Meta Quest|com\.unity\.xr\.oculus` | Meta Quest / Oculus |
| `PayPal|PayPalManager|PAYPAL` in C# or prefabs | PayPal (WebGL) |
| `FacebookSDK|FB\.Init|com\.facebook\.sdk` | Facebook Instant Games |

These platforms use UniPay's own billing plugins — **Unity IAP 5 has no equivalent for any of them.** If any are detected, they must be flagged as blockers (see Step 3, Route C).

---

## Step 3 — Route Based on Findings

Apply the first matching route below.

---

### Route A — Unsupported Platform Detected (stop)

**Condition:** Step 2 found Steam, Meta Quest, PayPal, or Facebook Instant Games signals.

Stop and report:

> "This project uses UniPay's [platform] integration. Unity IAP 5 does not support [platform] — there is no migration path for this billing backend. No changes will be made.
>
> If you want to continue using [platform] billing, keep UniPay. If you are dropping [platform] support entirely, remove the platform-specific UniPay integration manually first, then re-run this skill."

List every unsupported platform found. Do not proceed with any changes.

---

### Route B — Upgrade Required (Unity IAP v4 detected)

**Condition:** `com.unity.purchasing` version matches `4\.`, regardless of UniPay version.

Stop and report:

> "This project uses Unity IAP v4 as UniPay's billing backend. UniPay 6.2.0+ requires Unity IAP v5 and Unity 6.
>
> **Required upgrades before any IAP work can proceed:**
> 1. Upgrade the Unity Editor to the latest stable Unity 6 release.
> 2. Upgrade `com.unity.purchasing` to the latest stable v5 release via **Window > Package Manager**.
> 3. Upgrade UniPay to version 6.2.0 or later — purchase the latest version from the Unity Asset Store and follow FLOBUK's upgrade instructions.
>
> Once all three upgrades are complete, re-run this skill."

Do not make any code or package changes. Do not proceed.

---

### Route C — No Action Needed (UniPay 6.2.0+ with IAP v5)

**Condition:** Unity IAP v5 is installed and UniPay version is 6.2.0 or later (or version is unknown but IAP v5 is confirmed).

Check the exact `com.unity.purchasing` version from `Packages/manifest.json` and include it in the report:

**If `com.unity.purchasing` is v5.0–5.3:**

> "This project already uses UniPay [version] with Unity IAP v5.x as its billing backend. UniPay handles the IAP layer — no migration or conversion is needed.
>
> Note: IAP D2C Capabilities (third-party payment providers such as Stripe or Coda) require Unity IAP v5.4+. If you plan to add D2C support in the future, you will need to upgrade `com.unity.purchasing` to v5.4+ via **Window > Package Manager**.
>
> If you want to add new products, modify purchase logic, or handle platform-specific billing behavior, work within UniPay's API (see FLOBUK documentation). If you want to remove UniPay entirely and use Unity IAP 5 directly, that is a manual refactor — ask specifically for that and describe which UniPay features you are replacing."

**If `com.unity.purchasing` is v5.4+:**

> "This project already uses UniPay [version] with Unity IAP v5.4+ as its billing backend. UniPay handles the IAP layer — no migration or conversion is needed.
>
> If you want to add new products, modify purchase logic, or handle platform-specific billing behavior, work within UniPay's API (see FLOBUK documentation). If you want to remove UniPay entirely and use Unity IAP 5 directly, that is a manual refactor — ask specifically for that and describe which UniPay features you are replacing."

Do not make any changes. This is an informational stop.

---

