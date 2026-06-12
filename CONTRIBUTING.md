# Contributing

Thanks for wanting to add a skill. This repo is a shared, public collection of agent skills for Unity workflows, and new contributions are welcome.

## Before you start

This is a **public** repository on the open [skills.sh](https://skills.sh) standard. Whatever you contribute ships publicly, so a skill must not reference anything internal — no internal services, internal URLs, credentials, or confidential workflows. If a skill only makes sense inside Unity's network, it doesn't belong here.

## Skill structure

Each skill is a folder under `skills/`, with a `SKILL.md` at its root:

```
skills/
  your-skill/
    SKILL.md
```

`SKILL.md` starts with YAML frontmatter and then the instructions the agent follows. The `unity-cli` skill is a good template to copy:

```yaml
---
name: your-skill
description: Use when ... — one or two sentences describing exactly when an agent should reach for this skill.
allowed-tools:
  - Bash
---
```

- `name` — matches the folder name, kebab-case.
- `description` — this is what the agent reads to decide whether to trigger the skill, so be concrete about the situations it applies to. Lead with "Use when …".
- `allowed-tools` — optional; list the tools the skill needs (omit it if there's no restriction).

A README, CHANGELOG, and reference `.md` files alongside `SKILL.md` are all fine — the installer pulls the whole folder. Keep `SKILL.md` itself focused on instructions and push long reference material into separate files the skill links to.

Submit skills as plain folders committed to the repo. Don't check in a zipped `.skill` archive — agents read `SKILL.md` directly, and that's what the skills.sh tooling and review expect. You're free to package a `.skill` for distribution elsewhere.

## Versioning

There's no per-skill release mechanism. Versioning is just the repo's git history and PRs. A `CHANGELOG.md` inside your skill folder is welcome as documentation, but nothing automated reads it.

## Submitting

1. Add your skill folder under `skills/`.
2. Test it with realistic prompts in your own agent first.
3. Open a PR. Keep it to one skill per PR where you can.
4. A maintainer reviews and merges.

## Help

Questions or feedback? Post in the [Unity Discussions forum](https://discussions.unity.com/).
