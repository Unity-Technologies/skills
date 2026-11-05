# Graph API parts the manual does not cover yet

The Graph Toolkit manual pages cover the 6.6 Graph API: graph and node classes, ports,
options, context and block nodes, subgraphs, type casting, toolbar actions and `OnGraphChanged`. This
file covers what they do not: the change delta, and the Unity 6000.7 additions. Members marked
**6.7** do not exist on 6000.6. All types live in `Unity.GraphToolkit.Editor`; confirm any member not
listed here in the Script Reference before using it.

## Contents

1. Knowing what changed
2. Ports whose type is chosen or follows a connection (6.7)
3. Blackboard types and variable nodes (6.7)
4. Context menus (6.7)
5. Custom node UI (6.7)

## 1. Knowing what changed

```csharp
[NonSerialized] HashSet<Hash128> m_KnownNodes;

public override void OnGraphChanged(GraphLogger logger)
{
    // 6.7 and newer: added and modified nodes.
    foreach (var change in logger.GraphChanges.ChangedNodes)
        if (change.Node is ObjectiveNode objective && (change.ChangeKinds & ChangeKind.Added) != 0) { /* new */ }

    // Every version: removed nodes, and on 6.6 additions too, come from diffing IDs.
    var current = new HashSet<Hash128>(GetNodes().Select(n => n.ID));
    if (m_KnownNodes != null)
        foreach (var removed in m_KnownNodes.Except(current)) { /* removed */ }
    m_KnownNodes = current;
}
```

- **6.7** `GraphLogger.GraphChanges` is the change delta. It does not exist on 6.6.
- The delta lists added and modified elements only. Removed nodes, variables and subgraph nodes are
  never reported; removed ports are, under `ChangedNode.ChangedPorts` with `ChangeKind.Removed`.
- Members are in `graphchanges.md` and `changekind.md`. Each entry exposes its element by type,
  for example `ChangedNode.Node`, plus `ChangeKinds` flags to test with `&`.
- The ID cache is `[NonSerialized]` so it is not written to the asset, and it starts null after a
  domain reload, so the first call only fills it.
- Never mutate the graph in `OnGraphChanged`; it throws `InvalidOperationException`. Offer the fix as
  a `GraphLogAction` on the message instead.

## 2. Ports whose type is chosen or follows a connection (6.7)

- `WithDataTypes(params Type[])` or `WithDataTypes(IEnumerable<Type>)` on an untyped port builder makes
  a polymorphic port that accepts any of the listed types. At least one type; duplicates are ignored.
  On 6.6, use an untyped port and allow the connections in `Graph.IsConnectionAllowed`.
- `IPort.TrySetDataType(Type)` changes a port's type, and the override
  `Node.OnPortDataTypeChanged(IPort port, Type previous, Type next)` reacts to it, for ports whose type
  follows what they are connected to.
- `Graph.UndoBeginRecordGraph(string actionName, params Node[] nodesToRecord)` records specific nodes.

## 3. Blackboard types and variable nodes (6.7)

- Restrict the Blackboard type dropdown by overriding
  `protected virtual IEnumerable<Type> BuildAvailableVariableTypes(IReadOnlyCollection<Type> baseSupportedTypes)`
  and `BuildAvailableConstantTypes(...)` on the graph. `Graph.SupportedTypes`, `AvailableVariableTypes`
  and `AvailableConstantTypes` list the result. On 6.6 the type list is not customizable. A
  `StateMachine` overrides the parameterless `BuildAvailableVariableTypes()` and has no constant types.
- `Graph.AddVariableNode(IVariable, Vector2, VariableNodeMode)` with `VariableNodeMode.Get` or `Set`,
  read back through `IVariableNode.Mode`.

## 4. Context menus (6.7)

```csharp
[GraphMenu(typeof(MyGraph))]
static void AddGraphMenuEntries(GraphMenuContext context)
{
    if (context.ClickedObject is INode node)
        context.AppendAction("Inspect node", () => Debug.Log(node.Title));
    context.AppendSeparator();
}
```

The context has `object ClickedObject`, `Vector2 MousePosition`, `AppendAction(name, Action)`,
`AppendAction(name, Action<DropdownMenuAction>[, status callback])` and `AppendSeparator(subMenuPath)`.
`[BlackboardMenu(typeof(MyGraph))]` receives the same context for the Blackboard. State machine menus
are in `state-machine-api.md`.

## 5. Custom node UI (6.7)

`class MyNodeView : NodeView<MyNode>` adds UI Toolkit elements to a node. Discovery is by the generic
argument, walking up the node's inheritance chain. On 6.6 the only styling is USS through
`[Node(category, icon, title, stylesheet)]`, `Node.DefaultColor` and `Node.FillAmount`.

| Symptom | Cause | Fix |
|---|---|---|
| Duplicated labels or buttons pile up on the node | UI allocated in `OnViewAttached`, which fires on every re-attach | Allocate in `OnViewBuilt`, cache, append to `View.Root` |
| The custom element disappears after zooming far out and back | Culling clears `Root` | Re-add the cached element in `OnCullingChanged(false)` |

Also available: `OnViewDetached()`, `OnViewLODChanged(float zoom)`. The same rules apply to
`StateView<T>`. `ConditionView<T>` has no LOD or culling callbacks: `OnViewBuilt`, `OnViewAttached`,
`OnViewDetached` and `OnConditionChanged` only.
