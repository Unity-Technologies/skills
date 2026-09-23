## ActionBase properties

### `Id`

```csharp
public abstract int Id { get; }
```

A unique integer identifier for an action type. Pass this to the `RuleBlock(int actionId)` constructor.

## RuleBlock constructors

The RuleBlock API reference contains the following constructors.

### `RuleBlock(int actionId)`

This constructor creates a RuleBlock that will execute the action identified by `actionId`. The action instance is created lazily on first access.

```csharp
public RuleBlock(int actionId)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `actionId` | int | required | The ID of the action this RuleBlock will trigger. Use `ActionBase.Id` to obtain this value from an action instance. |

## RuleBlock properties

The RuleBlock API reference contains the following properties.

### `IsEnabled`

```csharp
public bool IsEnabled { get; set; }
```

Controls whether this block is active within its Rule. When `false`, the block is skipped during execution.
