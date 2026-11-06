# Record contacts with CollisionDebugger

Use `scripts/CollisionDebugger.cs` (the path is relative to this skill's root folder) to see which contacts the physics engine reports, without adding components to the scene or saving changes to it. It's an Editor script that reads `Physics.ContactEvent`, so it records contacts even where no `OnCollisionEnter` script exists. Every line it logs starts with `[CollisionDebugger]`.

1. Copy the script to `Assets/Editor/CollisionDebugger.cs` in the user's project, and wait for the Editor to compile it with zero console errors.
2. In Edit mode, start tracking the GameObjects in the suspect pair by name, for example `unity command script eval --code 'CollisionDebugger.Track("Bullet", "Wall");'`.
3. Enter Play mode, reproduce the problem, then exit Play mode. The debugger logs a summary and switches itself off when Play mode ends.
4. Read the console lines that start with `[CollisionDebugger]`, and compare them with the table below.
5. Delete `Assets/Editor/CollisionDebugger.cs` and its `.meta` file.

| Console output | Diagnosis |
|---|---|
| `Setup` report | Lists the Physics settings from `DynamicsManager.asset`, and the layers, triggers, body types, and layer overrides of the tracked objects. Check these settings first, because a wrong layer, trigger, or body type explains most missing contacts. |
| `[Contact Enter]` for the pair, but the user's `OnCollisionEnter` doesn't run | The physics engine sees the contact, so check the script placement and signature and **Invoke Collision Callbacks**. |
| A non-trigger collider had no contacts | The pair is filtered before contact: check the body types, layers, and Contact Pairs Mode in the `Setup` report, then tunneling. |
| `[Trigger overlap start]` for the pair, but `OnTriggerEnter` doesn't run | The shapes overlap, so check the Rigidbody rule, layers, and script. |
| A trigger never overlapped another collider | The shapes never meet: check their positions, sizes, and movement. |
| `Has 2D physics components` | 2D components are on an intended 3D setup. |
