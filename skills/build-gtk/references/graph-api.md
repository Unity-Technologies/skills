# Graph API reference (Unity 6.6 and newer)

All types live in `Unity.GraphToolkit.Editor` unless stated. Members marked **6.7** exist only on
Unity 6000.7 and newer. Every signature below was taken from the module source; when something is
not listed here, look it up at
`https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/Unity.GraphToolkit.Editor.<Type>.html`,
with `<VERSION>` the project's Editor version, for example `6000.6`.

## Contents

1. Graph class and asset lifecycle
2. Nodes
3. Ports
4. Node options
5. Context and block nodes
6. Subgraphs
7. Variables and constants
8. Validation with OnGraphChanged
9. Type casting between ports
10. Toolbar elements
11. Context menus (6.7)
12. Node look: colors, accent, USS, custom UI
13. Wires

## 1. Graph class and asset lifecycle

```csharp
[Graph(AssetExtension)]                      // optional second arg: GraphOptions flags
[Serializable]
class MyGraph : Graph
{
    public const string AssetExtension = "mygraph";

    [MenuItem("Assets/Create/My Tool/My Graph")]
    static void CreateAssetFile() => GraphDatabase.PromptInProjectBrowserToCreateNewAsset<MyGraph>();
}
```

`GraphOptions` flags: `SupportsSubgraphs`, `DisableAutoInclusionOfNodesFromGraphAssembly`.
`Default` and `None` are 0.

`GraphDatabase` (static):

- `PromptInProjectBrowserToCreateNewAsset<T>(string defaultName = "New Graph")`
- `T CreateGraph<T>(string assetPath)`, `T LoadGraph<T>(string assetPath)`, `SaveGraph(Graph)`
- `T LoadGraphForImporter<T>(string assetPath)` — the only loader to use inside a `ScriptedImporter`
- `GUID GetGraphAssetGUID(Graph)`, `string GetGraphAssetPath(Graph)`

`Graph` members:

- `Name`, `NodeCount`, `VariableCount`, `Hash128 ID`, `GUID AssetGuid`
- `GetNodes()`, `GetNode(int)`, `AddNode(Node)`, `RemoveNode(INode)`
- `bool Connect(IPort output, IPort input)`, `bool Disconnect(IPort output, IPort input)`,
  `Wire GetWire(IPort output, IPort input)`
- `CreateVariable<T>(name, defaultValue, VariableKind)`, `CreateVariable(name, Type, object, VariableKind)`,
  `GetVariables()`, `GetVariables(SortMethod)`, `GetVariable(int)`, `RemoveVariable(IVariable, bool forceRemove)`
- `CreateConstantNode<T>(Vector2 position, T defaultValue)`,
  `AddVariableNode(IVariable, Vector2 position)`, **6.7** `AddVariableNode(IVariable, Vector2, VariableNodeMode)`,
  `AddSubgraphNode(Graph subgraph, Vector2)`, `CreateLocalSubgraphNode<TSubGraph>(name, Vector2)`
- `virtual OnEnable()`, `virtual OnDisable()`, `virtual OnGraphChanged(GraphLogger)`
- `UndoBeginRecordGraph(string actionName)`, **6.7** `UndoBeginRecordGraph(string actionName, params Node[] nodesToRecord)`,
  `UndoEndRecordGraph()`
- `virtual bool IsConnectionAllowed(IPort output, IPort input)`
- **6.7** `SupportedTypes`, `AvailableVariableTypes`, `AvailableConstantTypes`, and the overrides
  `protected virtual IEnumerable<Type> BuildAvailableVariableTypes(IReadOnlyCollection<Type> baseSupportedTypes)`
  and `BuildAvailableConstantTypes(...)` to restrict the Blackboard type list
- `protected virtual OnDefineSubgraphNodeOptions(Node.IOptionDefinitionContext)`

`OnEnable` runs when the asset is created, loaded, reopened, or after a domain reload. Mutating the
graph in `OnEnable`, `OnDisable` or `OnGraphChanged` throws `InvalidOperationException`.

## 2. Nodes

```csharp
[Serializable]
[Node("Math/Basic", "Icons/Add.png", "Add", "Styles/AddNode.uss")]   // all args after the first are optional
class AddNode : Node
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<float>("a").Build();
        context.AddInputPort<float>("b").WithDefaultValue(1f).Build();
        context.AddOutputPort<float>("result").Build();
    }
}
```

