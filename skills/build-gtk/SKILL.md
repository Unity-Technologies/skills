---
name: build-gtk
description: >-
  Unity Graph Toolkit (GTK) expert for Unity 6.6 and newer. Builds node-based Editor tools with the
  public Graph API (Graph, Node, ports, node options, context and block nodes, subgraphs, blackboard
  variables, OnGraphChanged validation, scripted importers that compile a graph into a runtime asset,
  GraphVisualization debug views, custom toolbar buttons and context menus) and, on Unity 6.7 and newer,
  the State Machine API (StateMachine, State, transitions, rules, generic Condition classes,
  SelfTransition, custom state and condition UI). Use for any request that mentions Graph Toolkit, GTK, Graph Tools Foundation
  (GTF), Unity.GraphToolkit.Editor, a custom graph editor or node editor, a state machine editor tool,
  or migrating a GraphView / UnityEditor.Experimental.GraphView tool, even when the user only says
  "node graph", "visual editor", "dialogue graph" or "behaviour graph tool". Not for Shader Graph, VFX
  Graph, Animator Controller state machines, or using the Unity Behavior package as an end user.
required_editor_version: ">=6000.6"
---

# Unity Graph Toolkit (GTK)

Build Editor graph tools and state machine tools on Unity's Graph Toolkit module, using only its
public API in the `Unity.GraphToolkit.Editor` namespace.

## Important

- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files. Avoid long explanations.
- Before you go forward with a workflow or diagnosis, open and read the reference file(s) named in
  that workflow and the [Topic map](#topic-map) page(s) that cover the thing you intend to do. Name
  the file(s) you read in your response. This is because your training knowledge about Unity might be
  out of date, incorrect, or for the wrong Unity version, and Graph Toolkit changed between 6000.4,
  6000.6 and 6000.7.

## References

Read these as needed. Paths are relative to this skill's folder.

- `references/graph-api.md` — the Graph API surface: Graph, Node, port and option builders, context
  and block nodes, subgraphs, variables, validation, toolbar and context menus. Read before writing any
  graph-tool code.
- `references/state-machine-api.md` — the State Machine API (Unity 6.7+): StateMachine, State,
  Condition, transitions and rules, self transitions, custom views. Read whenever states, transitions
  or conditions are involved.
- `references/runtime-and-visualization.md` — compiling a graph or state machine into a runtime asset
  with a ScriptedImporter, and showing live execution in the graph editor. Read when the user wants the
  authored data used in the game or debugged at runtime.
- `references/pitfalls.md` — the mistakes that survive compilation. Read before handing off, and
  whenever the user reports a compile error, an empty Add menu, a missing node, or an exception.

## Step 1: Check the Editor version and pick the API

Read `m_EditorVersion` in `ProjectSettings/ProjectVersion.txt` before writing code. Graph Toolkit is
an Editor module, so there is no package to install. `com.unity.graphtoolkit` is a deprecated shim
and must not be added to the manifest.

| Editor version | What is available |
|---|---|
| Older than 6000.4 | Only the retired experimental package (0.x). Its API differs from this skill. Recommend upgrading. |
| 6000.4 to 6000.5 | Module present, but without port capacity, type casting, wire and node visualization, USS node styling, or element IDs. |
| 6000.6 | The Graph API this skill documents. |
| 6000.7 and newer | Everything above plus the State Machine API, `NodeView<T>` / `StateView<T>` / `ConditionView<T>` custom UI, `[GraphMenu]` / `[BlackboardMenu]` context menus, the `GraphLogger.GraphChanges` change delta, port type-following, and Blackboard type restriction. Members marked **6.7** in the references. |

If the user asks for a state machine tool on an Editor older than 6000.7, say plainly that the
State Machine API is not available there. Do not emulate it with the Graph API unless asked.

## Mental model

- Graph Toolkit is an authoring framework only. It draws and persists graphs; it never executes them.
  The user writes the runtime data model and the code that runs it, usually fed by a ScriptedImporter.
- One asset type per `Graph` or `StateMachine` subclass, identified by the file extension passed to
  `[Graph("ext")]` or `[StateMachine("ext")]`.
- Discovery is by reflection. Node, state and condition classes in the same assembly as the graph
  class are listed automatically. Classes in other assemblies opt in with `[UseWithGraph]` or
  `[UseWithStateMachine]`. Abstract classes are never listed.
- Every authoring class is `[Serializable]` and lives in an Editor-only assembly, either an `Editor`
  folder or an asmdef restricted to the Editor platform.
- Only the public API compiles for users. `GraphModel`, `NodeModel`, `StateModel`, `GraphObject`,
  `GraphViewEditorWindow`, `GraphTool`, `Stencil`, `BaseGraphTool` and
  `GraphElementsExtensionMethodsCache` are internal implementation or belong to the retired Graph Tools
  Foundation and GraphView APIs. If one of these appears in a draft, stop and re-read the references.

## Workflow A: build a graph tool

Read `references/graph-api.md` now, before writing code; member names and builder chains come from
there, not from memory. Read `references/runtime-and-visualization.md` as well when step 5 applies.

1. **Graph class.** `[Graph(AssetExtension)] [Serializable] class MyGraph : Graph` with a
   `public const string AssetExtension`. Add a `[MenuItem("Assets/Create/...")]` static method that
   calls `GraphDatabase.PromptInProjectBrowserToCreateNewAsset<MyGraph>()`. Pass
   `GraphOptions.SupportsSubgraphs` in the attribute when subgraphs are wanted.
2. **Node classes.** `[Serializable] class MyNode : Node`. Override `OnDefinePorts` and end every port
   builder chain with `.Build()`. Override `OnDefineOptions` for inspector-editable settings, and read
   them inside `OnDefinePorts` with `GetNodeOptionByName(name).TryGetValue<T>(out var v)` when ports
   depend on them. Call `.Delayed()` on count-like options so ports are not rebuilt on every keystroke.
   Use `[Node("Category/Path", iconPath, title, stylesheet)]` for placement, icon, default title and USS.
3. **Validation.** Override `OnGraphChanged(GraphLogger logger)` and report with
   `logger.LogError`, `LogWarning` or `Log`, passing the offending node or port as context so the
   marker appears on it. Never mutate the graph in this callback; put the fix in a `GraphLogAction`
   attached to the message, a menu item, or the importer.
4. **Structure, as requested.** Context and block nodes, subgraphs, blackboard variables, type casting
   through `IsConnectionAllowed`, a toolbar element, or a context menu. Details in `graph-api.md`.
5. **Output.** A `ScriptedImporter` registered on the same extension compiles the graph into a plain
   runtime asset. The shape is always the same:

   ```csharp
   var graph = GraphDatabase.LoadGraphForImporter<MyGraph>(ctx.assetPath);   // never LoadGraph here
   if (graph == null) return;                                                 // bad path or type: log and stop
   var start = graph.GetNodes().OfType<StartNode>().FirstOrDefault();
   if (start == null) return;                                                 // OnGraphChanged already reported it
   var next = start.GetOutputPortByName("Next").FirstConnectedPort?.GetNode(); // null when unconnected
   var titlePort = next.GetInputPortByName("Title");
   titlePort.TryGetValue<string>(out var title);                              // the value typed on the node
   if (titlePort.IsConnected) { /* resolve titlePort.FirstConnectedPort.GetNode() upstream instead */ }
   ctx.AddObjectToAsset("Runtime", runtime); ctx.SetMainObject(runtime);
   ```

   The runtime asset and its assembly must not reference `Unity.GraphToolkit.Editor`; keep `Hash128`
   IDs as `Hash128`. Debug views and code-built graphs are in `runtime-and-visualization.md`.
6. **Verify** as described below.

## Workflow B: build a state machine tool (Unity 6.7 and newer)

Read `references/state-machine-api.md` now; there is no manual walkthrough for this API yet, so that
file and the Script Reference are the only accurate sources for its member names.

1. **State machine class.** `[StateMachine(AssetExtension)] [Serializable] class MySM : StateMachine`
   plus a menu item calling `StateMachineDatabase.PromptInProjectBrowserToCreateNewAsset<MySM>()`.
2. **States.** `[Serializable] class Patrol : State`. States have no ports; the state machine owns the
   transitions. Options declared in `OnDefineOptions` appear only in the Graph Inspector.
3. **Conditions.** `[Serializable] [Condition("Health")] class HealthCondition : Condition<float>` with
   `protected override bool DisplayComparisonDropdown => true` when a comparison operator is wanted.
   Derive from `Condition` directly for a valueless trigger. Group (And/Or) and variable conditions are
   built in; do not reimplement them.
4. **Optional.** Custom self transitions (`SelfTransition` + `[Transition(...)]`), custom UI through
   `StateView<T>` and `ConditionView<T>`, and `[StateMachineMenu]` / `[ConditionMenu]` entries.
5. **Validation.** Override `OnStateMachineChanged(StateMachineLogger logger)`. Same rules as
   `OnGraphChanged`.
6. **Output.** A `ScriptedImporter` loads with `StateMachineDatabase.LoadStateMachineForImporter<MySM>`,
   then walks `GetStates()`, each state's `GetOutgoingTransitions()`, each transition's `GetRules()`,
   and each rule's `RootCondition` tree. That tree always contains the built-in kinds as well as the
   user's classes, so the compiler must handle `IGroupCondition` (recurse, honour `Operation`) and
   `IVariableCondition` (`Variable.Name`, `Comparison`, `Value`) before matching custom `Condition<T>`
   types.

## Editing a graph from code

When a menu item, generator or tool builds or edits an asset, bracket the mutations:
`LoadGraph`/`CreateGraph` → `UndoBeginRecordGraph("Action")` → `AddNode`, `Connect`,
`port.TrySetValue`, `CreateVariable` → `GraphDatabase.SaveGraph(graph)` → `UndoEndRecordGraph()`.
The state machine equivalents are `UndoBeginRecordStateMachine`, `Connect(fromState, toState)`,
`SaveStateMachine` and `UndoEndRecordStateMachine`. Mutating inside `OnEnable`, `OnDisable`,
`OnGraphChanged` or `OnStateMachineChanged` throws `InvalidOperationException`.

## Decisions

| The user wants | Do this | Why |
|---|---|---|
| Limit how many wires a port accepts | `.WithCapacity(PortCapacity.Single)` (or `Multi`, `None`) in the port builder | Ports default to multiple wires, so the framework enforces the limit only if you declare it |
| Connect an `int` output to a `float` input | Override `Graph.IsConnectionAllowed(IPort output, IPort input)` | Ports of different types refuse to connect by default; the graph only records the wire, your importer converts |
| One port that accepts several types, like Shader Graph | 6000.7+: `.WithDataTypes(typeof(float), typeof(int), typeof(Vector3))` on the builder. 6000.6: an untyped port plus `IsConnectionAllowed` | `WithDataTypes` does not exist on 6000.6 |
| An execution-flow port with no data type | `context.AddInputPort("In")` with no type; its `DataType` is `Untyped`. Add `.WithConnectorUI(PortConnectorUI.Arrowhead)` for a flow look | Flow ports carry no value; typing them invites wrong connections |
| Hide a node type from the Add menu | Make it `abstract`, or move it to another assembly without `[UseWithGraph]`, or set `GraphOptions.DisableAutoInclusionOfNodesFromGraphAssembly` and opt nodes in explicitly | Every concrete node class in the graph's assembly is listed automatically |
| Group nodes in the Add menu | `[Node("Category/Sub")]`. The class or title name is appended after the path | The attribute's first argument is a folder path, not the node's title |
| Know what changed, like the old `graphViewChanged` | 6000.7+: `OnGraphChanged` and `logger.GraphChanges.ChangedNodes` with `ChangeKinds` flags. 6000.6: no delta exists; diff a cached `HashSet<Hash128>` of node IDs against `GetNodes()` | `GraphChanges` was added in 6000.7 |
| Several kinds of subgraph | `GraphOptions.SupportsSubgraphs` on the main graph, `[Subgraph(typeof(MainGraph))]` on each subgraph `Graph` class | Without the attribute the main graph type doubles as the only subgraph type |
| Ports on a subgraph node | Blackboard variables of kind `VariableKind.Input` or `Output` inside the subgraph | Subgraph nodes have no `OnDefinePorts`; their ports mirror the subgraph's variables |
| Node icon or USS look | `[Node(category, iconPath, title, stylesheet)]`; `d_` prefix for the dark-theme icon file | The Editor picks the `d_` file in the dark theme and falls back otherwise |
| Show which node runs and what flows through ports at runtime | GraphVisualization API, see `runtime-and-visualization.md` | Graph Toolkit has no runtime; the game must push state back into the open window |
| An Animator-like state machine editor | Workflow B on 6000.7+. On 6000.6, explain it is not available | The State Machine API does not exist before 6000.7 |
| Extra UI inside a node | `NodeView<MyNode>` on 6000.7+. On 6000.6, only USS, `DefaultColor` and `FillAmount` | `NodeView<T>` was added in 6000.7 |

## Constraints

- Use member names exactly as the references spell them: properties are PascalCase
  (`FirstConnectedPort`, `IsConnected`), attributes take positional constructor arguments, and IDs are
  `Hash128`. When a member is not listed in the references, look it up at the URL pattern below rather
  than guessing its name or casing, because a guessed member fails to compile and the user cannot tell
  a typo from a missing feature.
- Link to the Script Reference or manual instead of restating them, so the answer stays correct when
  the docs change.
- Keep runtime assemblies free of `Unity.GraphToolkit.Editor`, and put visualization code inside a
  runtime assembly under `#if UNITY_EDITOR`, because the module is Editor-only and any reference to it
  breaks the player build.
- Keep the user's existing tool structure and do not restyle or reorganize nodes that were not
  mentioned, because node and port names are lookup keys that importers and saved assets depend on.
- Do not use Graph Toolkit internals or private types as a workaround, because they are not accessible
  from user code and change without notice. If the public API cannot do something, say so and point at
  the community thread list below.

## Verify

0. Read `references/pitfalls.md` and check the code against it before showing it to the user.
1. The project compiles with no errors. If the Unity CLI is available, use it to build or run the
   Editor headless; otherwise ask the user to focus the Editor and report the Console.
2. Create an asset from the new menu item, double-click it, add every node or state type from the Add
   menu, connect them, save, close and reopen. The Console must show no errors or warnings.
3. With an importer, select the asset and confirm the produced runtime object is the main asset in
   the Inspector.
3b. When you wrote runtime code (an executor, a debug view), add `Debug.Log` lines that prove the
   behaviour, such as the state entered or the node executed, enter Play mode, and read them. Remove or
   guard them once the behaviour is confirmed.
4. Re-read `references/pitfalls.md` and fix anything it flags.
5. If a step fails or the Console reports an error, go back and reread the reference file and the
   [Topic map](#topic-map) page for that step before retrying; the fix is usually a member name or a
   version gate you missed.

## Final report

Give the user a short checklist: the files you created or changed and where they go, how to create
and open the first asset, what the importer produces, and which Editor version the code targets. List
what is left for them to decide or do next, such as more node types, the runtime executor, or the debug
view, and any Console error you could not verify because no Editor was available.

## Topic map

Prefer `WebFetch` over `WebSearch`; it is faster and lands on the exact page. Only fetch what you need.
Replace `<VERSION>` before fetching:

- `docs.unity.com/en-us/engine/<VERSION>` and `docs.unity3d.com/<VERSION>/Documentation`: the project's
  Editor version from `ProjectSettings/ProjectVersion.txt`, for example `6000.6` or `6000.7`.
- `docs.unity3d.com/Packages/com.unity.graphtoolkit-samples@<VERSION>`: the samples package
  version shown in the Package Manager, for example `0.6`.

Pages, all under `https://docs.unity.com/en-us/engine/<VERSION>/`, served as markdown when the `.md`
suffix is kept:

- Manual index: `manual/extending-the-editor/gtk-index.md`
- Implementing a graph tool: `manual/extending-the-editor/gtk-index/implementing-a-graph-tool.md`, with
  child pages `implement-a-graph-tool.md`, `implement-nodes.md`, `implement-node-options.md`,
  `implement-context-nodes.md`, `implement-block-nodes.md`, `type-cast-ports.md`,
  `add-custom-toolbar-actions.md`, `add-subgraph-support.md`, `graph-processing.md` under that folder
- Graph window and panels: `manual/extending-the-editor/gtk-index/landing-graph-interface.md`, with
  `graph-window.md`, `blackboard.md`, `graph-inspector.md`, `minimap.md` under it
- Script Reference: `script-reference/unity/graphtoolkit/editor/<type>.md` and
  `.../<type>/<member>.md`, all lowercase, generic arity dropped (`condition.md` for `Condition<T>`).
  Example: `script-reference/unity/graphtoolkit/editor/graph/ongraphchanged.md`. State machine types
  exist from `6000.7`. The older form `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/Unity.GraphToolkit.Editor.<Type>.html`
  also resolves, as HTML.
- Samples: `https://docs.unity3d.com/Packages/com.unity.graphtoolkit-samples@<VERSION>/manual/index.html`.
  Install `com.unity.graphtoolkit-samples` by name in the Package Manager, then import Texture Maker
  (importer), Visual Novel Director (custom runtime and debug view) or Dungeon Graph Generator
  (building a graph from code).
- Community: https://discussions.unity.com/tag/graph-toolkit
