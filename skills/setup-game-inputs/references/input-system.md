## Table of Contents
- [Performance Notes](#performance-notes)
- [0. Package Installation + Project Setting Check (Must Do First)](#0-package-installation-project-setting-check-must-do-first)
- [1. Pre-Flight Check (Crucial)](#1-pre-flight-check-crucial)
- [2. Gather Missing Information](#2-gather-missing-information)
- [3. Planning & Execution Steps](#3-planning-execution-steps)
- [4. Validation Checklist (Must Confirm)](#4-validation-checklist-must-confirm)
- [5. Final Confirmation Message (What reporting back)](#5-final-confirmation-message-what-reporting-back)
- [Important API notes](#important-api-notes)
- [Core Concepts Reference](#core-concepts-reference)
- [Responding to Actions Reference](#responding-to-actions-reference)
- [PlayerInput Component Reference](#playerinput-component-reference)
- [PlayerInputManager Reference (Multiplayer)](#playerinputmanager-reference-multiplayer)
- [UI Support Reference](#ui-support-reference)
- [Interactions Reference](#interactions-reference)
- [Composite Bindings Reference](#composite-bindings-reference)
- [Interactive Rebinding Reference](#interactive-rebinding-reference)
- [Processors Reference](#processors-reference)
- [Direct Device Access Reference (Prototyping Only)](#direct-device-access-reference-prototyping-only)
- [Migration from Legacy Input Manager](#migration-from-legacy-input-manager)
- [Common Mistakes to Avoid](#common-mistakes-to-avoid)


## Performance Notes
- Do this thoroughly.
- Quality is more important than speed.

## 0. Package Installation + Project Setting Check (Must Do First)

1. Package Installation Check
First of all **verify that the com.unity.inputsystem package is installed**
**Install if Missing:** add the package to the project manifest — `Packages/manifest.json`,
under `dependencies`:

```json
"com.unity.inputsystem": "<current 1.x version>"
```

Don't invent the version string. Read the current one from the Unity registry —
`https://packages.unity.com/com.unity.inputsystem` lists every published version — or copy the version an
adjacent Unity package in this manifest already uses. A version that doesn't exist makes
Unity fail resolution **silently**, so a wrong guess looks like nothing happened.

Unity resolves the new dependency the next time the Editor regains focus. This needs no
Editor connection, which is why it's the default route here.

If you do have a live Editor to run C# in, the equivalent is:

```csharp
using UnityEditor.PackageManager;

var request = Client.Add("com.unity.inputsystem");
UnityEngine.Debug.Log("Requested com.unity.inputsystem. Progress shows in the Package Manager window.");
```
**Proceed:** Only continue to the next steps once InputSystem is confirmed to be installed.

2. Active Input Handling Check
After verifying the package is installed, check the project's Active Input Handling setting:
- **Input System Package (New)** — only the new Input System is active. `ENABLE_INPUT_SYSTEM` is defined.
- **Input Manager (Old)** — only the legacy Input Manager is active. `ENABLE_LEGACY_INPUT_MANAGER` is defined.
- **Both** — both systems are active. Both defines are set. Use Input System (new) as default.

Changing the Active Input Handling setting requires an Editor restart.

## 1. Pre-Flight Check (Crucial)

### Check Project-Wide Actions (CRITICAL — DO NOT GREP PROJECT FILES)

**DO NOT** search or grep `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/EditorBuildSettings.asset`, or any other project settings files to find the project-wide actions asset. The reference is stored internally via `EditorBuildSettings` config objects and is **not human-readable** in project files. Attempting to grep these files will fail and waste time.

**The ONLY correct way** to check and manage project-wide actions is the C# API below, run in a live Editor:

**To check if project-wide actions are assigned and inspect their contents:**

Two routes. The file route needs no Editor: `.inputactions` assets are JSON on disk, so
glob for `*.inputactions` and read one directly to see its action maps, actions and
bindings. What a file cannot tell you is which asset is *assigned* project-wide — that
lives in project settings.

With a live Editor to run C# in:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

var actions = InputSystem.actions;
if (actions == null)
{
    Debug.Log("No project-wide Input Actions asset is currently assigned.");
    Debug.Log("To create one: Edit > Project Settings > Input System Package > Create a new project-wide Action Asset");

    // Also check if any .inputactions assets exist in the project that could be assigned
    var guids = UnityEditor.AssetDatabase.FindAssets("t:InputActionAsset");
    if (guids.Length > 0)
    {
        Debug.Log($"Found {guids.Length} InputActionAsset(s) in the project that could be assigned:");
        foreach (var guid in guids)
        {
            var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"  - {assetPath}");
        }
    }
    return "no project-wide actions assigned";
}

var path = UnityEditor.AssetDatabase.GetAssetPath(actions);
var report = new System.Text.StringBuilder();
report.AppendLine($"Project-wide actions asset: {actions.name} ({path})");
report.AppendLine($"Action Maps ({actions.actionMaps.Count}):");
foreach (var map in actions.actionMaps)
{
    report.AppendLine($"  - {map.name} ({map.actions.Count} actions)");
    foreach (var action in map.actions)
    {
        report.AppendLine($"      {action.name} (Type: {action.type}, ExpectedControlType: {action.expectedControlType}, Bindings: {action.bindings.Count})");
    }
}
report.AppendLine($"Control Schemes ({actions.controlSchemes.Count}):");
foreach (var scheme in actions.controlSchemes)
{
    report.AppendLine($"  - {scheme.name}");
}
return report.ToString();
```

Return the report rather than only logging it: logs land in the Editor console, while the
returned value is what comes back to whoever ran the snippet.

**To assign an existing .inputactions asset as project-wide:**
```csharp
var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/MyActions.inputactions");
if (asset != null)
{
    InputSystem.actions = asset;
    UnityEngine.Debug.Log($"Assigned '{asset.name}' as project-wide actions.");
}
```
Note: `InputSystem.actions` can only be assigned in Edit mode (not Play mode) and the asset must be a persistent file on disk inside the Assets folder.

**To find all .inputactions assets in the project:**
```csharp
var guids = UnityEditor.AssetDatabase.FindAssets("t:InputActionAsset");
foreach (var guid in guids)
{
    var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    UnityEngine.Debug.Log($"Found: {assetPath}");
}
```

### UI Input Module sanity (if the project uses UI)
If the user uses Unity UI (uGUI):
- Find (or create) an EventSystem.
- Ensure it has `InputSystemUIInputModule`.
- If `StandaloneInputModule` is present, remove it to avoid conflicts.

For UI Toolkit (Unity 2023.2+): The UI actions defined in the default project-wide actions directly map to UI Toolkit input. `InputSystemUIInputModule` component is not needed.

For UI Toolkit (pre-2023.2): `InputSystemUIInputModule` component must be used.

## 2. Gather Missing Information
Before invoking tools, ensure the following details exist. If not, ask the user:

### Core questions
* **Asset name/path:** Where input actions should be? (Default: Assets/Input/InputActions.inputactions)
* **Action maps:** e.g. Player, UI, Vehicle, Debug
* **Target GameObject(s):** which object gets PlayerInput / input scripts? (player prefab, character root, etc.)
* **Platforms/devices:** Keyboard&Mouse, Gamepad, Touch, XR?
* **Gameplay actions needed:** e.g. Move, Look, Jump, Sprint, Crouch, Interact, Fire, Aim, Pause, Navigate UI

### Per-action details (important)
* **For each action, gather:**
    * **Action Type:** Value / Button / PassThrough
    * **Expected Control Type:** Vector2, Axis, Button, Delta, etc.
    * **Bindings:** default keys/buttons, plus optional composites (2D Vector WASD, arrow keys)
    * **Interactions/Processors:** Hold/Tap, Press behavior, Deadzone, Normalize, Invert Y, Sensitivity

### Action Type Selection Guide

| Action Type | Use When | Behavior |
|-------------|----------|----------|
| **Value** (default) | Continuous inputs: movement sticks, triggers, mouse delta | Tracks the most actuated control. Performs initial state check on enable. Conflict resolution picks highest magnitude. |
| **Button** | Discrete press actions: jump, fire, interact | Like Value but only binds to `ButtonControl`. No initial state check (avoids re-triggering held buttons on enable). |
| **PassThrough** | Multi-device monitoring, UI pointer actions, raw input | No conflict resolution. Every bound control change fires a callback. No single "driving" control. |


## 3. Planning & Execution Steps

0. Identify existing input patterns and architecture in the project and follow them

1. Create/Update the Input Actions asset
- Reuse existing Input Actions asset (or create new one)
- Create Input control schemes for required devices (or reuse existing). Don't skip this step, it is important for local multiplayer to have control schemes.
- Create required Action Maps (or reuse existing).
- Create Actions with correct types/control types.
- Add Bindings (including composites like WASD for Move).

2. Generate a C# wrapper (Optional)
If the user wants strongly-typed code or it is the pattern in the project:
- Enable Generate C# Class on the .inputactions asset
- Set wrapper class name (e.g. GameInput)
- Ensure it regenerates when the asset changes

3. Hook into gameplay
Base it on existing input patterns in the project.

* **Option 1 - PlayerInput-based setup**
- Add PlayerInput to the player root (or ensure it exists).
- Assign the Input Actions asset to PlayerInput.actions.
- Set:
    - Default Map (e.g. Player)
    - Notification Behavior based on the project patterns or what the user specified (Prefer "Invoke CSharp Events" if no preference nor existing pattern exist):
      * **Send Messages** when the PlayerInputs sends messages (Default)
        - Implement or make sure there are functions in a script to take the messages
        - Make sure the PlayerInput.NotificationBehaviour uses SendMessages
        - Method signature: `public void OnActionName()` or `public void OnActionName(InputValue value)`
        - The component must be on the same GameObject as PlayerInput
        - `InputValue` is only valid during the callback; do not store it
      * **Broadcast Messages** same as Send Messages but also sends to child GameObjects
        - Method signature: same as Send Messages
        - Component can be on the same or any child GameObject
      * **Invoke CSharp Events** when a C# script subscribes for the PlayerInput
        - Make a script to subscribe for the `onActionTriggered` event on the PlayerInput
        - Make sure the PlayerInput.NotificationBehaviour uses InvokeCSharpEvents
        - Method signature: receives `InputAction.CallbackContext`
      * **Invoke Unity Events** when the PlayerInputs has set up unity events to call functions
        - Implement or make sure there are functions in a script to take unity event
        - Set up the unity events on the PlayerInput to call the functions
        - Make sure the PlayerInput.NotificationBehaviour uses InvokeUnityEvents
        - Method signature: `public void OnActionName(InputAction.CallbackContext context)`
- If using control schemes, set Default Control Scheme (optional).

**CRITICAL PlayerInput rule:** When writing input code that works with PlayerInput, do NOT use `InputSystem.actions`. Use `playerInput.actions` instead. PlayerInput creates private copies of actions for device filtering in multiplayer. Using `InputSystem.actions` bypasses automatic device assignment.

**Project-wide actions + PlayerInput caveat:** With project-wide actions, all action maps may be enabled by default. Disable `InputSystem.actions` and enable only the map PlayerInput should use:
```csharp
void Start()
{
    playerInput = GetComponent<PlayerInput>();
    InputSystem.actions.Disable();
    playerInput.currentActionMap?.Enable();
}
```

* **Option 2 - InputAction asset reference** (Default)
    - Create a script that owns an InputActionAsset / generated wrapper instance.
    - Enable/disable maps in OnEnable/OnDisable.
    - Subscribe to performed/canceled events.

Example with generated C# wrapper:
```csharp
public class MyPlayerScript : MonoBehaviour, IGameplayActions
{
    MyPlayerControls controls;

    public void OnEnable()
    {
        if (controls == null)
        {
            controls = new MyPlayerControls();
            controls.gameplay.SetCallbacks(this);
        }
        controls.gameplay.Enable();
    }

    public void OnDisable()
    {
        controls.gameplay.Disable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        var value = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context) { }
}
```

* **Option 3 - Project-Wide Actions** (Simplest)
    - First check if a project-wide asset is assigned using the script from Section 1 "Check Project-Wide Actions". Do NOT grep ProjectSettings files.
    - If no project-wide asset is assigned, either create one via the asset creation API (see API note 1) and assign it with `InputSystem.actions = asset;`, or instruct the user to go to Edit > Project Settings > Input System Package > Create a new project-wide Action Asset.
    - Project-wide actions are enabled by default and ready to use.
    - Hook actions into gameplay using `InputSystem.actions.FindAction("Move")`. Cache references in `Start()`, do NOT call `FindAction` every frame.

Example:
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    InputAction moveAction;
    InputAction jumpAction;

    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    void Update()
    {
        Vector2 moveValue = moveAction.ReadValue<Vector2>();

        if (jumpAction.WasPressedThisFrame())
        {
            // Jump logic
        }
    }
}
```

4. UI support (if requested)
- Ensure EventSystem + InputSystemUIInputModule exists.
- Ensure there's a UI action map (or use Unity's default UI actions pattern).
- Confirm the UI module references correct actions (depending on project setup).

Required UI actions (names and types must match for UI Toolkit compatibility via Project-Wide Input Actions: `InputSystem.actions`):

| Action | Action Type | Control Type | Description |
|--------|-------------|--------------|-------------|
| Navigate | PassThrough | Vector2 | D-pad / arrow key navigation |
| Submit | Button | Button | Confirm selection |
| Cancel | Button | Button | Exit interaction |
| Point | PassThrough | Vector2 | Cursor position |
| Click | PassThrough | Button | Primary click |
| RightClick | PassThrough | Button | Secondary click |
| MiddleClick | PassThrough | Button | Middle click |
| ScrollWheel | PassThrough | Vector2 | Scroll input |
| Tracked Device Position | PassThrough | Vector3 | XR position |
| Tracked Device Orientation | PassThrough | Quaternion | XR rotation |

**IMPORTANT:** Pointer-type UI actions (Point, Click, RightClick, MiddleClick, ScrollWheel) MUST be set to PassThrough type so multiple devices can feed input without filtering.

## 4. Validation Checklist (Must Confirm)
- Input System package installed
- Active Input Handling set correctly
- No UI module conflicts (StandaloneInputModule removed if necessary, InputSystemUIInputModule not required by UI Toolkit)
- The input action asset is not corrupted after the changes
- Modifications to input action assets do not result in missing input action references
- The input actions references assigned to the scripts where it is needed
- The input action asset has input control schemes
- Actions have correct types (Value for continuous, Button for discrete, PassThrough for multi-device)
- Composite bindings are correctly configured (2D Vector for WASD, 1D Axis for left/right, etc.)

## 5. Final Confirmation Message (What reporting back)
Summarize what was created/changed:
- Input Actions asset path + action maps/actions
- Control schemes + bindings
- PlayerInput setup (target object, default map, notification behavior)
- UI EventSystem module state
- Any restart requirement (Active Input Handling change)


## Important API notes

0. Never edit inputaction asset Json directly, always use the InputActionAsset API, run in a live Editor, to edit the asset.

1. CreateAsset() should not be used to create a file of type 'inputactions'.
To create and save the '.inputaction' files use the next code example:
```csharp
InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();

string json = asset.ToJson();
File.WriteAllText(path, json);
```

2. Do NOT use 'Input.' class to handle inputs, it might result with exceptions at runtime. Use `InputSystem.actions.FindAction()` or action references instead.

3. To add or remove input control scheme use `asset.AddControlScheme(InputControlScheme)`

4. To add an action to a map use the API example that follows `public static InputAction AddAction(this InputActionMap map, string name, InputActionType type = InputActionType.Value, string binding = null, string interactions = null, string processors = null, string groups = null, string expectedControlLayout = null)`

5. To add a composite binding use `AddCompositeBinding`:
```csharp
moveAction.AddCompositeBinding("2DVector")
    .With("Up", "<Keyboard>/w")
    .With("Down", "<Keyboard>/s")
    .With("Left", "<Keyboard>/a")
    .With("Right", "<Keyboard>/d");
```

6. To add a simple binding use `AddBinding`:
```csharp
fireAction.AddBinding("<Mouse>/leftButton");
fireAction.AddBinding("<Gamepad>/rightTrigger");
```

7. Enable/Disable actions and maps:
```csharp
// Enable a single action
myAction.Enable();

// Enable an entire action map
gameplayMap.Enable();

// Disable
myAction.Disable();
gameplayMap.Disable();
```
DO not change bindings while an action is enabled. Disable first, modify, then re-enable.

8. To find an action in an asset or project-wide actions:
```csharp
// By action name (searches all maps)
var action = asset.FindAction("Jump");

// By map/action path (disambiguates if name collisions exist)
var action = asset.FindAction("Player/Jump");
```

9. CRITICAL: Project-Wide Actions are NOT in ProjectSettings files.
**NEVER** search, grep, or read `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/EditorBuildSettings.asset`, or any other settings files to find input actions. The project-wide actions reference is stored internally via `EditorBuildSettings` config objects (binary format, not greppable). Always use `InputSystem.actions` to read the current project-wide actions, and `InputSystem.actions = asset` to assign them. See Section 1 "Check Project-Wide Actions" for the complete script.

10. CRITICAL: Correct Unity Input System API Names

| WRONG (Hallucinated) | CORRECT |
|---------------------|---------|
| `InputSystem.GetDevice<Keyboard>()` | `Keyboard.current` |
| `InputSystem.GetDevice<Mouse>()` | `Mouse.current` |
| `InputSystem.GetDevice<Gamepad>()` | `Gamepad.current` |
| `Input.GetAxis("Horizontal")` | `InputSystem.actions.FindAction("Move").ReadValue<Vector2>().x` |
| `Input.GetButtonDown("Jump")` | `InputSystem.actions.FindAction("Jump").WasPressedThisFrame()` |
| `Input.GetButton("Jump")` | `InputSystem.actions.FindAction("Jump").IsPressed()` |
| `Input.GetButtonUp("Jump")` | `InputSystem.actions.FindAction("Jump").WasReleasedThisFrame()` |
| `Input.mousePosition` | `Mouse.current.position.ReadValue()` |
| `Input.GetMouseButtonDown(0)` | `Mouse.current.leftButton.wasPressedThisFrame` |
| `Input.GetKey(KeyCode.Space)` | `Keyboard.current.spaceKey.isPressed` |
| `Input.GetKeyDown(KeyCode.Space)` | `Keyboard.current.spaceKey.wasPressedThisFrame` |
| `InputActionMap.FromJson` creating an asset | `InputActionAsset.FromJson` for full assets |

## Core Concepts Reference

### Actions
Actions are named, game-meaningful inputs ("Jump", "Move") decoupled from hardware. They allow separating the purpose of an input from the device controls that perform it.

Each action has:
- A **name** (unique within its action map)
- A unique **ID** (persists across renames)
- An **Action Type** (Value, Button, or PassThrough)
- An **Expected Control Type** (Vector2, Button, Axis, etc.)

Actions are a runtime-only feature. Do NOT use them in Editor window code.

### Action Maps
Action maps group actions for a context (e.g., "Player", "UI", "Vehicle"). Enable/disable entire maps as a unit to switch input contexts.

### Input Action Assets
`.inputactions` files stored in JSON format containing action maps, actions, bindings, and control schemes. The recommended workflow is one asset assigned as project-wide actions.

### Project-Wide Actions
One asset designated globally via **Edit > Project Settings > Input System Package**. Accessible as `InputSystem.actions`. Preloaded at startup. Actions are enabled by default.

To create and assign default project-wide actions, go to **Edit > Project Settings > Input System Package** and click "Create a new project-wide Action Asset". This creates `InputSystem_Actions.inputactions` with default Player and UI action maps.

### Control Schemes
Groups of bindings and devices (e.g., "Keyboard&Mouse", "Gamepad"). Used for:
- Enabling/disabling sets of bindings
- PlayerInput automatic device pairing
- UI device switching feedback

### Bindings
Links an action to device control(s) via control paths. Types:
- **Normal binding**: direct path like `<Gamepad>/leftStick`
- **Composite binding**: synthesizes a value from multiple part bindings (e.g., WASD → Vector2)

Key binding properties:

| Property | Description |
|----------|-------------|
| `path` | Control path identifying the control(s). Example: `"<Gamepad>/leftStick"` |
| `overridePath` | Non-destructive override of `path`. Used for runtime rebinding. |
| `effectivePath` | Returns `overridePath` if set, otherwise `path`. |
| `action` | Name or ID of the action this binding triggers. |
| `groups` | Semicolon-separated binding groups (used for control schemes). Example: `"Keyboard&Mouse;Gamepad"` |
| `interactions` | Semicolon-separated interactions. Example: `"hold(duration=0.75)"` |
| `processors` | Semicolon-separated processors. Example: `"invertVector2(invertX=false)"` |
| `isComposite` | Whether this binding is a composite root. |
| `isPartOfComposite` | Whether this binding is a part of a composite. |

Control path syntax:
- `<Gamepad>/buttonSouth` — matches on any gamepad
- `<DualShockGamepad>/buttonSouth` — matches only PlayStation controllers
- `<Gamepad>/button*` — wildcard matching
- `*/{Submit}` — matches any control with "Submit" usage on any device

## Responding to Actions Reference

### Polling (Recommended for Gameplay)
Read values in `Update()`. Cache action references in `Start()`.

| Method | Description |
|--------|-------------|
| `ReadValue<T>()` | Current value of the action. Type must match the bound control's value type. |
| `IsPressed()` | True if actuation is above press point and hasn't fallen to release threshold. |
| `WasPressedThisFrame()` | True if actuation crossed press point this frame. |
| `WasReleasedThisFrame()` | True if actuation fell from above press point to at/below release threshold this frame. |
| `WasPerformedThisFrame()` | True if the action's phase became Performed this frame (interaction-driven). |
| `WasCompletedThisFrame()` | True if the action's phase changed away from Performed this frame. |

### Callbacks (Event-Driven)
Subscribe to action phase callbacks for sporadic or multi-listener setups.

```csharp
action.started += ctx => { /* Interaction started */ };
action.performed += ctx => { /* Interaction completed */ };
action.canceled += ctx => { /* Interaction interrupted/released */ };
```

`InputAction.CallbackContext` is only valid during the callback. Do not store it.

**Action Phases:**

| Phase | Description |
|-------|-------------|
| `Disabled` | Action is disabled and can't receive input. |
| `Waiting` | Action is enabled and waiting for input. |
| `Started` | Input has started an interaction with the action. |
| `Performed` | An interaction with the action has been completed. |
| `Canceled` | An interaction with the action has been interrupted. |

### Default Interaction Behavior by Action Type

| Callback | Value | Button | PassThrough |
|----------|-------|--------|-------------|
| `started` | Control changed away from default value | Button started being pressed | Not used |
| `performed` | Control changed value | Button crossed press threshold | Control changed value |
| `canceled` | Controls no longer actuated | Button released | Action disabled |

### Other Callback Options
- `InputActionMap.actionTriggered` — single callback for all actions in a map (receives started, performed, canceled)
- `InputSystem.onActionChange` — global callback for all action-related changes

## PlayerInput Component Reference

The PlayerInput component provides:
- Configuring how Actions map to methods or callbacks
- Handling local multiplayer: device filtering, screen splitting

### Configuration Properties

| Property | Description |
|----------|-------------|
| **Actions** | The Input Actions asset (project-wide or standalone asset) |
| **Default Scheme** | Control scheme to enable by default |
| **Default Map** | Action map to enable by default. If None, no actions are enabled. |
| **Camera** | Player camera (only needed for split-screen) |
| **Behavior** | Notification method: Send Messages, Broadcast Messages, Invoke Unity Events, Invoke C# Events |

### Notification Behaviors

| Behavior | How it Works | Method Signature |
|----------|-------------|-----------------|
| **Send Messages** | `GameObject.SendMessage` on the PlayerInput's GameObject | `void OnActionName()` or `void OnActionName(InputValue value)` |
| **Broadcast Messages** | `GameObject.BroadcastMessage` down the hierarchy | Same as Send Messages |
| **Invoke Unity Events** | Separate UnityEvent per action, configurable in Inspector | `void OnActionName(InputAction.CallbackContext context)` |
| **Invoke C# Events** | Plain C# events: `onActionTriggered`, `onDeviceLost`, `onDeviceRegained` | `void Handler(InputAction.CallbackContext context)` |

### Action Map Switching
```csharp
// Switch by name
playerInput.SwitchCurrentActionMap("UI");

// Check current
var currentMap = playerInput.currentActionMap;

// Deactivate/Activate all input
playerInput.DeactivateInput();
playerInput.ActivateInput(); // Re-enables default action map
```

### Device Lost/Regained
PlayerInput sends `DeviceLostMessage` and `DeviceRegainedMessage` notifications when devices disconnect/reconnect.

### UI Integration
Assign an `InputSystemUIInputModule` reference to PlayerInput's `UI Input Module` field. Both must use the same Input Actions asset. PlayerInput will configure the UI module to use the same action/device configuration.

For multiplayer UI, use `MultiplayerEventSystem` instead of `EventSystem`. Each player gets their own `MultiplayerEventSystem` + `InputSystemUIInputModule` + `PlayerInput`.

## PlayerInputManager Reference (Multiplayer)

Used alongside PlayerInput for local multiplayer.

| Property | Description |
|----------|-------------|
| **Player Prefab** | Must have a PlayerInput component |
| **Join Behavior** | Join When Button Is Pressed / Join When Join Action Is Triggered / Manual |
| **Max Players** | Maximum player count (-1 = unlimited) |
| **Split Screen** | Enable/configure split-screen rendering |

Each PlayerInput instance gets a private copy of actions with device filtering. Players are automatically paired to unique devices.

## UI Support Reference

### InputSystemUIInputModule
Required for Unity UI (uGUI). Replaces `StandaloneInputModule`.

| Property | Description |
|----------|-------------|
| Move Repeat Delay | Initial delay before repeat navigation events |
| Move Repeat Rate | Interval between repeat navigation events |
| Actions Asset | Input Action Asset driving the UI |
| Deselect on Background Click | Clear selection when clicking empty space (default: true) |
| Pointer Behavior | How multiple pointers are handled |

### Pointer Behaviors

| Mode | Description |
|------|-------------|
| **Single Mouse or Pen But Multi Touch And Track** | Default. Mouse/pen unified; touch and tracked devices are separate. |
| **Single Unified Pointer** | All input unified into one pointer. |
| **All Pointers As Is** | Every device is its own pointer. |

### UI Toolkit Compatibility

| UI Solution | Compatible | UI Input Module Required |
|-------------|------------|-------------------------|
| UI Toolkit (2023.2+) | Yes | Not required |
| UI Toolkit (pre-2023.2) | Yes | Required |
| Unity UI (uGUI) | Yes | Required |
| IMGUI | No (use "Both" Active Input Handling for IMGUI + Input System coexistence) |

## Interactions Reference

Interactions are input patterns that drive action phase transitions. Applied to bindings or actions.

### Built-in Interactions

| Interaction | Description | Key Parameters |
|-------------|-------------|----------------|
| **Default** | Applied when no interaction is specified. Behavior varies by action type. | — |
| **Press** | Explicit button-press pattern. | `pressPoint`, `behavior` (PressOnly/ReleaseOnly/PressAndRelease) |
| **Hold** | Requires holding a control for a duration. | `duration` (default: `InputSettings.defaultHoldTime`), `pressPoint` |
| **Tap** | Press and release within a duration. | `duration` (default: `InputSettings.defaultTapTime`), `pressPoint` |
| **SlowTap** | Hold for minimum duration, then release to trigger. | `duration` (default: `InputSettings.defaultSlowTapTime`), `pressPoint` |
| **MultiTap** | Multiple taps in succession (e.g., double-click). | `tapCount` (default: 2), `tapTime`, `tapDelay`, `pressPoint` |

### Interaction Phase Behavior

**Hold:**
- `started` → control crosses press point
- `performed` → held above press point for >= duration
- `canceled` → released before duration elapsed

**Tap:**
- `started` → control crosses press point
- `performed` → released before duration elapsed
- `canceled` → held too long (>= duration)

**Adding Interactions:**
```csharp
// In code
action.AddBinding("<Gamepad>/buttonSouth")
    .WithInteractions("hold(duration=0.4)");

// On action directly
var action = new InputAction(interactions: "hold(duration=0.4)");
```

Multiple interactions on a binding are processed in order. The first to trigger "consumes" the input.

### Timeout Completion
```csharp
// Get progress of hold/tap interaction (0 to 1)
float progress = action.GetTimeoutCompletionPercentage();
```

## Composite Bindings Reference

Composites combine multiple controls into a single value.

### Built-in Composites

| Composite | Output Type | Parts | Usage |
|-----------|-------------|-------|-------|
| **1D Axis** | `float` | Positive, Negative | Left/Right, triggers |
| **2D Vector** (Dpad) | `Vector2` | Up, Down, Left, Right | WASD movement, D-pad |
| **3D Vector** | `Vector3` | Up, Down, Left, Right, Forward, Backward | 3D movement |
| **One Modifier** | Any | Modifier, Binding | SHIFT+Key shortcuts |
| **Two Modifiers** | Any | Modifier1, Modifier2, Binding | CTRL+SHIFT+Key |

### Code Examples

```csharp
// 1D Axis
myAction.AddCompositeBinding("1DAxis")
    .With("Positive", "<Keyboard>/d")
    .With("Negative", "<Keyboard>/a");

// 2D Vector (WASD)
myAction.AddCompositeBinding("2DVector")
    .With("Up", "<Keyboard>/w")
    .With("Down", "<Keyboard>/s")
    .With("Left", "<Keyboard>/a")
    .With("Right", "<Keyboard>/d");

// 2D Vector with mode
myAction.AddCompositeBinding("2DVector(mode=2)") // mode=2 is Analog
    .With("Up", "<Gamepad>/leftStick/up")
    .With("Down", "<Gamepad>/leftStick/down")
    .With("Left", "<Gamepad>/leftStick/left")
    .With("Right", "<Gamepad>/leftStick/right");

// One Modifier (SHIFT+1)
myAction.AddCompositeBinding("OneModifier")
    .With("Binding", "<Keyboard>/1")
    .With("Modifier", "<Keyboard>/ctrl");

// Two Modifiers (CTRL+SHIFT+1)
myAction.AddCompositeBinding("TwoModifiers")
    .With("Button", "<Keyboard>/1")
    .With("Modifier1", "<Keyboard>/leftCtrl")
    .With("Modifier2", "<Keyboard>/leftShift");
```

### 2D Vector Mode Parameter

| Mode | Value | Description |
|------|-------|-------------|
| DigitalNormalized | 0 | Default. Inputs treated as on/off, vector normalized (diamond-shaped range). |
| Digital | 1 | On/off but not normalized. Diagonals have magnitude > 1. |
| Analog | 2 | Full floating-point values. Down and Left inverted. |

Each composite part can have multiple bindings (e.g., both WASD and arrow keys for the same 2D Vector).

## Interactive Rebinding Reference

Allow users to customize bindings at runtime.

### Performing a Rebind
```csharp
void RemapButtonClicked(InputAction actionToRebind)
{
    var rebindOperation = actionToRebind
        .PerformInteractiveRebinding()
        .Start();
}
```
IMPORTANT: Dispose `RebindingOperation` instances via `Dispose()` to prevent memory leaks.

### Configuration Options
- `WithExpectedControlType()` — filter by control type
- `WithControlsExcluding()` — exclude specific controls
- `WithCancelingThrough()` — set a cancel control
- `WithTargetBinding()` / `WithBindingGroup()` — target specific bindings

### Save and Load Rebinds
```csharp
// Save
var rebinds = playerInput.actions.SaveBindingOverridesAsJson();
PlayerPrefs.SetString("rebinds", rebinds);

// Load (removes existing overrides by default)
var rebinds = PlayerPrefs.GetString("rebinds");
playerInput.actions.LoadBindingOverridesFromJson(rebinds);
```

### Restore Defaults
```csharp
// Remove overrides from a single action
playerInput.actions["fire"].RemoveAllBindingOverrides();

// Remove all overrides from all actions
playerInput.actions.RemoveAllBindingOverrides();
```

### Display Binding Strings
```csharp
// Get display string for an action
string displayStr = action.GetBindingDisplayString();

// Get display string for a specific binding index
string displayStr = action.GetBindingDisplayString(1);

// Get with device/control info (for icon replacement)
string displayStr = action.GetBindingDisplayString(0, out string deviceLayout, out string controlPath);
```

### Apply Binding Overrides (Non-Interactive)
```csharp
// Override a binding path
playerInput.actions["fire"].ApplyBindingOverride("<Gamepad>/leftTrigger");

// Override by binding index
var jumpAction = playerInput.actions["Jump"];
var bindingIndex = jumpAction.GetBindingIndexForControl(Keyboard.current.spaceKey);
jumpAction.ApplyBindingOverride(bindingIndex, "<Keyboard>/enter");
```

Override properties (`overridePath`, `overrideProcessors`, `overrideInteractions`) are NOT saved with the asset JSON. Use `SaveBindingOverridesAsJson` / `LoadBindingOverridesFromJson` separately.

## Processors Reference

Processors transform input values. Applied to bindings or actions. Stack with processors on controls.

Common processors:
- `invertVector2(invertX=true,invertY=true)` — invert axes
- `scaleVector2(x=1,y=1)` — scale axes
- `stickDeadzone(min=0.125,max=0.925)` — apply deadzone to stick input
- `axisDeadzone(min=0.125,max=0.925)` — apply deadzone to single axis
- `normalize(min=0,max=1,zero=0)` — normalize to range
- `clamp(min=0,max=1)` — clamp value
- `invert` — invert a single float value
- `scale(factor=1)` — scale a single float value

```csharp
// In code
action.AddBinding("<Gamepad>/leftStick")
    .WithProcessors("stickDeadzone(min=0.2,max=0.9)");

// On action
var action = new InputAction(processors: "invertVector2(invertX=false)");
```

### Parameter Overrides for Sensitivity
```csharp
// Adjust mouse sensitivity separately from gamepad
var look = new InputAction("look", type: InputActionType.Value);
look.AddBinding("<Mouse>/delta", groups: "KeyboardMouse", processors: "scaleVector2");
look.AddBinding("<Gamepad>/rightStick", groups: "Gamepad", processors: "scaleVector2");

look.ApplyParameterOverride("scaleVector2:x", 0.5f, InputBinding.MaskByGroup("KeyboardMouse"));
look.ApplyParameterOverride("scaleVector2:y", 0.5f, InputBinding.MaskByGroup("KeyboardMouse"));

look.ApplyParameterOverride("scaleVector2:x", 2f, InputBinding.MaskByGroup("Gamepad"));
look.ApplyParameterOverride("scaleVector2:y", 2f, InputBinding.MaskByGroup("Gamepad"));
```

## Direct Device Access Reference (Prototyping Only)

For quick prototyping or fixed-device scenarios. Less flexible than actions.

```csharp
// Keyboard
if (Keyboard.current.spaceKey.wasPressedThisFrame) { }
if (Keyboard.current.wKey.isPressed) { }

// Mouse
Vector2 mousePos = Mouse.current.position.ReadValue();
if (Mouse.current.leftButton.wasPressedThisFrame) { }
Vector2 mouseDelta = Mouse.current.delta.ReadValue();

// Gamepad
var gamepad = Gamepad.current;
if (gamepad == null) return; // No gamepad connected
Vector2 move = gamepad.leftStick.ReadValue();
if (gamepad.buttonSouth.wasPressedThisFrame) { }
if (gamepad.rightTrigger.wasPressedThisFrame) { }
```

Always null-check `*.current` as devices may not be connected.

## Migration from Legacy Input Manager

When migrating from old `Input` class to Input System:

| Legacy (Old) | Input System (New) — Actions Approach | Input System (New) — Direct Approach |
|--------------|--------------------------------------|--------------------------------------|
| `Input.GetAxis("Horizontal")` | `moveAction.ReadValue<Vector2>().x` | `Keyboard.current.dKey.ReadValue() - Keyboard.current.aKey.ReadValue()` |
| `Input.GetButton("Fire1")` | `fireAction.IsPressed()` | `Mouse.current.leftButton.isPressed` |
| `Input.GetButtonDown("Jump")` | `jumpAction.WasPressedThisFrame()` | `Keyboard.current.spaceKey.wasPressedThisFrame` |
| `Input.GetButtonUp("Jump")` | `jumpAction.WasReleasedThisFrame()` | `Keyboard.current.spaceKey.wasReleasedThisFrame` |
| `Input.mousePosition` | `pointerAction.ReadValue<Vector2>()` | `Mouse.current.position.ReadValue()` |
| `Input.GetMouseButtonDown(0)` | `clickAction.WasPressedThisFrame()` | `Mouse.current.leftButton.wasPressedThisFrame` |
| `Input.GetKey(KeyCode.Space)` | `action.IsPressed()` | `Keyboard.current.spaceKey.isPressed` |
| `Input.touches` / `Input.touchCount` | Use `EnhancedTouchSupport` | `EnhancedTouch.Touch.activeTouches` |

Preprocessor defines for conditional compilation:
- `#if ENABLE_INPUT_SYSTEM` — new Input System is active
- `#if ENABLE_LEGACY_INPUT_MANAGER` — old Input Manager is active
- Both can be true when Active Input Handling is set to "Both"

Old API surface "Unity Input" resides in class `UnityEngine.Input`. Input System package API surface resides in root namespace `UnityEngine.InputSystem`.
The following exceptions exist and may be used regardless of Active Input Handling setting:
- `UnityEngine.Input.location`
- `UnityEngine.Input.stylusTouchSupported`
- `UnityEngine.Input.mousePresent`
- `UnityEngine.Input.multiTouchEnabled`

## Common Mistakes to Avoid

### 1. Using `Input.` Class with Input System
**Problem:** `Input.GetAxis`, `Input.GetButtonDown` etc. throw exceptions when only Input System (new) is active.
**Solution:** Use `InputSystem.actions.FindAction()` or direct device access (`Keyboard.current`, etc.).

### 2. Calling FindAction Every Frame
**Problem:** `InputSystem.actions.FindAction("Move")` in `Update()` is wasteful.
**Solution:** Cache the `InputAction` reference in `Start()` or `Awake()`.

### 3. Using InputSystem.actions with PlayerInput
**Problem:** `InputSystem.actions` is the singleton copy. PlayerInput creates private copies for device filtering.
**Solution:** Use `playerInput.actions` when working with PlayerInput.

### 4. Not Disabling Actions Before Modifying Bindings
**Problem:** Changing bindings while actions are enabled causes temporary disable/re-enable of all actions.
**Solution:** Disable the action or map, make changes, then re-enable.

### 5. Storing InputAction.CallbackContext
**Problem:** Context struct is only valid during the callback.
**Solution:** Read values during the callback; don't store the context for later use.

### 6. Wrong Action Type for UI
**Problem:** Using Value type for UI pointer actions causes only one device to drive input.
**Solution:** UI pointer actions (Point, Click, ScrollWheel, etc.) must be PassThrough.

### 7. Missing Control Schemes
**Problem:** Local multiplayer doesn't pair devices correctly.
**Solution:** Always create control schemes with required devices. PlayerInput uses these for automatic device pairing.

### 8. Forgetting to Dispose RebindingOperation
**Problem:** `PerformInteractiveRebinding()` allocates unmanaged memory.
**Solution:** Always call `Dispose()` on the `RebindingOperation` when done.

### 9. Not Saving Binding Overrides Separately
**Problem:** `overridePath` is not saved with `InputActionAsset.ToJson()`.
**Solution:** Use `SaveBindingOverridesAsJson()` / `LoadBindingOverridesFromJson()` and persist via PlayerPrefs or file.

### 10. Editing .inputactions JSON Directly
**Problem:** Manual JSON edits can corrupt the asset, break binding IDs, or lose data.
**Solution:** Always modify assets programmatically through the InputActionAsset API, run in a live Editor.

### 11. Searching ProjectSettings Files for Input Actions
**Problem:** Grepping or reading `ProjectSettings/ProjectSettings.asset` or other settings files to find the project-wide actions asset. The reference is stored via `EditorBuildSettings` config objects in binary format and is not searchable in text files. This always fails and wastes multiple tool calls.
**Solution:** Always use `InputSystem.actions` to check the current project-wide actions. See Section 1 "Check Project-Wide Actions" for the complete script.