`NodeAttribute(categoryPath[, iconPath[, title[, stylesheet]]])`. The category path is a folder path
in the Add menu; the node's name is appended to it, so `"Math/Basic"` lists the node under
`Math/Basic/<title>`. Icons: provide `name.png` and `d_name.png` (dark theme), 128 px or larger.

`Node` members: `Graph`, `Hash128 ID`, `Title`, `Subtitle`, `Tooltip`, `Color DefaultColor`,
`float FillAmount`, `Vector2 Position`, `RemoveFromGraph()`,
`GetInputPorts()`, `GetInputPort(int)`, `GetInputPortByName(string)`, `InputPortCount`,
`GetOutputPorts()`, `GetOutputPort(int)`, `GetOutputPortByName(string)`, `OutputPortCount`,
`NodeOptions`, `GetNodeOption(int)`, `GetNodeOptionByName(string)`, `NodeOptionCount`.

Overrides: `OnEnable()`, `OnDisable()`, `OnDefinePorts(IPortDefinitionContext)`,
`OnDefineOptions(IOptionDefinitionContext)`, **6.7** `OnPortDataTypeChanged(IPort, Type previous, Type next)`.
Set a dynamic default title in `OnEnable` with `Title = ...`.

Discovery: node classes in the graph's assembly are listed automatically unless the graph sets
`GraphOptions.DisableAutoInclusionOfNodesFromGraphAssembly`. Nodes elsewhere use
`[UseWithGraph(typeof(MyGraph), typeof(OtherGraph))]`. Abstract and open generic classes are skipped.

## 3. Ports

`IPortDefinitionContext`:

- `IInputPortBuilder AddInputPort(string portName)`, `IInputPortBuilder<T> AddInputPort<T>(string)`
- `IOutputPortBuilder AddOutputPort(string portName)`, `IOutputPortBuilder<T> AddOutputPort<T>(string)`

Builder methods, chain then finish with `.Build()` which returns the `IPort`:

| Method | Available on | Effect |
|---|---|---|
| `WithDisplayName(string)` | all | Label shown on the node; the constructor name stays the lookup key |
| `WithTooltip(string)` | all | Hover text |
| `WithConnectorUI(PortConnectorUI.Circle | Arrowhead)` | all | Connector shape |
| `AsVertical()` | all | Top/bottom placement; labels hidden; not on block nodes |
| `WithCapacity(PortCapacity.None | Single | Multi)` | all | Max wire count |
| `WithDataType<T>()` / `WithDataType(Type)` | untyped builders | Give a type after the fact |
| **6.7** `WithDataTypes(params Type[])` / `WithDataTypes(IEnumerable<Type>)` | untyped builders | Polymorphic port that accepts any of the listed types; at least one type, duplicates ignored |
| `WithDefaultValue(T)` | typed input | Value used when unconnected; editable on the node |
| `Delayed()` | input | Commit on Enter or focus loss |
| `AsTextArea(minLines, maxLines)` | input (string) | Multi-line field |

A port built without a type has `DataType == typeof(Untyped)`; use it for execution flow.

`IPort`: `Type DataType`, `Name`, `DisplayName`, `Tooltip`, `PortDirection Direction`,
`bool IsConnected`, `IPort FirstConnectedPort`, `GetConnectedPorts(List<IPort>)`,
`bool TryGetValue<T>(out T)`, `bool TrySetValue<T>(T)`, `Hash128 ID`, and the extension
`port.GetNode()`.

`TryGetValue` returns the embedded value of an unconnected input; when connected, follow
`FirstConnectedPort.GetNode()` upstream instead.

## 4. Node options

Options are fields shown on the node body and in the Graph Inspector.

```csharp
const string k_Count = "Count";

protected override void OnDefineOptions(IOptionDefinitionContext context)
{
    context.AddOption<int>(k_Count).WithDisplayName("Input count").WithDefaultValue(2).Delayed();
    context.AddOption<Mode>("Mode").ShowInInspectorOnly();
}

protected override void OnDefinePorts(IPortDefinitionContext context)
{
    GetNodeOptionByName(k_Count).TryGetValue<int>(out var count);
    for (var i = 0; i < count; i++)
        context.AddInputPort<float>($"in{i}").Build();
}
```

