using System.Runtime.InteropServices;
using UnityEngine;

namespace RiseOn.Vibration.IOS {
    /// <summary>
    /// Plays vibrations through ROVibration.mm: levels with the UIKit impact generator, patterns with Core Haptics.<br/>
    /// Lives in its own assembly because IL2CPP links __Internal functions at build time on iOS and WebGL, and only iOS builds carry the native file.<br/>
    /// The assembly also compiles in the Editor, so the code is checked there, but only installs itself on iOS.
    /// </summary>
    internal static class IOSVibration {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install() {
            if (Application.platform is not RuntimePlatform.IPhonePlayer) return;

            Vibration.Install(PlayLevel, PlayPattern);
        }

        private static void PlayLevel(VibrationLevel level) {
            ROVibrationPlayLevel((int)level);
        }

        private static void PlayPattern(VibrationPattern pattern) {
            ROVibrationPlayPattern(pattern.Pulses, pattern.Pulses.Length);
        }

        [DllImport("__Internal")]
        private static extern void ROVibrationPlayLevel(int level);

        // VibrationPulse is blittable, so the array goes over as a pointer to its elements, without a copy.
        [DllImport("__Internal")]
        private static extern void ROVibrationPlayPattern(VibrationPulse[] pulses, int count);
    }
}