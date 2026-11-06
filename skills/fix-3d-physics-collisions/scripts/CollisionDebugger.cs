// Editor-only contact recorder for the fix-3d-physics-collisions skill.
//
// It adds no components and saves nothing to the scene. In Play mode it sets Collider.providesContacts on the
// tracked colliders, which Unity reverts when Play mode ends, reads Physics.ContactEvent, flattens and filters
// the contact buffer in a job, and logs the result to the Console with the [CollisionDebugger] prefix.
//
// 1. Copy this file to Assets/Editor/CollisionDebugger.cs and wait for the Editor to compile it.
// 2. In Edit mode, call CollisionDebugger.Track("Bullet", "Wall") with the GameObject names to watch.
// 3. Enter Play mode and reproduce the problem. Contact enter and exit events are logged as they happen.
// 4. Exit Play mode. A summary of every tracked pair is logged, and the debugger switches itself off.
// 5. Delete this file and its .meta file.
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class CollisionDebugger
{
    const string k_Prefix = "[CollisionDebugger]";
    const string k_ActiveKey = "CollisionDebugger.Active";
    const string k_TrackedKey = "CollisionDebugger.Tracked";
    const int k_MaxEventLogs = 200;

    enum Phase : byte { Stay, Enter, Exit }

    struct PairRecord
    {
        public EntityId collider;
        public EntityId otherCollider;
        public EntityId body;
        public EntityId otherBody;
        public Phase phase;
        public int contactCount;
        public float impulse;
        public bool tracked;
    }

    struct PairStats
    {
        public int enter;
        public int stay;
        public int exit;
        public int maxContacts;
        public float maxImpulse;
        public EntityId body;
        public EntityId otherBody;
    }

    // Flattens every ContactPair in the buffer into one record, and marks the records that involve a tracked
    // collider or body. The buffer is read-only and only valid until the next simulation step.
    struct FlattenContactsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<ContactPairHeader>.ReadOnly headers;
        [ReadOnly] public NativeArray<int> offsets;
        [ReadOnly] public NativeArray<EntityId> trackedIds;
        [NativeDisableParallelForRestriction] public NativeArray<PairRecord> records;

        public void Execute(int index)
        {
            var header = headers[index];
            var body = header.bodyEntityId;
            var otherBody = header.otherBodyEntityId;
            var pairCount = header.pairCount;

            for (int i = 0; i < pairCount; i++)
            {
                ref readonly var pair = ref header.GetContactPair(i);
                var record = new PairRecord
                {
                    collider = pair.colliderEntityId,
                    otherCollider = pair.otherColliderEntityId,
                    body = body,
                    otherBody = otherBody,
                    phase = pair.isCollisionEnter ? Phase.Enter : pair.isCollisionExit ? Phase.Exit : Phase.Stay,
                    contactCount = pair.contactCount,
                    impulse = pair.impulseSum.magnitude,
                };
                record.tracked = Contains(record.collider) || Contains(record.otherCollider)
                    || Contains(body) || Contains(otherBody);
                records[offsets[index] + i] = record;
            }
        }

        bool Contains(EntityId id)
        {
            for (int i = 0; i < trackedIds.Length; i++)
                if (trackedIds[i] == id)
                    return true;
            return false;
        }
    }

    // Object.GetEntityId is public from Unity 6000.5, which removes GetInstanceID. In 6000.3 and 6000.4 the
    // instance ID converts to the same EntityId that contact events report.
    static EntityId IdOf(Object obj)
    {
#if UNITY_6000_5_OR_NEWER
        return obj.GetEntityId();
#else
        return obj.GetInstanceID();
#endif
    }

    static bool s_Attached;
    static int s_EventLogs;
    static NativeArray<EntityId> s_TrackedIds;
    static readonly List<Collider> s_TrackedColliders = new List<Collider>();
    static readonly Dictionary<EntityId, string> s_Names = new Dictionary<EntityId, string>();
    static readonly Dictionary<(EntityId, EntityId), PairStats> s_Stats = new Dictionary<(EntityId, EntityId), PairStats>();
    static readonly HashSet<EntityId> s_Touched = new HashSet<EntityId>();
    static readonly HashSet<(Collider, Collider)> s_Overlaps = new HashSet<(Collider, Collider)>();
    static readonly HashSet<Collider> s_EverOverlapped = new HashSet<Collider>();
    static readonly List<(Collider, Collider)> s_OverlapScratch = new List<(Collider, Collider)>();
    static Collider[] s_OverlapHits = new Collider[64];

    static CollisionDebugger()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        AssemblyReloadEvents.beforeAssemblyReload += Detach;
    }

    /// Records contacts for the named GameObjects, and their child colliders, in the next Play mode session.
    /// At least one name is required, because only tracked colliders are set to provide contacts.
    /// Call this in Edit mode.
    public static void Track(string gameObjectName, params string[] moreGameObjectNames)
    {
        var names = new List<string> { gameObjectName };
        if (moreGameObjectNames != null)
            names.AddRange(moreGameObjectNames);
        names.RemoveAll(string.IsNullOrWhiteSpace);
        if (names.Count == 0)
        {
            Debug.LogError($"{k_Prefix} Pass the name of at least one GameObject to track.");
            return;
        }

        SessionState.SetString(k_TrackedKey, string.Join("\n", names));
        SessionState.SetBool(k_ActiveKey, true);
        var what = string.Join(", ", names);
        if (EditorApplication.isPlaying)
            Debug.LogWarning($"{k_Prefix} Tracking {what} from the next Play mode session. Exit and re-enter Play mode, because the debugger finds the tracked GameObjects when Play mode starts.");
        else
            Debug.Log($"{k_Prefix} Tracking {what}. Enter Play mode to record.");
    }

    /// Switches the debugger off without waiting for Play mode to end.
    public static void Stop()
    {
        if (s_Attached)
            LogSummary();
        Detach();
        SessionState.EraseBool(k_ActiveKey);
        SessionState.EraseString(k_TrackedKey);
    }

    static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (!SessionState.GetBool(k_ActiveKey, false))
            return;

        switch (change)
        {
            case PlayModeStateChange.EnteredPlayMode:
                Attach();
                break;
            case PlayModeStateChange.ExitingPlayMode:
                if (s_Attached)
                    LogSummary();
                Detach();
                SessionState.EraseBool(k_ActiveKey);
                SessionState.EraseString(k_TrackedKey);
                break;
        }
    }

    static void Attach()
    {
        if (s_Attached)
            return;

        s_Names.Clear();
        s_Stats.Clear();
        s_Touched.Clear();
        s_Overlaps.Clear();
        s_EverOverlapped.Clear();
        s_TrackedColliders.Clear();
        s_EventLogs = 0;

        var names = SessionState.GetString(k_TrackedKey, string.Empty)
            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var ids = new List<EntityId>();
        var report = new StringBuilder($"{k_Prefix} Setup\n");
        LogGlobalSettings(report);

        var tracked = new List<GameObject>();
        foreach (var name in names)
        {
            var matches = FindInLoadedScenes(name);
            if (matches.Count == 0)
                report.AppendLine($"- '{name}': no GameObject with this name in the loaded scenes, including inactive ones.");
            tracked.AddRange(matches);
        }

        foreach (var go in tracked)
        {
            LogGameObject(go, report);
            foreach (var col in go.GetComponentsInChildren<Collider>(true))
            {
                // Physics.ContactEvent only reports colliders that provide contacts. This change is made in
                // Play mode only, so Unity discards it when Play mode ends.
                col.providesContacts = true;
                s_TrackedColliders.Add(col);
                ids.Add(IdOf(col));
                s_Names[IdOf(col)] = PathOf(col);
                if (col.attachedRigidbody != null)
                {
                    ids.Add(IdOf(col.attachedRigidbody));
                    s_Names[IdOf(col.attachedRigidbody)] = PathOf(col.attachedRigidbody);
                }
            }
        }

        LogPairFilters(tracked, report);
        Debug.Log(report.ToString());

        if (ids.Count == 0)
            Debug.LogWarning($"{k_Prefix} None of the tracked GameObjects has a 3D Collider, so there is nothing to record.");

        s_TrackedIds = new NativeArray<EntityId>(ids.ToArray(), Allocator.Persistent);
        Physics.ContactEvent += OnContactEvent;
        EditorApplication.update += CheckTriggerOverlaps;
        s_Attached = true;
    }

    static void Detach()
    {
        if (!s_Attached)
            return;
        Physics.ContactEvent -= OnContactEvent;
        EditorApplication.update -= CheckTriggerOverlaps;
        if (s_TrackedIds.IsCreated)
            s_TrackedIds.Dispose();
        s_Attached = false;
    }

    static void OnContactEvent(PhysicsScene scene, NativeArray<ContactPairHeader>.ReadOnly headers)
    {
        var headerCount = headers.Length;
        if (headerCount == 0)
            return;

        var offsets = new NativeArray<int>(headerCount, Allocator.TempJob);
        var total = 0;
        for (int i = 0; i < headerCount; i++)
        {
            offsets[i] = total;
            total += headers[i].pairCount;
        }

        var records = new NativeArray<PairRecord>(total, Allocator.TempJob);
        var job = new FlattenContactsJob
        {
            headers = headers,
            offsets = offsets,
            trackedIds = s_TrackedIds,
            records = records,
        };

        // The contact buffer is only valid until the next simulation step, so complete the job before returning.
        job.Schedule(headerCount, 16).Complete();

        for (int i = 0; i < total; i++)
        {
            var r = records[i];
            if (r.tracked)
                Record(r);
        }

        records.Dispose();
        offsets.Dispose();
    }

    static void Record(in PairRecord r)
    {
        s_Touched.Add(r.collider);
        s_Touched.Add(r.otherCollider);
        s_Touched.Add(r.body);
        s_Touched.Add(r.otherBody);

        var key = r.collider.CompareTo(r.otherCollider) <= 0 ? (r.collider, r.otherCollider) : (r.otherCollider, r.collider);
        s_Stats.TryGetValue(key, out var stats);
        stats.body = r.body;
        stats.otherBody = r.otherBody;
        stats.maxContacts = Math.Max(stats.maxContacts, r.contactCount);
        stats.maxImpulse = Math.Max(stats.maxImpulse, r.impulse);
        switch (r.phase)
        {
            case Phase.Enter: stats.enter++; break;
            case Phase.Exit: stats.exit++; break;
            default: stats.stay++; break;
        }
        s_Stats[key] = stats;

        if (r.phase == Phase.Stay || s_EventLogs > k_MaxEventLogs)
            return;
        if (++s_EventLogs > k_MaxEventLogs)
        {
            Debug.Log($"{k_Prefix} More than {k_MaxEventLogs} contact events. Further events are only counted in the summary.");
            return;
        }
        Debug.Log($"{k_Prefix} [Contact {r.phase}] frame {Time.frameCount}: {NameOf(r.collider)} <-> {NameOf(r.otherCollider)} " +
            $"| bodies: {NameOf(r.body)} / {NameOf(r.otherBody)} | contacts: {r.contactCount} | impulse: {r.impulse:F3}");
    }

    // Physics.ContactEvent never reports triggers, so compare the geometry of tracked colliders instead.
    // An overlap here means the shapes intersect. Whether Unity sends OnTrigger messages for it also depends on
    // the layers, the body types, and Contact Pairs Mode, which the Setup report lists.
    static void CheckTriggerOverlaps()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || s_TrackedColliders.Count == 0)
            return;

        s_OverlapScratch.Clear();
        foreach (var col in s_TrackedColliders)
        {
            if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
                continue;
            var hitCount = OverlapBounds(col);
            for (int i = 0; i < hitCount; i++)
            {
                var other = s_OverlapHits[i];
                if (other == col || (!col.isTrigger && !other.isTrigger))
                    continue;
                var ct = col.transform;
                var ot = other.transform;
                if (!Physics.ComputePenetration(col, ct.position, ct.rotation, other, ot.position, ot.rotation, out _, out _))
                    continue;
                // Store the trigger first. When both are triggers, order them so the pair is logged once.
                var pair = col.isTrigger && (!other.isTrigger || IdOf(col).CompareTo(IdOf(other)) < 0) ? (col, other) : (other, col);
                if (!s_OverlapScratch.Contains(pair))
                    s_OverlapScratch.Add(pair);
            }
        }

        foreach (var pair in s_OverlapScratch)
        {
            if (!s_Overlaps.Add(pair))
                continue;
            s_EverOverlapped.Add(pair.Item1);
            s_EverOverlapped.Add(pair.Item2);
            Debug.Log($"{k_Prefix} [Trigger overlap start] frame {Time.frameCount}: trigger {PathOf(pair.Item1)} overlaps {PathOf(pair.Item2)} " +
                $"| bodies: {BodyType(pair.Item1)} / {BodyType(pair.Item2)}");
        }

        s_Overlaps.RemoveWhere(pair =>
        {
            if (s_OverlapScratch.Contains(pair))
                return false;
            Debug.Log($"{k_Prefix} [Trigger overlap end] frame {Time.frameCount}: {(pair.Item1 ? PathOf(pair.Item1) : "(destroyed)")} / {(pair.Item2 ? PathOf(pair.Item2) : "(destroyed)")}");
            return true;
        });
    }

    // Collects every collider that overlaps the bounds of col into s_OverlapHits, and returns the count. It queries
    // the collider's own physics scene, because a scene loaded with LocalPhysicsMode.Physics3D has its own world.
    // A full buffer can mean results were dropped, so grow it and query again until the result is complete.
    static int OverlapBounds(Collider col)
    {
        var bounds = col.bounds;
        var physicsScene = col.gameObject.scene.GetPhysicsScene();
        while (true)
        {
            var hitCount = physicsScene.OverlapBox(bounds.center, bounds.extents, s_OverlapHits, Quaternion.identity,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            if (hitCount < s_OverlapHits.Length)
                return hitCount;
            s_OverlapHits = new Collider[s_OverlapHits.Length * 2];
        }
    }

    static void LogSummary()
    {
        var sb = new StringBuilder($"{k_Prefix} Summary for frames up to {Time.frameCount}\n");
        if (s_Stats.Count == 0)
            sb.AppendLine("- No contacts were recorded for the tracked colliders.");
        foreach (var entry in s_Stats)
        {
            var s = entry.Value;
            sb.AppendLine($"- {NameOf(entry.Key.Item1)} <-> {NameOf(entry.Key.Item2)} | bodies: {NameOf(s.body)} / {NameOf(s.otherBody)} " +
                $"| enter: {s.enter}, stay: {s.stay}, exit: {s.exit} | max contacts: {s.maxContacts} | max impulse: {s.maxImpulse:F3}");
        }

        foreach (var col in s_TrackedColliders)
        {
            if (col == null || s_Touched.Contains(IdOf(col)))
                continue;
            if (!col.isTrigger)
                sb.AppendLine($"- {PathOf(col)} had no contacts. Check its layers, the Layer Collision Matrix, its Rigidbody, and Contact Pairs Mode in the Setup report.");
            else if (!s_EverOverlapped.Contains(col))
                sb.AppendLine($"- Trigger {PathOf(col)} never overlapped another collider. Check its position, size, and layers in the Setup report.");
        }
        Debug.Log(sb.ToString());
    }

    // The Physics settings, read from the project's DynamicsManager.asset because some of them, such as
    // Contact Pairs Mode, have no scripting API. Where a script changed a setting at runtime, both values are listed.
    static void LogGlobalSettings(StringBuilder sb)
    {
        sb.AppendLine($"- Unity {Application.unityVersion} | Time.timeScale: {Time.timeScale}");
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset");
        if (assets.Length == 0)
        {
            sb.AppendLine("- Couldn't load ProjectSettings/DynamicsManager.asset to read the Physics settings.");
            return;
        }

        var settings = new SerializedObject(assets[0]);
        var line = new StringBuilder("- Physics settings");
        AppendSetting(settings, line, "m_ContactPairsMode", "Contact Pairs Mode", null, k_ContactPairsModes);
        AppendSetting(settings, line, "m_SimulationMode", "Simulation Mode", ((int)Physics.simulationMode).ToString());
        AppendSetting(settings, line, "m_InvokeCollisionCallbacks", "Invoke Collision Callbacks", Physics.invokeCollisionCallbacks.ToString());
        AppendSetting(settings, line, "m_GenerateOnTriggerStayEvents", "Generate OnTriggerStay Events", null);
        AppendSetting(settings, line, "m_QueriesHitTriggers", "Queries Hit Triggers", Physics.queriesHitTriggers.ToString());
        AppendSetting(settings, line, "m_QueriesHitBackfaces", "Queries Hit Backfaces", Physics.queriesHitBackfaces.ToString());
        AppendSetting(settings, line, "m_DefaultContactOffset", "Default Contact Offset", Physics.defaultContactOffset.ToString());
        AppendSetting(settings, line, "m_SleepThreshold", "Sleep Threshold", Physics.sleepThreshold.ToString());
        sb.AppendLine(line.ToString());
    }

    static readonly string[] k_ContactPairsModes =
        { "Default Contact Pairs", "Enable Kinematic Kinematic Pairs", "Enable Kinematic Static Pairs", "Enable All Contact Pairs" };

    static void AppendSetting(SerializedObject settings, StringBuilder line, string propertyName, string label, string runtimeValue, string[] names = null)
    {
        var p = settings.FindProperty(propertyName);
        if (p == null)
        {
            line.Append($" | {label}: not found");
            return;
        }

        string raw;
        switch (p.propertyType)
        {
            case SerializedPropertyType.Boolean: raw = p.boolValue.ToString(); break;
            case SerializedPropertyType.Float: raw = p.floatValue.ToString(); break;
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.Enum: raw = p.intValue.ToString(); break;
            default: raw = p.propertyType.ToString(); break;
        }

        var display = raw;
        if (names != null && p.intValue >= 0 && p.intValue < names.Length)
            display = $"{raw} ({names[p.intValue]})";
        else if (p.propertyType == SerializedPropertyType.Enum && p.enumValueIndex >= 0 && p.enumValueIndex < p.enumDisplayNames.Length)
            display = $"{raw} ({p.enumDisplayNames[p.enumValueIndex]})";

        line.Append($" | {label}: {display}");
        if (runtimeValue != null && runtimeValue != raw)
            line.Append($" (changed by a script to {runtimeValue})");
    }

    static void LogGameObject(GameObject go, StringBuilder sb)
    {
        sb.AppendLine($"- {PathOf(go)} | activeInHierarchy: {go.activeInHierarchy} | layer: {go.layer} ({LayerMask.LayerToName(go.layer)}) " +
            $"| lossyScale: {go.transform.lossyScale}");
        if (go.GetComponent<CharacterController>() != null)
            sb.AppendLine("  - Has a CharacterController: it reports hits through OnControllerColliderHit, not OnCollisionEnter.");
        if (go.GetComponentInChildren<Collider2D>(true) != null || go.GetComponentInParent<Rigidbody2D>() != null)
            sb.AppendLine("  - Has 2D physics components, which never interact with 3D colliders.");

        foreach (var col in go.GetComponentsInChildren<Collider>(true))
        {
            var mesh = col as MeshCollider;
            var parentBody = col.GetComponentInParent<Rigidbody>(true);
            if (mesh != null && !mesh.convex && !mesh.isTrigger && col.attachedRigidbody == null && parentBody != null && !parentBody.isKinematic)
                sb.AppendLine($"  - Non-convex MeshCollider under the dynamic Rigidbody {PathOf(parentBody)}: Unity logs an error and doesn't attach the collider to the body.");
            var meshInfo = mesh == null ? string.Empty
                : $" | convex: {mesh.convex} | mesh: {(mesh.sharedMesh ? mesh.sharedMesh.name : "none")} | isReadable: {(mesh.sharedMesh ? mesh.sharedMesh.isReadable.ToString() : "n/a")}";
            sb.AppendLine($"  - {col.GetType().Name} on {PathOf(col)} | enabled: {col.enabled} | isTrigger: {col.isTrigger} " +
                $"| includeLayers: {col.includeLayers.value} | excludeLayers: {col.excludeLayers.value} | contactOffset: {col.contactOffset}{meshInfo} " +
                $"| body: {BodyType(col)}");
        }

        foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
            sb.AppendLine($"  - Rigidbody on {PathOf(rb)} | isKinematic: {rb.isKinematic} | collisionDetectionMode: {rb.collisionDetectionMode} " +
                $"| detectCollisions: {rb.detectCollisions} | includeLayers: {rb.includeLayers.value} | excludeLayers: {rb.excludeLayers.value}");
    }

    static void LogPairFilters(List<GameObject> tracked, StringBuilder sb)
    {
        for (int a = 0; a < tracked.Count; a++)
        {
            for (int b = a + 1; b < tracked.Count; b++)
            {
                var ga = tracked[a];
                var gb = tracked[b];
                sb.AppendLine($"- {ga.name} / {gb.name} | layers ignored by the Layer Collision Matrix: {Physics.GetIgnoreLayerCollision(ga.layer, gb.layer)}");
                foreach (var ca in ga.GetComponentsInChildren<Collider>(true))
                    foreach (var cb in gb.GetComponentsInChildren<Collider>(true))
                        if (Physics.GetIgnoreCollision(ca, cb))
                            sb.AppendLine($"  - Physics.IgnoreCollision is set for {PathOf(ca)} / {PathOf(cb)}");
            }
        }
    }


    static List<GameObject> FindInLoadedScenes(string name)
    {
        var result = new List<GameObject>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name)
                        result.Add(t.gameObject);
        }
        return result;
    }

    static string BodyType(Collider col)
    {
        var rb = col.attachedRigidbody;
        if (rb != null)
            return rb.isKinematic ? $"Kinematic ({rb.name})" : $"Dynamic ({rb.name})";
        var ab = col.attachedArticulationBody;
        return ab != null ? $"ArticulationBody ({ab.name})" : "Static";
    }

    static string NameOf(EntityId id)
    {
        if (id == default)
            return "(none)";
        if (s_Names.TryGetValue(id, out var name))
            return name;
        var obj = Resources.EntityIdToObject(id);
        name = obj != null ? PathOf(obj) : $"(destroyed {id})";
        s_Names[id] = name;
        return name;
    }

    static string PathOf(Object obj)
    {
        var t = obj is Component c ? c.transform : obj is GameObject g ? g.transform : null;
        if (t == null)
            return obj.name;
        var path = t.name;
        for (var p = t.parent; p != null; p = p.parent)
            path = p.name + "/" + path;
        return obj is Component ? $"{path} ({obj.GetType().Name})" : path;
    }
}
