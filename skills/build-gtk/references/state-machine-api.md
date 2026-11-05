# State Machine API reference (Unity 6.7 and newer)

All types live in `Unity.GraphToolkit.Editor`. None of this exists on Unity 6000.6 or older. There is
no manual walkthrough yet: this file covers the patterns, and the Script Reference has the members.
Page names such as `statemachine.md` are under
`https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unity/graphtoolkit/editor/`, with
`<VERSION>` at least `6000.7`.

## Contents

1. Concepts
2. StateMachine class and asset lifecycle
3. States
4. Transitions and rules
5. Conditions
6. Custom self transitions
7. Variables
8. Validation with OnStateMachineChanged
9. Context menus
10. Custom UI for states and conditions
11. Subgraph states
12. Reading a state machine for a runtime

## 1. Concepts

- A `StateMachine` is a graph whose nodes are `State`s and whose wires are transitions. The state
  machine owns transitions; states have no ports to define.
- A transition connects a `FromState` to a `ToState`. A **self transition** has the same state on both
  ends and is anchored on it.
- A transition carries one or more **rules** (`ITransitionRule`). The transition is taken when the
  conditions of any enabled rule hold. Each rule has a `RootCondition`, an `IGroupCondition` combining
  child conditions with And/Or.
- Conditions are either built in (group, variable comparison) or user types derived from
  `Condition<T>` / `Condition`.
- Everything about which state is active, when transitions fire, and what a condition means at
  runtime is the user's code. Graph Toolkit stores the authoring data only.

## 2. StateMachine class and asset lifecycle

```csharp
[StateMachine(AssetExtension)]               // optional second arg: StateMachineOptions flags
[Serializable]
class EnemyBrain : StateMachine
{
    public const string AssetExtension = "enemybrain";

    [MenuItem("Assets/Create/AI/Enemy Brain")]
    static void Create() => StateMachineDatabase.PromptInProjectBrowserToCreateNewAsset<EnemyBrain>();
}
```

`StateMachineOptions` flags: `SupportsSubgraphs`,
`DisableAutoInclusionOfStatesFromStateMachineAssembly`. `Default` and `None` are 0. The flag applies
to states, conditions and self transitions alike.

Members of `StateMachine` and `StateMachineDatabase` are in the Script Reference (`statemachine.md`,
`statemachinedatabase.md`). Behavior the reference does not spell out:

- `Connect(from, to)` creates a new transition with one empty rule on every call, so calling it twice
  on the same pair gives two transitions. Add rules to an existing transition instead of reconnecting.
  `Connect(s, s)` adds a rule to the state's self transition when it already has one.
- Use `LoadStateMachineForImporter<T>` in importers, `LoadStateMachine<T>` everywhere else, and call
  `SaveStateMachine` after edits from code.
- Bracket edits in `UndoBeginRecordStateMachine` / `UndoEndRecordStateMachine`, passing the
  conditions you edit to the first call.

Mutating in `OnEnable`, `OnDisable` or `OnStateMachineChanged` throws `InvalidOperationException`.

## 3. States

```csharp
[Serializable]
[State("Movement", "Icons/Patrol.png", "Patrol")]        // args after the first are optional
class PatrolState : State
{
    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<float>("Speed").WithDefaultValue(2f).WithTooltip("Units per second");
        context.AddOption<string>("Notes").AsTextArea();
    }
}
```

`StateAttribute(categoryPath[, iconPath[, title[, stylesheet]]])` mirrors `NodeAttribute`.

State options appear only in the Graph Inspector, never on the state itself.
`State.IOptionDefinitionContext`: `AddOption<T>(name)`, `AddOption(name, Type)`. Builder
`IStateOptionBuilder`: `WithDisplayName`, `WithTooltip`, `WithDefaultValue`, `Delayed`, `AsTextArea`,
`Build()`. Read back with `GetOptionByName(name).TryGetValue<T>(out v)`; `IStateOption` also has
`DataType`, `Name`, `DisplayName`, `Tooltip`, `TrySetValue<T>`.

