using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.AdaptivePerformance;

namespace AdaptivePerformanceIntegration
{
    /// <summary>
    /// Dynamically adjusts visual quality settings in response to hardware thermal/performance
    /// signals relayed by <see cref="AdaptivePerformanceSignalManager"/>.
    /// 
    /// The adapter maps discrete hardware states (Normal, Elevated, Throttling Imminent, Throttling)
    /// to designer-defined quality tiers, then applies both general Unity quality knobs and
    /// (optionally) URP-specific <see cref="AdaptivePerformanceRenderSettings"/> scalers for each tier.
    /// 
    /// An optional "sticky downgrade" policy ensures that once the device has been stressed,
    /// quality never climbs back up during the session—useful for preventing oscillation on
    /// devices that hover near a thermal boundary.
    /// </summary>
    public class AdaptiveQualityAdapter : MonoBehaviour
    {
        /// <summary>
        /// Defines the full set of rendering parameters for a single quality tier.
        /// </summary>
        [System.Serializable]
        public struct QualityTierSettings
        {
            public string Name;

            // --- General Scalers ---

            [Header("General Scalers")]

            /// <summary>Multiplier layered on top of the base render scale.</summary>
            public float RenderScaleMultiplier;

            /// <summary>Divisor applied to the device refresh rate to determine target FPS (e.g. 1, 2, 4).</summary>
            public int FrameRateDivisor;

            /// <summary>LOD bias multiplier—lower values force lower-detail meshes earlier.</summary>
            public float LODBias;

            /// <summary>Physics step interval; larger values reduce physics CPU cost at the expense of accuracy.</summary>
            public float FixedDeltaTime;

            /// <summary>Main camera far clip plane; reducing it culls distant geometry.</summary>
            public float FarClipPlane;

            // --- URP Adaptive Performance Scalers (Note: Only applies if using URP) ---

            [Header("URP Scalers (Inactive in Built-in)")]

            /// <summary>Multiplier for the maximum shadow draw distance inside URP.</summary>
            public float MaxShadowDistanceMultiplier;

            /// <summary>Multiplier for the main directional light's shadow map resolution.</summary>
            public float MainLightShadowmapResolutionMultiplier;

            /// <summary>Bias added to the shadow cascade count (negative values reduce cascades).</summary>
            public int ShadowCascadesBias;

            /// <summary>Bias added to the anti-aliasing quality level (negative values lower AA quality).</summary>
            public int AAQualityBias;

            /// <summary>Bias added to the shadow quality level (negative values lower shadow fidelity).</summary>
            public int ShadowQualityBias;

            /// <summary>When true, dynamic batching is skipped to save CPU overhead.</summary>
            public bool SkipDynamicBatching;

            /// <summary>When true, transparent objects are not rendered—a significant GPU savings.</summary>
            public bool SkipTransparentObjects;

            /// <summary>When true, front-to-back sorting is skipped to reduce CPU sort time.</summary>
            public bool SkipFrontToBackSorting;

            /// <summary>Draw distance multiplier for decal projectors.</summary>
            public float DecalsDrawDistance;

            /// <summary>Bias for the color grading LUT resolution (lower = faster, less accurate color).</summary>
            public float LutBias;
        }

        [Header("Components")]
        /// <summary>
        /// Reference to the signal manager that translates raw Adaptive Performance data
        /// into actionable hardware-state events this adapter subscribes to.
        /// </summary>
        public AdaptivePerformanceSignalManager SignalManager;

        [Header("Settings")]
        /// <summary>
        /// When enabled, the adapter only ever moves to a *lower* quality tier during the
        /// session—it will never return to a higher one even if thermal conditions improve.
        /// This prevents visual "popping" on devices that repeatedly cross a thermal boundary.
        /// </summary>
        [Tooltip("If true, once the game quality drops, it never returns to a higher tier.")]
        public bool StickyDowngrade = true;

