using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEssentials
{
    public static class CelestialLightingController
    {
        public static bool IsSunLightAboveHorizon { get; private set; } = false;
        public static bool IsMoonLightAboveHorizon { get; private set; } = false;

        public static void UpdateLightProperties(Light sunLight, Light moonLight, SunProperties sunProperties, MoonProperties moonProperties, float spaceWeight)
        {
            UpdateSunLightProperties(sunLight, (float)sunProperties.ElevationAngle, spaceWeight);
            UpdateMoonLightProperties(moonLight, (float)moonProperties.ElevationAngle, moonProperties.Illumination, spaceWeight);

            UpdateLightShadows(sunLight, moonLight, spaceWeight);
            UpdateSunLensFlare(sunLight);
        }

        private static void UpdateLightShadows(Light sunLight, Light moonLight, float spaceWeight)
        {
            if (spaceWeight >= 0.3f)
            {
                sunLight.shadows = LightShadows.None;
                moonLight.shadows = LightShadows.None;
                return;
            }

            const float nauticalTwilight = 0.1f;
            IsSunLightAboveHorizon = -nauticalTwilight < Vector3.Dot(-sunLight.transform.forward, Vector3.up);
            sunLight.shadows = (IsSunLightAboveHorizon) ? LightShadows.Soft : LightShadows.None;

            IsMoonLightAboveHorizon = -nauticalTwilight < Vector3.Dot(-moonLight.transform.forward, Vector3.up);
            moonLight.shadows = (IsMoonLightAboveHorizon && !IsSunLightAboveHorizon) ? LightShadows.Soft : LightShadows.None;
        }

        private static void UpdateSunLightProperties(Light sunLight, float sunElevationAngle, float spaceWeight)
        {
            // Calculate sun intensity and color
            float sunIntensity = CalculateSunIntensity(sunElevationAngle);

            const float minIntensity = 5000f; // Night sun intensity in lux
            sunIntensity = Mathf.Max(minIntensity, sunIntensity);

            // Update SunLight component
            sunLight.lightUnit = LightUnit.Lux;

            const float spaceMinIntensity = 300f; // Space sun intensity in lux
            // Blend the sun intensity with space weight
            sunLight.intensity = Mathf.Lerp(sunIntensity, spaceMinIntensity, spaceWeight);
        }

        private static float CalculateSunIntensity(float elevationAngleDegrees)
        {
            if (elevationAngleDegrees <= 0f)
                return 0f; // Sun below horizon

            const float maxIntensity = 120000f; // Noon sun intensity in lux
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

        private static void UpdateMoonLightProperties(Light moonLight, float moonElevationAngle, double moonIllumination, float spaceWeight)
        {
            // Calculate moon intensity and color
            float moonIntensity = CalculateMoonIntensity(moonElevationAngle, moonIllumination);
            //Color moonColor = CalculateMoonColor(moonElevationAngle, moonIllumination);

            if (IsSunLightAboveHorizon)
                moonIntensity = 0;

            // Update MoonLight component
            moonLight.lightUnit = LightUnit.Lux;

            const float spaceMinIntensity = 0; // Space moon intensity in lux
            // Blend the sun intensity with space weight
            moonLight.intensity = Mathf.Lerp(moonIntensity, spaceMinIntensity, spaceWeight);
        }

        private static float CalculateMoonIntensity(float elevationAngleDegrees, double illuminationFraction)
        {
            if (elevationAngleDegrees <= 0f)
                return 0f; // Moon below horizon

            const float maxIntensity = 0.5f; // Full moon intensity in lux
            return (float)(illuminationFraction * maxIntensity);
        }
    }
}