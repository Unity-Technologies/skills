# Ghost authoring

Links in this file resolve to the skill directory's `references/` folder, the folder this file is in. Project and package paths in code spans are relative to the user's project or the package root.

Read this file only after workflow step 3 selected Unity Netcode. It picks the authoring layer. Read *Choose the right approach* on the Netcode `ghostobject/ghostobject-use` page first: both layers share one replication pipeline and can be combined.

## Fallback order (new projects)

1. `GhostObject`/`GhostBehaviour` for a GameObject project.
2. Direct ECS ghosts for an ECS-native project, or when the package has no GameObject layer, the bridge has a reproduced defect, or it cannot express a required system.

## A step down needs evidence

Moving a GameObject project from GhostObject to direct ECS needs a reproduced defect or a named missing API, because direct ECS changes the team's whole workflow. Record which one in the decision record.

## Before you design

- Scene-placed ghosts, hierarchy or predicted Rigidbody: read the installed package's `Documentation~/ghostobject/` pages (`ghostobject-replicate`, `ghostobject-prediction`, `ghostobject-physics`) for the current limits, because they differ between package builds.
- Existing NGO game moving a subsystem: read *NGO upgrade path* in [architecture.md](architecture.md).
