using System.Runtime.CompilerServices;

// The platform assemblies install their implementation through the internal Vibration.Install; nobody else gets to see it.
[assembly: InternalsVisibleTo("RiseOn.Vibration.Android")]
[assembly: InternalsVisibleTo("RiseOn.Vibration.iOS")]
