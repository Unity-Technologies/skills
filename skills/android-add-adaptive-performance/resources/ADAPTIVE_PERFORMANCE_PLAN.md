# Adaptive Performance Signal Handler Skill Plan
Target: Unity >=6.0 (6000.0.0), Android only
Constraints:
- Do not make any subjective calls
- Follow the full implementation plan strictly
- All steps should be done by you, unless it is explicitly mentioned that the user should do it
- Do not install any packages
- No flicker detection
- Stop at each step that says to ask the user a question and ask the question. You should not get all the way to the end and ask a lot of questions at once. This overwhelms the user.
- Do not add the bootstrap GameObject before adding and writing all necessary code
- Use AdaptivePerformanceIntegration as the namespace for any code
- Do not create any placeholder files when creating folders

## Step 1: Gather minimum required information
Do not output code until all answers are collected.

1. Attempt to detect graphics pipeline (URP/HDRP/Built-in) but if there is any doubt or inablity to do so, ask the user directly.
2. Tier structure:
   - Default: 3 tiers (0..2) meaning Best/Medium/Low
   - Ask the user if they want to change the number of tiers and determine good names to map them to on your own.
3. Hardware condition reversal behavior:
   - Ask the user if they want to return to “Best” when conditions normalize or keep the game running at the lowest detected quality (sticky downgrade).

## Step 2 (Skip if not using URP) Create quality settings and URP Render Pipeline assets
- Create one "mobile_adaptive" and one "mobile_max" quality setting.
- Each one should be set to affect Android only
- For each of the mobile quality settings, create and set a matching URP Render Pipeline Asset in Assets/AdaptivePerformanceManager/Settings/ named "urp_mobile_adaptive" and "urp_mobile_max" respectively.
- Turn on "Use Enable Adaptive Performance" for the URP Render Pipeline asset "urp_mobile_adaptive".
- Turn off "Use Enable Adaptive Performance" for the URP Render Pipeline asset "urp_mobile_max".

## Step 3: Generate the “signal handling” script only
Add AdaptivePerformanceSignalManager from resources/AdaptivePerformanceSignalManager.cs to Assets/AdaptivePerformanceManager/ and adjust based on the decided tier structure

## Step 4: Write an integration the user can customize
Here are the frame rates that each display type supports so you will want to try and determine the Hz of the display to know which FPS numbers you will be able to drop to.
60 Hz displays: 60 FPS, 30 FPS
90 Hz displays: 90 FPS, 45 FPS, 30 FPS
120 Hz displays: 120 FPS, 60 FPS, 40 FPS, 30 FPS

Include all available scalers. Important: Do not skip any scaler.
Warn the user with a code comment in AdaptiveQualityAdapter if any particular scaler may negatively affect gameplay or high visual impact.

For all pipelines:
General performance scalers (Do not skip ANY of these under any circumstances):
General scalers	Min Scale	Max Scale	Max Level	Visual Impact	Target	Setting scaled
AdaptiveLOD	0.4	1	3	High	GPU	QualitySettings.lodBias
AdaptiveResolution	0.5	1	9	Low	GPU/FillRate	AdaptivePerformanceRenderSettings.RenderScaleMultiplier and AdaptivePerformanceRenderSettings.ScalableBuffers
AdaptiveFramerate	15	60	45	High	CPU/GPU/FillRate	Application.targetFrameRate
AdaptiveViewDistance	50	1,000	40	High	GPU	Camera.main.farClipPlane
AdaptivePhysics	0.5	1	5	Low	CPU	Time.fixedDeltaTime

Additional scalers for URP pipelines only:
Universal Render Pipeline (URP) scalers (Do not skip ANY of these under any circumstances)
URP scalers	Min Scale	Max Scale	Max Level	Visual Impact	Target	Setting scaled
AdaptiveBatching	0	1	1	Medium	CPU	AdaptivePerformanceRenderSettings.SkipDynamicBatching
AdaptiveLUT	0	1	1	Medium	CPU/GPU	AdaptivePerformanceRenderSettings.LutBias
AdaptiveMSAA	0	1	2	Medium	GPU/FillRate	AdaptivePerformanceRenderSettings.AntiAliasingQualityBias
AdaptiveShadowCascade	0	1	2	Medium	CPU/GPU	AdaptivePerformanceRenderSettings.MainLightShadowCascadesCountBias
AdaptiveShadowDistance	0.15	1	3	Low	GPU	AdaptivePerformanceRenderSettings.MaxShadowDistanceMultiplier
AdaptiveShadowQuality	0	1	3	High	CPU/GPU	AdaptivePerformanceRenderSettings.ShadowQualityBias
AdaptiveShadowmapResolution	0.15	1	3	Low	GPU	AdaptivePerformanceRenderSettings.MainLightShadowmapResolutionMultiplier
AdaptiveSorting	0	1	1	Medium	CPU	AdaptivePerformanceRenderSettings.SkipFrontToBackSorting
AdaptiveTransparency	0	1	1	High	GPU	AdaptivePerformanceRenderSettings.SkipTransparentObjects
AdaptiveDecals	0.01	1	20	Medium	GPU	AdaptivePerformanceRenderSettings.DecalsDrawDistance

Add the example adapter script from resources/AdaptiveQualityAdapter.cs and place it in Assets/AdaptivePerformanceManager/. Add code comments above any lines for scalers that cause high visual impact (but leave the code intact).

## Step 5: Add bootstrap GameObject
- Determine what scene the "AdaptivePerformanceSignalManager" bootstrap GameObject will be added to
- Add the "AdaptivePerformanceSignalManager" bootstrap GameObject to the scene
- Add the AdaptivePerformanceSignalManager and AdaptiveQualityAdapter components to the AdaptivePerformanceSignalManager GameObject and configure them
- Save the scene

## Step 6: Produce a final checklist
- Include a checklist of what was done to the project and what steps the user should do on their own.
- Ask the user to go to Project Settings -> Adaptive Performance and check the "Enable Adaptive Performance" checkbox if it is not checked.
- Ask the user to check the "Android Provider" box in the Providers section in the Adaptive Performance settings.
- If new Quality settings were added, encourage the user to adjust the settings for each one and to check and adjust the URP Render Pipeline assets associated with each.
	- The URP Render Pipeline asset for mobile_adaptive should have Enable Adaptive Performance turned on so ask the user to check.
	- The URP Render Pipeline asset for mobile_max should have Enable Adaptive Performance turned off on so ask the user to check.
- Give a brief explanation of how "AdaptivePerformanceSignalManager.cs" is the main script they would edit to expand or change what was done.
- Let the user know they can also implement AdaptiveLayerCulling via Camera.main.layerCullDistances but it takes detailed. setup from the user
- Another thing that isn't really controlled in any of the settings we've talked about above is the postprocessing stack, the VolumeProfiles defined in Volumes in scene. Tell them this is something they can implement on their own for even better adaptive performance.