`State` / `IState` members: `StateMachine`, `Hash128 ID`, `Title`, `Subtitle`, `Tooltip`,
`Color DefaultColor`, `float FillAmount`, `Vector2 Position`, `bool IsConnected`,
`GetIncomingTransitions()`, `GetOutgoingTransitions()`, `Options`, `OptionCount`, `GetOption(int)`,
`GetOptionByName(string)`, `RemoveFromStateMachine()`. `OnEnable()` and `OnDisable()` are on the
`State` class only, not on `IState`.

Discovery: states in the state machine's assembly are listed automatically; elsewhere use
`[UseWithStateMachine(typeof(EnemyBrain))]`. `[UseWithGraph]` is ignored by state machines.

## 4. Transitions and rules

Users create transitions by dragging from a state's edge (Shift + left mouse also starts one). In
code use `stateMachine.Connect(from, to)`.

Members are in `itransition.md`, `itransitionrule.md` and `igroupcondition.md`. A transition always
keeps at least one rule; removing the last one is refused. Each rule's `RootCondition` is an
`IGroupCondition` whose `Operation` is `GroupConditionOperation.And` or `GroupConditionOperation.Or`;
`Get()` enumerates its children, `Count` and `Get(int)` index them. Regular transitions are styled through
`ITransition` properties, not subclassed.

## 5. Conditions

```csharp
[Serializable]
[Condition("Distance to player")]                 // title in the Add menu of a rule
class DistanceCondition : Condition<float>
{
    public override string Tooltip => "Compared against the distance to the player.";
    protected override bool DisplayComparisonDropdown => true;
    protected override IReadOnlyList<ConditionComparison> SupportedComparisons =>
        new[] { ConditionComparison.Less, ConditionComparison.Greater };
}

[Serializable]
[Condition("Took damage")]
class TookDamageCondition : Condition { }         // valueless trigger, label-only row
```

- Members are in `condition.md` (both `Condition` and `Condition<T>`) and `conditioncomparison.md`.
  `DisplayComparisonDropdown` defaults to false; override `SupportedComparisons` to limit the operators.
- Built-in conditions, created in code with `Condition.CreateGroupCondition()` and
  `Condition.CreateVariableCondition(IVariable variable, object defaultValue, ConditionComparison comparison = ConditionComparison.Equal)`.
  Read them through `IGroupCondition` and `IVariableCondition` (`Variable`, `Comparison`, `Value`).
- Without a `[Condition]` attribute the type name is used as the title. Abstract and open generic
  condition classes are not listed.
- Same discovery rules as states: same assembly, or `[UseWithStateMachine(...)]`.

`Condition` has no lifecycle callbacks, so an `OnEnable` override on a condition does not compile.
Only `StateMachine`, `State` and `SelfTransition` expose `OnEnable` / `OnDisable` here.

Editing a condition value from code: `UndoBeginRecordStateMachine("Set distance", condition)`,
assign `condition.Value`, `UndoEndRecordStateMachine()`, then `SaveStateMachine`.

## 6. Custom self transitions

```csharp
[Serializable]
[UseWithStateMachine(typeof(EnemyBrain))]
[Transition("", "Icons/Loop.png", "Repeat")]      // (categoryPath[, iconPath[, title]])
class RepeatTransition : SelfTransition
{
    public override void OnEnable()
    {
        base.OnEnable();
        LineColor = Color.cyan;
        Tooltip = "Loops the state while its conditions hold.";
    }
}
```

Custom self transitions appear in a state's Create Transition menu. `SelfTransition` exposes the
`ITransition` members plus `Tooltip`, `OnEnable`, `OnDisable`. Regular state-to-state transitions are
not subclassed; style them through `ITransition` properties.

## 7. Variables

`stateMachine.CreateVariable<int>("Health", 100)` declares a Blackboard variable that the built-in
variable condition can compare against. `IVariable.StateMachine` gives the owner. Variable kinds and
members are the same as in the Graph API.

## 8. Validation with OnStateMachineChanged

```csharp
public override void OnStateMachineChanged(StateMachineLogger logger)
{
    foreach (var state in GetStates())
        if (!state.GetOutgoingTransitions().Any() && state is not EndState)
            logger.LogWarning("Dead end: add a transition out of this state.", state);

    foreach (var change in logger.StateMachineChanges.ChangedTransitions)
        if ((change.ChangeKinds & ChangeKind.Added) != 0) { /* new transition */ }
}
```

