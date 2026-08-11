## Contents
- [Actions](#actions)
- [RuleSets](#rulesets)
  - Description
  - Modifying RuleSets
  - Creating RuleSets
  - Setting Action Parameters
  - Running RuleSets
  - Validation Checklist
- [Action utility functions](#action-utility-functions) — `GetActionsList`, `GetActionDefinitions`, `SetActionParameter`
- [Action utility output types](#action-utility-output-types) — `ActionInfo`, `ActionDefinition`, `ActionParameterInfo`, `EnumInfo`

---

## Actions

Actions are classes derived from UnityEditor.PixyzPlugin4Unity.Actions.ActionBase that execute a task on a list of input objects.
It is expected that users will create additional Action classes to extend the Actions available in the base package.

## RuleSets

### Description
RuleSets are a SerializedObject containing a list of Rule instances, which contain a list of Action instances. RuleSets are used to ensure a set of Actions are executed in a specific order.
RuleSets are derived from ScriptableObject and must end with the '.asset' extension.
RuleSets support conversion to json. This will include information about the Actions they contain.

### Modifying RuleSets

#### Step 1: Load RuleSet
- Use AssetDatabase.LoadAssetAtPath to load the asset from memory.

#### Step 2: Construct the script
- Read the API reference for the RuleSet class. Also read the API reference for the Rule and RuleBlock classes if required.
- Construct a script using the APIs from those files, to run as C# in a live Editor. Here is an
  example that adds a Decimate Action to a RuleSet:

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.PixyzPlugin4Unity.RuleEngine;

string path = "Assets/Rulesets/OptimizationRuleSet.asset";
RuleSet ruleSet = AssetDatabase.LoadAssetAtPath<RuleSet>(path);

if (ruleSet == null)
    throw new System.Exception($"RuleSet not found at {path}");

Undo.RecordObject(ruleSet, "Add Decimate action");

if (ruleSet.RulesCount == 0)
    throw new System.Exception($"No rules found in RuleSet {path}");

Rule rule = ruleSet.GetRule(0);

// Decimate Action ID is 277054868
RuleBlock decimateBlock = new RuleBlock(277054868);

rule.AppendBlock(decimateBlock);

EditorUtility.SetDirty(ruleSet);
AssetDatabase.SaveAssets();

return $"Added Decimate action to {path} (Rule index 0)";
```

A proper script for modifying a RuleSet has the following traits:
- Does not use the ScriptableObject API (this bypasses necessary event triggers).

#### Step 3: Validation
- Run the script as C# in a live Editor.
- Validate the RuleSet against the validation checklist.


### Creating RuleSets

#### Step 1: Create RuleSet
- Create the UnityEditor.PixyzPlugin4Unity.RuleEngine.RuleSet asset.
Continue to Step 2 if actions need to be added to the RuleSet. If not, add the GetContextGameObjects action and jump to Step 3.

#### Step 2: Add Actions

**Preflight**
- Ensure the RuleSet exists.
- Choose the combination of Actions that will best perform the requested procedure. NEVER create new Actions without explicit permission. Instead, use the Actions returned by `GetActionsList`.
- Divide Actions into Rules based on the GameObject they need to act upon. Each Rule initially executes on every GameObject unless the input is narrowed with a Filter action. Example: If only lights need to be disabled and only meshes with >10000 vertices need to be decimated, two rules will be needed as this is two different groups of GameObjects.

**Adding Rules**
- Check whether the existing Rule(s) in the RuleSet is just the GetContextGameObjects action. If it is, append the group of actions to it rather than creating a new Rule.
- If a new Rule needs to be created, add it to the RuleSet.
- Add Actions to the Rules.
- Set action parameters if required — see Setting Action Parameters below.

**Technical Notes**
- All Actions derive from the ActionBase class.
- Actions are located in the UnityEditor.PixyzPlugin4Unity.Actions namespace.

#### Step 3: Validation
- Validate the RuleSet logic using the validation checklist.


### Setting Action Parameters

#### Step 1: Gather data
- Gather any missing information needed to call `ATTAssistantUtilities.SetActionParameter`. If you need to retrieve a GlobalObjectId, first read the GlobalObjectId class to choose the correct function to call.
- Call `ATTAssistantUtilities.SetActionParameter` to set the parameter.
- If the result is false and not an exception, retry a maximum of three times.
- Follow a path based on the result.

#### Path A: AITypeSecurityException
Follow these steps if `ATTAssistantUtilities.SetActionParameter` threw an AITypeSecurityException.
- Inform the user the parameter cannot be set programmatically for security reasons.
- Advise the user to manually set the parameter and what value to set it to.

#### Path B: Exception
Follow these steps if `ATTAssistantUtilities.SetActionParameter` threw any other exception.
- Warn the user the property was unable to be set.
- Advise the user to manually set the parameter and what value to set it to.

#### Path C: Success
Perform these steps if `ATTAssistantUtilities.SetActionParameter` returned true.
- If the property was set to a scene GameObject, NEVER validate it because it is not persistent. INSTEAD warn the user the property value is temporary and will be lost.
- Report the success to the user.

#### Path D: Failure
Perform this step if `ATTAssistantUtilities.SetActionParameter` always returns false.
- Report the failure to the user and advise them to set the parameter manually. Tell them what the property should be set to.

All paths are exclusive.

**Technical Notes**
- For the Decimate action specifically, if mesh quality is going to be set to a preset, the Criterion parameter must also be set to Quality.
- Prefer using presets when possible rather than individually setting each value.
- If a preset is used, avoid changing values the preset changed unless requested otherwise.

**Safety & Constraints**
1. **One-Strike Rule**: If `ATTAssistantUtilities.SetActionParameter` throws an AITypeSecurityException, you MUST TERMINATE the task immediately. Do NOT use raw C# execution, reflection, or any other method to bypass this. Follow the steps in Path A as your final actions.


### Running RuleSets
Use `ATTAssistantUtilities.RunRuleSet()` instead of the RuleSet's public API to run a RuleSet.
Only one RuleSet must be running at a time.
When running a RuleSet, remind the user it is a background task/asynchronous.


### Validation Checklist
- The first Action in each Rule is GetContextGameObjects or RunRules.
- If the RunRules Action is in a Rule, it is the only Action.
- Each Rule has at least one Action.


## Action utility functions

The following functions are from `Unity.Pixyz.Plugin4Unity.Editor.AI.ATTAssistantUtilities`.

### `GetActionsList`

Returns all Rule Engine actions available in the project, including user-defined actions. Use this when you do not already know an action's ID. Pass the returned IDs to `GetActionDefinitions` to inspect parameters.

```csharp
public static ActionInfo[] GetActionsList()
```

Returns an `ActionInfo[]` containing the name, tooltip, and ID of every available action.

### `GetActionDefinitions`

Returns parameter definitions for one or more actions by unqualified class name (e.g. `"Decimate"`, not `"UnityEditor.PixyzPlugin4Unity.Actions.Decimate"`). Use this before `SetActionParameter` to obtain correct parameter names and types. Throws if an action class name is not found.

```csharp
public static ActionDefinition[] GetActionDefinitions(string[] actionClassNames)
```

Returns an `ActionDefinition[]`, each containing the action ID and its full parameter list.

### `SetActionParameter`

Sets a `UserParameter` field value on an action within a RuleSet. Use `GetActionDefinitions` first to obtain the correct parameter name. Returns `false` if the field was not found.

```csharp
public static bool SetActionParameter(string ruleSetPath, int ruleIndex, int ruleblockIndex, string parameterName, string value)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ruleSetPath` | string | required | Project-relative path to the RuleSet asset. |
| `ruleIndex` | int | required | Zero-based index of the rule containing the action. |
| `ruleblockIndex` | int | required | Zero-based index of the action block within the rule. |
| `parameterName` | string | required | The `ParameterPath` value from `GetActionDefinitions`. Must not be fully qualified. |
| `value` | string | required | String representation of the value to set. For Unity assets or scene objects, provide a `GlobalObjectId` string. For `LayerMask`, use layer names separated by `\|`. |

Returns `true` if the parameter was set and the RuleSet saved, `false` if the field was not found.

## Action utility output types

### `ActionInfo`

| Field | Type | Description |
|-------|------|-------------|
| `Name` | string | Fully qualified class name of the action. |
| `Description` | string | Tooltip text describing what the action does. |
| `ID` | int | Unique integer ID. Pass to `GetActionDefinitions` or use as `RuleBlock` action ID. |

### `ActionDefinition`

| Field | Type | Description |
|-------|------|-------------|
| `ID` | int | Unique integer ID of the action. |
| `Parameters` | `ActionParameterInfo[]` | All configurable `UserParameter` fields on the action. |

### `ActionParameterInfo`

| Field | Type | Description |
|-------|------|-------------|
| `Name` | string | Immediate field name. |
| `ParameterPath` | string | Full dot-separated path to pass as `parameterName` to `SetActionParameter` (e.g. `"advancedParametersQuality.surfacicTolerance"`). |
| `Type` | string | Fully qualified type name of the field. |
| `Description` | string | Tooltip describing the parameter. |
| `IsConditional` | bool | `true` if this parameter is only visible under certain conditions. |
| `PossibleEnumValues` | `EnumInfo[]` | Valid values if the parameter is an enum type. |
| `NestedParameters` | `ActionParameterInfo[]` | Child parameters for struct fields. |

### `EnumInfo`

| Field | Type | Description |
|-------|------|-------------|
| `Label` | string | Name of the enum value. |
| `Value` | Int64 | Underlying integer value of the enum member. |
