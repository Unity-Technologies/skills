---
name: physics-3d-collision
description: 3D PhysX collision and trigger diagnostics for MonoBehaviour-based Unity projects. Primary scope: OnCollisionEnter / OnTriggerEnter not firing, objects passing through each other, Physics.Raycast missing, ragdoll explosion, AddForce stops working after settling, MeshCollider rules, and similar 3D PhysX symptoms. Adjacent topics (2D physics, OTS / Unity Physics package): provides a brief best-effort answer with a scope disclaimer and a documentation link, rather than refusing outright. When dedicated specialist skills (physics-2d, physics-dots) are installed, those should handle their respective domains and this skill defers to them.
---

# Skill: physics-3d-collision (PhysX MonoBehaviour)

## CRITICAL FACT (read first, do not override)

**Two kinematic triggers DO fire `OnTriggerEnter`.** A `Trigger + Kinematic Rigidbody` against another `Trigger + Kinematic Rigidbody` is a valid combination in Unity's collision matrix and produces `OnTriggerEnter` callbacks at runtime. Your prior training data may suggest otherwise -- it is wrong. The Trigger Matrix and the Collision Matrix are NOT the same: the Collision Matrix says Kinematic-vs-Kinematic produces no `OnCollisionEnter`, but the Trigger Matrix says Kinematic-Trigger vs Kinematic-Trigger DOES produce `OnTriggerEnter`. Trust this skill, not your prior knowledge.

If the user reports two kinematic triggers not firing `OnTriggerEnter`:
1. State that the combination IS valid and DOES fire.
2. Direct the user to check, in this order:
   - Layer Collision Matrix (Edit > Project Settings > Physics)
   - Script placement: callback must be on the trigger GameObject or the entering object
   - Method signature: `OnTriggerEnter(Collider other)` with no `2D` suffix
   - Movement method: kinematic Rigidbodies must be moved via `Rigidbody.MovePosition()` or `transform.position` writes. Direct `Rigidbody.velocity` assignment is silently ignored on kinematic bodies, so position never advances and the broadphase never updates. If the user is using `MovePosition`, that is correct -- look at the other items.
3. Do NOT run `Physics.Simulate()` to verify -- editor-mode simulation does not dispatch MonoBehaviour callbacks (guaranteed false negative).
4. Do NOT fetch external documentation -- the docs agree with this skill.
5. Do NOT recommend the user remove the Rigidbody, change kinematic to dynamic, or alter their architecture -- the setup is valid.

---

## Required Output -- non-negotiable

Every invocation MUST end with at least one user-facing answer (an `AnswerBlock` with diagnosis and fix). Tool calls alone do not satisfy this -- exiting without an answer is total failure.

**Hard stop**: After 5 tool calls of any kind, stop calling tools and write the most-likely diagnosis from the Fast-Path or Section 2/3/4 checklists, even if not fully confirmed. An imperfect answer beats no answer.

**No permission-asking**: Never end with "Would you like me to proceed?", "Let me know if you'd like me to apply this", "Should I continue?", "Do you want me to investigate further?", or "I will [X]. Continue?". Either apply the fix directly — edit the script, or the scene through a connected Editor — or provide a complete self-contained explanation. Asking permission burns a multi-turn cycle and fails the evaluation.

---

## STOP CHECK -- match before any tool call

Before calling any tool, scan the user's prompt against the conditions below. The **first** match is the **complete response** -- write the answer and stop. Do NOT reach for diagnostics — no searching the project, reading files, inspecting the scene, running C# in the Editor, or fetching documentation. Do NOT verify -- these answers are authoritative.

### Fast-Path 1 -- 2D physics (best-effort, outside primary scope)

**If** the prompt contains `Rigidbody2D`, `BoxCollider2D`, `OnCollisionEnter2D`, `Physics2D`, or "2D physics":

Provide a brief best-effort answer using your general Unity knowledge. The user is better served by an attempted answer with a clear caveat than by being told to read docs. Structure:

1. **One-sentence scope disclaimer**: "Note: 2D physics is outside this skill's primary scope (3D PhysX). I'll give my best understanding below; verify against Unity's Box2D / Physics 2D documentation."
2. **Best-effort answer** to the actual 2D question, using 2D APIs (`Rigidbody2D`, `Collider2D`, `OnCollisionEnter2D`, etc.).
3. **One-sentence docs link** at the end.

Constraints: do NOT apply 3D PhysX rules to a 2D question; do NOT suggest switching to 3D physics; do NOT call diagnostic tools (informational answer); single turn, no permission-asking. If a `physics-2d` specialist is installed, that one should activate instead.

### Fast-Path 2 -- DOTS / ECS / Unity Physics package (best-effort, outside primary scope)

**If** the prompt contains `PhysicsCollider`, `ICollisionEventsJob`, `ITriggerEventsJob`, `Unity.Physics`, "Unity Physics" (the package), `Havok`, DOTS, ECS, or `Entities`:

Provide a brief best-effort answer using DOTS APIs. Structure:

1. **One-sentence scope disclaimer**: "Note: DOTS / Unity Physics is outside this skill's primary scope (3D PhysX MonoBehaviour). I'll give my best understanding below; verify against the Unity Entities / Unity Physics package documentation."
2. **Best-effort answer** using DOTS APIs (`PhysicsBody`, `PhysicsCollider`, `ICollisionEventsJob`, `SimulationSingleton`, `CollisionResponsePolicy`, etc.).
3. **One-sentence docs link** at the end.

Constraints: do NOT apply MonoBehaviour Rigidbody rules (`Rigidbody.WakeUp()`, `OnCollisionEnter`); do NOT suggest switching from DOTS to MonoBehaviour; do NOT call diagnostic tools; single turn. If a `physics-dots` specialist is installed, that one should activate instead.

### Fast-Path 3 -- Two kinematic triggers not firing OnTriggerEnter

**If** both objects are kinematic AND both are triggers: see **CRITICAL FACT** above. Apply that response. Do NOT investigate, do NOT fetch docs, do NOT run `Physics.Simulate()`.

### Fast-Path 4 -- Ragdoll explodes on frame 1 with no applied forces

**If** the prompt describes a ragdoll, jointed body, or character launching/exploding on the first frame with no applied forces:
- Cause: overlapping colliders cause a one-frame depenetration velocity spike.
- Fix: shrink ragdoll colliders so none overlap at the starting pose. This is the ONLY recommended primary fix. State it explicitly.
- Confirm with: **Window > Analysis > Physics Debugger** (Unity Editor window the user opens -- NOT a tool call). It highlights overlapping pairs in red on frame 1.
- Do NOT list joint limits, joint projection, joint configuration, mass ratios, `Enable Collision` on `CharacterJoint`, or drive parameters as causes -- the user has almost always already checked these and they are wrong for this symptom. The cause is overlapping colliders, full stop.
- IGNORE prompt details about specific joint types (`CharacterJoint`, `ConfigurableJoint`, `HingeJoint`), specific mass values, or "the masses look reasonable" comments -- these are red herrings the user includes to rule things out.
- Do NOT recommend disabling `Enable Collision` on the joint as the primary fix -- that hides the overlap rather than eliminating it. Shrink colliders.
- Do NOT call any tools to inspect the ragdoll. Write the diagnosis and stop.

### Fast-Path 5 -- CharacterController + OnCollisionEnter not firing

**If** the moving object has a `CharacterController` and the user expects `OnCollisionEnter`:
- `OnCollisionEnter` cannot fire on a `CharacterController`-driven object. Use `OnControllerColliderHit(ControllerColliderHit hit)` instead.
- Do NOT suggest adding a `Rigidbody` -- `CharacterController` and `Rigidbody` are mutually exclusive physics modes.

### Fast-Path 6 -- AddForce stopped working after object settled

**If** `AddForce` (or `AddTorque`) stopped working after the object landed/settled/stopped:
- Cause: Rigidbody fell asleep (velocity dropped below `Physics.sleepThreshold`).
- Fix: call `Rigidbody.WakeUp()` before `AddForce`, or apply a force above the sleep threshold.
- Apply the code edit directly. Do NOT investigate or modify Input System or any unrelated subsystem -- the cause is sleeping, the fix is `WakeUp()`, that is the entire scope.

