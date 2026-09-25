## Contents
- [RuleSet methods](#ruleset-methods) — `GetRule`, `GetRuleIndex`, `RemoveRuleAt`, `RemoveRule`, `InsertRule`, `AppendRule`, `IsValid`
- [RuleSet properties](#ruleset-properties) — `RulesCount`
- [RuleSet utility functions](#ruleset-utility-functions) — `RunRuleSet`

---

## RuleSet methods

The RuleSet API reference contains the following methods.

### `GetRule`

This method retrieves the `Rule` at the specified index.

```csharp
public Rule GetRule(int i)
```

`GetRule` accepts the following parameter.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `i` | int | required | Zero-based index of the rule to retrieve. |

This method returns the `Rule` at the given index.

### `GetRuleIndex`

This method returns the index of the given `Rule` within the RuleSet. Returns `-1` and logs an error if the RuleSet is currently running.

```csharp
public int GetRuleIndex(Rule rule)
```

`GetRuleIndex` accepts the following parameter.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `rule` | Rule | required | The `Rule` whose index to find. |

This method returns an `int` index, or `-1` if the RuleSet is running or the rule is not found.

### `RemoveRuleAt`

This method removes the rule at the specified index. Has no effect and logs an error if the RuleSet is currently running.

```csharp
public void RemoveRuleAt(int index)
```

`RemoveRuleAt` accepts the following parameter.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `index` | int | required | Zero-based index of the rule to remove. |

### `RemoveRule`

This method removes a specific `Rule` instance from the RuleSet. Has no effect and logs an error if the RuleSet is currently running.

```csharp
public void RemoveRule(Rule rule)
```

`RemoveRule` accepts the following parameter.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `rule` | Rule | required | The `Rule` instance to remove. |

### `InsertRule`

This method inserts a `Rule` at the specified index, shifting subsequent rules down. Has no effect and logs an error if the RuleSet is currently running.

```csharp
public void InsertRule(int index, Rule rule, bool notify)
```

`InsertRule` accepts the following parameters.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `index` | int | required | Zero-based position at which to insert the rule. |
| `rule` | Rule | required | The `Rule` instance to insert. |
| `notify` | bool | required | When `true`, notifies the UI that the RuleSet has changed. |

### `AppendRule`

This method adds a `Rule` to the end of the RuleSet. Has no effect and logs an error if the RuleSet is currently running.

```csharp
public void AppendRule(Rule rule)
```

`AppendRule` accepts the following parameter.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `rule` | Rule | required | The `Rule` instance to append. |

### `IsValid`

This method validates that every enabled action in the RuleSet has valid input. It skips disabled rules.

```csharp
public bool IsValid()
```

This method accepts no parameters. It returns `true` if all enabled actions pass validation, or `false` if any action reports an error.

## RuleSet properties

The RuleSet API reference contains the following properties.

### `RulesCount`

```csharp
public int RulesCount { get; }
```

The number of `Rule` instances currently in the RuleSet.

## RuleSet utility functions

The following functions are from `Unity.Pixyz.Plugin4Unity.Editor.AI.ATTAssistantUtilities`.

### `RunRuleSet`

This function executes all rules in a RuleSet asset against the current scene selection, or the entire scene if nothing is selected.

```csharp
public static void RunRuleSet(string rulesetPath)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `rulesetPath` | string | required | Project-relative path to the RuleSet asset to run. |
