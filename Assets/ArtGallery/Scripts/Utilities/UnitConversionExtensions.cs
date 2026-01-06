using UnityEngine;

namespace Utilities
{
    /// <summary>
    /// Extensions for converting between Unity units (meters), feet, and inches.
    /// </summary>
    public static class UnitConversionExtensions
    {
        private const float FeetPerUnityUnit = 3.28084f;      // 1 meter = 3.28084 feet
        private const float InchesPerUnityUnit = 39.3700787f; // 1 meter = 39.3700787 inches

        /// <summary>
        /// Converts Unity units (meters) to feet.
        /// Usage: float feet = 1f.ToFeet();
        /// </summary>
        public static float ToFeet(this float unityUnits)
        {
            return unityUnits * FeetPerUnityUnit;
        }

        /// <summary>
        /// Converts feet to Unity units (meters).
        /// Usage: float units = 3.28084f.FromFeet();
        /// </summary>
        public static float FromFeet(this float feet)
        {
            return feet / FeetPerUnityUnit;
        }

        /// <summary>
        /// Converts Unity units (meters) to inches.
        /// Usage: float inches = 1f.ToInches();
        /// </summary>
        public static float ToInches(this float unityUnits)
        {
            return unityUnits * InchesPerUnityUnit;
        }

        /// <summary>
        /// Converts inches to Unity units (meters).
        /// Usage: float units = 39.37f.FromInches();
        /// </summary>
        public static float FromInches(this float inches)
        {
            return inches / InchesPerUnityUnit;
        }
    }
}
