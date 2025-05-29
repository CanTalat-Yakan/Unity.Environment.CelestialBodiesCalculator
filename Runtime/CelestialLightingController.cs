using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEssentials
{
    public static class CelestialLightingController
    {
        public static bool IsSunLightAboveHorizon { get; private set; } = false;
        public static bool IsMoonLightAboveHorizon { get; private set; } = false;

        public static void UpdateLightProperties(Light sunLight, Light moonLight, SunProperties sunProperties, MoonProperties moonProperties)
        {
            UpdateSunLightProperties(sunLight, (float)sunProperties.ElevationAngle);
            UpdateMoonLightProperties(moonLight, (float)moonProperties.ElevationAngle, moonProperties.Illumination);

            UpdateLightShadows(sunLight, moonLight);
            UpdateSunLensFlare(sunLight);
        }

        private static void UpdateLightShadows(Light sunLight, Light moonLight)
        {
            const float nauticalTwilight = 0.1f;

            IsSunLightAboveHorizon = -nauticalTwilight < Vector3.Dot(-sunLight.transform.forward, Vector3.up);
            sunLight.shadows = (IsSunLightAboveHorizon) ? LightShadows.Soft : LightShadows.None;

            IsMoonLightAboveHorizon = -nauticalTwilight < Vector3.Dot(-moonLight.transform.forward, Vector3.up);
            moonLight.shadows = (IsMoonLightAboveHorizon && !IsSunLightAboveHorizon) ? LightShadows.Soft : LightShadows.None;
        }

        private static void UpdateSunLightProperties(Light sunLight, float sunElevationAngle)
        {
            // Calculate sun intensity and color
            float sunIntensity = CalculateSunIntensity(sunElevationAngle);

            float minIntensity = 5000f; // Night sun intensity in lux
            sunIntensity = Mathf.Max(minIntensity, sunIntensity);

            // Update SunLight component
            sunLight.lightUnit = LightUnit.Lux;
            sunLight.intensity = sunIntensity;

        }

        private static float CalculateSunIntensity(float elevationAngleDegrees)
        {
            if (elevationAngleDegrees <= 0f)
                return 0f; // Sun below horizon

            float maxIntensity = 120000f; // Noon sun intensity in lux
            float elevationRadians = elevationAngleDegrees * Mathf.Deg2Rad;

            // Use sine of elevation angle for intensity
            float intensityFactor = Mathf.Sin(elevationRadians);

            return intensityFactor * maxIntensity;
        }

        private static void UpdateSunLensFlare(Light sunLight)
        {
            var lensFlare = sunLight.GetComponent<LensFlareComponentSRP>();
            if (lensFlare == null)
                return;

            // Calculate the dot product between the sun's direction and the up vector
            float dotProduct = Vector3.Dot(-sunLight.transform.forward, Vector3.up);

            // Map the dot product to a 0 to 1 range
            float intensityFactor = Mathf.InverseLerp(0f, 1f, dotProduct);

            // Apply the falloff curve
            intensityFactor = Mathf.Pow(intensityFactor, 1);

            // Set the lens flare intensity
            lensFlare.intensity = intensityFactor * 1;
        }

        private static void UpdateMoonLightProperties(Light moonLight, float moonElevationAngle, double moonIllumination)
        {
            // Calculate moon intensity and color
            float moonIntensity = CalculateMoonIntensity(moonElevationAngle, moonIllumination);
            //Color moonColor = CalculateMoonColor(moonElevationAngle, moonIllumination);

            if (IsSunLightAboveHorizon)
                moonIntensity = 0;

            // Update MoonLight component
            moonLight.lightUnit = LightUnit.Lux;
            moonLight.intensity = moonIntensity;
        }

        private static float CalculateMoonIntensity(float elevationAngleDegrees, double illuminationFraction)
        {
            if (elevationAngleDegrees <= 0f)
                return 0f; // Moon below horizon

            float maxIntensity = 0.5f; // Full moon intensity in lux
            return (float)(illuminationFraction * maxIntensity);
        }
    }
}