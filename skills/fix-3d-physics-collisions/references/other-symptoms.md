# Other physics symptoms

Read the section that matches the user's symptom. Replace `<VERSION>` in links with the project's Unity major.minor version, for example `6000.3`, and read the linked page before acting.

- Callbacks fire in the Editor but not in a build
- Collider moved by script isn't seen until the next physics step
- Visible gap before surfaces touch
- Rigidbody stops responding after it settles

## Callbacks fire in the Editor but not in a build

Check these in order, because each one only shows up in a Player build:

1. **Code stripping.** If the build strips the callback script, add `[Preserve]` to the class, or preserve its assembly in a `link.xml` file.
2. **Layer names.** If the code looks up layers by name, check that the names match the project's Tags and Layers settings exactly.
3. **Fixed Timestep.** If physics timing differs between platforms, set **Edit > Project Settings > Time > Fixed Timestep** explicitly.
4. **MeshCollider Read/Write.** Read the [Mesh collider component reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/physics-section/physics-overview/collision-section/collider-shapes/mesh-colliders/class-mesh-collider.md), because when a mesh needs **Read/Write** for its collider differs between versions.

## Collider moved by script isn't seen until the next physics step

Read [Physics.SyncTransforms](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/physics/synctransforms.md). Recommend `Rigidbody.MovePosition` instead of writing `transform.position`. Only if a same-frame query must see a position written by a script, call `Physics.SyncTransforms()` once after the write, not every frame, because it's expensive.

## Visible gap before surfaces touch

The gap is the collider's contact offset. Read [Collider.contactOffset](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/collider/contactoffset.md). Lower the offset on the affected colliders. Otherwise, if the gap must stay for stable contacts, move the visual mesh slightly inside the collider. Keep the offset above `0`, because Unity rejects `0` or less with an error listed in the skill's Console errors table and keeps the old value.

## Rigidbody stops responding after it settles

Read [Rigidbody sleeping](https://docs.unity.com/en-us/engine/<VERSION>/manual/physics-section/physics-overview/physics-optimization/cpu/rigidbody-sleeping.md). Call [`Rigidbody.WakeUp()`](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/rigidbody/wakeup.md) before applying the force. Only if the object must never sleep, set its `sleepThreshold` to `0`.
