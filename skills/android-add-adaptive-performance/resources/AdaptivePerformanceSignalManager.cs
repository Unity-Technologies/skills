using System;
using UnityEngine;
using UnityEngine.AdaptivePerformance;

namespace AdaptivePerformanceIntegration
{
    /// <summary>
    /// Game-agnostic Adaptive Performance signal handler.
    /// Reads thermal/performance state via the official API and reports the effective state index.
    /// 
    /// This MonoBehaviour polls the Adaptive Performance subsystem at a configurable interval,
    /// evaluates the current thermal warning level and temperature, and maps them to a simplified
    /// integer state index (0–3). Downstream systems (e.g., quality adapters) subscribe to
    /// <see cref="HardwareStateChangedEvent"/> to react to thermal changes without depending
    /// on the Adaptive Performance API directly.
    /// 
    /// Effective States:
    /// 0: Normal (Cool)              – No thermal concerns.
    /// 1: Elevated (Pre-warning)     – Temperature has risen past the preemptive threshold
    ///                                  but no official warning has been issued yet.
    /// 2: Throttling Imminent        – The subsystem reports <see cref="WarningLevel.ThrottlingImminent"/>.
    /// 3: Throttling                 – The subsystem reports <see cref="WarningLevel.Throttling"/>;
    ///                                  the device is actively reducing clock speeds.
    /// </summary>
    public class AdaptivePerformanceSignalManager : MonoBehaviour
    {
        /// <summary>
        /// Serializable policy block that controls how and when thermal state is evaluated.
        /// Exposed in the Inspector so designers can tune thresholds per-project.
        /// </summary>
        [Serializable]
        public class StatePolicy
        {
            /// <summary>
            /// Normalized temperature level (0–1) above which the manager reports state 1
            /// ("Elevated") even when no official thermal warning has been raised. This lets
            /// the game begin reducing load before the hardware signals a problem.
            /// </summary>
            [Tooltip("Temperature level (0-1) above which we report 'Elevated' (1) even if there is no official thermal warning.")]
            [Range(0f, 1f)]
            public float PreemptiveTemperatureThreshold = 0.5f;

            /// <summary>
            /// How often (in seconds) the manager re-evaluates thermal state in Update().
            /// Lower values give faster reactions but cost more CPU.
            /// </summary>
            [Header("Polling interval (seconds)")]
            [Min(0.1f)]
            public float PollIntervalSeconds = 1.0f;

            /// <summary>
            /// Master switch. When false, polling, evaluation, and event firing are all skipped.
            /// </summary>
            [Header("Enable/disable adaptation")]
            public bool Enabled = true;
        }

        /// <summary>
        /// Inspector-exposed policy controlling thresholds, polling rate, and the enable flag.
        /// </summary>
        [Header("Policy")]
        public StatePolicy Policy = new StatePolicy();

        /// <summary>
        /// When true, state transitions and subsystem acquisition are logged to the console
        /// via <see cref="Debug.Log"/>.
        /// </summary>
        [Header("Debug")]
        public bool VerboseLogging = true;

        /// <summary>
        /// Fired whenever the effective hardware state index (0–3) changes.
        /// Subscribers receive the new state index.
        /// </summary>
        public event Action<int> HardwareStateChangedEvent;

        /// <summary>
        /// Optional debug/telemetry event that carries the raw warning-level name and
        /// normalized temperature (0–1) each time the state transitions.
        /// </summary>
        public event Action<string, float> AdaptiveStateChangedEvent;

        /// <summary>
        /// The most recently computed effective hardware state index (0–3).
        /// Initialized to -1 to guarantee the first evaluation always triggers events.
        /// </summary>
        public int CurrentStateIndex { get; private set; } = -1;

        /// <summary>Cached reference to the Adaptive Performance subsystem instance.</summary>
        private IAdaptivePerformance _ap;

        /// <summary>Unscaled timestamp of the next scheduled poll.</summary>
        private float _nextPollTime;

        /// <summary>
        /// Guards against double-subscribing to the thermal event if
        /// <see cref="SubscribeThermalEvent"/> is called more than once.
        /// </summary>
        private bool _subscribedToThermalEvent;

        /// <summary>
        /// Marks this GameObject as persistent across scene loads so thermal monitoring
        /// is never interrupted by scene transitions.
        /// </summary>
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Attempts to acquire the Adaptive Performance subsystem on the first frame
        /// and schedules the initial poll.
        /// </summary>
        private void Start()
        {
            if (!Policy.Enabled) return;

            TryAcquireInstance();
            _nextPollTime = Time.unscaledTime + Policy.PollIntervalSeconds;
        }

        /// <summary>
        /// Cleans up the thermal event subscription when this component is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            UnsubscribeThermalEvent();
        }

