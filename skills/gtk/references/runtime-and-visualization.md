# From authored graph to runtime, and back

Graph Toolkit has no runtime. The pattern used by every official sample is: a `ScriptedImporter`
registered on the graph's extension compiles the authored graph into a plain runtime asset at import
time, the game loads that asset, and, optionally, editor-only code mirrors execution back into the
open graph window through the GraphVisualization API.

Contents

1. Assembly layout
2. The importer
3. Walking nodes and ports
4. Runtime data model
5. GraphVisualization: live debug view (6.6+)
6. Building graphs from code

## 1. Assembly layout

- `Editor/` (or an Editor-only asmdef): graph, nodes, importer, toolbar, views. References
  `Unity.GraphToolkit.Editor`.
- `Runtime/` asmdef: the runtime data model and executor. Must not reference
  `Unity.GraphToolkit.Editor`. Visualization calls live here under `#if UNITY_EDITOR`, because they
  need the editor to be running the game.
- If the runtime asset type is referenced from the importer, the Editor asmdef references the Runtime
  asmdef, never the reverse.

## 2. The importer

```csharp
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;

[ScriptedImporter(1, DialogueGraph.AssetExtension)]       // bump the version to force reimport
class DialogueGraphImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var graph = GraphDatabase.LoadGraphForImporter<DialogueGraph>(ctx.assetPath);
        if (graph == null)
        {
            Debug.LogError($"Failed to load dialogue graph: {ctx.assetPath}");
            return;
        }

        var runtime = ScriptableObject.CreateInstance<DialogueRuntimeGraph>();
        runtime.GraphId = graph.ID;
        Build(graph, runtime);

        ctx.AddObjectToAsset("Runtime", runtime);
        ctx.SetMainObject(runtime);             // the .ext file now behaves as a DialogueRuntimeGraph in inspectors
    }
}
```

- Use `LoadGraphForImporter`, not `LoadGraph`, inside importers. The state machine equivalent is
  `StateMachineDatabase.LoadStateMachineForImporter<T>`.
- A null graph means the path, type or file is wrong; log and return.
- Validation errors belong in `OnGraphChanged`, which already shows markers; the importer only needs
  to bail out quietly when the graph is invalid.
- Re-import happens on save. Sub-assets the game needs (textures, meshes) are added with further
  `AddObjectToAsset` calls.

## 3. Walking nodes and ports

```csharp
static INode Next(INode node, string outputPortName)
{
    var port = node.GetOutputPortByName(outputPortName);
    return port?.FirstConnectedPort?.GetNode();          // null when unconnected
}

static T ValueOf<T>(INode node, string inputPortName)
{
    var port = node.GetInputPortByName(inputPortName);
    if (port.IsConnected)
    {
        var source = port.FirstConnectedPort.GetNode();  // resolve upstream: constant node, variable node, or your node
        if (source is IConstantNode constant && constant.TryGetValue<T>(out var c)) return c;
        if (source is IVariableNode variable && variable.Variable.TryGetDefaultValue<T>(out var d)) return d;
    }
    port.TryGetValue<T>(out var embedded);               // the value typed on the node
    return embedded;
}
```

- Start from a dedicated entry node type: `graph.GetNodes().OfType<StartNode>().FirstOrDefault()`.
- Ports with `PortCapacity.Multi` need `GetConnectedPorts(list)` rather than `FirstConnectedPort`.
- Record `node.ID`, `port.ID` and wire endpoints (`outputPort.ID`, `inputPort.ID`) in the runtime
  asset when a debug view is planned; the visualization API is keyed by these `Hash128` values.
- Block nodes: iterate `contextNode.BlockNodes`; a block's `Index` is its order.
- Subgraph nodes: `((ISubgraphNode)node).GetSubgraph()` returns the nested `Graph`; recurse.

## 4. Runtime data model

Keep it minimal and Unity-serializable: a `ScriptableObject` with a list of `[Serializable]` node
records or `[SerializeReference]` polymorphic nodes, referencing each other by index or `Hash128`.
Store only what execution needs; positions, titles and colors stay in the authoring asset. The
Visual Novel Director sample is the reference layout: `Runtime/` asmdef with the data model and a
`MonoBehaviour` executor that walks the records.

