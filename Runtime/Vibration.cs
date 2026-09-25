using System;

namespace RiseOn.Vibration {
    /// <summary>
    /// Plays short vibrations on Android and iOS, from a ready-made level down to a pattern of your own.<br/>
    /// Does nothing in the Editor, on other platforms and on devices without a vibrator.<br/>
    /// Main thread only.
    /// </summary>
    public static class Vibration {
        // Installed at startup by the code of the platform the game runs on; stay null everywhere else.
        private static Action<VibrationLevel>   playLevel;
        private static Action<VibrationPattern> playPattern;

        // Reused by the one-pulse overload; the platforms read it before Play returns and keep nothing.
        private static readonly VibrationPulse[] singlePulse = new VibrationPulse[1];

        /// <summary>
        /// Turns every vibration on or off, on by default.<br/>
        /// Not saved: set it from the game's own settings at startup.
        /// </summary>
        public static bool Enabled { get; set; } = true;

        /// <summary>
        /// Plays a ready-made tap of the given strength.<br/>
        /// Uses the system's own tap on iOS, so it feels native there.
        /// </summary>
        public static void Play(VibrationLevel level) {
            if (Enabled) playLevel?.Invoke(level);
        }

        /// <summary>One pulse of <paramref name="durationMs"/> milliseconds at <paramref name="intensity"/> (0 to 1).</summary>
        public static void Play(int durationMs, float intensity = 1f) {
            singlePulse[0] = new VibrationPulse(durationMs, intensity);
            Play(new VibrationPattern(singlePulse));
        }

        /// <summary>
        /// <paramref name="pulses"/> one after another.<br/>
        /// Allocates the array on each call; keep a <see cref="VibrationPattern"/> for vibrations played often.
        /// </summary>
        public static void Play(params VibrationPulse[] pulses) {
            Play(new VibrationPattern(pulses));
        }

        /// <summary>
        /// Plays <paramref name="pattern"/>: made in code once and kept, or set up in the Inspector.<br/>
        /// On iOS it plays through Core Haptics; iPhones without it (before the iPhone 8) get a system tap for each pulse instead.
        /// </summary>
        public static void Play(VibrationPattern pattern) {
            if (Enabled && pattern.Pulses is { Length: > 0 }) playPattern?.Invoke(pattern);
        }

        internal static void Install(Action<VibrationLevel> platformPlayLevel, Action<VibrationPattern> platformPlayPattern) {
            playLevel   = platformPlayLevel;
            playPattern = platformPlayPattern;
        }
    }
}