        /// <summary>
        /// Per-frame update. If the subsystem has not been acquired yet, retries at the
        /// configured poll interval. Once acquired, evaluates thermal state on each poll tick.
        /// Uses <see cref="Time.unscaledTime"/> so polling is unaffected by time scale.
        /// </summary>
        private void Update()
        {
            if (!Policy.Enabled) return;

            // Subsystem not yet available – retry acquisition on the next poll tick.
            if (_ap == null || !_ap.Active)
            {
                if (Time.unscaledTime >= _nextPollTime)
                {
                    _nextPollTime = Time.unscaledTime + Policy.PollIntervalSeconds;
                    TryAcquireInstance();
                }
                return;
            }

            // Wait until the next scheduled poll.
            if (Time.unscaledTime < _nextPollTime) return;
            _nextPollTime = Time.unscaledTime + Policy.PollIntervalSeconds;

            EvaluateAndReport();
        }

        /// <summary>
        /// Fetches the singleton <see cref="IAdaptivePerformance"/> instance from
        /// <see cref="Holder.Instance"/>. If successful, subscribes to thermal events
        /// and performs an immediate evaluation so there is no gap until the first poll.
        /// </summary>
        private void TryAcquireInstance()
        {
            _ap = Holder.Instance;
            if (_ap == null || !_ap.Active) return;

            Log($"Adaptive Performance acquired. Active={_ap.Active}");
            SubscribeThermalEvent();
            EvaluateAndReport();
        }

        /// <summary>
        /// Subscribes to the subsystem's push-based <see cref="IThermalStatus.ThermalEvent"/>
        /// so the manager can react immediately when the device raises or clears a warning,
        /// rather than waiting for the next poll tick.
        /// </summary>
        private void SubscribeThermalEvent()
        {
            if (_subscribedToThermalEvent || _ap?.ThermalStatus == null) return;
            _ap.ThermalStatus.ThermalEvent += OnThermalEvent;
            _subscribedToThermalEvent = true;
        }

        /// <summary>
        /// Removes the thermal event subscription, preventing callbacks after this
        /// component has been destroyed or disabled.
        /// </summary>
        private void UnsubscribeThermalEvent()
        {
            if (!_subscribedToThermalEvent || _ap?.ThermalStatus == null) return;
            _ap.ThermalStatus.ThermalEvent -= OnThermalEvent;
            _subscribedToThermalEvent = false;
        }

        /// <summary>
        /// Callback invoked by the Adaptive Performance subsystem when thermal conditions
        /// change. Delegates to <see cref="EvaluateAndReport(ThermalMetrics)"/>.
        /// </summary>
        /// <param name="metrics">Latest thermal metrics snapshot from the subsystem.</param>
        private void OnThermalEvent(ThermalMetrics metrics)
        {
            if (!Policy.Enabled) return;
            EvaluateAndReport(metrics);
        }

        /// <summary>
        /// Convenience overload that pulls the latest <see cref="ThermalMetrics"/>
        /// from the cached subsystem reference and forwards to the evaluation logic.
        /// </summary>
        private void EvaluateAndReport()
        {
            if (_ap?.ThermalStatus == null) return;
            EvaluateAndReport(_ap.ThermalStatus.ThermalMetrics);
        }

        /// <summary>
        /// Core evaluation logic. Maps the subsystem's <see cref="WarningLevel"/> and
        /// normalized temperature to an integer state index (0–3).
        ///
        /// State mapping:
        ///   <see cref="WarningLevel.Throttling"/>          → 3
        ///   <see cref="WarningLevel.ThrottlingImminent"/>   → 2
        ///   <see cref="WarningLevel.NoWarning"/> with temp ≥ threshold → 1
        ///   Otherwise                                       → 0
        ///
        /// If the computed index differs from <see cref="CurrentStateIndex"/>, both
        /// <see cref="HardwareStateChangedEvent"/> and <see cref="AdaptiveStateChangedEvent"/>
        /// are fired.
        /// </summary>
        /// <param name="metrics">Thermal metrics snapshot to evaluate.</param>
        private void EvaluateAndReport(ThermalMetrics metrics)
        {
            int stateIndex = 0;

            switch (metrics.WarningLevel)
            {
                case WarningLevel.Throttling:
                    stateIndex = 3;
                    break;
                case WarningLevel.ThrottlingImminent:
                    stateIndex = 2;
                    break;
                case WarningLevel.NoWarning:
                default:
                    if (metrics.TemperatureLevel >= Policy.PreemptiveTemperatureThreshold)
                        stateIndex = 1;
                    break;
            }

            if (stateIndex != CurrentStateIndex)
            {
                CurrentStateIndex = stateIndex;
                Log($"Hardware State changed: {stateIndex} (warning={metrics.WarningLevel}, temp={metrics.TemperatureLevel:F2})");
                HardwareStateChangedEvent?.Invoke(CurrentStateIndex);
                AdaptiveStateChangedEvent?.Invoke(metrics.WarningLevel.ToString(), metrics.TemperatureLevel);
            }
        }

        /// <summary>
        /// Writes a timestamped debug message to the Unity console when
        /// <see cref="VerboseLogging"/> is enabled.
        /// </summary>
        /// <param name="msg">Message to log, automatically prefixed with "[AdaptivePerformance]".</param>
        private void Log(string msg)
        {
            if (VerboseLogging) Debug.Log($"[AdaptivePerformance] {msg}");
        }
    }
}
