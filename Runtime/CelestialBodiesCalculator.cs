using System;
using static UnityEssentials.CelestialCalculationUtilities;

namespace UnityEssentials
{
    [Serializable]
    public enum SunPhase
    {
        Night,
        AstronomicalTwilight,
        NauticalTwilight,
        CivilTwilight,
        Day
    }

    [Serializable]
    public enum MoonPhase
    {
        NewMoon,
        WaxingCrescent,
        FirstQuarter,
        WaxingGibbous,
        FullMoon,
        WaningGibbous,
        LastQuarter,
        WaningCrescent
    }

    public struct SunProperties
    {
        public double AzimuthAngle;
        public double ElevationAngle;
        public SunPhase Phase;
    }

    public struct MoonProperties
    {
        public double Distance;
        public double Illumination;
        public double AzimuthAngle;
        public double ElevationAngle;
        public MoonPhase Phase;
    }

    public static class CelestialBodiesCalculator
    {
        public static string CurrentSunPhase { get; private set; }
        public static string CurrentMoonPhase { get; private set; }

        public static SunProperties GetSunProperties(DateTime dateTime, float latitude, float longitude)
        {
            var (azimuth, altitude) = GetSunPosition(dateTime, latitude, longitude);

            var sunProperties = new SunProperties();
            sunProperties.AzimuthAngle = azimuth * Rad2Deg;
            sunProperties.ElevationAngle = altitude * Rad2Deg;
            sunProperties.Phase = GetSunPhase(sunProperties.ElevationAngle);

            return sunProperties;
        }

        public static (double x, double y, double z) GetSunDirection(DateTime dateTime, float latitude, float longitude)
        {
            var (azimuth, altitude) = GetSunPosition(dateTime, latitude, longitude);
            return AzimuthAltitudeToVector(azimuth, altitude);
        }

        public static (double Azimuth, double Altitude) GetSunPosition(DateTime dateTime, double latitude, double longitude)
        {
            double julianDays = ToJulianDays(dateTime);
            double longitudeRadians = Rad * -longitude;
            double latitudeRadians = Rad * latitude;

            var sunCoordinates = SunCoordinates(julianDays);
            double hourAngle = SiderealTime(julianDays, longitudeRadians) - sunCoordinates.RightAscension;

            double altitude = Math.Asin(Math.Sin(latitudeRadians) * Math.Sin(sunCoordinates.Declination) +
                                        Math.Cos(latitudeRadians) * Math.Cos(sunCoordinates.Declination) * Math.Cos(hourAngle));
            double azimuth = Math.Atan2(Math.Sin(hourAngle),
                                        Math.Cos(hourAngle) * Math.Sin(latitudeRadians) -
                                        Math.Tan(sunCoordinates.Declination) * Math.Cos(latitudeRadians)) + Math.PI;

            return (azimuth, altitude);
        }

        public static MoonProperties GetMoonProperties(DateTime dateTime, float latitude, float longitude)
        {
            var (azimuth, altitude, distance) = GetMoonPosition(dateTime, latitude, longitude);

            var moonProperties = new MoonProperties();
            moonProperties.Distance = distance;
            moonProperties.AzimuthAngle = azimuth * Rad2Deg;
            moonProperties.ElevationAngle = altitude * Rad2Deg;

            var (phase, illumination, angle) = GetMoonIllumination(dateTime);
            moonProperties.Phase = GetMoonPhase(phase);
            moonProperties.Illumination = illumination;

            return moonProperties;
        }

        public static (double x, double y, double z) GetMoonDirection(DateTime dateTime, float latitude, float longitude)
        {
            var (azimuth, altitude, distance) = GetMoonPosition(dateTime, latitude, longitude);
            return AzimuthAltitudeToVector(azimuth, altitude);
        }

        public static (double Azimuth, double Altitude, double Distance) GetMoonPosition(DateTime date, double latitude, double longitude)
        {
            double julianDays = ToJulianDays(date);
            double longitudeRadians = Rad * -longitude;
            double latitudeRadians = Rad * latitude;

            var moonCoordinates = MoonCoordinates(julianDays);
            double hourAngle = SiderealTime(julianDays, longitudeRadians) - moonCoordinates.RightAscension;

            double altitude = Math.Asin(Math.Sin(latitudeRadians) * Math.Sin(moonCoordinates.Declination) +
                                        Math.Cos(latitudeRadians) * Math.Cos(moonCoordinates.Declination) * Math.Cos(hourAngle));
            double azimuth = Math.Atan2(Math.Sin(hourAngle),
                                        Math.Cos(hourAngle) * Math.Sin(latitudeRadians) -
                                        Math.Tan(moonCoordinates.Declination) * Math.Cos(latitudeRadians)) + Math.PI;
            altitude += AstroRefraction(altitude);

            return (azimuth, altitude, moonCoordinates.Distance);
        }

