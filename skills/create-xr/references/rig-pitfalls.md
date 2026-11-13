# Rig pitfalls

Read before starting the rig procedure. Each one cost a wasted iteration before.

## Correct vs wrong

| Context | CORRECT | WRONG (do not use) |
|---|---|---|
| Rig creation | Place the Starter Assets `XR Origin (XR Rig)` prefab | Build a rig from individual components |
| Grab and point interaction | `NearFarInteractor` (near and far casting) | `XRDirectInteractor`, `XRRayInteractor` |
| Poke | `XRPokeInteractor` | `NearFarInteractor` |
| Teleport aiming | `XRRayInteractor` on the Teleport Interactor child | `NearFarInteractor` |
| Rig root component | `Unity.XR.CoreUtils.XROrigin` (com.unity.xr.core-utils) | `XRRig` (deprecated) |
| Sample import API | `UnityEditor.PackageManager.UI.Sample` | `UnityEditor.PackageManager.Sample` |
| Interaction manager | `UnityEngine.XR.Interaction.Toolkit.XRInteractionManager` | `...Toolkit.Interactions.XRInteractionManager` |
| Existing custom rig | Ask: replace, or use a new scene | Try to fix or complete it |

## Rules

1. **Match sample names exactly.** Use `displayName == "Starter Assets"`, never `.Contains()` — "AR Starter Assets" also contains it.

2. **Two objects are named "Main Camera".** The scene template's camera is at the scene root; the rig has its own child "Main Camera". Target the template camera by hierarchy path `/Main Camera` or by checking it has no parent — a name-only lookup can return the rig's camera.

3. **Disabling the Teleport Interactor alone does not turn teleport off.** `ControllerInputActionManager` re-activates it when the teleport input fires. Clear `m_TeleportMode` and `m_TeleportModeCancel` instead — see rig-structure.md.

4. **A move stick cannot turn.** On a controller with `m_SmoothMotionEnabled` = true, the turn and snap-turn actions are disabled. Turning only works on controllers set to teleport + turn.

5. **`ControllerInputActionManager` is a sample script, not a package type.** Do not reference its type in `eval` code — match it by `GetType().Name` and read or write its fields with `SerializedObject`.

6. **Starter Assets requires TMP Essentials.** Without them a project validation error remains. Import them with `TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false)` — the validation rule's own fix opens a window and does nothing from the CLI.

7. **A freshly started Editor may have no scene open.** `SceneManager.GetActiveScene().path` is empty and scene queries return nothing. Open a scene before placing the rig.

8. **Hand tracking only: `XRInputModalityManager` alone does not hide the controllers.** It still shows a controller the user picks up, and that controller's sticks still move the user. Clear its controller references, deactivate the controllers, and set the providers' controller inputs to Unused. In other setups, controllers are inactive in Play mode when no controller is tracked — that is expected.

9. **In edit mode the Teleport Interactor is active.** `ControllerInputActionManager` deactivates it when Play mode starts. Do not treat it as an error in edit-mode checks.

10. **Turning far casting off on a hand does not hold on its own.** In the hands rig, `PokeGestureDetector` events turn `enableFarCasting` off when a poke gesture starts and back on when it ends. Disable the hand's `PokeGestureDetector` too.

11. **Deactivating a hand's Poke Interactor GameObject does not turn hand poke off.** `MetaSystemGestureDetector` on the hand calls `SetActive(true)` on it whenever the hand activates. Disable the `XRPokeInteractor` component instead.

12. **Never modify an existing rig, even when the user asks you to change it.** Its structure and earlier overrides are unknown, so changing it can leave old settings behind (e.g., disabled turn providers that break turning). Ask replace (deactivate it, place a new rig) or new scene. A disabled component is not proof of the prefab default.

13. **Don't activate the hand mesh objects in the hands rig.** `LeftHandQuestVisual` / `RightHandQuestVisual` (and the Android XR ones) are inactive in the prefab on purpose: the rig shows the right hand meshes for the platform at runtime.

14. **Use the XR Interaction Simulator, never the XR Device Simulator.** XRI ships both samples; the Device Simulator is the old one. Do not turn on "Use Classic XR Device Simulator" either.

*Add only rules where the error message alone does not point to the fix.*