`StateMachineLogger` and `StateMachineChanges` work like `GraphLogger` and `GraphChanges`
(`statemachinelogger.md`, `statemachinechanges.md`): pass a state or transition as the context to put
the marker on it, and attach a `StateMachineLogAction` for a fix button.

## 9. Context menus

```csharp
[StateMachineMenu(typeof(EnemyBrain))]
static void AddEntries(StateMachineMenuContext context)
{
    switch (context.ClickedObject)
    {
        case IState state:           context.AppendAction("Log state", () => Debug.Log(state.Title)); break;
        case ITransition transition: context.AppendAction("Log rules", () => Debug.Log(transition.RuleCount)); break;
        default:                     context.AppendAction("Canvas action", () => { }); break;
    }
}

[ConditionMenu(typeof(EnemyBrain))]
static void AddConditionEntries(ConditionMenuContext context)   // context.StateMachine, .Transition, .Rule
{
}
```

## 10. Custom UI for states and conditions

- `class PatrolView : StateView<PatrolState>`: `State`, `View.Root`, `OnViewBuilt()`,
  `OnViewAttached()`, `OnViewDetached()`, `OnViewLODChanged(float)`, `OnCullingChanged(bool)`.
  Allocate elements in `OnViewBuilt` and re-add them in `OnCullingChanged(false)`; never allocate in
  `OnViewAttached`.
- `class DistanceView : ConditionView<DistanceCondition>` (no LOD or culling callbacks): `Condition`,
  `View.Root`, `OnViewBuilt()`, `OnViewAttached()`, `OnViewDetached()`, `OnConditionChanged()`, and
  the toggles `protected virtual bool DisplayValueField`, `DisplayTitleLabel`. Add to `Root`; do not
  remove or reparent the built-in row elements. View instances are never serialized.
- Exceptions thrown by a view are logged and the built-in UI still renders.

## 11. Subgraph states

With `StateMachineOptions.SupportsSubgraphs`, a state can hold a nested state machine.
`ISubgraphState : IState` marks such states; `StateMachineChanges.ChangedSubgraphStates` reports them.

## 12. Reading a state machine for a runtime

```csharp
[ScriptedImporter(1, EnemyBrain.AssetExtension)]
class EnemyBrainImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var sm = StateMachineDatabase.LoadStateMachineForImporter<EnemyBrain>(ctx.assetPath);
        if (sm == null) { Debug.LogError($"Failed to load {ctx.assetPath}"); return; }

        var runtime = ScriptableObject.CreateInstance<RuntimeBrain>();
        foreach (var state in sm.GetStates())
        {
            var rs = new RuntimeState { Id = state.ID, Name = state.Title };
            state.GetOptionByName("Speed")?.TryGetValue<float>(out rs.Speed);
            foreach (var transition in state.GetOutgoingTransitions())
                foreach (var rule in transition.GetRules())
                    if (rule.Enabled)
                        rs.Transitions.Add(new RuntimeTransition
                        {
                            Target = transition.ToState.ID,
                            Predicate = Compile(rule.RootCondition)
                        });
            runtime.States.Add(rs);
        }
        ctx.AddObjectToAsset("Runtime", runtime);
        ctx.SetMainObject(runtime);
    }

    static RuntimePredicate Compile(ICondition condition) => condition switch
    {
        IGroupCondition g      => RuntimePredicate.Group(g.Operation, g.Get().Select(Compile)),
        IVariableCondition v   => RuntimePredicate.Variable(v.Variable.Name, v.Comparison, v.Value),
        DistanceCondition d    => RuntimePredicate.Distance(d.Comparison, d.Value),
        TookDamageCondition    => RuntimePredicate.Trigger("TookDamage"),
        _                      => RuntimePredicate.Never
    };
}
```

Identify the entry state by a dedicated `State` subclass or a state machine option; the API has no
built-in "default state". Keep `Hash128` IDs in the runtime asset if the GraphVisualization API will
highlight states later.