        /// <summary>
        /// Ordered array of quality tiers from highest fidelity (index 0) to lowest.
        /// </summary>
        public QualityTierSettings[] Tiers = new QualityTierSettings[]
        {
            new QualityTierSettings
            {
                Name = "Best",
                RenderScaleMultiplier = 1.0f,
                FrameRateDivisor = 1,
                LODBias = 2.0f,
                FixedDeltaTime = 0.0166f,
                FarClipPlane = 1000f,
                MaxShadowDistanceMultiplier = 1.0f,
                MainLightShadowmapResolutionMultiplier = 1.0f,
                ShadowCascadesBias = 0,
                AAQualityBias = 0,
                ShadowQualityBias = 0,
                SkipDynamicBatching = false,
                SkipTransparentObjects = false,
                SkipFrontToBackSorting = false,
                DecalsDrawDistance = 1.0f,
                LutBias = 1.0f
            },
            new QualityTierSettings
            {
                Name = "Medium",
                RenderScaleMultiplier = 0.85f,
                FrameRateDivisor = 2,
                LODBias = 1.5f,
                FixedDeltaTime = 0.02f,
                FarClipPlane = 800f,
                MaxShadowDistanceMultiplier = 0.75f,
                MainLightShadowmapResolutionMultiplier = 0.75f,
                ShadowCascadesBias = -1,
                AAQualityBias = -1,
                ShadowQualityBias = -1,
                SkipDynamicBatching = false,
                SkipTransparentObjects = false,
                SkipFrontToBackSorting = false,
                DecalsDrawDistance = 0.75f,
                LutBias = 0.75f
            },
            new QualityTierSettings
            {
                Name = "Low",
                RenderScaleMultiplier = 0.7f,
                FrameRateDivisor = 4,
                LODBias = 1.0f,
                FixedDeltaTime = 0.0333f,
                FarClipPlane = 500f,
                MaxShadowDistanceMultiplier = 0.5f,
                MainLightShadowmapResolutionMultiplier = 0.5f,
                ShadowCascadesBias = -2,
                AAQualityBias = -2,
                ShadowQualityBias = -2,
                SkipDynamicBatching = true,
                SkipTransparentObjects = true,
                SkipFrontToBackSorting = true,
                DecalsDrawDistance = 0.5f,
                LutBias = 0.5f
            }
        };

        [Header("Hardware State Mapping")]
        public HardwareStateMapping[] StateMappings = new HardwareStateMapping[]
        {
            new HardwareStateMapping { StateLabel = "Normal", TargetTierName = "Best" },
            new HardwareStateMapping { StateLabel = "Elevated", TargetTierName = "Medium" },
            new HardwareStateMapping { StateLabel = "Throttling Imminent", TargetTierName = "Low" },
            new HardwareStateMapping { StateLabel = "Throttling", TargetTierName = "Low" }
        };

        [System.Serializable]
        public struct HardwareStateMapping
        {
            public string StateLabel;
            public string TargetTierName;
        }

        [Header("Runtime Info (Read Only)")]
        public int MaxTierIndexReached = 0;
        public int CurrentTierIndex = 0;
        public int AppliedTierIndex = 0;

        private bool _urpEnabled = false;

        private void OnEnable()
        {
            if (SignalManager != null)
                SignalManager.HardwareStateChangedEvent += OnHardwareStateChanged;
        }

        private void OnDisable()
        {
            if (SignalManager != null)
                SignalManager.HardwareStateChangedEvent -= OnHardwareStateChanged;
        }

        private void Start()
        {
            // Detect URP without direct reference to its types to avoid compilation errors in Built-in projects
            _urpEnabled = GraphicsSettings.currentRenderPipeline != null &&
                          GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("Universal");

            Debug.Log("[AdaptiveQualityAdapter] Initialized and monitoring hardware signals.");
        }

