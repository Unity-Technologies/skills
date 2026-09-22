# Pitfalls that survive compilation, and the ones that do not

Check every item before handing off. Each entry: symptom, cause, fix.

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

- **Symptom:** code uses `GraphModel`, `NodeModel`, `StateModel`, `GraphObject`, `GraphTool`,
  `GraphViewEditorWindow`, `Stencil`, `BlackboardGraphModel`, `GraphElementsExtensionMethodsCache`,
  `ModelView`, or `using Unity.GraphToolkit.ItemLibrary.Editor`.
  **Cause:** these come from the retired Graph Tools Foundation API, the internal implementation, or
  the 2025 experimental package. They are not accessible from user code.
  **Fix:** rewrite against `Graph`, `Node`, `State`, `GraphDatabase`, `StateMachineDatabase` and the
  builders in `graph-api.md`.
- **Symptom:** `using UnityEditor.Experimental.GraphView;`, `GraphView`, `Port.Create`, `Edge`,
  `graphViewChanged`, `NodeCreationRequest`.
  **Cause:** the older GraphView API. Graph Toolkit is not a drop-in replacement; there is no manual
  view code at all.
  **Fix:** model the tool as `Graph` + `Node` classes and let the window be provided. Change tracking
  moves to `OnGraphChanged` and `GraphChanges`.
- **Symptom:** the answer tells the user to install `com.unity.graphtoolkit`.
  **Cause:** confusion with the experimental package. The module is built in from 6000.4 and the
  package name is now a deprecated shim.
  **Fix:** remove the package step; check `ProjectVersion.txt` instead.
- **Symptom:** `StateMachine`, `State`, `Condition<T>` on a 6000.6 project fail to resolve.
  **Cause:** State Machine API is 6000.7 and newer.
  **Fix:** tell the user; do not fake the types.

## Discovery and menus

- **Symptom:** a node or state never appears in the Add menu.
  **Causes:** class is `abstract`; it is in another assembly without `[UseWithGraph]` /
  `[UseWithStateMachine]`; the graph sets `DisableAutoInclusionOf...FromGraphAssembly`; the class is
  an open generic; a block node lacks `[UseWithContext]`.
- **Symptom:** a node appears in an odd folder.
  **Cause:** `[Node("Math/Add")]` puts the node under `Math/Add/<title>`; the last segment is a
  category, not the title.
  **Fix:** `[Node("Math", null, "Add")]` or set `Title` in `OnEnable`.
- **Symptom:** a base node type the user never meant to expose is listed.
  **Cause:** it is concrete. Mark shared base classes `abstract`.

## Ports and options

- **Symptom:** a port is defined but nothing shows on the node.
  **Cause:** the builder chain lacks `.Build()`.
- **Symptom:** `GetInputPortByName` or `GetNodeOptionByName` returns the wrong element.
  **Cause:** two ports, or two options, share a name. Names are lookup keys; keep them unique.
- **Symptom:** ports rebuild on every keystroke of an integer option; typing "12" flashes 1 then 12
  inputs.
  **Fix:** `.Delayed()` on the option.
- **Symptom:** `GetNodeOptionByName(...)` returns null inside `OnDefinePorts`.
  **Cause:** the name differs from `OnDefineOptions` (use a `const`), or the option is defined after
  it is read.
- **Symptom:** `TryGetValue` on a connected input returns the stale embedded value.
  **Cause:** `TryGetValue` reads the node's own field. Follow `FirstConnectedPort.GetNode()` upstream
  when `IsConnected`.
- **Symptom:** designers connect several wires to a port meant for one.
  **Fix:** `.WithCapacity(PortCapacity.Single)`.
- **Symptom:** `AsVertical()` has no effect.
  **Cause:** the node is a `BlockNode`; block ports are always horizontal.

## Lifecycle and mutation

- **Symptom:** `InvalidOperationException: Cannot change the graph in OnEnable, OnDisable and
  OnGraphChanged`.
  **Fix:** move the mutation to a menu item, toolbar button, `GraphLogAction`, or importer; wrap it in
  `UndoBeginRecordGraph` / `UndoEndRecordGraph`.
- **Symptom:** programmatic edits vanish on reopen.
  **Cause:** `GraphDatabase.SaveGraph` was not called, or `LoadGraph` was used inside an importer.
- **Symptom:** undo history is polluted or Ctrl+Z does nothing after a generator ran.
  **Cause:** missing `UndoBeginRecordGraph` / `UndoEndRecordGraph` pair.
- **Symptom:** `OnEnable` runs several times.
  **Cause:** expected; it fires on load, reopen and domain reload. Keep it idempotent.

## Serialization

- **Symptom:** nodes disappear or reset after reopening the asset or after a domain reload.
  **Cause:** a node, state, condition or self transition class lacks `[Serializable]`, or a graph
  class field is not serializable.
- **Symptom:** compile error on a class declaration decorated with `[SerializeReference]`.
  **Cause:** `SerializeReference` is a field attribute. Classes take `[Serializable]`; use
  `[SerializeReference]` only on polymorphic fields in the runtime data model.
- **Symptom:** `OnEnable` override on a `Condition` does not compile.
  **Cause:** `Condition` has no lifecycle callbacks; only `Graph`, `StateMachine`, `Node`, `State`
  and `SelfTransition` expose `OnEnable` / `OnDisable`.
- **Symptom:** a field on a state machine that must not persist is written to the asset.
  **Fix:** `[NonSerialized]` or `[field: NonSerialized]` on the property.
- **Symptom:** build errors about `Unity.GraphToolkit.Editor` in the player.
  **Cause:** authoring types referenced from a runtime assembly.
  **Fix:** move them to an Editor assembly; guard visualization code with `#if UNITY_EDITOR`.

## Custom UI (6.7)

- **Symptom:** duplicated labels or buttons accumulate on a node or state.
  **Cause:** UI allocated in `OnViewAttached`, which fires on every re-attach.
  **Fix:** allocate in `OnViewBuilt`, cache, and re-add in `OnCullingChanged(false)`.
- **Symptom:** the custom element disappears after zooming far out and back.
  **Cause:** culling clears `Root`; re-add the cached element in `OnCullingChanged(false)`.
- **Symptom:** built-in condition row fields missing or misplaced.
  **Cause:** the `ConditionView<T>` removed or reparented them.
  **Fix:** only append to `Root`; hide built-ins with `DisplayValueField` / `DisplayTitleLabel`.

## Visualization

- **Symptom:** `Motion.Play` and previews do nothing.
  **Causes:** the context was created with the runtime asset's own ID instead of the authored
  `Graph.ID`; the graph window is not open (`IsGraphLoaded` is false); the `*Enabled` toggle is off;
  the context was disposed.
- **Symptom:** previews from the previous step stay on screen.
  **Fix:** `ClearPreview()` when leaving a node, or `ClearAllVisualization()` at run end.

## Importers

- **Symptom:** the `.ext` asset shows as a generic Graph Toolkit asset instead of the runtime type.
  **Cause:** `ctx.SetMainObject(runtime)` missing.
- **Symptom:** importer changes have no effect on existing assets.
  **Fix:** bump the `ScriptedImporter` version number, or reimport.
- **Symptom:** importer throws on an empty graph.
  **Fix:** return early when the entry node is missing; `OnGraphChanged` already reports it.
