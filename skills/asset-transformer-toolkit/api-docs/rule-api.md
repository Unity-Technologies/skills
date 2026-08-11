## Contents
- [Rule constructors](#rule-constructors) — `Rule()`
- [Rule methods](#rule-methods) — `GetBlock`, `GetBlockIndex`, `RemoveBlockAt`, `RemoveBlock`, `AppendBlock`, `InsertBlock`, `IsLastBlock`
- [Rule properties](#rule-properties) — `Name`, `IsEnabled`, `BlocksCount`, `Blocks`

---

## Rule constructors

The Rule API reference contains the following constructors.

### `Rule()`

This constructor creates an empty Rule with no blocks.

```csharp
public Rule()
```

## Rule methods

The Rule API reference contains the following methods.

### `GetBlock`

This method retrieves the `RuleBlock` at the specified index.

```csharp
public RuleBlock GetBlock(int i)
```

`GetBlock` accepts the following parameter.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `i` | int | required | Zero-based index of the block to retrieve. |

This method returns the `RuleBlock` at the given index.

### `GetBlockIndex`

This method returns the index of the given `RuleBlock` within the Rule.

```csharp
public int GetBlockIndex(RuleBlock block)
```

`GetBlockIndex` accepts the following parameter.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `block` | RuleBlock | required | The `RuleBlock` whose index to find. |

This method returns an `int` index, or `-1` if the block is not found.

### `RemoveBlockAt`

This method removes the `RuleBlock` at the specified index.

```csharp
public void RemoveBlockAt(int index)
```

`RemoveBlockAt` accepts the following parameter.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `index` | int | required | Zero-based index of the block to remove. |

### `RemoveBlock`

This method removes a specific `RuleBlock` instance from the Rule.

```csharp
public void RemoveBlock(RuleBlock block)
```

`RemoveBlock` accepts the following parameter.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `block` | RuleBlock | required | The `RuleBlock` instance to remove. |

### `AppendBlock`

This method adds a `RuleBlock` to the end of the Rule and sets its back-reference to this Rule.

```csharp
public void AppendBlock(RuleBlock block)
```

`AppendBlock` accepts the following parameter.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `block` | RuleBlock | required | The `RuleBlock` instance to append. |

### `InsertBlock`

This method inserts a `RuleBlock` at the specified index, shifting subsequent blocks down. If `index` is beyond the last position, the block is appended instead.

```csharp
public void InsertBlock(RuleBlock block, int index)
```

`InsertBlock` accepts the following parameters.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `block` | RuleBlock | required | The `RuleBlock` instance to insert. |
| `index` | int | required | Zero-based position at which to insert the block. |

## Rule properties

The Rule API reference contains the following properties.

### `Name`

```csharp
public string Name { get; set; }
```

The display name of the Rule. Setting this property notifies the UI.

> **Note:** C# property names are case-sensitive. Use `Name` (capital N) — `name` does not exist and will not compile.

```csharp
rule.Name = "My Rule";
string ruleName = rule.Name;
```

### `IsEnabled`

```csharp
public bool IsEnabled { get; set; }
```

Controls whether the Rule is active within its RuleSet. When `false`, the Rule is skipped during execution. Setting this property notifies the UI.

### `BlocksCount`

```csharp
public int BlocksCount { get; }
```

The number of `RuleBlock` instances currently in the Rule.

### `Blocks`

```csharp
public IEnumerable<RuleBlock> Blocks { get; }
```

An enumerable over all `RuleBlock` instances in the Rule, in execution order.
