namespace RiseOn.Vibration {
    /// <summary>
    /// How strong <see cref="Vibration.Play(VibrationLevel)"/> taps.<br/>
    /// The values cross into the native iOS code and into serialized fields, so they are spelled out rather than left to declaration order.
    /// </summary>
    public enum VibrationLevel {
        /// <summary>The lightest tap.</summary>
        Light = 0

       ,

        /// <summary>A firm tap.</summary>
        Medium = 1

       ,

        /// <summary>The strongest tap.</summary>
        Heavy = 2
    }
}
