using System;

namespace SbcBaladyChecker.Core
{
    /// <summary>
    /// Utility class for exact conversion between Revit internal feet and SI metric units (mm, m, m2).
    /// </summary>
    public static class SbcUnits
    {
        public const double FeetToMmFactor = 304.8;
        public const double FeetToMeterFactor = 0.3048;
        public const double SqFeetToSqMeterFactor = 0.09290304;

        public static double FeetToMm(double feet) => feet * FeetToMmFactor;
        public static double MmToFeet(double mm) => mm / FeetToMmFactor;

        public static double FeetToMeters(double feet) => feet * FeetToMeterFactor;
        public static double MetersToFeet(double meters) => meters / FeetToMeterFactor;

        public static double SqFeetToSqMeters(double sqFeet) => sqFeet * SqFeetToSqMeterFactor;
        public static double SqMetersToSqFeet(double sqMeters) => sqMeters / SqFeetToSqMeterFactor;

        public static string FormatMm(double feet) => $"{Math.Round(FeetToMm(feet), 0)} مم";
        public static string FormatMeters(double feet) => $"{Math.Round(FeetToMeters(feet), 2)} م";
        public static string FormatSqMeters(double sqFeet) => $"{Math.Round(SqFeetToSqMeters(sqFeet), 2)} م²";
    }
}
