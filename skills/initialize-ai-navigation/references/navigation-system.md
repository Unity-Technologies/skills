#> **Which snippets go where.** The recipes below — movement, patrol, click-to-move, animation
> coupling, path inspection — are **written as game scripts**: save them into the project with
> their `using UnityEngine.AI;` and short type names intact. Only the Editor-side operations
> (installing the package, querying the live scene) are meant to be passed to `eval`, and those
> are fully qualified because `eval` takes no usings.

# Table of Contents
- [Performance Notes](#performance-notes)
- [Information Gathering Checklist](#information-gathering-checklist)
- [Component Setup Guide](#component-setup-guide)
- [Common Recipes](#common-recipes)
- [Important API Notes](#important-api-notes)
- [Core Concepts Reference](#core-concepts-reference)
- [Component Reference — NavMesh Surface](#component-reference--navmesh-surface)
- [Component Reference — NavMesh Agent](#component-reference--navmesh-agent)
- [Component Reference — NavMesh Obstacle](#component-reference--navmesh-obstacle)
- [Component Reference — NavMesh Link](#component-reference--navmesh-link)
- [Component Reference — NavMesh Modifier](#component-reference--navmesh-modifier)
- [Component Reference — NavMesh Modifier Volume](#component-reference--navmesh-modifier-volume)
- [Navigation Areas and Costs](#navigation-areas-and-costs)
- [Mixing Components Guide](#mixing-components-guide)
- [Coupling Animation and Navigation](#coupling-animation-and-navigation)
- [Troubleshooting Decision Tree](#troubleshooting-decision-tree)
- [Common Mistakes to Avoid](#common-mistakes-to-avoid)


## Performance Notes
- Do this thoroughly.
- Quality is more important than speed.
- Always inspect existing navigation setup before adding new components.


## Information Gathering Checklist

Before creating components, ensure the following details exist. If not, ask the user:

### Core Questions
* **What needs a NavMesh?** Which GameObjects or areas represent walkable surfaces? (floor, terrain, platforms)
* **Agent type:** What kind of characters navigate? (humanoid, large vehicle, small creature) — determines radius, height, step height, slope
* **Agent behavior:** What should agents do? (move to target, patrol, follow player, click-to-move)
* **Obstacles:** Are there dynamic obstacles agents must avoid? (crates, doors, vehicles)
* **Links:** Are there gaps, jumps, or disconnected areas that agents must cross?
* **Areas and costs:** Are there different terrain types with different traversal costs? (water, mud, roads)

### Per-Agent Details
* **Speed:** Maximum movement speed (default: 3.5 units/sec)
* **Angular Speed:** Maximum rotation speed (default: 120 deg/sec)
* **Acceleration:** How quickly the agent reaches max speed (default: 8 units/sec²)
* **Stopping Distance:** How close the agent gets before stopping (default: 0)
* **Auto Braking:** Should the agent slow down near destination? (yes for move-to, no for patrol)


## Component Setup Guide

### NavMesh Surface (Walkable Area)

The NavMeshSurface component defines and builds the navigation mesh.

1. **Select the geometry** that represents your walkable area (floor, terrain, or a parent containing all walkable children).
2. **Add component:** `NavMeshSurface` via **Add Component > Navigation > NavMesh Surface**.
3. **Configure:**
   - **Agent Type:** Match the agent type that will use this NavMesh.
   - **Default Area:** Usually "Walkable".
   - **Use Geometry:** "Render Meshes" (visual geometry) or "Physics Colliders" (collision geometry — agents walk closer to edges).
   - **Collect Objects:** "All Game Objects" (default), "Current Object Hierarchy" (only children of this GameObject), or "Volume" (within a bounding box).
   - **Include Layers:** Filter which layers contribute to the NavMesh.
   - **Generate Links:** Enable to auto-generate jump-across and drop-down links during bake.
4. **Bake:** Click **Bake** in the Inspector. The NavMesh appears as a blue overlay.

**Multiple surfaces:** A scene can have multiple NavMeshSurface components for different agent types or different areas. Only enabled surfaces on active GameObjects load their NavMesh data.

**Runtime baking:** For procedural levels, call `NavMeshSurface.BuildNavMesh()` at runtime:
```csharp
var surface = targetGameObject.GetComponent<NavMeshSurface>();
surface.BuildNavMesh();
Debug.Log("NavMesh baked at runtime.");
```

### NavMesh Agent (Pathfinding Character)

1. **Select the character** GameObject.
2. **Add component:** `NavMeshAgent` via **Add Component > Navigation > NavMesh Agent**.
3. **Configure steering:**
   - **Speed:** Match movement animation speed (default: 3.5).
   - **Angular Speed:** 120 deg/sec is typical.
   - **Acceleration:** 8 is responsive; lower for heavier characters.
   - **Stopping Distance:** 0 for precise arrival; increase for loose following.
   - **Auto Braking:** On for move-to-target; off for continuous patrol.
4. **Configure obstacle avoidance:**
   - **Radius:** Should match character width roughly.
   - **Height:** Should match character height.
   - **Quality:** High Quality for important agents; reduce for crowds.
   - **Priority:** 0–99, lower = higher priority. Important agents push through crowds.
5. **Configure pathfinding:**
   - **Auto Traverse OffMesh Link:** On (unless custom link traversal is needed).
   - **Auto Repath:** On for agents that should retry when paths are blocked.
   - **Area Mask:** Select which area types this agent can use.

### NavMesh Obstacle (Dynamic Blockers)

For physics-controlled or dynamic objects that agents should avoid:

1. **Select the obstacle** GameObject.
2. **Add component:** `NavMeshObstacle` via **Add Component > Navigation > NavMesh Obstacle**.
3. **Configure:**
   - **Shape:** Box or Capsule — pick whichever fits the object.
   - **Center/Size:** Auto-fits to renderer; adjust if needed.
   - **Carve:** Enable for stationary obstacles that should cut holes in the NavMesh.
     - **Move Threshold:** Distance before the carved hole updates (default: 0.1).
     - **Time To Stationary:** Seconds before the obstacle is considered stopped (default: 0.5).
     - **Carve Only Stationary:** On for physics objects (best performance); off for large slow-moving obstacles like tanks.

**When to carve vs. obstruct:**
- **Moving obstacles** (vehicles, player): Leave Carve off — use local avoidance.
- **Stationary or semi-stationary obstacles** (crates, barrels, doors): Enable Carve — agents plan paths around them.

### NavMesh Link (Bridge Disconnected Areas)

For jumps, drops, doors, or any shortcut that isn't walkable surface:

1. **Create two marker objects** (empty GameObjects or small cylinders) at the link start and end positions.
2. **Add component:** `NavMeshLink` to a GameObject via **Add Component > Navigation > NavMesh Link**.
3. **Configure:**
   - **Agent Type:** Which agent type can use this link.
   - **Start Transform / End Transform:** Assign the marker objects.
   - **Width:** 0 for point-to-point; positive for a span agents can enter along.
   - **Bidirectional:** On for two-way traversal; off for one-way (e.g., drop-down only).
   - **Area Type:** Usually "Jump" for auto-links; set custom type for doors etc.
   - **Cost Override:** Override the traversal cost if needed.
   - **Activated:** Must be on for agents to use the link.
4. **Verify:** Both ends must connect to a NavMesh (visible as circles/dark edges in Scene view with NavMesh debug on).

**Auto-generated links:** Enable **Generate Links** on the NavMeshSurface and configure **Drop Height** and **Jump Distance** in the agent type settings (Window > AI > Navigation > Agents tab) for automatic link generation during bake.

### NavMesh Modifier (Per-GameObject)
Adjusts how a specific GameObject (and optionally its children) contributes to the NavMesh:
- **Mode:** "Add or Modify Object" (include) or "Remove Object" (exclude from NavMesh).
- **Affected Agents:** Which agent types are affected.
- **Apply to Children:** Cascade to child hierarchy.
- **Override Area:** Change the area type for this object.
- **Override Generate Links:** Force include/exclude from link generation.

### NavMesh Modifier Volume (Region-Based)
Changes the area type within a defined box volume:
- **Size / Center:** Define the box region.
- **Area Type:** The area type to stamp onto NavMeshes within this volume.
- **Affected Agents:** Which agent types are affected.

Use Modifier Volumes for areas that don't correspond to separate geometry (e.g., marking part of a floor as "Water" or "Not Walkable").


## Common Recipes

### Move to a Transform Target
```csharp
using UnityEngine;
using UnityEngine.AI;

public class MoveToTarget : MonoBehaviour
{
    public Transform goal;
    NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.destination = goal.position;
    }
}
```

### Click-to-Move (Mouse Raycast)
```csharp
using UnityEngine;
using UnityEngine.AI;

public class ClickToMove : MonoBehaviour
{
    NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 100f))
            {
                agent.destination = hit.point;
            }
        }
    }
}
```

### Patrol Between Waypoints
```csharp
using UnityEngine;
using UnityEngine.AI;

public class Patrol : MonoBehaviour
{
    public Transform[] points;
    int destPoint = 0;
    NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = false;
        GotoNextPoint();
    }

    void GotoNextPoint()
    {
        if (points.Length == 0) return;
        agent.destination = points[destPoint].position;
        destPoint = (destPoint + 1) % points.Length;
    }

    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
            GotoNextPoint();
    }
}
```

### Agent Speed Control for Corners
```csharp
using UnityEngine;
using UnityEngine.AI;

public class AgentSpeedController : MonoBehaviour
{
    NavMeshAgent agent;
    Vector3[] pathCorners = new Vector3[3];

    [SerializeField] Transform target;
    float maxSpeedStraight;
    [SerializeField] float maxSpeedAtCorner = 0.1f;
    [SerializeField] float distanceThreshold = 0.5f;

    void OnEnable()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.SetDestination(target.position);
            maxSpeedStraight = agent.speed;
        }
    }

    void Update()
    {
        if (agent == null) return;

        int numCorners = agent.path.GetCornersNonAlloc(pathCorners);
        if (numCorners > 2)
        {
            Vector3 first = (pathCorners[1] - pathCorners[0]).normalized;
            Vector3 second = (pathCorners[2] - pathCorners[1]).normalized;
            float speedFactor = Mathf.Clamp01(Vector3.Dot(first, second));
            float distance = Vector3.Distance(pathCorners[0], pathCorners[1]);
            float distanceRatio = Mathf.Clamp01(distance / distanceThreshold);
            float angleMaxSpeed = Mathf.Lerp(maxSpeedAtCorner, maxSpeedStraight, speedFactor);
            agent.speed = Mathf.Lerp(angleMaxSpeed, maxSpeedStraight, distanceRatio);
        }
        else
        {
            agent.speed = maxSpeedStraight;
        }
    }
}
```

### Agent-Driven Animation (Agent Moves, Animation Follows)
Use NavMeshAgent velocity to drive Animator blend parameters. Simple approach with foot-sliding trade-off.
```csharp
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class NavAgentAnimator : MonoBehaviour
{
    Animator anim;
    NavMeshAgent agent;
    Vector2 smoothDeltaPosition;
    Vector2 velocity;

    void Start()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        agent.updatePosition = false;
    }

    void Update()
    {
        Vector3 worldDelta = agent.nextPosition - transform.position;
        float dx = Vector3.Dot(transform.right, worldDelta);
        float dy = Vector3.Dot(transform.forward, worldDelta);
        Vector2 deltaPosition = new Vector2(dx, dy);

        float smooth = Mathf.Min(1.0f, Time.deltaTime / 0.15f);
        smoothDeltaPosition = Vector2.Lerp(smoothDeltaPosition, deltaPosition, smooth);

        if (Time.deltaTime > 1e-5f)
            velocity = smoothDeltaPosition / Time.deltaTime;

        bool shouldMove = velocity.magnitude > 0.5f && agent.remainingDistance > agent.radius;

        anim.SetBool("move", shouldMove);
        anim.SetFloat("velx", velocity.x);
        anim.SetFloat("vely", velocity.y);
    }

    void OnAnimatorMove()
    {
        transform.position = agent.nextPosition;
    }
}
```

**Animation-Driven Agent** (higher animation quality, agent follows):
Replace `OnAnimatorMove()` to use animation root position with NavMesh height:
```csharp
void OnAnimatorMove()
{
    Vector3 position = anim.rootPosition;
    position.y = agent.nextPosition.y;
    transform.position = position;
}
```
Pull character towards agent if drift exceeds radius (add at end of `Update()`):
```csharp
if (worldDelta.magnitude > agent.radius)
    transform.position = agent.nextPosition - 0.9f * worldDelta;
```

### Runtime NavMesh Baking
```csharp
using UnityEngine;
using Unity.AI.Navigation;

public class RuntimeNavMeshBaker : MonoBehaviour
{
    NavMeshSurface surface;

    void Start()
    {
        surface = GetComponent<NavMeshSurface>();
        surface.BuildNavMesh();
    }

    public void RebakeNavMesh()
    {
        surface.UpdateNavMesh(surface.navMeshData);
    }
}
```

### Package installation from a live Editor

Editing `Packages/manifest.json` is the route that needs no Editor. When one is connected,
this is the equivalent C#:

```csharp
// Fully qualified: this runs through `eval`, which rejects `using` directives.
var request = UnityEditor.PackageManager.Client.Add("com.unity.ai.navigation@2");
UnityEngine.Debug.Log("Requested com.unity.ai.navigation@2. Progress shows in the Package Manager window.");
```


## Important API Notes

0. **Namespace:** All navigation components are in `UnityEngine.AI`. The package components (NavMeshSurface, NavMeshLink, NavMeshModifier, NavMeshModifierVolume) are in `Unity.AI.Navigation`.

1. **Setting destination:** Use `agent.destination = position;` or `agent.SetDestination(position);`. Both trigger pathfinding. `SetDestination` returns `bool` indicating if the path request was submitted.

2. **Checking path status:** Use `agent.pathStatus` to check if the path is complete, partial, or invalid:
```csharp
if (agent.pathStatus == NavMeshPathStatus.PathComplete)
    // Full path to destination
else if (agent.pathStatus == NavMeshPathStatus.PathPartial)
    // Can only reach partway
else
    // No path at all (PathInvalid)
```

3. **Checking remaining distance:** Use `agent.remainingDistance`. IMPORTANT: Check `agent.pathPending` first — `remainingDistance` is unreliable while a path is being calculated:
```csharp
if (!agent.pathPending && agent.remainingDistance < 0.5f)
    // Arrived at destination
```

4. **Stopping the agent:** Set `agent.isStopped = true;` to pause movement (retains path). Set `agent.ResetPath();` to clear the path entirely.

5. **Warping the agent:** Use `agent.Warp(position);` to teleport the agent to a new position on the NavMesh. Do NOT set `transform.position` directly — the agent may become desynced from the NavMesh.

6. **NavMesh sampling:** To find the nearest point on the NavMesh:
```csharp
if (NavMesh.SamplePosition(sourcePosition, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
{
    Vector3 nearestNavMeshPoint = hit.position;
}
```

7. **Raycast on NavMesh:** To check if there is an unobstructed path between two points on the NavMesh:
```csharp
NavMeshHit hit;
if (agent.Raycast(targetPosition, out hit))
{
    // Path is blocked; hit.position is the point where it's blocked
    // hit.distance is the distance to the blocking point
}
```

8. **Path calculation without movement:** Calculate a path without moving the agent:
```csharp
NavMeshPath path = new NavMeshPath();
if (agent.CalculatePath(targetPosition, path))
{
    // path.corners contains the waypoints
    // path.status tells if the path is complete, partial, or invalid
}
```

9. **Off-mesh link traversal:** When `autoTraverseOffMeshLink` is disabled, handle manually:
```csharp
if (agent.isOnOffMeshLink)
{
    OffMeshLinkData data = agent.currentOffMeshLinkData;
    // Animate/teleport from data.startPos to data.endPos
    agent.CompleteOffMeshLink();
}
```

10. **NavMeshSurface baking via script:** The package provides `NavMeshSurface.BuildNavMesh()` for editor and runtime baking, and `NavMeshSurface.UpdateNavMesh(navMeshData)` for incremental updates.

11. **Area cost overrides per agent:**
```csharp
// Make "Water" area (index 3) cost 5x for this specific agent
agent.SetAreaCost(3, 5.0f);
```

12. **CRITICAL: Correct API Names**

| WRONG (Hallucinated) | CORRECT |
|---------------------|---------|
| `NavMeshAgent.Move(position)` for teleporting | `NavMeshAgent.Warp(position)` |
| `NavMeshAgent.Stop()` | `NavMeshAgent.isStopped = true;` |
| `NavMeshAgent.Resume()` | `NavMeshAgent.isStopped = false;` |
| `NavMeshAgent.target` | `NavMeshAgent.destination` |
| `NavMesh.Bake()` | `NavMeshSurface.BuildNavMesh()` (package API) |
| `NavMeshAgent.navMeshPath` | `NavMeshAgent.path` |
| `NavMeshPath.waypoints` | `NavMeshPath.corners` |
| `agent.velocity` for setting | `agent.velocity` is read-only for current velocity; use `agent.speed` for max speed |


## Core Concepts Reference

### NavMesh (Navigation Mesh)
A mesh Unity generates to approximate walkable areas. Stored as convex polygons with neighbor connectivity. Built by the NavMeshSurface component via voxelization of scene geometry.

### How Pathfinding Works
1. Start and destination positions are mapped to the nearest NavMesh polygons.
2. A* algorithm searches connected polygons to find the shortest path (a "corridor" of polygons).
3. The agent steers towards the next visible corner of the corridor.
4. Obstacle avoidance (RVO — reciprocal velocity obstacles) adjusts velocity to prevent collisions with other agents and NavMesh edges.
5. A simple dynamic model applies acceleration for smooth movement.
6. After movement, the agent position is constrained back onto the NavMesh.

### Global vs. Local Navigation
- **Global:** Finding the corridor path across the entire NavMesh. Expensive but infrequent.
- **Local:** Steering towards the next corner, avoiding other agents frame-by-frame. Cheap but continuous.

### Agent Types
Defined in **Window > AI > Navigation > Agents tab**. Each type specifies:
- **Radius / Height:** Cylinder dimensions for NavMesh baking clearance.
- **Step Height:** Max step the agent can climb.
- **Max Slope:** Steepest walkable incline (degrees).
- **Drop Height / Jump Distance:** Limits for auto-generated links.

A NavMeshSurface bakes for one agent type. Multiple surfaces with different agent types support multiple character sizes.

### Voxels and Bake Quality
The bake process rasterizes geometry into a 3D voxel grid. Smaller voxels = more accurate NavMesh but slower baking.
- Default: 3 voxels per agent radius (good for doorways and general use).
- Big open areas: 1–2 voxels per radius (faster).
- Tight indoor areas: 4–6 voxels per radius (more detail).
- More than 8 voxels per radius rarely helps.


## Component Reference — NavMesh Surface

| Property | Description |
|---|---|
| **Agent Type** | Which agent type this NavMesh is built for. |
| **Default Area** | Area type assigned to generated NavMesh (Walkable, Not Walkable, Jump, or custom). |
| **Generate Links** | Auto-generate jump-across and drop-down links between collected objects. |
| **Use Geometry** | "Render Meshes" or "Physics Colliders". Colliders let agents walk closer to edges. |
| **Collect Objects** | "All Game Objects", "Volume", "Current Object Hierarchy", or "NavMeshModifier Component Only". |
| **Include Layers** | Layer mask filtering which objects contribute to bake. |

### Advanced Settings
| Property | Description |
|---|---|
| **Override Voxel Size** | Override the default voxel size (1/3 agent radius). |
| **Override Tile Size** | Override default tile size (256 voxels). Smaller tiles = faster carving but more NavMesh fragmentation. Use 64–128 for scenes with many obstacles. |
| **Minimum Region Area** | Remove small disconnected NavMesh patches below this size. |
| **Build Height Mesh** | Generate extra data for accurate vertical agent placement (e.g., stairs). Uses more memory. |


## Component Reference — NavMesh Agent

### Main
| Property | Description |
|---|---|
| **Agent Type** | Must match a NavMeshSurface's agent type to use that NavMesh. |
| **Base Offset** | Height offset of the collision cylinder relative to the transform pivot. |

### Steering
| Property | Description |
|---|---|
| **Speed** | Max movement speed (units/sec). |
| **Angular Speed** | Max rotation speed (deg/sec). |
| **Acceleration** | Max acceleration (units/sec²). |
| **Stopping Distance** | Agent stops this far from destination. |
| **Auto Braking** | Slow down near destination. Disable for continuous patrol loops. |

### Obstacle Avoidance
| Property | Description |
|---|---|
| **Radius** | Agent collision radius. |
| **Height** | Agent height clearance. |
| **Quality** | Avoidance quality. Reduce for large crowds. "None" = no active avoidance. |
| **Priority** | 0–99 (lower = higher priority). Agents avoid higher-priority agents. |

### Path Finding
| Property | Description |
|---|---|
| **Auto Traverse OffMesh Link** | Automatically cross NavMesh Links and OffMesh Links. Disable for custom traversal (animation). |
| **Auto Repath** | Retry pathfinding when reaching end of a partial path. |
| **Area Mask** | Which area types this agent can traverse. |


## Component Reference — NavMesh Obstacle

| Property | Description |
|---|---|
| **Shape** | Box or Capsule. |
| **Center / Size** | (Box) Obstacle dimensions relative to transform. |
| **Center / Radius / Height** | (Capsule) Obstacle dimensions relative to transform. |
| **Carve** | Cut a hole in the NavMesh when stationary. |
| **Move Threshold** | Distance moved before the carved hole updates. |
| **Time To Stationary** | Seconds idle before the obstacle is treated as stationary. |
| **Carve Only Stationary** | Only carve when stopped (best performance for physics objects). |

### When to Use Carving vs. Obstruction
| Scenario | Carve | Reason |
|----------|-------|--------|
| Moving vehicle / player | Off | Use local avoidance; carving is too expensive for moving objects. |
| Stationary crate / barrel | On | Agents plan paths around; carving recalculates only when moved. |
| Large slow-moving obstacle (tank) | On, Carve Only Stationary = Off | Carve updates when moved past threshold. |
| Sparsely scattered small objects | Off | Local avoidance handles these cheaply. |
| Object that fully blocks a corridor | On | Agents need global pathfinding to find alternate routes. |


## Component Reference — NavMesh Link

| Property | Description |
|---|---|
| **Agent Type** | Which agent type can use this link. |
| **Start Transform / Start Point** | Start position (Transform takes precedence over Point). |
| **End Transform / End Point** | End position (Transform takes precedence over Point). |
| **Width** | 0 = point-to-point line; positive = span with width. |
| **Cost Override** | Override traversal cost (deselect to use area type cost). |
| **Auto Update Positions** | Update link ends when transforms move. |
| **Bidirectional** | Allow traversal in both directions. |
| **Area Type** | Walkable, Not Walkable, Jump, or custom. |
| **Activated** | Must be enabled for agents to use the link. Disabled = red gizmo. |

### Troubleshooting Links
- Both ends must be over a NavMesh — check with NavMesh debug visualization.
- Agent's Area Mask must include the link's Area Type.
- Activated must be enabled.
- Agent Type on the link must match the traversing agent's type.


## Component Reference — NavMesh Modifier

| Property | Description |
|---|---|
| **Mode** | "Add or Modify Object" (include) or "Remove Object" (exclude). |
| **Affected Agents** | Which agent types are affected (All, None, or specific). |
| **Apply to Children** | Cascade to child GameObjects. Another Modifier further down overrides. |
| **Override Area** | Change the area type for this object. |
| **Override Generate Links** | Force include/exclude from link generation. |

Replaces the legacy "Navigation Static" flag. Works with runtime baking.


## Component Reference — NavMesh Modifier Volume

| Property | Description |
|---|---|
| **Size** | Box dimensions (XYZ). |
| **Center** | Box center relative to GameObject. |
| **Area Type** | Area type to stamp within this volume. |
| **Affected Agents** | Which agent types are affected. |

When multiple volumes overlap, the highest-index area type wins. **Not Walkable always takes precedence** regardless of index.


## Navigation Areas and Costs

### Built-In Area Types
| Area | Index | Description |
|------|-------|-------------|
| **Walkable** | 0 | Generic walkable area. |
| **Not Walkable** | 1 | Blocks navigation; always takes precedence in overlaps. |
| **Jump** | 2 | Assigned to auto-generated links. |

29 custom area types are available (indices 3–31). Define them in **Window > AI > Navigation > Areas tab**.

### How Cost Works
Path cost = `distance × area cost`. Higher cost areas are treated as longer distances by A*. All costs must be > 1.0.

Example: If "Water" has cost 3.0, a 10-unit path through water costs the same as a 30-unit path on "Walkable" (cost 1.0). The pathfinder prefers the 30-unit dry route only if it exists.

### Per-Agent Cost Override
```csharp
// Make area index 4 ("Mud") cost 5x for this agent
agent.SetAreaCost(4, 5.0f);
```

### Area Mask
Each agent has an area mask controlling which areas it can use. Set in Inspector or via script:
```csharp
// Allow only Walkable (bit 0) and custom area 3 (bit 3)
agent.areaMask = (1 << 0) | (1 << 3);
```
Use case: Zombies cannot open doors → uncheck "Door" area in zombie agents' mask.


## Mixing Components Guide

### NavMeshAgent + Physics
- Agents do NOT need colliders to avoid each other (navigation handles this).
- To push physics objects or use triggers: add Collider + Rigidbody with **Is Kinematic = true**.
- NEVER have both NavMeshAgent and non-kinematic Rigidbody active simultaneously — both try to move the transform, causing undefined behavior.
- You can use a NavMeshAgent for player movement without physics. Set low avoidance priority (high number) so the player brushes through crowds, and move via `NavMeshAgent.velocity`.

### NavMeshAgent + Animator (Root Motion)
Both try to move the transform each frame. Pick ONE information flow:

**Option A — Animation follows agent (simpler, some foot-sliding):**
- Let NavMeshAgent control position.
- Feed `agent.velocity` to Animator parameters for blend tree selection.

**Option B — Agent follows animation (higher quality, more complex):**
- Set `agent.updatePosition = false` and `agent.updateRotation = false`.
- Use difference between `agent.nextPosition` and `anim.rootPosition` to drive animation.
- In `OnAnimatorMove()`, use animation root with NavMesh height.

### NavMeshAgent + NavMeshObstacle
- **Do NOT have both active on the same GameObject.** The agent will try to avoid itself.
- Use case: Deactivate the agent and activate the obstacle when a character "dies" to make others path around the body.

### NavMeshObstacle + Physics
- Add NavMeshObstacle to physics objects that agents should be aware of.
- If the object has a Rigidbody, the obstacle velocity is obtained from it automatically for prediction.


## Coupling Animation and Navigation

### Setup Requirements
1. **Animator Controller** with a 2D blend tree for strafe animations (velx, vely parameters) and an Idle state with a "move" bool parameter.
2. **NavMeshAgent** on the same GameObject, with speed matching the animation's maximum velocity.
3. The locomotion script (see [Agent-Driven Animation recipe](#agent-driven-animation-agent-moves-animation-follows)).

### Blend Tree Configuration
- Type: **2D Simple Directional**
- Compute positions: **Velocity XZ**
- Parameters: `velx` (float), `vely` (float)
- Include 7 directional run clips + 1 run-in-place clip (prevents foot-sliding in blends)
- Idle → Move transition: use `move` bool, disable **Has Exit Time**, set transition duration ~0.1s

### Head Look-At (Optional Quality Improvement)
Use `Animator.SetLookAtPosition()` in `OnAnimatorIK()` to have the character look toward `agent.steeringTarget` (the next path corner).


## Troubleshooting Decision Tree

### Agent doesn't move at all
1. Is there a baked NavMesh in the scene? → Check for NavMeshSurface with baked data.
2. Is the agent positioned on or near the NavMesh? → Use `NavMesh.SamplePosition()` to verify. Warp the agent if needed.
3. Is the agent's agent type matching a baked NavMeshSurface agent type?
4. Is `agent.isStopped` set to `true`? → Set to `false`.
5. Has a destination been set? → Check `agent.hasPath` or `agent.destination`.
6. Is the agent enabled and the GameObject active?

### Agent moves but can't reach destination
1. Check `agent.pathStatus`:
   - `PathPartial` → Destination is on a disconnected NavMesh region. Add a NavMeshLink or extend the NavMesh.
   - `PathInvalid` → Destination is not on any NavMesh. Verify the destination point is on walkable area.
2. Is the destination's area type included in the agent's Area Mask?
3. Is a NavMeshObstacle with Carve blocking the only path? → Check for alternate routes or disable the obstacle.

### Agent takes a weird/long path
1. Check area costs — high-cost areas make shorter physical paths appear longer to the pathfinder.
2. Check NavMesh quality — large polygons next to small ones can cause suboptimal node placement. Reduce voxel size for problem areas.
3. Check for unnecessary NavMesh Links that create shortcuts to unintended areas.

### Agent slides through obstacles
1. Is the obstacle a NavMeshObstacle? Without this component, navigation ignores it.
2. Is Carve enabled? Without carving, the agent uses local avoidance only (limited radius).
3. Is the obstacle's shape and size correct? Check Center/Size match the visual mesh.

### Agent vibrates or jitters
1. Is both a non-kinematic Rigidbody and NavMeshAgent active? → Set Rigidbody to kinematic.
2. Is both a NavMeshAgent and NavMeshObstacle active on the same GameObject? → Disable one.
3. Is the agent stuck between two carving obstacles? → Adjust obstacle placement or sizes.

### NavMesh Link not working
1. Are both ends connected to the NavMesh? → Enable NavMesh debug visualization in Scene view.
2. Is the link's Activated property enabled? (Red gizmo = deactivated.)
3. Does the agent's Area Mask include the link's Area Type?
4. Does the link's Agent Type match the agent's Agent Type?
5. Is `autoTraverseOffMeshLink` enabled on the agent? (Or is custom traversal code handling it?)

### NavMesh bake produces unexpected results
1. Check **Use Geometry** — "Render Meshes" includes visual geometry; "Physics Colliders" includes colliders only.
2. Check **Collect Objects** — "Current Object Hierarchy" only includes children.
3. Check **Include Layers** — objects on excluded layers are ignored.
4. Check NavMeshModifiers — an object may be set to "Remove Object".
5. Check voxel size — too large skips small geometry details.
6. Check agent type settings — radius/height/step height/slope may exclude certain surfaces.


## Common Mistakes to Avoid

### 1. No NavMesh Baked
**Problem:** Adding a NavMeshAgent but forgetting to bake a NavMesh. The agent has nowhere to navigate.
**Solution:** Always ensure at least one NavMeshSurface exists and has been baked.

### 2. Agent Type Mismatch
**Problem:** NavMeshAgent's agent type doesn't match any NavMeshSurface's agent type. The agent cannot find any NavMesh.
**Solution:** Ensure the agent type on both the NavMeshSurface and NavMeshAgent match.

### 3. Setting transform.position Directly
**Problem:** Moving a NavMeshAgent by setting `transform.position` desyncs it from the NavMesh.
**Solution:** Use `agent.Warp(position)` to teleport, or `agent.destination` / `agent.SetDestination()` for pathfinding.

### 4. NavMeshAgent + NavMeshObstacle on Same GameObject
**Problem:** The agent tries to avoid itself, causing erratic movement or getting stuck.
**Solution:** Only have one active at a time. Toggle between them based on state (alive vs. dead).

### 5. Non-Kinematic Rigidbody with NavMeshAgent
**Problem:** Both the physics engine and the navigation system try to move the transform each frame.
**Solution:** If you need both, set `Rigidbody.isKinematic = true`.

### 6. Checking remainingDistance While Path Is Pending
**Problem:** `agent.remainingDistance` returns unreliable values while `agent.pathPending` is true.
**Solution:** Always guard with `if (!agent.pathPending && agent.remainingDistance < threshold)`.

### 7. Forgetting Auto Braking for Patrol
**Problem:** Agent slows to a crawl at each patrol waypoint because Auto Braking is on.
**Solution:** Set `agent.autoBraking = false` for continuous patrol movement.

### 8. Obstacles Without Carve for Static Blockers
**Problem:** A stationary obstacle blocks a corridor but agents walk into it because only local avoidance is used (no carving).
**Solution:** Enable **Carve** on stationary obstacles that block paths so the global pathfinder routes around them.

### 9. NavMesh Link Ends Not on NavMesh
**Problem:** Link gizmo shows disconnected ends (gray lines). Agents can't use the link.
**Solution:** Position link start and end points directly over baked NavMesh surfaces. Check with NavMesh debug visualization.

### 10. Using Deprecated OffMeshLink Instead of NavMeshLink
**Problem:** `OffMeshLink` is the legacy component. `NavMeshLink` from the AI Navigation package is the modern replacement with more features (width, transforms, auto-update).
**Solution:** Always use `NavMeshLink` (from `Unity.AI.Navigation` namespace) for new setups. Migrate existing `OffMeshLink` components.

### 11. Not Re-Baking After Scene Changes
**Problem:** NavMesh doesn't reflect newly added or moved geometry.
**Solution:** Re-bake the NavMesh after modifying scene geometry, modifiers, or surface settings. For runtime changes, call `NavMeshSurface.BuildNavMesh()` or `UpdateNavMesh()`.

### 12. Voxel Size Too Large for Narrow Passages
**Problem:** Doorways or narrow corridors are missing from the NavMesh because the voxel grid is too coarse.
**Solution:** Reduce voxel size (or use the default 3 voxels per agent radius). For tight spaces, use 4–6 voxels per radius.
