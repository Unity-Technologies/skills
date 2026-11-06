# Collision callback checklist

Work through these steps when `OnCollisionEnter`, `OnCollisionStay`, or `OnCollisionExit` doesn't fire, or objects pass through each other. Stop at the first step that confirms the cause. Replace `<VERSION>` in links with the project's Unity major.minor version, for example `6000.3`.

If no step confirms a cause, read [Introduction to collision](https://docs.unity.com/en-us/engine/<VERSION>/manual/physics-section/physics-overview/collision-section/colliders-overview.md) and [Introduction to rigid body physics](https://docs.unity.com/en-us/engine/<VERSION>/manual/physics-section/physics-overview/rigidbody/rigidbodies-overview.md) for the wider picture, then try the collision debugger listed in the skill.

- Step 1: Rigidbody and body types
- Step 2: Project-wide physics settings
- Step 3: Layers
- Step 4: `isTrigger` mismatch
- Step 5: Collider enabled and active
- Step 6: Script location and signature
- Step 7: MeshCollider rules
- Step 8: Non-uniform scale
- Step 9: Child Rigidbody breaking the compound
- Step 10: Tunneling
- Step 11: Overlapping colliders at simulation start
- Investigation hygiene

## Step 1: Rigidbody and body types

Read [Interaction between collider types](https://docs.unity.com/en-us/engine/<VERSION>/manual/physics-section/physics-overview/collision-section/collider-interactions/collider-types-interaction.md) for which pairs send collision messages. Check both GameObjects and their parents for a `Rigidbody` (not `Rigidbody2D`), because a Rigidbody on a parent owns the child colliders.

If the moving object has a `CharacterController`, apply the CharacterController Fast-Path in the skill instead.

If both bodies are kinematic, or one is kinematic and the other static, recommend making the moving body dynamic, because a kinematic body is never pushed back by the contact. Otherwise, if the body must stay kinematic, set **Contact Pairs Mode** to the value the pair needs, as listed in the skill's CRITICAL FACT.

## Step 2: Project-wide physics settings

If you can read the project files, read `ProjectSettings/DynamicsManager.asset`. Otherwise, ask the user to check **Edit > Project Settings > Physics**. Check these settings against the [Physics settings](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity-editor/editor-settings-reference/comp-manager-group/class-physics-manager.md) page, because each one can stop every collision callback in the project:

- **Invoke Collision Callbacks** (`m_InvokeCollisionCallbacks`) is off.
- **Simulation Mode** (`m_SimulationMode`) is **Script**.
- **GameObject SDK** is **None**.

## Step 3: Layers

Read [Layer-based collision detection](https://docs.unity.com/en-us/engine/<VERSION>/manual/physics-section/physics-overview/collision-section/collision-detection/layer-based-collision.md). Check that the Layer Collision Matrix allows the two layers to collide, and check the **Layer Overrides** (Include Layers, Exclude Layers) on each Collider and Rigidbody, because they take priority over the matrix. State the diagnosis directly, without running C# to list layers.

To stop specific layers colliding, use the Layer Collision Matrix, not `Physics.IgnoreLayerCollision`, because the call affects every object on both layers and persists across scene loads. `Physics.IgnoreCollision` is stored on the Collider instance, so call it again in `OnEnable` for every pooled or newly instantiated object.

## Step 4: `isTrigger` mismatch

Check that neither collider has **Is Trigger** on, as described in [OnCollision events](https://docs.unity.com/en-us/engine/<VERSION>/manual/physics-section/physics-overview/collision-section/collider-interactions/oncollision.md).

## Step 5: Collider enabled and active

Check that both Colliders are enabled and their GameObjects are active in the hierarchy, because Unity ignores disabled colliders without an error.

## Step 6: Script location and signature

The script must be on the GameObject that owns the Collider or its Rigidbody. Use `OnCollisionEnter(Collision collision)`: a `2D` suffix or a `Collision2D` parameter compiles but is never called in 3D physics.

## Step 7: MeshCollider rules

Read [Configure Rigidbody colliders](https://docs.unity.com/en-us/engine/<VERSION>/manual/physics-section/physics-overview/rigidbody/rigidbody-configure-colliders.md) and [Introduction to Mesh colliders](https://docs.unity.com/en-us/engine/<VERSION>/manual/physics-section/physics-overview/collision-section/collider-shapes/mesh-colliders/introduction.md), and ask the user for Console errors that mention the mesh. If the error matches a row in the skill's Console errors table, apply the fix in that row. A non-convex MeshCollider on static geometry needs no fix.

## Step 8: Non-uniform scale

If a parent has non-uniform scale, read the non-uniform scaling section of [Transform](https://docs.unity.com/en-us/engine/<VERSION>/manual/working-with-gameobjects/gameobject-fundamentals/class-transform.md), because it distorts child primitive colliders while the mesh looks correct. Apply the scale in the modelling tool before export, so the model imports at `(1, 1, 1)`.

## Step 9: Child Rigidbody breaking the compound

Search the hierarchy for Rigidbody components below the root body, and remove unintended ones, because they split the compound body. For details, read [Introduction to compound colliders](https://docs.unity.com/en-us/engine/<VERSION>/manual/physics-section/physics-overview/collision-section/collider-shapes/compound-colliders/introduction.md).

## Step 10: Tunneling

Intermittent callbacks on fast objects mean tunneling. Read [Choose a collision detection mode](https://docs.unity.com/en-us/engine/<VERSION>/manual/physics-section/physics-overview/collision-section/collision-detection/choose-collision-detection-mode.md) before picking a mode, and again if the object still tunnels.

1. Set **Rigidbody > Collision Detection** to `Continuous Speculative` as the default. Otherwise, if two fast dynamic Rigidbodies still pass through each other, use `Continuous Dynamic`.
2. Write the full mode name, because `Continuous` alone names a different mode.
3. For a bullet or projectile, also mention [`Physics.SphereCast`](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/physics/spherecast.md) along the trajectory each frame.

## Step 11: Overlapping colliders at simulation start

If colliders overlap when they're spawned or enabled, read [Ragdoll stability](https://docs.unity.com/en-us/engine/<VERSION>/manual/physics-section/physics-overview/ragdoll/ragdoll-stability.md) and apply the ragdoll Fast-Path in the skill: move or shrink the colliders so none overlap at the start.

## Investigation hygiene

If you change a project setting to reproduce or verify a cause, such as Queries Hit Triggers or the Layer Collision Matrix, record the original value and restore it before you finish, because the user didn't ask for that change.
