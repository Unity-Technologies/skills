# Trigger callback checklist

Work through these steps when `OnTriggerEnter`, `OnTriggerStay`, or `OnTriggerExit` doesn't fire. Stop at the first step that confirms the cause. Replace `<VERSION>` in links with the project's Unity major.minor version, for example `6000.3`.

1. **Is Trigger and Rigidbody.** Read [OnTrigger events](https://docs.unity.com/en-us/engine/<VERSION>/manual/physics-section/physics-overview/collision-section/collider-interactions/ontrigger.md) and [Interaction between collider types](https://docs.unity.com/en-us/engine/<VERSION>/manual/physics-section/physics-overview/collision-section/collider-interactions/collider-types-interaction.md), and check the pair against them. If both are kinematic triggers, apply the skill's CRITICAL FACT instead.
2. **Layers.** Read [Layer-based collision detection](https://docs.unity.com/en-us/engine/<VERSION>/manual/physics-section/physics-overview/collision-section/collision-detection/layer-based-collision.md). Check the Layer Collision Matrix, and the **Layer Overrides** on each Collider and Rigidbody, because they take priority over the matrix.
3. **Signature.** Use `OnTriggerEnter(Collider other)`, because `OnTriggerEnter2D(Collider2D other)` is never called in 3D physics.
4. **Script location.** The script must be on the trigger GameObject or the entering object, not on an unrelated parent or manager.
5. **Non-convex MeshCollider trigger.** If the Console shows the trigger error in the skill's Console errors table, apply the fix in that row.
