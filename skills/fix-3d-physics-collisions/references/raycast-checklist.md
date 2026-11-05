# Raycast checklist

Work through these steps when `Physics.Raycast` doesn't hit an object and no raycast Fast-Path in the skill matched. Stop at the first step that confirms the cause. Replace `<VERSION>` in links with the project's Unity major.minor version, for example `6000.3`, and read the [Physics.Raycast](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/physics/raycast.md) page before changing the call.

1. **LayerMask.** If the call passes a `layerMask`, check that it includes the target layer. Build the mask with `LayerMask.GetMask("LayerName")`, because a hand-written bit mask is easy to get wrong.
2. **Non-convex MeshCollider on a dynamic Rigidbody.** If the Console shows the dynamic Rigidbody error in the skill's Console errors table, apply the fix in that row.
3. **Back faces.** If the ray reaches a MeshCollider from behind, read the mesh face direction section of [Introduction to Mesh colliders](https://docs.unity.com/en-us/engine/<VERSION>/manual/physics-section/physics-overview/collision-section/collider-shapes/mesh-colliders/introduction.md), and move the ray origin to the outward side of the mesh. Only enable **Queries Hit Backfaces** if the ray must start inside a hollow mesh, because it changes every query in the project.
4. **UI targets.** If the target is a UI Canvas element, use the UI's `GraphicRaycaster`, because `Physics.Raycast` only hits colliders.
