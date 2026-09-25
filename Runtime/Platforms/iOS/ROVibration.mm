#import <UIKit/UIKit.h>
#import <CoreHaptics/CoreHaptics.h>

// RiseOn.Vibration.VibrationPulse, field for field.
typedef struct {
    int32_t delayMs;
    int32_t durationMs;
    float intensity;
} ROVibrationPulse;

// Created on first use and kept, so repeated taps reuse them.
static UIImpactFeedbackGenerator* ROImpactGenerator(UIImpactFeedbackStyle style) {
    static UIImpactFeedbackGenerator* generators[UIImpactFeedbackStyleHeavy + 1];
    if (generators[style] == nil) {
        generators[style] = [[UIImpactFeedbackGenerator alloc] initWithStyle:style];
    }
    return generators[style];
}

// VibrationLevel 0, 1, 2 are UIImpactFeedbackStyleLight, Medium, Heavy.
extern "C" void ROVibration_PlayLevel(int32_t level) {
    if (level < UIImpactFeedbackStyleLight || level > UIImpactFeedbackStyleHeavy) return;

    [ROImpactGenerator((UIImpactFeedbackStyle)level) impactOccurred];
}

// Nil on hardware without Core Haptics: iPhones before the iPhone 8, and iPads, which have no haptics at all.
static CHHapticEngine* ROHapticEngine(void) {
    static CHHapticEngine* engine;
    static BOOL unavailable;
    if (engine != nil || unavailable) return engine;

    if (!CHHapticEngine.capabilitiesForHardware.supportsHaptics) {
        unavailable = YES;
        return nil;
    }

    NSError* error = nil;
    engine = [[CHHapticEngine alloc] initAndReturnError:&error];
    if (engine == nil) {
        NSLog(@"[ROVibration] Core Haptics engine unavailable: %@", error);
        unavailable = YES;
        return nil;
    }

    // The system stops an idle engine; every play starts it again.
    engine.autoShutdownEnabled = YES;
    // After the haptic server resets, the engine has to be started again.
    engine.resetHandler = ^{
        [engine startAndReturnError:nil];
    };
    return engine;
}

// Without Core Haptics a pulse can't last, so each one becomes a system tap at the moment it starts.
static void ROPlayPulsesAsTaps(const ROVibrationPulse* pulses, int32_t count) {
    UIImpactFeedbackGenerator* generator = ROImpactGenerator(UIImpactFeedbackStyleMedium);
    NSTimeInterval time = 0;
    for (int32_t i = 0; i < count; i++) {
        time += MAX(0, pulses[i].delayMs) / 1000.0;
        float intensity = MIN(MAX(pulses[i].intensity, 0.0f), 1.0f);
        if (pulses[i].durationMs > 0 && intensity > 0) {
            dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(time * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
                [generator impactOccurredWithIntensity:intensity];
            });
        }
        time += MAX(0, pulses[i].durationMs) / 1000.0;
    }
}

extern "C" void ROVibration_PlayPattern(const ROVibrationPulse* pulses, int32_t count) {
    if (pulses == NULL || count <= 0) return;

    CHHapticEngine* engine = ROHapticEngine();
    if (engine == nil) {
        ROPlayPulsesAsTaps(pulses, count);
        return;
    }

    NSMutableArray<CHHapticEvent*>* events = [NSMutableArray arrayWithCapacity:count];
    NSTimeInterval time = 0;
    for (int32_t i = 0; i < count; i++) {
        time += MAX(0, pulses[i].delayMs) / 1000.0;
        NSTimeInterval duration = MAX(0, pulses[i].durationMs) / 1000.0;
        float intensity = MIN(MAX(pulses[i].intensity, 0.0f), 1.0f);
        if (duration > 0 && intensity > 0) {
            NSArray<CHHapticEventParameter*>* parameters = @[
                [[CHHapticEventParameter alloc] initWithParameterID:CHHapticEventParameterIDHapticIntensity value:intensity],
                [[CHHapticEventParameter alloc] initWithParameterID:CHHapticEventParameterIDHapticSharpness value:0.5f],
            ];
            [events addObject:[[CHHapticEvent alloc] initWithEventType:CHHapticEventTypeHapticContinuous
                                                            parameters:parameters
                                                          relativeTime:time
                                                              duration:duration]];
        }
        time += duration;
    }
    if (events.count == 0) return;

    NSError* error = nil;
    CHHapticPattern* pattern = [[CHHapticPattern alloc] initWithEvents:events parameters:@[] error:&error];
    id<CHHapticPatternPlayer> player = pattern == nil ? nil : [engine createPlayerWithPattern:pattern error:&error];
    if (player == nil || ![engine startAndReturnError:&error] || ![player startAtTime:CHHapticTimeImmediate error:&error]) {
        NSLog(@"[ROVibration] Pattern not played: %@", error);
    }
}