## 5. GraphVisualization: live debug view (6.6+)

Namespace `Unity.GraphToolkit.Editor.GraphVisualization`, editor-only.

```csharp
#if UNITY_EDITOR
using Unity.GraphToolkit.Editor.GraphVisualization;
#endif

class DialogueRunner : MonoBehaviour
{
#if UNITY_EDITOR
    Context m_Debug;
#endif

    void OnEnable()
    {
#if UNITY_EDITOR
        m_Debug = Registry.CreateVisualizationContext(runtimeGraph.GraphId);   // the authored Graph.ID
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        m_Debug?.Dispose();
#endif
    }

    void Enter(DialogueRuntimeNode node)
    {
#if UNITY_EDITOR
        if (m_Debug is { IsValid: true, IsGraphLoaded: true })
        {
            var nodeRef = m_Debug.GetNodeReference(node.Id);
            m_Debug.Motion.Play(nodeRef, 1f);                              // animate the accent bar
            m_Debug.GetPortReference(node.TextPortId).SetPreview(node.Text);
            var wire = m_Debug.GetWireReference(node.OutPortId, node.NextInPortId);
            wire.WidthOverride = 6f;
            m_Debug.Motion.Play(wire, 1f);
        }
#endif
    }
}
```

`Registry`: `Context CreateVisualizationContext(Hash128 graphID)`, `Context GetActiveContext(Hash128 graphID)`.

`Context` (`IDisposable`): `IsValid`, `IsGraphLoaded`, `GraphMotion Motion`,
`GetNodeReference(Hash128)`, `GetPortReference(Hash128)`, `GetWireReference(Hash128 outputPortID, Hash128 inputPortID)`,
`GetStateReference(Hash128)` (6.7), `GetTransitionReference(Hash128)` (6.7), `GetConditionReference(Hash128)` (6.7),
toggles `NodeCustomizationEnabled`, `PortPreviewEnabled`, `WireCustomizationEnabled`,
`StateCustomizationEnabled`, `TransitionCustomizationEnabled`, `ConditionCustomizationEnabled`,
and `ClearAllVisualization()`.

- `NodeReference`: `FillAmount`, `ClearCustomization()`.
- `PortReference`: `SetPreview(string)`, `TryGetPreview(out string)`, `ClearPreview()`.
- `WireReference`: `IsDashed`, `WidthOverride`, `Opacity`, `ClearCustomization()`.
- `GraphMotion`: `Play(ref, float speed = 1f)`, `Stop(ref)`, `Pause(ref)` for node, wire, state and
  transition references.

Rules of thumb from the samples: create one context per run and dispose it when the run ends; clear
previews when the flow moves on so they do not pile up; leave traversed wires thicker but stopped to
show the path taken; add a `[GraphToolbarElement]` toggle that flips the `*Enabled` properties so the
debug view can be switched off.

## 6. Building graphs from code

Generators, migrations and tests build assets without the UI:

```csharp
var graph = GraphDatabase.LoadGraph<DungeonGraph>(path) ?? GraphDatabase.CreateGraph<DungeonGraph>(path);
graph.UndoBeginRecordGraph("Generate dungeon");

var start = new StartNode { Position = new Vector2(0, 0) };
graph.AddNode(start);
start.GetInputPortByName("Prefab").TrySetValue(startRoomPrefab);

var room = new EncounterNode { Position = new Vector2(300, 0) };
graph.AddNode(room);
graph.Connect(start.GetOutputPortByName("Output"), room.GetInputPortByName("Input"));

var enemies = graph.CreateVariable<GameObject>("Enemy");
var enemyNode = graph.AddVariableNode(enemies, new Vector2(300, 150));
graph.Connect(enemyNode.GetOutputPort(0), room.GetInputPortByName("Enemy"));

GraphDatabase.SaveGraph(graph);
graph.UndoEndRecordGraph();
```

Ports exist as soon as the node is added to the graph, because adding triggers `OnDefinePorts`.
Set `Position` before or after adding; both work. State machines follow the same shape with
`AddState`, `Connect(fromState, toState)`, `SaveStateMachine` and the `UndoBeginRecordStateMachine`
pair.
