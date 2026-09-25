using System;
using UnityEngine;
using UnityEngine.Android;

namespace RiseOn.Vibration.Android {
    /// <summary>
    /// Plays vibrations through the Android Vibrator: API 26 and later as waveforms with amplitudes, older versions from the timings alone.<br/>
    /// Lives in its own assembly like the iOS code, so other platforms' builds don't carry it.<br/>
    /// The assembly also compiles in the Editor, so the code is checked there, but only installs itself on Android.
    /// </summary>
    internal static class AndroidVibration {
        // The levels as pulses. Many devices can't vary the amplitude and run every pulse at full strength,
        // so the length alone has to tell the levels apart there. The lengths were picked by feel on such a device,
        // where anything under about 30 ms can't be felt at all.
        private static readonly VibrationPattern[] levelPatterns = {
            new(new VibrationPulse(40, 0.5f))
          , new(new VibrationPulse(55, 0.75f))
          , new(new VibrationPulse(80, 1f))
        };

        // Null until the first vibration asks, then whether the device has a vibrator.
        private static bool? canVibrate;
        private static AndroidJavaObject vibrator;

        // Null below API 26, where vibrate only takes the timings.
        private static AndroidJavaClass effectClass;

        // The arguments of Vibrator.vibrate for each level, indexed by its value.
        private static object[][] levelArgs;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install() {
            if (Application.platform is not RuntimePlatform.Android) return;

            Vibration.Install(PlayLevel, PlayPattern);
        }

        private static void PlayLevel(VibrationLevel level) {
            if (!Setup()) return;

            vibrator.Call("vibrate", levelArgs[(int)level]);
        }

        private static void PlayPattern(VibrationPattern pattern) {
            if (!Setup()) return;

            var args = GetVibrateArgs(pattern);
            if (args is null) return;

            vibrator.Call("vibrate", args);
            // The effect has been handed to the system; drop our reference to it now rather than at the next GC.
            (args[0] as IDisposable)?.Dispose();
        }

        // Talks to Java on the first vibration rather than at startup, when nothing guarantees the activity is ready.
        private static bool Setup() {
            if (canVibrate is { } known) return known;

            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            var sdk = version.GetStatic<int>("SDK_INT");

            vibrator   = GetVibrator(sdk);
            canVibrate = vibrator is not null && vibrator.Call<bool>("hasVibrator");
            if (canVibrate is false) return false;

            if (sdk >= 26) effectClass = new AndroidJavaClass("android.os.VibrationEffect");
            levelArgs = Array.ConvertAll(levelPatterns, GetVibrateArgs);
            return true;

            static AndroidJavaObject GetVibrator(int sdk) {
                // Owned by Unity, so it isn't disposed here.
                var activity = AndroidApplication.currentActivity;
                if (sdk < 31) return activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

                using var manager = activity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager");
                return manager.Call<AndroidJavaObject>("getDefaultVibrator");
            }
        }

        // Timings alternate off and on in milliseconds, starting with off, and the -1 passed along means play once.
        // Amplitudes go from 1 to 255 and only count on devices with amplitude control.
        // Returns null when the pattern has nothing to feel.
        private static object[] GetVibrateArgs(VibrationPattern pattern) {
            var pulses     = pattern.Pulses;
            var timings    = new long[pulses.Length * 2];
            var amplitudes = new int[pulses.Length * 2];
            var count      = 0;
            var pause      = 0L;
            foreach (var pulse in pulses) {
                var amplitude = Mathf.RoundToInt(Mathf.Clamp01(pulse.Intensity) * 255);
                pause += Math.Max(0, pulse.DelayMs);
                if (amplitude is 0 || pulse.DurationMs <= 0) {
                    // A silent pulse is just a longer pause.
                    pause += Math.Max(0, pulse.DurationMs);
                    continue;
                }

                timings[count]        = pause;
                timings[count + 1]    = pulse.DurationMs;
                amplitudes[count + 1] = amplitude;
                count += 2;
                pause =  0;
            }

            if (count is 0) return null;

            Array.Resize(ref timings, count);
            Array.Resize(ref amplitudes, count);
            return effectClass is null
                ? new object[] { timings, -1 }
                : new object[] { effectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1) };
        }
    }
}
