---
name: build-gtk
description: >-
  Builds graph tools with Unity Graph Toolkit (GTK) on 6.6+, state machines on 6.7+, and importers for
  runtime assets. Use this skill for graph, node or state machine editors, or GraphView migrations. Not
  for Shader Graph, VFX Graph or Animator.
required_editor_version: ">=6000.6"
---

# Unity Graph Toolkit (GTK)

Build Editor graph tools and state machine tools on Unity's Graph Toolkit module, using only its
public API in the `Unity.GraphToolkit.Editor` namespace.

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for
  each step.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask
  for. See [Final step](#final-step).
- Avoid long explanations.
- **Graph Toolkit is a built-in module, not a package.** Never add `com.unity.graphtoolkit` to the
  manifest; it is a deprecated shim. Editor code needs no asmdef reference to use it.
- **Use only the public API.** If a draft uses any name from the left column, stop and rewrite it with
  the right column:

  | Never write | Write instead |
  |---|---|
  | `GraphModel`, `GraphObject`, `GraphTool`, `Stencil`, `BaseGraphTool` | `Graph` and `GraphDatabase` |
  | `NodeModel`, `PortModel`, `WireModel` | `Node`, `IPort`, `Wire` |
  | `StateModel`, `TransitionModel`, `ConditionModel` | `State`, `ITransition`, `Condition<T>` |
  | `GraphViewEditorWindow`, `GraphElementsExtensionMethodsCache`, `ModelView` | the window is provided; `NodeView<T>` on 6000.7+ for custom UI |
  | `UnityEditor.Experimental.GraphView`, `graphViewChanged`, `Port.Create` | model the tool as `Graph` and `Node` classes; track changes in `OnGraphChanged` |
  | `com.unity.graphtoolkit` in the manifest | nothing; the module is built in |

- **Spell members exactly.** Properties are PascalCase (`FirstConnectedPort`, `IsConnected`),
  attributes take positional arguments, and IDs are `Hash128`, not strings. Look up any member you
  have not seen in the docs or references instead of guessing it.
- **Every authoring class is `[Serializable]`** (graph, node, state, condition, self transition) and
  lives in Editor-only code. Otherwise nodes reset or vanish after a domain reload.
- **Keep runtime assemblies free of `Unity.GraphToolkit.Editor`.** Guard visualization calls with
  `#if UNITY_EDITOR`, because the module is Editor-only and any reference to it breaks the player build.
- **Use `LoadGraphForImporter` (not `LoadGraph`) inside a `ScriptedImporter`.** The state machine
  equivalent is `StateMachineDatabase.LoadStateMachineForImporter<T>`.
- **Wrap every programmatic edit** in `UndoBeginRecordGraph` / `UndoEndRecordGraph` and call
  `GraphDatabase.SaveGraph`. Never mutate the graph inside `OnEnable`, `OnDisable` or `OnGraphChanged`;
  it throws `InvalidOperationException`.

## Step 1

Before you go forward with a workflow or diagnosis:

1. Get the Editor version from the user's message, or else from `m_EditorVersion` in
   `ProjectSettings/ProjectVersion.txt`, and pick the API from the table below. A version the user
   names wins over a newer project. Ask only when neither gives it.
2. Open and read the [documentation page(s)](#topic-map) that cover what you intend to do or fix. Where
   the manual has no page, read the reference file named in [Step 3](#step-3-where-the-docs-stop).
3. Name the file(s) you read in your response.

A missing Unity project or unreachable docs do not block the answer: write the code from this skill
and its references, say where each file goes, and name what you could not check.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong
Unity version. Graph Toolkit changed significantly between 6000.4, 6000.6 and 6000.7.

| Editor version | Available |
|---|---|
| Older than 6000.4 | Only the retired experimental package (0.x). Its API differs from this skill. Recommend upgrading. |
| 6000.4 to 6000.5 | Module present, but without port capacity, type casting, wire and node visualization, USS node styling, or element IDs. |
| 6000.6 | The Graph API. |
| 6000.7 and newer | Everything above plus the State Machine API, `NodeView<T>` / `StateView<T>` / `ConditionView<T>` custom UI, `[GraphMenu]` / `[BlackboardMenu]` context menus, the `GraphLogger.GraphChanges` change delta, polymorphic and type-following ports, and Blackboard type restriction. |

If the user asks for a state machine tool on an Editor older than 6000.7, say plainly that the State
Machine API is not available there. Do not emulate it with the Graph API unless asked.

## Step 2: Start from the graph tool skeleton

The `implement-a-graph-tool.md` and `implement-nodes.md` pages explain this in full. When they cannot
be fetched, start from:

```csharp
[Graph(AssetExtension)] [Serializable]                     // GraphOptions.SupportsSubgraphs as a second argument
class DialogueGraph : Graph
{
    public const string AssetExtension = "dialogue";
    [MenuItem("Assets/Create/Dialogue/Dialogue Graph")]
    static void Create() => GraphDatabase.PromptInProjectBrowserToCreateNewAsset<DialogueGraph>();
}

[Serializable] [Node("Dialogue")]
class LineNode : Node
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("In").Build();                                       // untyped: execution flow
        context.AddInputPort<string>("Text").Build();
        context.AddOutputPort("Next").WithCapacity(PortCapacity.Single).Build();  // outputs accept many wires by default
    }
}
```

Different port types do not connect unless `Graph.IsConnectionAllowed(IPort output, IPort input)`
allows it; the graph only records the wire, and the importer does the conversion.

### Gotchas

| Symptom | Fix |
|---|---|
| A typed input port refuses a second wire, or an output accepts several | Typed inputs default to `PortCapacity.Single`; untyped ports and outputs to `Multi`. Set `WithCapacity` explicitly |
| `GetInputPortByName` or `GetNodeOptionByName` returns the wrong element or null | Port and option names are lookup keys: keep them unique, share a `const` between `OnDefineOptions` and `OnDefinePorts` |
| Ports rebuild on every keystroke of a count option | `.Delayed()` on the option |
| `AsVertical()` has no effect | Block node ports are always horizontal |
| A node lands in an odd Add-menu folder such as `Math/Add/Add` | The first `[Node]` argument is a folder path; the title is appended to it |
| `OnEnable` runs several times | Expected on load, reopen and domain reload: keep it idempotent |
| Compile error on a class marked `[SerializeReference]` | That attribute is for fields; classes take `[Serializable]` |

## Step 3: Where the docs stop

The manual covers the 6.6 Graph API. These parts have no manual page yet, so read the reference file
before writing code. Paths are relative to this skill's folder.

- **Importer to a runtime asset, and wiring the debug view from runtime code:** [`references/runtime-and-visualization.md`](references/runtime-and-visualization.md).
  The importer always has this shape:

  ```csharp
  var graph = GraphDatabase.LoadGraphForImporter<MyGraph>(ctx.assetPath);    // never LoadGraph here
  if (graph == null) return;                                                  // bad path or type: log and stop
  var start = graph.GetNodes().OfType<StartNode>().FirstOrDefault();
  if (start == null) return;                                                  // OnGraphChanged already reported it
  var next = start.GetOutputPortByName("Next").FirstConnectedPort?.GetNode(); // null when unconnected
  var titlePort = next.GetInputPortByName("Title");
  titlePort.TryGetValue<string>(out var title);                               // the value typed on the node
  if (titlePort.IsConnected) { /* resolve titlePort.FirstConnectedPort.GetNode() upstream instead */ }
  ctx.AddObjectToAsset("Runtime", runtime); ctx.SetMainObject(runtime);
  ```

  The runtime asset keeps `Hash128` IDs as `Hash128` and never references `Unity.GraphToolkit.Editor`.
- **State machines (6000.7+):** [`references/state-machine-api.md`](references/state-machine-api.md). The core types:

  ```csharp
  [StateMachine(AssetExtension)] [Serializable]
  class EnemyBrain : StateMachine
  {
      public const string AssetExtension = "enemybrain";
      [MenuItem("Assets/Create/AI/Enemy Brain")]
      static void Create() => StateMachineDatabase.PromptInProjectBrowserToCreateNewAsset<EnemyBrain>();
  }

  [Serializable] class Patrol : State { }                     // no ports; the state machine owns transitions

  [Serializable] [Condition("Distance to player")]
  class DistanceCondition : Condition<float>                  // derive from Condition for a valueless trigger
  {
      protected override bool DisplayComparisonDropdown => true;
  }
  ```

  Validation goes in `OnStateMachineChanged(StateMachineLogger)`. An importer walks
  `GetStates()`, each state's `GetOutgoingTransitions()`, each transition's `GetRules()` and each rule's
  `RootCondition`. That tree always contains built-in conditions, so handle `IGroupCondition` (recurse,
  honor `Operation`, a `GroupConditionOperation` of `And` or `Or`) and `IVariableCondition` before matching custom `Condition<T>` types.
- **Knowing what changed, and the other 6.7 Graph API additions:** [`references/graph-api-gaps.md`](references/graph-api-gaps.md).
  On 6000.7+, `logger.GraphChanges.ChangedNodes` reports added and modified nodes. Removed nodes are
  never reported, so on every version, and for all changes on 6000.6, diff a `[NonSerialized]`
  `HashSet<Hash128>` of node IDs against `GetNodes()`.

## Topic map

The docs are online at `https://docs.unity.com/en-us/engine/<VERSION>/`. Replace `<VERSION>` with the
project's Editor version from `ProjectSettings/ProjectVersion.txt`, for example `6000.6` or `6000.7`.
Pages are served as markdown when the `.md` suffix is kept. Prefer `WebFetch` over `WebSearch`; it is
faster and lands on the exact page. Only fetch what you need.

- **GTK index**: `manual/extending-the-editor/gtk-index.md`, the landing page for all GTK manual topics
  - **Implementing a graph tool**: `manual/extending-the-editor/gtk-index/implementing-a-graph-tool.md`,
    with these pages under it:
    - `implement-a-graph-tool.md`: graph class, asset creation, menu item
    - `implement-nodes.md`: node class, `OnDefinePorts`, discovery rules
    - `implement-node-options.md`: `IOptionDefinitionContext`, `Delayed`, `ShowInInspectorOnly`
    - `implement-context-nodes.md`: `ContextNode`, `BlockNode`, `UseWithContext`
    - `implement-block-nodes.md`: block port rules, horizontal-only ports
    - `type-cast-ports.md`: `IsConnectionAllowed`, cross-type connections
    - `add-custom-toolbar-actions.md`: `GraphToolbarElement`, `IGraphWindow`
    - `add-subgraph-support.md`: `GraphOptions.SupportsSubgraphs`, subgraph ports from Blackboard variables
    - `graph-processing.md`: `OnGraphChanged`, `GraphLogger`, `GraphLogAction`, fix buttons
  - **Graph window and panels**: `manual/extending-the-editor/gtk-index/landing-graph-interface.md`, with
    `graph-window.md`, `blackboard.md`, `graph-inspector.md` and `minimap.md` under it
  - **Debug graph view** (manual from `6000.7`; the API is the same on 6.6):
    `manual/extending-the-editor/gtk-index/landing-implementing-debug-graph-view.md`, with
    `concept-graph-visualization.md`, `task-display-node-progress.md`, `task-display-port-values.md`,
    `task-display-errors-in-graph.md` and `task-customize-wire-display.md` under it
- **Samples**: `https://docs.unity3d.com/Packages/com.unity.graphtoolkit-samples@<PKG_VERSION>/manual/index.html`.
  Install `com.unity.graphtoolkit-samples` by name in the Package Manager, replace `<PKG_VERSION>` with
  the version shown there, then import Texture Maker (importer), Visual Novel Director (runtime and live
  debug view) or Dungeon Graph Generator (building a graph from code).
- **Community**: `https://discussions.unity.com/tag/graph-toolkit`, for known issues and workarounds not
  yet in the docs.

### API reference

If the Editor install folder has a `Data/Documentation` folder (for example
`.../Unity/Hub/Editor/6000.6.0f1/Editor/Data/Documentation`), read the HTML files in its
`en/ScriptReference` folder. Otherwise the Script Reference is online:

```
https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unity/graphtoolkit/editor/<typename>.md
```

Type names are all lowercase with generic arity dropped (`condition.md` for `Condition<T>`). Member
pages nest one level deeper: `.../graph/ongraphchanged.md`. The older HTML form also resolves:
`https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/Unity.GraphToolkit.Editor.<Type>.html`.

**Graph API (6.6+):** `Graph`, `GraphDatabase`, `GraphOptions`, `GraphLogger`, `GraphLogAction`,
`GraphChanges` (6.7), `ChangedNode` (6.7), `ChangeKind` (6.7), `Node`, `NodeAttribute`, `NodeView<T>` (6.7), `IPort`, `IPortDefinitionContext`,
`IInputPortBuilder`, `IOutputPortBuilder`, `PortCapacity`, `PortConnectorUI`, `PortDirection`,
`INodeOption`, `IOptionDefinitionContext`, `ContextNode`, `BlockNode`, `UseWithContext`, `UseWithGraph`,
`ISubgraphNode`, `Subgraph` (attribute), `IVariable`, `IVariableNode`, `VariableKind`, `IConstantNode`,
`IGraphWindow`, `GraphToolbarElement`, `GraphMenu` (6.7), `BlackboardMenu` (6.7), `Wire`

**State Machine API (6.7+):** `StateMachine`, `StateMachineDatabase`, `StateMachineOptions`,
`StateMachineLogger`, `StateMachineLogAction`, `StateMachineChanges`, `State`, `StateAttribute`,
`StateView<T>`, `IState`, `ITransition`, `ITransitionRule`, `ICondition`, `IGroupCondition`,
`GroupConditionOperation`, `IVariableCondition`,
`Condition`, `Condition<T>`, `ConditionAttribute`, `ConditionView<T>`, `ConditionComparison`,
`SelfTransition`, `TransitionAttribute`, `ISubgraphState`, `StateMachineMenu`, `ConditionMenu`,
`UseWithStateMachine`

**GraphVisualization API (6.6+),** namespace `Unity.GraphToolkit.Editor.GraphVisualization`, pages
under `.../graphvisualization/<typename>.md`: `Registry`, `Context`, `GraphMotion`, `NodeReference`,
`PortReference`, `WireReference`, `StateReference` (6.7), `TransitionReference` (6.7),
`ConditionReference` (6.7)

## Final step

Do the following checks:

1. Check the project has zero console errors. Use `unity command console status` for counts. Without
   the Unity CLI, ask the user to select the Editor and report what is displayed in the Console.
2. Create an asset from the new menu item, add every node or state type from the Add menu, connect
   them, save, close and reopen. The Console must show no errors or warnings. A type missing from the
   Add menu is abstract, generic, or in another assembly without `[UseWithGraph]` /
   `[UseWithStateMachine]`.
3. With an importer, confirm the produced runtime object is the main asset.
4. Stop there unless the user asked you to prove it works or you suspect a specific fault.
5. If you do enter Play mode, do it once only to chase a fault you already suspect. Read the world
   through `unity command script eval` rather than adding logging. Take no screenshots as they take too
   much time.

If a check fails, go back and reread the [docs pages](#topic-map) or reference file for that step to
find out what you missed.

## Final report

Give the user a concise but not dense checklist of what you did and why. If the user needs to follow up
with more steps or decisions, list them. For example:

```
I made the following changes:

[✓] Created `Editor/DialogueGraph.cs`: the graph class registered on the `.dlg` extension.
[✓] Created `Editor/DialogueNode.cs`: a start node and a line node with text and next ports.
[✓] Created `Editor/DialogueImporter.cs`: compiles the graph into a `DialogueRuntimeGraph` asset.

To create your first graph:
1. Right-click in the Project window > Create > Dialogue > Dialogue Graph.
2. Double-click the asset to open the graph window.
3. Add a Start node and a Line node from the Add menu and connect them.
4. Save. The Inspector should show the compiled `DialogueRuntimeGraph` as the main asset.

Still to decide:
- More node types (choice branches, wait, end).
- The runtime executor (a MonoBehaviour that walks the runtime asset).
- A live debug view using the GraphVisualization API.
```