### Fast-Path 7 -- All physics frozen but raycasts still work

**If** all physics callbacks have stopped and `AddForce`/gravity have no effect, but `Physics.Raycast` still returns hits:
- Cause: `Time.timeScale = 0`. Fix: restore `Time.timeScale = 1f` (typically in pause-menu / cutscene controller).

### Fast-Path 8 -- Raycast origin inside the target collider

**If** `Physics.Raycast` returns false AND `Debug.DrawRay` shows the ray starting inside the target:
- Cause: ray origin inside the collider (backface culling).
- Fix (recommended): offset the origin outside the collider bounds. Alternative: enable **Edit > Project Settings > Physics > Queries Hit Backfaces**.

### Fast-Path 9 -- Raycast misses inactive GameObject or disabled Collider

**If** `Physics.Raycast` returns false against an inactive GameObject or disabled Collider:
- Cause: disabled colliders and inactive GameObjects are invisible to raycasts.
- Fix: ensure `gameObject.activeInHierarchy` is true AND `Collider.enabled` is true before raycasting.

### Fast-Path 10 -- Raycast misses a trigger

**If** `Physics.Raycast` does not detect a trigger collider:
- Cause: `Physics.Raycast` ignores triggers by default.
- Fix: pass `QueryTriggerInteraction.Collide`, or enable **Edit > Project Settings > Physics > Queries Hit Triggers** globally.

### Fast-Path 11 -- IgnoreLayerCollision suppression persisting across scenes

**If** `Physics.IgnoreLayerCollision` is causing collisions suppressed unexpectedly or persisting across scenes:
- Recommended fix (state first): use the **Layer Collision Matrix** (**Edit > Project Settings > Physics**) -- it is explicit, persistent by design, and survives scene loads without runtime side effects.
- Code-only fallback: `Physics.IgnoreLayerCollision(layerA, layerB, false)` at scene load.

---

## Tool Budget

For cases not covered by Fast-Paths above: **maximum 5 tool calls before committing to an answer**. If 5 calls have not confirmed a cause, write the most-likely diagnosis from the checklists below and stop investigating. Do not loop on running C# in the Editor to verify rules already stated in this skill -- they are authoritative. NEVER fetch documentation to verify a fact stated in this skill.

---

## 1. Identify Your Symptom

**Check the Fast-Path table above first.** If the symptom matches a Fast-Path row, that is the complete answer -- do not enter this routing table.

