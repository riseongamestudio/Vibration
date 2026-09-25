using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RiseOn.Vibration {
    /// <summary>
    /// One pulse of a <see cref="VibrationPattern"/>: a pause, then the vibrator on for a while at some strength.<br/>
    /// The layout crosses into the native iOS code as is, so keep the fields, their types and their order.
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct VibrationPulse {
        [SerializeField, Min(0)]         private int   delayMs;
        [SerializeField, Min(0)]         private int   durationMs;
        [SerializeField, Range(0f, 1f)] private float intensity;

        /// <summary>Pause before the pulse, in milliseconds.</summary>
        public int DelayMs => delayMs;

        /// <summary>How long the pulse vibrates, in milliseconds.</summary>
        public int DurationMs => durationMs;

        /// <summary>
        /// Strength from 0 to 1; a pulse at 0 is just a longer pause.<br/>
        /// Only Android devices with amplitude control and iPhones with Core Haptics follow it, others vibrate at full strength.
        /// </summary>
        public float Intensity => intensity;

        /// <summary>A pulse of <paramref name="durationMs"/> at <paramref name="intensity"/>, after a pause of <paramref name="delayMs"/>.</summary>
        public VibrationPulse(int durationMs, float intensity = 1f, int delayMs = 0) {
            this.delayMs    = Math.Max(0, delayMs);
            this.durationMs = Math.Max(0, durationMs);
            this.intensity  = Mathf.Clamp01(intensity);
        }
    }
}
