# LevelPlay Unity Integration Skill

![Beta](https://img.shields.io/badge/status-beta-orange) ![Version](https://img.shields.io/badge/version-0.7.0-blue) ![License](https://img.shields.io/badge/license-Unity%20Companion-blue)

> 🧪 **Note:** This skill is in beta and will be shaped by your feedback. Try it out and [let us know what you think](https://docs.google.com/forms/d/e/1FAIpQLSe7WvWozJ67KjgOLglSBvLug8JdgEYk895nn_BHZs0HS_bWJA/viewform)!

A skill that guides Unity developers through integrating the LevelPlay SDK using the Ads Mediation package in Unity Package Manager: from installation to fully working rewarded ads, interstitials, and banners.

Compatible with Claude Code, GitHub Copilot, Cursor, Cline, and [50+ other agents](https://skills.sh).

## What it does

When you activate this skill, your agent walks you step by step through the complete LevelPlay integration:

1. **Installing the SDK** via the Ads Mediation package in Unity Package Manager
2. **Resolving native dependencies** for Android and iOS builds
3. **Collecting credentials** from the LevelPlay dashboard
4. **Configuring privacy compliance** (GDPR, CCPA, COPPA) if needed
5. **Initializing the SDK** in your project, with three code organization options to fit your existing setup
6. **Recommending an ad unit strategy** based on your goals (revenue-focused, UX-focused, or balanced)
7. **Implementing ad formats** — rewarded ads, interstitials, and banners — with production-ready C# code
8. **Testing and validating** using mock ads in the Unity Editor and the LevelPlay Test Suite on device

The skill also covers iOS-specific setup (App Tracking Transparency, SKAdNetwork), impression-level revenue tracking (ILRD) for analytics platforms, bid floors, and common troubleshooting.

## Requirements

- A Unity project using an LTS or actively developed version of the Unity Editor
- LevelPlay Unity package and SDK version 9.4.0+
- A LevelPlay account: [get started here](https://platform.ironsrc.com/)

Documentation for setting up the LevelPlay Unity package: see the [Unity Package Integration guide](https://docs.unity.com/en-us/grow/levelplay/sdk/unity/package-integration).

## Installation

```bash
npx skills add Unity-Technologies/skills
```

Then activate the `levelplay-unity-integration` skill in your agent.

## Using the skill

Type `/levelplay-unity-integration` to activate the skill, then describe what you want to do:

- *"I want to add rewarded ads to my Unity game"*
- *"Help me integrate LevelPlay into my project"*
- *"I need to add interstitial ads between levels"*
- *"I have LevelPlay installed, help me implement banner ads"*

You can jump in at any step. If the Unity package and SDK are already installed, your agent will pick up from where you are.

## Privacy & Legal

> **Note:** This skill provides technical integration guidance, including for LevelPlay's privacy APIs. It is not legal advice, and it does not determine which laws apply to your app — that depends on your users, your data practices, and your distribution. Consult your own legal counsel, and refer to [Regulation Advanced Settings for Unity](https://docs.unity.com/en-us/grow/levelplay/sdk/unity/regulation-advanced-settings) for the authoritative LevelPlay documentation.

## What's in this folder

```
levelplay-unity-integration/
├── SKILL.md                   # Core skill instructions
├── references/                # Detailed API guides for each ad format
│   ├── rewarded-api.md
│   ├── interstitial-api.md
│   ├── banner-api.md
│   ├── ios-setup.md
│   ├── privacy-settings.md
│   ├── ilrd-api.md
│   ├── initialization-api.md
│   └── best-practices.md
├── CHANGELOG.md
└── README.md
```