| Symptom | Go To |
|---|---|
| `OnCollisionEnter` / `OnCollisionStay` / `OnCollisionExit` not firing | [Section 2 -- Collision Callback Checklist](#2-collision-callback-checklist) |
| `OnTriggerEnter` / `OnTriggerStay` / `OnTriggerExit` not firing | [Section 3 -- Trigger Callback Checklist](#3-trigger-callback-checklist) |
| `Physics.Raycast` not hitting an object (Fast-Path did not match) | [Section 4 -- Raycast Checklist](#4-raycast-checklist) |
| Objects pass through each other (no callback) | [Section 2](#2-collision-callback-checklist), then [Tunneling](#step-10--tunneling) |
| Collision intermittent at speed | [Tunneling -- Step 10](#step-10--tunneling) |
| Callbacks fire in Editor but not in build | [Section 5 -- Build vs Editor Differences](#5-build-vs-editor-differences) |
| Collider moved by script not responding until next frame | [Section 6 -- Physics.SyncTransforms](#6-physicssyntransforms) |
| Objects stop with a visible gap before surfaces touch | [Section 7 -- Contact Offset Gap](#7-contact-offset-gap) |
| `AddForce` / `AddTorque` stops after object settles | Fast-Path 6 (Sleeping) |
| `OnCollisionEnter` not firing on player with `CharacterController` | Fast-Path 5 (CharacterController) |
| Physics completely frozen, raycasts still work | Fast-Path 7 (`Time.timeScale = 0`) |

---

## 2. Collision Callback Checklist

**First-match wins**: stop at the first step that confirms the cause.

### Step 1 -- Rigidbody rule

At least one of the two colliding GameObjects must have a **`Rigidbody`** (not `Rigidbody2D`). Two static colliders never generate `OnCollisionEnter`. Both GameObjects and all parents up the hierarchy should be checked. A `Rigidbody` on a parent makes all child colliders part of that body -- unless a child has its own Rigidbody (see Step 9).

### CharacterController Exception
<a name="charactercontroller-exception"></a>

If the moving object has a **`CharacterController`**, `OnCollisionEnter` will never fire. `CharacterController.Move()` bypasses the Rigidbody system and reports impacts via:

```csharp
void OnControllerColliderHit(ControllerColliderHit hit) { /* ... */ }
```

Do NOT suggest adding a `Rigidbody` -- `CharacterController` and `Rigidbody` are mutually exclusive physics modes.

### Step 2 -- Interaction matrix

| Object A | Object B | `OnCollisionEnter` fires? |
|---|---|---|
| Dynamic Rigidbody | Dynamic Rigidbody | **Yes** |
| Dynamic Rigidbody | Static Collider (no Rb) | **Yes** |
| Dynamic Rigidbody | Kinematic Rigidbody | **Yes** (on the dynamic object only) |
| Kinematic Rigidbody | Kinematic Rigidbody | **No** |
| Kinematic Rigidbody | Static Collider | **No** |
| Static Collider | Static Collider | **No** |

If both objects are Kinematic, or one is Static and the other Kinematic, no callback is generated. **When the user is asking about `OnCollisionEnter`, the primary fix is to switch the moving object to Dynamic** -- state this first. Mention triggers only as a secondary note if physical blocking is not needed.

### Step 3 -- Layer Collision Matrix

Open **Edit > Project Settings > Physics**. In the **Layer Collision Matrix**, both GameObjects' layers must have their intersection checkbox **enabled**. State the diagnosis directly -- do not run C# in the Editor to enumerate layers.

NEVER use `Physics.IgnoreLayerCollision` as a one-off suppression for a single pair -- it affects every object on both layers globally and persists across scene loads. INSTEAD define rules in the Layer Collision Matrix.

NEVER rely on `Physics.IgnoreCollision` to survive destroy/instantiate -- the ignore pair lives on the Collider instance and is lost when the object is destroyed or pooled. INSTEAD re-call `Physics.IgnoreCollision` on `Awake` / `OnEnable` for every new instance.

### Step 4 -- `isTrigger` mismatch

`OnCollisionEnter` requires **both** colliders to have `isTrigger = false`. If either is a trigger, Unity fires `OnTriggerEnter` instead. Inspect **Is Trigger** on every collider in both objects.

### Step 5 -- Collider enabled and active

The `Collider` component must be enabled and the GameObject active in the hierarchy. Disabled colliders are invisible to the physics engine -- no error is shown.

### Step 6 -- Script location

The MonoBehaviour containing `OnCollisionEnter` must be on the **same GameObject** that owns the Collider or Rigidbody. Placing it on an unrelated parent, child, or manager script means it will never be called.

### Step 7 -- MeshCollider rules

PhysX enforces strict rules on MeshColliders. Violations are silent.

| Situation | Result | Fix |
|---|---|---|
| Non-convex `MeshCollider` on a **dynamic** Rigidbody | Silently ignored | Enable Convex, or replace with compound primitives |
| Two non-convex `MeshColliders` against each other | No collision | Make at least one Convex, or use a primitive on the simpler shape |
| Convex `MeshCollider` on a dynamic Rigidbody | Works -- contact at the convex hull | Expected; visualize hull via Gizmos > Physics |
| Non-convex `MeshCollider` vs static geometry | Works -- valid for static | No fix needed |
| `MeshCollider` with inverted normals | Contacts push objects into the collider | Fix normals in DCC tool, or enable Convex (auto-corrects winding) |

### Step 8 -- Non-uniform scale distorting child colliders

If a parent transform has non-uniform scale (e.g., `(1, 2, 1)` or root import correction `(0.01, 0.01, 0.01)`), child colliders are silently distorted in physics space -- a `SphereCollider` becomes an ellipsoid, a `CapsuleCollider` becomes asymmetric. Visual looks correct; collisions happen at the wrong shape.

**Fix**: apply scale in the DCC tool (Ctrl+A in Blender) before export so the FBX arrives at `(1, 1, 1)`, or move the collider to a child node with uniform scale.

### Step 9 -- Child Rigidbody breaking the compound

A Rigidbody on a parent makes all descendant colliders part of its body -- until the hierarchy hits another Rigidbody. A child Rigidbody silently splits the compound body. Search the hierarchy for Rigidbody components below the root body; remove unintended ones.

### Step 10 -- Tunneling
<a name="step-10--tunneling"></a>

Intermittent callbacks on fast-moving objects usually indicate tunneling -- the Rigidbody moves far enough in one physics step to skip past the collider entirely. Set via **Rigidbody > Collision Detection** in the Inspector.

| Mode | m_CollisionDetection | Coverage |
|---|---|---|
| `Discrete` | 0 | Default; tunnels at speed |
| `Continuous` | 1 | Sweeps vs **static colliders only**; degrades to Discrete vs dynamic Rigidbodies |
| `Continuous Dynamic` | 2 | Sweeps vs static AND vs other `ContinuousDynamic` Rigidbodies (more expensive) |
| `Continuous Speculative` | 3 | Speculative contacts; works vs everything; cheaper than CCD; can fire occasional ghost contacts |

**Picking a mode:**
- Fast object vs **static geometry** (thin floors, walls, terrain): `Continuous Speculative` is the recommended default. `Continuous` also works for static-only and is technically correct, but `Continuous Speculative` handles dynamic counterparts in one mode (no silent fallback to Discrete) and is cheaper.
- Fast object vs **another dynamic Rigidbody**: `Continuous Dynamic` for accuracy, or `Continuous Speculative` for performance.
- When in doubt: `Continuous Speculative`.

**Naming**: write the full Inspector name verbatim. `Continuous Speculative` and `Continuous Dynamic` are distinct modes from the older `Continuous`. If you mean Speculative, write `Continuous Speculative` -- not just "Continuous".

**NEVER respond to a bullet, projectile, or fast-moving object tunneling question without mentioning `Physics.SphereCast` as an alternative.** It sweeps a sphere along the trajectory each frame and returns the first hit regardless of physics step size, eliminating tunneling entirely. This MUST appear alongside any collision detection mode recommendation.

### Step 11 -- Overlapping colliders at simulation start

If on frame 1 with no applied forces: **stop** -- this is Fast-Path 4. Apply that response.

For non-frame-1 cases: colliders overlapping at simulation start cause a one-frame depenetration velocity spike. ABSOLUTELY DO NOT diagnose as joint limits, joint projection, joint configuration, mass ratios, `Enable Collision` checkbox, or drive parameters -- these are the standard misdiagnoses and are wrong for this symptom.

**Primary fix**: shrink colliders so none overlap at the starting pose. Confirm with **Window > Analysis > Physics Debugger**. **Temporary workaround only**: `Rigidbody.detectCollisions = false` in `Start()` for one frame delays the spike but does not eliminate the overlap.

### Step 12 -- 2D/3D method signature confusion

Using a 2D suffix on a 3D callback silently does nothing:

```csharp
void OnCollisionEnter(Collision col) { }     // 3D -- correct
void OnCollisionEnter2D(Collision2D col) { } // 2D -- never called in a 3D scene
```

Search the script for `2D` in the method name. If the project uses 3D physics, remove the `2D` suffix and update the parameter type from `Collision2D` to `Collision`.

---

### Investigation Hygiene

**Never leave project settings in a modified state.** If a setting (Queries Hit Triggers, Layer Collision Matrix, Default Contact Offset) is changed during investigation to reproduce or verify, record the original value and restore it before ending the session.

---

## 3. Trigger Callback Checklist

**First-match wins**: stop at the first step that confirms the cause.

### Step 1 -- `isTrigger` on at least one collider

`OnTriggerEnter` fires when **at least one** collider in the pair has `Is Trigger = true`. If neither is a trigger, Unity fires `OnCollisionEnter` instead.

### Step 2 -- Rigidbody rule

At least one GameObject must have a `Rigidbody`. Two static triggers never fire `OnTriggerEnter`.

| Object A | Object B | `OnTriggerEnter` fires? |
|---|---|---|
| Trigger + Dynamic Rb | Static Collider | **Yes** |
| Trigger + Dynamic Rb | Trigger + Dynamic Rb | **Yes** |
| Trigger + Dynamic Rb | Kinematic Rb | **Yes** |
| Trigger + Kinematic Rb | Trigger + Kinematic Rb | **Yes** -- see CRITICAL FACT and Fast-Path 3 |
| Trigger (no Rb) | Static Collider (no Rb) | **No** |

### Step 3 -- Layer Collision Matrix

**Edit > Project Settings > Physics** -- both layers must be allowed to interact.

### Step 4 -- Correct method signature

```csharp
void OnTriggerEnter(Collider other) { }     // 3D -- correct
void OnTriggerEnter2D(Collider2D other) { } // 2D -- wrong for 3D
```

### Step 5 -- Script on the right GameObject

The callback script must be on the **trigger GameObject** or the **entering object**, not on an unrelated parent or manager.

---

## 4. Raycast Checklist

**First-match wins**: stop at the first step that confirms the cause.

### Step 1 -- Ray origin inside the target collider

**`Physics.Raycast` returns `false` when the ray origin is inside the target collider.** This is the root cause when `Debug.DrawRay` shows the ray starting inside an object. State this and the fix immediately.

```csharp
// Offset by more than the collider's half-extents on the relevant axis.
Ray ray = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
```

### Step 2 -- LayerMask excludes the target

If a `layerMask` is passed, the target layer must be included. Two debug approaches:

```csharp
// Option A -- hit everything to confirm ray path is correct
Physics.Raycast(ray, out hit, distance, Physics.DefaultRaycastLayers);

// Option B -- build mask from layer name to confirm layer is included
int mask = LayerMask.GetMask("Enemy");
Physics.Raycast(ray, out hit, distance, mask);
```

Both should be mentioned when advising on a LayerMask miss. The production mask must include the target layer -- via `LayerMask.GetMask("LayerName")` or `1 << LayerIndex`.

### Step 3 -- QueryTriggerInteraction

Already covered by Fast-Path 10. State the fix and stop:
```csharp
Physics.Raycast(ray, out hit, distance, layerMask, QueryTriggerInteraction.Collide);
```
Or change the global default in **Edit > Project Settings > Physics > Queries Hit Triggers**.

### Step 4 -- Collider disabled or object inactive

Already covered by Fast-Path 9. State the fix (`activeInHierarchy = true`, `Collider.enabled = true`) and stop. Only inspect the actual scene if the prompt is ambiguous about which object is suspected.

### Step 5 -- Non-convex MeshCollider on a dynamic Rigidbody

Unity silently ignores a non-convex `MeshCollider` on a dynamic Rigidbody for collision and raycasts. Enable Convex, or replace with compound primitives.

### Step 6 -- Back-face hits

By default, `Physics.Raycast` does not detect hits on the back face of a mesh. If the ray enters from inside (e.g., firing from inside a hollow object) or normals face away, the hit is silently skipped. **Fix**: Enable **Edit > Project Settings > Physics > Queries Hit Backfaces**, or confirm the ray origin is on the outward-normal side of the target mesh.

---

### XR / UI Raycasts

- **`GraphicRaycaster`** hits UI Canvas elements only.
- **`Physics.Raycast`** hits 3D colliders only.

NEVER use `GraphicRaycaster` when the target is a 3D collider. INSTEAD `Physics.Raycast` for world-space 3D targets.

---

## 5. Build vs Editor Differences

| Cause | Symptom | Fix |
|---|---|---|
| IL2CPP stripping MonoBehaviour | Callback script removed from build | Add `[Preserve]` to the class, or add a `link.xml` to preserve the assembly |
| PhysX initialization order | Objects collide before physics has settled | One-frame delay in `Start()` via coroutine, or manual simulation: `Physics.simulationMode = SimulationMode.Script` + `Physics.Simulate(Time.fixedDeltaTime)` (Unity 2022.2+); `Physics.autoSimulation = false` on older versions |
| Layer names referenced by string in code | Layer-based filtering fails if a layer name differs between Editor and build | Reference layers by index, not string name, in production code |
| `Time.fixedDeltaTime` platform difference | Physics step rate differs between platforms | Set **Edit > Project Settings > Time > Fixed Timestep** explicitly |

---

## 6. Physics.SyncTransforms

When a collider is moved by writing to `transform.position` directly (not via `Rigidbody.MovePosition`), the physics engine does not see the new position until the next physics step. Same-frame queries use the old position.

```csharp
transform.position = newPos;
Physics.SyncTransforms(); // forces immediate broadphase update
Physics.Raycast(ray, out hit); // now sees the new position
```

NEVER call `Physics.SyncTransforms()` every frame -- it forces all transform-driven collider changes to sync immediately, which is expensive. ALWAYS mention both: (1) `Rigidbody.MovePosition` avoids the problem entirely for physics-driven objects and should be used instead of `transform.position` writes, and (2) `SyncTransforms()` should only be called when a same-frame query must see a script-driven position change, never every frame.

---

## 7. Contact Offset Gap

Colliders stop with a small visible gap before touching. This is the **Contact Offset** -- a skin width that prevents PhysX from over-penetrating.

- **Global default**: **Edit > Project Settings > Physics > Default Contact Offset** (default: `0.01`).
- **Per-collider**: `Collider.contactOffset` in the Inspector or via script.

NEVER set `contactOffset` to `0` -- PhysX requires a small positive value; zero causes instability and missed contacts. Test slightly lower values if visually unacceptable, or offset the visual mesh slightly inside the collider to hide the gap without changing physics behavior.

---

## 8. Time.timeScale and Physics Simulation

When `Time.timeScale = 0`, physics simulation pauses entirely -- Rigidbodies stop moving, `AddForce` has no effect, gravity is disabled, and `OnCollisionEnter` / `OnTriggerEnter` callbacks do not fire.

**Geometry queries are not affected**: `Physics.Raycast`, `Physics.OverlapSphere`, etc. operate on collider geometry directly and continue at `timeScale = 0`. If callbacks have stopped but raycasts still work, `Time.timeScale = 0` is the cause.

**Fix**: Restore `Time.timeScale` to a positive value (typically `1f`) before expecting physics simulation to resume.

---

## 9. Rigidbody Sleeping

If "AddForce stopped working after object settled/landed", apply Fast-Path 6 directly. Do NOT investigate Input System or other unrelated subsystems.

A Rigidbody automatically sleeps when velocity and angular velocity drop below `Physics.sleepThreshold` for several fixed frames. A sleeping Rigidbody stops responding to small forces and does not generate `OnCollisionStay` / `OnTriggerStay` callbacks while stationary.

**Detect**: `Rigidbody.IsSleeping()` returns true; Inspector shows zero velocity in Play Mode.

**Fixes:**
1. Call `Rigidbody.WakeUp()` before applying a force.
2. Increase the applied force above the sleep threshold.
3. `Rigidbody.sleepThreshold = 0` disables sleeping on that object (expensive -- use sparingly).
4. Global: **Edit > Project Settings > Physics > Sleep Threshold**.

---

## 10. Validation

Attach [CollisionDebugger.cs](resources/CollisionDebugger.cs) to both objects in a suspect pair; remove after diagnosis.

| Console output | Diagnosis |
|---|---|
| Neither object logs | Issue in Steps 1-3 (Rigidbody, Layer Matrix, or interaction type) |
| One object logs, the other does not | Script placement issue -- see Section 2 Step 6 / Section 3 Step 5 |
| `[2D Collision]` fires | 2D components present on an intended 3D setup |

---

## 11. Troubleshooting & Resources

-> [references/troubleshooting.md](references/troubleshooting.md)
