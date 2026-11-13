# Starter Assets XR Origin Rig — Structure Reference

## Table of Contents

- [Hierarchy](#hierarchy)
- [Root Components](#root-components)
- [Controller Components](#controller-components)
- [Locomotion Providers](#locomotion-providers)
- [How ControllerInputActionManager Routes the Thumbsticks](#how-controllerinputactionmanager-routes-the-thumbsticks)
- [Turning Teleport Off](#turning-teleport-off)
- [Hand Tracking Rig](#hand-tracking-rig)

## Hierarchy

```
XR Origin (XR Rig)
├── Camera Offset
│   ├── Main Camera
│   ├── Left Controller
│   │   ├── Poke Interactor
│   │   ├── Near-Far Interactor
│   │   └── Teleport Interactor
│   └── Right Controller
│       ├── Poke Interactor
│       ├── Near-Far Interactor
│       └── Teleport Interactor
└── Locomotion
    ├── Turn          (SnapTurnProvider, ContinuousTurnProvider)
    ├── Move          (DynamicMoveProvider)
    ├── Grab Move     (GrabMoveProvider, TwoHandedGrabMoveProvider) — inactive by default
    ├── Teleportation (TeleportationProvider)
    ├── Climb         (ClimbProvider, ClimbTeleportInteractor)
    ├── Gravity       (GravityProvider)
    └── Jump          (JumpProvider)
```

## Root Components

| Component | Role |
|---|---|
| XROrigin | Tracking origin; references the camera and Camera Offset |
| CharacterController | Collision for locomotion |
| InputActionManager | Enables the XRI default input actions |
| XRInputModalityManager | Switches between controllers and hands; deactivates controllers that are not tracked |

## Controller Components

| Component | Role |
|---|---|
| ControllerInputActionManager (sample script) | Routes the thumbstick to move, teleport, or turn; toggles the Teleport Interactor |
| XRInteractionGroup | Coordinates the Near-Far and Poke interactors |
| NearFarInteractor (child) | Near grab and far pointing, including UI by ray. Near and far casting can be turned off separately |
| XRPokeInteractor (child) | Poking buttons and UI |
| XRRayInteractor (child "Teleport Interactor") | Teleport aiming only |

## Locomotion Providers

All providers go through the LocomotionMediator and XRBodyTransformer on the Locomotion object. Keep those enabled even if every provider is disabled.

## How ControllerInputActionManager Routes the Thumbsticks

At runtime it enables input actions per controller:

| Action | Enabled when |
|---|---|
| Move | `m_SmoothMotionEnabled` = true |
| Teleport mode / cancel | `m_SmoothMotionEnabled` = false |
| Continuous turn | `m_SmoothMotionEnabled` = false and `m_SmoothTurnEnabled` = true |
| Snap turn | `m_SmoothMotionEnabled` = false and `m_SmoothTurnEnabled` = false |

So a controller is either a **move** stick (move and strafe, no turning) or a **teleport + turn** stick. Prefab default: left = move, right = teleport + snap turn.

## Turning Teleport Off

Disabling the Teleport Interactor or the TeleportationProvider is not enough on its own. When the teleport input fires, `ControllerInputActionManager` re-activates the Teleport Interactor and hides the Near-Far Interactor.

To turn teleport off on a controller that is a teleport + turn stick:

1. Clear `m_TeleportMode` and `m_TeleportModeCancel` on its `ControllerInputActionManager` (set them to `null`). The teleport input never fires; turning still works.
2. Disable the `TeleportationProvider`. Climb teleport (`ClimbTeleportInteractor`) also goes through it.

A move stick (`m_SmoothMotionEnabled` = true) already has its teleport input disabled.

## Hand Tracking Rig

`XR Origin Hands (XR Rig)` (Hands Interaction Demo sample) contains the controller rig above plus a **Left Hand** and **Right Hand**, each with a Poke Interactor and a Near-Far Interactor, and a **Hands Smoothing Post Processor**. `XRInputModalityManager` switches between the controllers and hands automatically.

On each hand, `PokeGestureDetector` events set the hand's `NearFarInteractor.enableFarCasting` off when a poke gesture starts and back on when it ends, and `MetaSystemGestureDetector` re-activates the hand's Poke Interactor GameObject whenever the hand activates.
