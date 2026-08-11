---
name: setup-game-inputs
description: Handles game input setup and configuration — player controls, action maps, bindings, control schemes, and Input Actions assets. Use when setting up keyboard, mouse, or gamepad input, configuring PlayerInput or UI input, generating C# input wrappers, implementing rebinding, or working with the Input System package or Legacy Input Manager.
---

## Step 1: Determine Active Input Handler

The system prompt context should provide the current active input system. If not check the project setting.

- **Input System Package (New)** — new Input System only; `ENABLE_INPUT_SYSTEM` is defined.
- **Input Manager (Old)** — legacy Input Manager only; `ENABLE_LEGACY_INPUT_MANAGER` is defined.
- **Both** — both defines are set.

Changing this setting requires an Editor restart. Read the matching path below before deep implementation.

## Path A: Input System (new)

**When:** Active Input Handler is **Input System Package (New)**.
Read [input-system.md](references/input-system.md)

## Path B: Legacy Input Manager (old)

**When:** Active Input Handler is **Input Manager (Old)** only.
Use **Project Settings > Input Manager** axes and `UnityEngine.Input` (`GetAxis`, `GetButton`, etc.).


## Path C: Both

**When:** Active Input Handler is **Both**.

Treat **Input System (new)** as the default.  Read reference [input-system.md](references/input-system.md)

