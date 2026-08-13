# Troubleshooting & Resources

## Troubleshooting Table

| Symptom | Likely Cause | Fix |
|---|---|---|
| No callback, no Rigidbody on either object | Two static colliders never generate callbacks | `Rigidbody` should be added to the moving object |
| `OnCollisionEnter` never fires on a player character | Player uses `CharacterController` instead of `Rigidbody` | Use `OnControllerColliderHit(ControllerColliderHit hit)` instead |
| Callback fires in Editor, silent in build | IL2CPP stripping MonoBehaviour | `[Preserve]` should be added, or a `link.xml` created |
| Callback fires once then stops | Rigidbody fell asleep | `Rigidbody.WakeUp()` should be called before applying force |
| `OnTriggerStay` / `OnCollisionStay` stops mid-session | Rigidbody fell asleep inside trigger or on collider | `Rigidbody.WakeUp()` should be called, or the sleep threshold reduced |
| All physics frozen — AddForce, gravity, callbacks all silent | `Time.timeScale` is `0` | Set `Time.timeScale = 1f` |
| Trigger fires but `OnCollisionEnter` does not | `isTrigger` is enabled | **Is Trigger** should be disabled if physical blocking is needed |
| Raycast hits everything except the target | Target layer excluded from layerMask | `target.layer` should be logged and the mask verified |
| Raycast hits nothing at all | Origin inside a collider | The ray origin should be offset; `Physics.OverlapSphere` can find enclosing colliders |
| Raycast misses the inside face of a wall or hollow mesh | Back-face hits disabled by default | Enable **Queries Hit Backfaces** in Edit > Project Settings > Physics |
| Two kinematic objects never interact | Kinematic × Kinematic produces no `OnCollisionEnter` | At least one should be made Dynamic, or triggers used |
| Fast bullet passes through wall | Tunneling — Discrete mode skips thin colliders | `Continuous Dynamic` should be used; or `Physics.SphereCast` for bullets |
| Ghost collisions (phantom hits before contact) | `Continuous Speculative` over-predicts contacts | `Continuous Dynamic` should be used for object-vs-object |
| Dynamic object with MeshCollider produces no callbacks | Non-convex MeshCollider on dynamic Rigidbody silently ignored | **Convex** should be enabled or compound primitives used |
| Two mesh objects never collide | Two non-convex MeshColliders cannot collide in PhysX | At least one should be made Convex; primitives used for the simpler shape |
| Collision fires at the wrong point on mesh | Convex hull diverges from visual mesh on concave shapes | Expected — hull contact only; visualized in Scene view (Gizmos > Physics) |
| Objects launch apart on the first frame | Colliders overlapping at simulation start — depenetration spike | Colliders should be shrunk so none overlap at spawn |
| Collider on child not part of parent physics body | Child has its own Rigidbody, splitting the compound | The unintended child Rigidbody should be removed |
| Collision stopped working after respawn or pool return | `Physics.IgnoreCollision` is per-instance | `Physics.IgnoreCollision` should be re-called on `Awake` / `OnEnable` |
| Objects outside a concave mesh pass through it | MeshCollider inverted normals — contacts deflect inward | Normals should be fixed in the DCC tool, or Convex enabled |
| Collider shape wrong despite looking correct in Scene view | Non-uniform scale on parent distorts child colliders | Scale should be applied in DCC tool so root arrives at `(1,1,1)` |
| Raycast misses a collider moved this frame | Physics broadphase not updated after `transform.position` write | `Physics.SyncTransforms()` should be called immediately after the position change |
| Objects stop with a visible gap before touching | Contact Offset skin gap | `Default Contact Offset` should be reduced carefully in Project Settings; very low values can reduce stability |

---

## Resources

- [Unity Manual: Collision callbacks and Rigidbody interaction](https://docs.unity3d.com/Manual/CollidersOverview.html)
- [Unity Manual: Layer Collision Matrix](https://docs.unity3d.com/Manual/LayerBasedCollision.html)
- [Unity Manual: Physics.Raycast](https://docs.unity3d.com/ScriptReference/Physics.Raycast.html)
- [Unity Manual: Rigidbody collision detection modes](https://docs.unity3d.com/Manual/RigidbodiesOverview.html)
- [Unity Manual: Physics.SyncTransforms](https://docs.unity3d.com/ScriptReference/Physics.SyncTransforms.html)
- [Unity Manual: Rigidbody.Sleep / WakeUp](https://docs.unity3d.com/ScriptReference/Rigidbody.WakeUp.html)
- [Unity Manual: CharacterController](https://docs.unity3d.com/Manual/class-CharacterController.html)
