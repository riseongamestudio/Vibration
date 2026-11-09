using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Android;

namespace RiseOn.Vibration.Android {
    /// <summary>
    /// Plays vibrations through the Android Vibrator: API 26 and later as waveforms with amplitudes, older versions from the timings alone.<br/>
    /// Every call to Java runs on a thread of its own, so the game never waits on the system's vibrator service.<br/>
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

        // The one vibration waiting for the thread: a newer one takes its place, as the older would only come late.
        private static readonly object gate = new();
        private static bool hasPending;
        private static int pendingLevel; // -1 for the waveform
        private static Waveform pendingWaveform;
        private static Thread thread;

        // Read on the main thread, before the thread first needs it.
        private static AndroidJavaObject activity;

        // The rest is the thread's alone.
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
            Post((int)level, default);
        }

        private static void PlayPattern(VibrationPattern pattern) {
            // Read now: the caller may reuse the pulses once Play returns.
            var waveform = Waveform.Of(pattern);
            if (waveform.Timings is null) return;

            Post(-1, waveform);
        }

        // The thread starts with the first vibration rather than at startup, when nothing guarantees the activity is ready.
        private static void Post(int level, Waveform waveform) {
            lock (gate) {
                hasPending      = true;
                pendingLevel    = level;
                pendingWaveform = waveform;
                Monitor.Pulse(gate);
            }

            if (thread is not null) return;

            activity = AndroidApplication.currentActivity;
            thread   = new Thread(Run) { IsBackground = true, Name = "RiseOn.Vibration" };
            thread.Start();
        }

        // Vibrator.vibrate waits on the system's vibrator service: 4 to 60 ms a call measured on a Samsung A30s
        // (Android 11), the first one with the setup over 150 ms.
        private static void Run() {
            AndroidJNI.AttachCurrentThread();

            while (true) {
                int level;
                Waveform waveform;
                lock (gate) {
                    while (!hasPending) Monitor.Wait(gate);

                    hasPending = false;
                    level      = pendingLevel;
                    waveform   = pendingWaveform;
                }

                if (!Setup()) continue;

                if (level >= 0) {
                    vibrator.Call("vibrate", levelArgs[level]);
                    continue;
                }

                var args = GetVibrateArgs(waveform);
                vibrator.Call("vibrate", args);
                // The effect has been handed to the system; drop our reference to it now rather than at the next GC.
                (args[0] as IDisposable)?.Dispose();
            }
        }

        private static bool Setup() {
            if (canVibrate is { } known) return known;

            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            var sdk = version.GetStatic<int>("SDK_INT");

            vibrator   = GetVibrator(sdk);
            canVibrate = vibrator is not null && vibrator.Call<bool>("hasVibrator");
            if (canVibrate is false) return false;

            if (sdk >= 26) effectClass = new AndroidJavaClass("android.os.VibrationEffect");
            levelArgs = Array.ConvertAll(levelPatterns, pattern => GetVibrateArgs(Waveform.Of(pattern)));
            return true;

            static AndroidJavaObject GetVibrator(int sdk) {
                // The activity is owned by Unity, so it isn't disposed here.
                if (sdk < 31) return activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

                using var manager = activity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager");
                return manager.Call<AndroidJavaObject>("getDefaultVibrator");
            }
        }

        private static object[] GetVibrateArgs(Waveform waveform) {
            return effectClass is null
                ? new object[] { waveform.Timings, -1 }
                : new object[] { effectClass.CallStatic<AndroidJavaObject>("createWaveform", waveform.Timings, waveform.Amplitudes, -1) };
        }

        // Timings alternate off and on in milliseconds, starting with off, and the -1 passed along with them means play once.
        // Amplitudes go from 1 to 255 and only count on devices with amplitude control.
        private readonly struct Waveform {
            public readonly long[] Timings; // Null when the pattern has nothing to feel
            public readonly int[] Amplitudes;

            private Waveform(long[] timings, int[] amplitudes) {
                Timings    = timings;
                Amplitudes = amplitudes;
            }

            public static Waveform Of(VibrationPattern pattern) {
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

                if (count is 0) return default;

                Array.Resize(ref timings, count);
                Array.Resize(ref amplitudes, count);
                return new Waveform(timings, amplitudes);
            }
        }
    }
}