        public static (double Phase, double Fraction, double Angle) GetMoonIllumination(DateTime dateTime)
        {
            double julianDays = ToJulianDays(dateTime);
            var sunCoordinates = SunCoordinates(julianDays);
            var moonCoordinates = MoonCoordinates(julianDays);

            const double sunDistance = 149598000; // Distance from Earth to Sun in km

            double cosinePhaseAngle = Math.Sin(sunCoordinates.Declination) * Math.Sin(moonCoordinates.Declination) +
                                      Math.Cos(sunCoordinates.Declination) * Math.Cos(moonCoordinates.Declination) *
                                      Math.Cos(sunCoordinates.RightAscension - moonCoordinates.RightAscension);

            // Clamp cosinePhaseAngle to the range [-1, 1]
            cosinePhaseAngle = Math.Clamp(cosinePhaseAngle, -1.0, 1.0);

            double phaseAngle = Math.Acos(cosinePhaseAngle);

            double phaseIncidence = Math.Atan2(sunDistance * Math.Sin(phaseAngle),
                                               moonCoordinates.Distance - sunDistance * Math.Cos(phaseAngle));

            double angle = Math.Atan2(
                Math.Cos(sunCoordinates.Declination) * Math.Sin(sunCoordinates.RightAscension - moonCoordinates.RightAscension),
                Math.Sin(sunCoordinates.Declination) * Math.Cos(moonCoordinates.Declination) -
                Math.Cos(sunCoordinates.Declination) * Math.Sin(moonCoordinates.Declination) *
                Math.Cos(sunCoordinates.RightAscension - moonCoordinates.RightAscension)
            );

            double fraction = (1 + Math.Cos(phaseIncidence)) / 2;
            double phase = 0.5 + 0.5 * phaseIncidence * (angle < 0 ? -1 : 1) / Math.PI;

            return (phase, fraction, angle);
        }

        public static SunPhase GetSunPhase(double altitudeDegrees)
        {
            SunPhase currentSunPhase;

            if (altitudeDegrees > 0)
                currentSunPhase = SunPhase.Day;
            else if (altitudeDegrees > -6)
                currentSunPhase = SunPhase.CivilTwilight;
            else if (altitudeDegrees > -12)
                currentSunPhase = SunPhase.NauticalTwilight;
            else if (altitudeDegrees > -18)
                currentSunPhase = SunPhase.AstronomicalTwilight;
            else
                currentSunPhase = SunPhase.Night;

            CurrentSunPhase = currentSunPhase.ToString();

            return currentSunPhase;
        }

        public static MoonPhase GetMoonPhase(double phase)
        {
            MoonPhase currentMoonPhase;

            if (phase == 0 || phase == 1)
                currentMoonPhase = MoonPhase.NewMoon;
            else if (phase > 0 && phase < 0.25)
                currentMoonPhase = MoonPhase.WaxingCrescent;
            else if (phase == 0.25)
                currentMoonPhase = MoonPhase.FirstQuarter;
            else if (phase > 0.25 && phase < 0.5)
                currentMoonPhase = MoonPhase.WaxingGibbous;
            else if (phase == 0.5)
                currentMoonPhase = MoonPhase.FullMoon;
            else if (phase > 0.5 && phase < 0.75)
                currentMoonPhase = MoonPhase.WaningGibbous;
            else if (phase == 0.75)
                currentMoonPhase = MoonPhase.LastQuarter;
            else
                currentMoonPhase = MoonPhase.WaningCrescent;

            CurrentMoonPhase = currentMoonPhase.ToString();

            return currentMoonPhase;
        }

        public static (double, double) UpdateMilkyWayPosition(DateTime dateTime, double latitude, double longitude)
        {
            // Galactic Center coordinates (RA and Dec)
            double galacticCenterRightAscension = 266.4; // degrees
            double galacticCenterDeclination = -29.0; // degrees

            // Convert to radians for calculations
            double rightAscensionRadians = galacticCenterRightAscension * Deg2Rad;
            double declinationRadians = galacticCenterDeclination * Deg2Rad;

            // Get sidereal time in degrees
            double siderealTime = GetLocalSiderealTime(dateTime, (float)longitude);

            // Calculate Hour Angle (HA) in radians
            double hourAngle = (siderealTime - galacticCenterRightAscension) * Deg2Rad;

            // Observer's latitude in radians
            double latitudeRadians = latitude * Deg2Rad;

            // Calculate altitude
            double sinAltitude = Math.Sin(declinationRadians) * Math.Sin(latitudeRadians) +
                                 Math.Cos(declinationRadians) * Math.Cos(latitudeRadians) * Math.Cos(hourAngle);
            double altitude = Math.Asin(sinAltitude);

            // Calculate azimuth
            double cosAzimuth = (Math.Sin(declinationRadians) - Math.Sin(altitude) * Math.Sin(latitudeRadians)) /
                                (Math.Cos(altitude) * Math.Cos(latitudeRadians));
            double azimuth = Math.Acos(cosAzimuth);

            // Adjust azimuth for quadrant
            if (Math.Sin(hourAngle) > 0)
                azimuth = 2 * Math.PI - azimuth;

            // Convert to degrees
            float azimuthDegrees = (float)(azimuth * Rad2Deg);
            float altitudeDegrees = (float)(altitude * Rad2Deg);

            return (azimuthDegrees, altitudeDegrees);
        }
    }
}