`IOptionDefinitionContext`: `AddOption<T>(string name)`, `AddOption(string name, Type dataType)`.
Builder: `WithDisplayName`, `WithTooltip`, `WithDefaultValue`, `Delayed`, `AsTextArea`,
`ShowInInspectorOnly`, `Build()` (optional; the option is registered without it).
Option names must be unique on the node.
`INodeOption`: `DataType`, `Name`, `DisplayName`, `Tooltip`, `TryGetValue<T>`. Options are
user-edited; set the initial value with `WithDefaultValue`, not `TrySetValue`.

Ports are rebuilt whenever an option changes, so `OnDefinePorts` must be a pure function of the
option values.

## 5. Context and block nodes

```csharp
[Serializable] class OnUpdateContext : ContextNode { }

[Serializable]
[UseWithContext(typeof(OnUpdateContext))]      // several context types allowed; derived contexts qualify
class LogBlock : BlockNode
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<string>("Message").Build();
    }
}
```

`ContextNode`: `BlockCount`, `BlockNodes`, `GetBlock(int)`, `AddBlockNode(BlockNode)`,
`InsertBlockNode(int, BlockNode)`, `CreateBlockNode<T>(int index = -1)`, `RemoveBlockNode`,
`ClearBlockNodes()`. `BlockNode`: `ContextNode`, `Index`. Block ports are always horizontal.

## 6. Subgraphs

- Enable with `[Graph(ext, GraphOptions.SupportsSubgraphs)]` on the main graph.
- Without further attributes the main graph type doubles as the subgraph type. To use dedicated
  subgraph classes, mark each with `[Subgraph(typeof(MainGraph))]`; several are allowed and each gets
  its own "Create <Name> Subgraph from Selection" action.
- Two flavours: asset subgraphs (a separate `.ext` file, `AddSubgraphNode(graph, position)`) and
  local subgraphs stored inside the parent (`CreateLocalSubgraphNode<TSub>(name, position)`).
- The subgraph node's ports come from the subgraph's Blackboard variables with `VariableKind.Input`
  or `VariableKind.Output`.
- `ISubgraphNode.GetSubgraph()` returns the nested `Graph`. Override
  `OnDefineSubgraphNodeOptions` on the main graph to add options to subgraph nodes.

## 7. Variables and constants

- `graph.CreateVariable<float>("Speed", 1f, VariableKind.Local)` declares a Blackboard variable.
  `VariableKind`: `Local`, `Input`, `Output`.
- `IVariable`: `Name`, `DataType`, `VariableKind`, `IsConnected`, `NodeCount`, `Graph`, **6.7** `StateMachine`, `Hash128 ID`,
  `TryGetDefaultValue<T>`, `TrySetDefaultValue<T>`, `GetNodes(List<IVariableNode>)`,
  `RemoveFromGraph(bool forceRemove)`.
- `graph.AddVariableNode(variable, position)` drops a variable onto the canvas; `IVariableNode.Variable`.
  **6.7** adds the `VariableNodeMode.Get | Set` overload and `IVariableNode.Mode`.
- `graph.CreateConstantNode<int>(position, 5)`; `IConstantNode.DataType`, `TryGetValue`, `TrySetValue`.
- **6.7** Restrict the type dropdown by overriding `BuildAvailableVariableTypes` and
  `BuildAvailableConstantTypes`; `Graph.SupportedTypes` lists the defaults. On 6.6 the Blackboard
  type list is not customizable from the public API.

## 8. Validation with OnGraphChanged

```csharp
public override void OnGraphChanged(GraphLogger logger)
{
    var starts = GetNodes().OfType<StartNode>().ToList();
    if (starts.Count == 0)
        logger.LogError("Add a Start node.");                       // marker on the graph
    foreach (var extra in starts.Skip(1))
        logger.LogWarning("Only one Start node is used.", extra,     // marker on the node
            new GraphLogAction("Remove this node", n =>
            {
                UndoBeginRecordGraph("Remove extra Start");
                RemoveNode((INode)n);
                UndoEndRecordGraph();
            }));

    // 6.7 and newer only: the change delta. Added and modified nodes are reported; removed nodes are
    // not, only removed ports are (inside ChangedPorts with ChangeKind.Removed).
    foreach (var change in logger.GraphChanges.ChangedNodes)
        if (change.Node is ObjectiveNode objective) { /* added or modified: check it */ }
}
```

