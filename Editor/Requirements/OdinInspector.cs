// Odin Inspector comes from the Asset Store, so package.json cannot pull it in.
// Without it every other assembly in this package is left out, and this one line says why.
#if !ODIN_INSPECTOR
#error RiseOn.Vibration needs Odin Inspector. Install it from the Asset Store, or add the ODIN_INSPECTOR scripting define for the current platform.
#endif