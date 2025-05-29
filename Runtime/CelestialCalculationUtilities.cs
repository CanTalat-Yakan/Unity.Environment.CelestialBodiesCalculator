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

        public static double SunMeanAnomaly(double d) =>
            Rad * (357.5291 + 0.98560028 * d);

        public static double EclipticLongitude(double m)
        {
            double c = Rad * (1.9148 * Math.Sin(m) + 0.02 * Math.Sin(2 * m) + 0.0003 * Math.Sin(3 * m)); // Equation of center
            double p = Rad * 102.9372; // Perihelion of the Earth
            return m + c + p + Math.PI;
        }

        public static (double Declination, double RightAscension) SunCoordinates(double d)
        {
            double m = SunMeanAnomaly(d);
            double l = EclipticLongitude(m);

            double dec = Math.Asin(Math.Sin(l) * Math.Sin(EarthObliquity));
            double ra = Math.Atan2(Math.Cos(EarthObliquity) * Math.Sin(l), Math.Cos(l));
            return (dec, ra);
        }

        public static (double Declination, double RightAscension, double Distance) MoonCoordinates(double d)
        {
            double L = Rad * (218.316 + 13.176396 * d); // Mean longitude
            double M = Rad * (134.963 + 13.064993 * d); // Mean anomaly
            double F = Rad * (93.272 + 13.229350 * d);  // Mean distance

            double l = L + Rad * 6.289 * Math.Sin(M);   // Longitude
            double b = Rad * 5.128 * Math.Sin(F);       // Latitude
            double dt = 385001 - 20905 * Math.Cos(M);   // Distance to Moon in km

            double ra = Math.Atan2(Math.Sin(l) * Math.Cos(EarthObliquity) - Math.Tan(b) * Math.Sin(EarthObliquity), Math.Cos(l));
            double dec = Math.Asin(Math.Sin(b) * Math.Cos(EarthObliquity) + Math.Cos(b) * Math.Sin(EarthObliquity) * Math.Sin(l));

            return (dec, ra, dt);
        }

        public static double SiderealTime(double d, double lw) =>
            Rad * (280.16 + 360.9856235 * d) - lw;

        public static double AstroRefraction(double h)
        {
            if (h < 0) h = 0; // The formula works for positive altitudes only.
            return 0.0002967 / Math.Tan(h + 0.00312536 / (h + 0.08901179));
        }

        public static int GetDaysInMonth(int year, int month) =>
            DateTime.DaysInMonth(year, month);

        public static double GetLocalSiderealTime(DateTime dateTime, float longitude)
        {
            // Greenwich Mean Sidereal Time (GMST)
            double jd = ToJulianDays(dateTime);
            double gmst = 280.46061837 + 360.98564736629 * (jd - 2451545.0);

            // Adjust for observer's longitude
            double lst = gmst + longitude;

            // Normalize to 0-360 degrees
            return lst % 360.0;
        }

        public static (double x, double y, double z) AzimuthAltitudeToVector(double azimuth, double altitude)
        {
            // Adjust azimuth to be measured from the north, positive clockwise
            double az = azimuth;

            // Convert to Cartesian coordinates
            double x = Math.Cos(altitude) * Math.Sin(az);
            double y = Math.Sin(altitude);
            double z = Math.Cos(altitude) * Math.Cos(az);

            return (x, y, z);
        }

    }
}
