# Pitfalls that survive compilation, and the ones that do not

Check every row before handing off. File paths in this document resolve to this file's folder
(`references/`); `SKILL.md` is one level up.

## Contents

- Wrong API generation
- Discovery and menus
- Ports and options
- Lifecycle and mutation
- Serialization
- Custom UI (6.7)
- Visualization
- Importers

## Wrong API generation

| Symptom | Cause | Fix |
|---|---|---|
| Code uses `GraphModel`, `NodeModel`, `StateModel`, `GraphObject`, `GraphTool`, `GraphViewEditorWindow`, `Stencil`, `BlackboardGraphModel`, `GraphElementsExtensionMethodsCache`, `ModelView`, or `using Unity.GraphToolkit.ItemLibrary.Editor` | Retired Graph Tools Foundation API, internal implementation, or the 2025 experimental package; none is accessible from user code | Rewrite against `Graph`, `Node`, `State`, `GraphDatabase`, `StateMachineDatabase` and the builders in `graph-api.md` |
| `using UnityEditor.Experimental.GraphView;`, `GraphView`, `Port.Create`, `Edge`, `graphViewChanged`, `NodeCreationRequest` | The older GraphView API; Graph Toolkit is not a drop-in replacement and has no hand-written view code | Model the tool as `Graph` and `Node` classes and let the window be provided; change tracking moves to `OnGraphChanged` |
| The answer tells the user to install `com.unity.graphtoolkit` | Confusion with the experimental package; the module is built in from 6000.4 and that package name is a deprecated shim | Remove the install step; check `ProjectVersion.txt` instead |
| `StateMachine`, `State`, `Condition<T>` fail to resolve on a 6000.6 project | The State Machine API is 6000.7 and newer | Tell the user; do not fake the types |

## Discovery and menus

| Symptom | Cause | Fix |
|---|---|---|
| A node or state never appears in the Add menu | The class is `abstract`, or an open generic, or in another assembly without `[UseWithGraph]` / `[UseWithStateMachine]`, or the graph sets `DisableAutoInclusionOf...FromGraphAssembly`, or a block node lacks `[UseWithContext]` | Make the class concrete and closed, add the matching `UseWith...` attribute, or drop the flag |
| A node appears in an odd folder such as `Math/Add/Add` | `[Node("Math/Add")]` treats the whole first argument as a category path; the title is appended after it | `[Node("Math", null, "Add")]`, or set `Title` in `OnEnable` |
| A base node type the user never meant to expose is listed | It is concrete | Mark shared base classes `abstract` |

## Ports and options

| Symptom | Cause | Fix |
|---|---|---|
| A port is defined but nothing shows on the node | The builder chain lacks `.Build()` | End every chain with `.Build()` |
| `GetInputPortByName` or `GetNodeOptionByName` returns the wrong element | Two ports, or two options, share a name; names are lookup keys | Give every port and option a unique name |
| Ports rebuild on every keystroke of an integer option; typing "12" flashes 1 then 12 inputs | The option commits on each character | `.Delayed()` on the option |
| `GetNodeOptionByName(...)` returns null inside `OnDefinePorts` | The name differs from the one in `OnDefineOptions`, or the option is defined after it is read | Share a `const` for the name and define options before ports read them |
| `TryGetValue` on a connected input returns the stale embedded value | `TryGetValue` reads the node's own field, not the wire | When `IsConnected`, follow `FirstConnectedPort.GetNode()` upstream |
| Designers connect several wires to a port meant for one | Ports accept multiple wires by default | `.WithCapacity(PortCapacity.Single)` |
| `AsVertical()` has no effect | The node is a `BlockNode`; block ports are always horizontal | Keep block ports horizontal, or use a regular node |

## Lifecycle and mutation

| Symptom | Cause | Fix |
|---|---|---|
| `InvalidOperationException: Cannot change the graph in OnEnable, OnDisable and OnGraphChanged` | Graph mutation inside a locked callback | Move the mutation to a menu item, toolbar button, `GraphLogAction` or importer, wrapped in `UndoBeginRecordGraph` / `UndoEndRecordGraph` |
| Programmatic edits vanish on reopen | `GraphDatabase.SaveGraph` was not called, or `LoadGraph` was used inside an importer | Call `SaveGraph` after editing; use `LoadGraphForImporter` in importers |
| Undo history is polluted or Ctrl+Z does nothing after a generator ran | Missing `UndoBeginRecordGraph` / `UndoEndRecordGraph` pair | Bracket the whole batch in one pair |
| `OnEnable` runs several times | Expected: it fires on load, reopen and domain reload | Keep `OnEnable` idempotent |

## Serialization

| Symptom | Cause | Fix |
|---|---|---|
| Nodes disappear or reset after reopening the asset or a domain reload | A node, state, condition or self transition class lacks `[Serializable]`, or a graph field is not serializable | Add `[Serializable]` to every authoring class; use serializable field types |
| Compile error on a class declaration decorated with `[SerializeReference]` | `SerializeReference` is a field attribute | Classes take `[Serializable]`; use `[SerializeReference]` only on polymorphic fields in the runtime data model |
| `OnEnable` override on a `Condition` does not compile | `Condition` has no lifecycle callbacks; only `Graph`, `StateMachine`, `Node`, `State` and `SelfTransition` expose `OnEnable` / `OnDisable` | Remove the override; initialise in the condition's field initialisers or view |
| A field on a state machine that must not persist is written to the asset | Serializable fields on the class are saved | `[NonSerialized]` on the field, or `[field: NonSerialized]` on the property |
| Build errors about `Unity.GraphToolkit.Editor` in the player | Authoring types referenced from a runtime assembly | Move them to an Editor assembly; guard visualization code with `#if UNITY_EDITOR` |

## Custom UI (6.7)

| Symptom | Cause | Fix |
|---|---|---|
| Duplicated labels or buttons accumulate on a node or state | UI allocated in `OnViewAttached`, which fires on every re-attach | Allocate in `OnViewBuilt`, cache, and re-add in `OnCullingChanged(false)` |
| The custom element disappears after zooming far out and back | Culling clears `Root` | Re-add the cached element in `OnCullingChanged(false)` |
| Built-in condition row fields missing or misplaced | The `ConditionView<T>` removed or reparented them | Only append to `Root`; hide built-ins with `DisplayValueField` / `DisplayTitleLabel` |

## Visualization

| Symptom | Cause | Fix |
|---|---|---|
| `Motion.Play` and previews do nothing | The context was created with the runtime asset's own ID instead of the authored `Graph.ID`; or the graph window is not open (`IsGraphLoaded` is false); or the `*Enabled` toggle is off; or the context was disposed | Create the context with `Graph.ID`, check `IsValid` and `IsGraphLoaded`, and keep the toggles on while running |
| Previews from the previous step stay on screen | Previews persist until cleared | `ClearPreview()` when leaving a node, or `ClearAllVisualization()` at run end |

## Importers

| Symptom | Cause | Fix |
|---|---|---|
| The `.ext` asset shows as a generic Graph Toolkit asset instead of the runtime type | `ctx.SetMainObject(runtime)` missing | Call `SetMainObject` after `AddObjectToAsset` |
| Importer changes have no effect on existing assets | Import results are cached per importer version | Bump the `ScriptedImporter` version number, or reimport |
| Importer throws on an empty graph | No entry node to start from | Return early when the entry node is missing; `OnGraphChanged` already reports it |
