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

Actions are classes derived from `UnityEditor.PixyzPlugin4Unity.Actions.ActionBase` that run a task on a list of input objects.
Users can write their own Action classes, so call `GetActionsList` to see which Actions the project has instead of assuming.

## RuleSets

### Description
A RuleSet is a ScriptableObject asset that holds a list of Rules. Each Rule holds a list of RuleBlocks, and each RuleBlock runs one Action. The Editor UI calls RuleBlocks "Actions". A RuleSet runs its Actions in a fixed order.
Save RuleSets with the `.asset` extension, because Unity only saves a ScriptableObject as an asset with that extension.
A RuleSet can convert to JSON, including information about the Actions it contains.

### Modifying RuleSets

#### Step 1: Load RuleSet
1. Load the asset from disk with `AssetDatabase.LoadAssetAtPath`.

#### Step 2: Construct the script
1. Read the API reference for the RuleSet class, and for the Rule and RuleBlock classes if you need them.
2. Write a script with those APIs to run as C# in a live Editor. Here is an
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

Change the RuleSet only through the RuleSet, Rule, and RuleBlock methods, as in the example. Don't edit its fields through `SerializedObject`, because that skips the events the RuleSet raises to update its UI and internal state.

#### Step 3: Validation
1. Run the script as C# in a live Editor.
2. Check the RuleSet against the [validation checklist](#validation-checklist).


### Creating RuleSets

#### Step 1: Create RuleSet
1. Create the `UnityEditor.PixyzPlugin4Unity.RuleEngine.RuleSet` asset.
2. If the user wants Actions in the RuleSet, continue to Step 2. Otherwise, add a Rule with only the GetContextGameObjects Action and jump to Step 3, because every Rule must start with GetContextGameObjects or RunRules to be valid.

#### Step 2: Add Actions

**Preflight**
1. Check the RuleSet exists.
2. Choose the combination of Actions that will best perform the requested procedure. NEVER create new Actions without explicit permission, because a new Action adds C# code to the user's project that they didn't ask for. Instead, use the Actions returned by `GetActionsList`.
3. Divide Actions into Rules based on the GameObject they need to act upon. Each Rule initially executes on every GameObject unless the input is narrowed with a Filter action. Example: If only lights need to be disabled and only meshes with >10000 vertices need to be decimated, two rules will be needed as this is two different groups of GameObjects.

**Adding Rules**
1. Check whether an existing Rule holds only the GetContextGameObjects Action. If it does, append the Actions to it instead of creating a new Rule, because an empty starter Rule does nothing on its own.
2. Otherwise, add a new Rule to the RuleSet.
3. Add the Actions to the Rules.
4. If the user asked for specific Action settings, set them as described in [Setting Action Parameters](#setting-action-parameters).

**Technical Notes**
- All Actions derive from the ActionBase class.
- Actions are located in the UnityEditor.PixyzPlugin4Unity.Actions namespace.

#### Step 3: Validation
1. Check the RuleSet against the [validation checklist](#validation-checklist).


### Setting Action Parameters

#### Step 1: Gather data
1. Gather any missing information needed to call `ATTAssistantUtilities.SetActionParameter`. If you need a GlobalObjectId, first read the `GlobalObjectId` API page to choose the correct function to call.
2. Call `ATTAssistantUtilities.SetActionParameter` to set the parameter.
3. If it returns `false` without an exception, check the parameter name against `GetActionDefinitions` and retry, up to three times in total, because `false` means the name didn't match a field and a corrected name usually fixes it.
4. Follow the path that matches the result.

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
- For the Decimate Action, if you set mesh quality to a preset, also set the Criterion parameter to Quality, because the preset only takes effect when Criterion is Quality.
- Use a preset rather than setting each value, because presets set related values that work together.
- If you use a preset, don't change values the preset set unless the user asks, because that breaks the combination the preset chose.

**Safety & Constraints**
1. **One-Strike Rule**: If `ATTAssistantUtilities.SetActionParameter` throws an AITypeSecurityException, you MUST TERMINATE the task immediately, because the toolkit deliberately blocks setting this parameter from code, and any workaround would defeat that safeguard. Do NOT use raw C# execution, reflection, or any other method to bypass this. Follow the steps in Path A as your final actions.


### Running RuleSets
Use `ATTAssistantUtilities.RunRuleSet()` instead of the RuleSet's public API to run a RuleSet, because it reports progress through Unity's background task progress window, so the user can watch the run. Running it through the RuleSet API shows no progress.
Run only one RuleSet at a time, because a running RuleSet changes scene objects, and a second one would act on objects the first is still changing.
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
