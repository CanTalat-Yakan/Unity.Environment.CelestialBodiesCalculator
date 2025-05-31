using System;

namespace UnityEssentials
{
    public static class CelestialCalculationUtilities
    {
        public const double Rad = Math.PI / 180.0;
        public const double EarthObliquity = Rad * 23.4397;
        public const double Deg2Rad = Math.PI / 180;
        public const double Rad2Deg = 57.29578;

        public static double ToJulianDays(DateTime date) =>
            date.ToUniversalTime().Subtract(new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc)).TotalDays;

        public static double SunMeanAnomaly(double daysSinceJ2000) =>
            Rad * (357.5291 + 0.98560028 * daysSinceJ2000);

        public static double EclipticLongitude(double meanAnomaly)
        {
            double equationOfCenter = Rad * (1.9148 * Math.Sin(meanAnomaly) + 0.02 * Math.Sin(2 * meanAnomaly) + 0.0003 * Math.Sin(3 * meanAnomaly));
            double perihelion = Rad * 102.9372;
            return meanAnomaly + equationOfCenter + perihelion + Math.PI;
        }

        public static (double Declination, double RightAscension) SunCoordinates(double daysSinceJ2000)
        {
            double meanAnomaly = SunMeanAnomaly(daysSinceJ2000);
            double eclipticLongitude = EclipticLongitude(meanAnomaly);

            double declination = Math.Asin(Math.Sin(eclipticLongitude) * Math.Sin(EarthObliquity));
            double rightAscension = Math.Atan2(Math.Cos(EarthObliquity) * Math.Sin(eclipticLongitude), Math.Cos(eclipticLongitude));
            return (declination, rightAscension);
        }

        public static (double Declination, double RightAscension, double Distance) MoonCoordinates(double daysSinceJ2000)
        {
            double meanLongitude = Rad * (218.316 + 13.176396 * daysSinceJ2000);
            double meanAnomaly = Rad * (134.963 + 13.064993 * daysSinceJ2000);
            double meanDistance = Rad * (93.272 + 13.229350 * daysSinceJ2000);

            double longitude = meanLongitude + Rad * 6.289 * Math.Sin(meanAnomaly);
            double latitude = Rad * 5.128 * Math.Sin(meanDistance);
            double distanceToMoon = 385001 - 20905 * Math.Cos(meanAnomaly);

            double rightAscension = Math.Atan2(Math.Sin(longitude) * Math.Cos(EarthObliquity) - Math.Tan(latitude) * Math.Sin(EarthObliquity), Math.Cos(longitude));
            double declination = Math.Asin(Math.Sin(latitude) * Math.Cos(EarthObliquity) + Math.Cos(latitude) * Math.Sin(EarthObliquity) * Math.Sin(longitude));

            return (declination, rightAscension, distanceToMoon);
        }

        public static double SiderealTime(double daysSinceJ2000, double longitudeWest) =>
            Rad * (280.16 + 360.9856235 * daysSinceJ2000) - longitudeWest;

        public static double AstroRefraction(double altitude)
        {
            if (altitude < 0) altitude = 0;
            return 0.0002967 / Math.Tan(altitude + 0.00312536 / (altitude + 0.08901179));
        }

        public static int GetDaysInMonth(int year, int month) =>
            DateTime.DaysInMonth(year, month);

        public static double GetLocalSiderealTime(DateTime dateTime, float longitude)
        {
            // Greenwich Mean Sidereal Time (GMST)
            double julianDays = ToJulianDays(dateTime);
            double greenwichMeanSiderealTime = 280.46061837 + 360.98564736629 * (julianDays - 2451545.0);

            // Adjust for observer's longitude
            double localSiderealTime = greenwichMeanSiderealTime + longitude;

            // Normalize to 0-360 degrees
            return localSiderealTime % 360.0;
        }

        public static (double x, double y, double z) AzimuthAltitudeToVector(double azimuth, double altitude)
        {
            double x = Math.Cos(altitude) * Math.Sin(azimuth);
            double y = Math.Sin(altitude);
            double z = Math.Cos(altitude) * Math.Cos(azimuth);

            return (x, y, z);
        }
    }
}
