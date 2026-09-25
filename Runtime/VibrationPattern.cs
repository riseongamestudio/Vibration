using System;
using UnityEngine;

namespace RiseOn.Vibration {
    /// <summary>
    /// A vibration of your own: pulses played one after another, for when the three <see cref="VibrationLevel"/> are not enough.<br/>
    /// Build it once and keep it, since it only holds on to the pulse array it was given.
    /// </summary>
    [Serializable]
    public struct VibrationPattern {
        [SerializeField] private VibrationPulse[] pulses;

        internal VibrationPulse[] Pulses => pulses;

        /// <summary>Plays <paramref name="pulses"/> in order.</summary>
        public VibrationPattern(params VibrationPulse[] pulses) {
            this.pulses = pulses;
        }
    }
}