`GraphLogger`: `LogError`, `LogWarning`, `Log`, each as `(object message, object context = null)` or
`(message, context, GraphLogAction)`. Context may be a node, a port, or null for the whole graph.
Messages also go to the Console. `GraphLogAction(string description, Action<object> action)` shows a
fix button on the marker.

**6.7** `GraphLogger.GraphChanges` and `ChangeKind` are the change delta. They do not exist on 6.6.
The delta lists added and modified elements only; removed nodes, variables and subgraph nodes are
not reported (removed ports are, under `ChangedNode.ChangedPorts`). So on every version, detecting a
removed node means keeping a `[NonSerialized] HashSet<Hash128>` of node IDs on the graph and diffing
it against `GetNodes()` each call; on 6.6 that diff is also the only way to find additions.

`GraphChanges`: `ChangedNodes`, `ChangedVariables`, `ChangedConstantNodes`, `ChangedSubgraphNodes`.
Entry types and their element property: `ChangedNode.Node` (`INode`), `ChangedVariable.Variable`,
`ChangedConstantNode.ConstantNode`, `ChangedSubgraphNode.SubgraphNode`; each also has `Hash128 ID` and
`ChangeKind ChangeKinds`, and `ChangedNode` adds `ChangedPorts` (`ChangedPort.Port`, `ID`, `ChangeKinds`).
`ChangeKind` flags: `Layout`, `Style`, `Data`, `Topology`, `Grouping`, `Added`, `Removed`,
`PortChanged`, `RecreateView`.

## 9. Type casting between ports

```csharp
public override bool IsConnectionAllowed(IPort output, IPort input)
{
    if (output.DataType == typeof(int) && input.DataType == typeof(float)) return true;
    return base.IsConnectionAllowed(output, input);
}
```

The graph only records that the wire exists; the importer or runtime performs the conversion.
**6.7** `IPort.TrySetDataType(Type)` and `Node.OnPortDataTypeChanged` support ports whose type follows
a connection.

## 10. Toolbar elements

```csharp
[GraphToolbarElement("MyTool/Bake", typeof(MyGraph), order: 200)]
class BakeButton : EditorToolbarButton, IAccessContainerWindow
{
    public EditorWindow containerWindow { get; set; }

    public BakeButton()
    {
        text = "Bake";
        clicked += () =>
        {
            if (containerWindow is IGraphWindow window && window.Graph is MyGraph graph)
                Bake(graph);
        };
    }
}
```

`UnityEditor.Toolbars.EditorToolbarButton` and `EditorToolbarToggle` both work. `IGraphWindow.Graph`
gives the open graph.

## 11. Context menus (6.7)

```csharp
[GraphMenu(typeof(MyGraph))]
static void AddGraphMenuEntries(GraphMenuContext context)
{
    if (context.ClickedObject is INode node)
        context.AppendAction("Inspect node", () => Debug.Log(node.Title));
    context.AppendSeparator();
}
```

`MenuContext`: `object ClickedObject`, `Vector2 MousePosition`, `AppendAction(name, Action)`,
`AppendAction(name, Action<DropdownMenuAction>[, status callback])`, `AppendSeparator(subMenuPath)`.
`[BlackboardMenu(typeof(MyGraph))]` receives the same context for the Blackboard.

## 12. Node look: colors, accent, USS, custom UI

- `Node.DefaultColor` sets the accent bar color; `Node.FillAmount` fills it as a progress bar.
- `[Node(category, icon, title, stylesheet)]` applies a USS file to the node's element.
- **6.7**: `class MyNodeView : NodeView<MyNode>` adds UI Toolkit elements. Allocate in
  `OnViewBuilt()` and append to `View.Root`; re-add in `OnCullingChanged(false)`; never allocate in
  `OnViewAttached()`, which fires repeatedly. Also `OnViewDetached`, `OnViewLODChanged(float zoom)`.
  Discovery is by the generic argument, walking up the node inheritance chain.

## 13. Wires

`graph.GetWire(output, input)` returns a `Wire` with `OutputPort`, `InputPort`, `Graph`,
`float WidthOverride`, `float Opacity`, `bool IsDashed`. These are authoring-time styles; for runtime
styling use `WireReference` in `runtime-and-visualization.md`.
