# Gather Requirements

How to interpret common requests and what to find out before migrating.

Links to other reference files point to files in this `references/` folder. Any other relative paths resolve to the skill directory.

## 2. Gather Requirements

Determine what the user actually wants. If the request is ambiguous, ask.

Use these defaults for common requests:

| User says | Default interpretation |
|-----------|------------------------|
| "Upgrade this project to URP" | Full-project migration |
| "Move this 2D project to URP" | Built-in 2D to URP 2D migration |
| "Convert these materials" | Targeted material conversion |
| "My materials turned pink" | Post-migration shader/material troubleshooting |
| "Lighting looks wrong after URP" | Quality and visual parity troubleshooting |
| "Do not change anything yet" | Planning / audit only |

### Information to Gather

- **Scope:** full project, selected materials, or troubleshooting only
- **Target renderer:** standard URP or URP 2D
- **Validation targets:** which scenes, prefabs, or cameras matter most to the user
- **Risk tolerance:** whether limited manual follow-up is acceptable
- **Rendering dependencies:** custom shaders, Asset Store shaders, PPv2, baked lighting/lightmaps/probes, image effects, command buffers, replacement shaders, or multiple Quality levels
- **Execution permission:** planning only vs actual project changes