        private void OnHardwareStateChanged(int stateIndex)
        {
            int targetTierIndex = ResolveStateToTierIndex(stateIndex);
            CurrentTierIndex = targetTierIndex;

            if (StickyDowngrade)
            {
                if (targetTierIndex > MaxTierIndexReached)
                {
                    MaxTierIndexReached = targetTierIndex;
                    ApplyQuality(targetTierIndex);
                    AppliedTierIndex = targetTierIndex;
                }
                else
                {
                    Debug.Log($"[AdaptiveQualityAdapter] Sticky policy: Ignoring return to tier index {targetTierIndex}. Current Max Tier index is {MaxTierIndexReached}.");
                }
            }
            else
            {
                ApplyQuality(targetTierIndex);
                AppliedTierIndex = targetTierIndex;
            }
        }

        private int ResolveStateToTierIndex(int stateIndex)
        {
            string targetName = (StateMappings != null && stateIndex < StateMappings.Length)
                ? StateMappings[stateIndex].TargetTierName
                : string.Empty;

            for (int i = 0; i < Tiers.Length; i++)
            {
                if (Tiers[i].Name == targetName) return i;
            }

            return Mathf.Max(0, Tiers.Length - 1);
        }

        private void ApplyQuality(int tierIndex)
        {
            if (tierIndex < 0 || tierIndex >= Tiers.Length) return;

            QualityTierSettings settings = Tiers[tierIndex];
            Debug.Log($"[AdaptiveQualityAdapter] Applied Quality Tier {tierIndex} ({settings.Name})");

            // --- General Scalers ---
            Application.targetFrameRate = CalculateTargetFrameRate(settings.FrameRateDivisor);
            QualitySettings.lodBias = settings.LODBias;
            Time.fixedDeltaTime = settings.FixedDeltaTime;

            if (Camera.main != null)
                Camera.main.farClipPlane = settings.FarClipPlane;

            // AdaptivePerformanceRenderSettings applies generally if the package is present
            AdaptivePerformanceRenderSettings.RenderScaleMultiplier = settings.RenderScaleMultiplier;

            if (!_urpEnabled) return;

            // --- URP Specific Scalers (Only if URP is active) ---
            AdaptivePerformanceRenderSettings.MaxShadowDistanceMultiplier = settings.MaxShadowDistanceMultiplier;
            AdaptivePerformanceRenderSettings.MainLightShadowmapResolutionMultiplier = settings.MainLightShadowmapResolutionMultiplier;
            AdaptivePerformanceRenderSettings.MainLightShadowCascadesCountBias = settings.ShadowCascadesBias;
            AdaptivePerformanceRenderSettings.AntiAliasingQualityBias = settings.AAQualityBias;
            AdaptivePerformanceRenderSettings.ShadowQualityBias = settings.ShadowQualityBias;
            AdaptivePerformanceRenderSettings.SkipDynamicBatching = settings.SkipDynamicBatching;
            AdaptivePerformanceRenderSettings.SkipTransparentObjects = settings.SkipTransparentObjects;
            AdaptivePerformanceRenderSettings.SkipFrontToBackSorting = settings.SkipFrontToBackSorting;
            AdaptivePerformanceRenderSettings.DecalsDrawDistance = settings.DecalsDrawDistance;
            AdaptivePerformanceRenderSettings.LutBias = settings.LutBias;
        }

        private int CalculateTargetFrameRate(int divisor)
        {
            // Unity 6+ uses RefreshRateRatio for precise display frequencies
            double refreshRate = Screen.currentResolution.refreshRateRatio.value;
            
            // On some platforms or when not yet available, fallback to a sensible default
            if (refreshRate <= 0) refreshRate = 60.0;

            // Enforce "progressive halving" (1, 2, 4, 8...) by snapping the divisor to the next power of two.
            // This ensures target FPS aligns with display VSync intervals for smooth pacing.
            int effectiveDivisor = Mathf.NextPowerOfTwo(Mathf.Max(1, divisor));

            int target = Mathf.RoundToInt((float)(refreshRate / effectiveDivisor));
            
            // Requirement: Frame rate can't go below 30.
            return Mathf.Max(30, target);
        }
    }
}
