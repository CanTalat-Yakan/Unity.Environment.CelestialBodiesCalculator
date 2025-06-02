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
            UpdateSunLensFlare(sunLight, spaceWeight);
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

            const float minIntensity = 5_000f; // Night sun intensity in lux
            sunIntensity = Mathf.Max(minIntensity, sunIntensity);

            const float spaceMinIntensity = 300f; // Space sun intensity in lux
            // Blend the sun intensity with space weight
            sunLight.lightUnit = LightUnit.Lux;
            sunLight.intensity = Mathf.Lerp(sunIntensity, spaceMinIntensity, spaceWeight);
        }

        private static float CalculateSunIntensity(float elevationAngleDegrees)
        {
            if (elevationAngleDegrees <= 0f)
                return 0f; // Sun below horizon

            const float maxIntensity = 120_000f; // Noon sun intensity in lux
            float elevationRadians = elevationAngleDegrees * Mathf.Deg2Rad;

            // Use sine of elevation angle for intensity
            float intensityFactor = Mathf.Sin(elevationRadians);
            return intensityFactor * maxIntensity;
        }

        private static void UpdateSunLensFlare(Light sunLight, float spaceWeight)
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
            lensFlare.intensity = intensityFactor * (1 - spaceWeight);
        }

        private static void UpdateMoonLightProperties(Light moonLight, float moonElevationAngle, double moonIllumination, float spaceWeight)
        {
            // Calculate moon intensity and color
            var moonIntensity = CalculateMoonIntensity(moonElevationAngle, moonIllumination);
            var moonColor = CalculateMoonColor(moonElevationAngle, moonIllumination);

            moonLight.color = moonColor;

            const float spaceMinIntensity = 0;
            moonLight.lightUnit = LightUnit.Lux;
            moonLight.intensity = Mathf.Lerp(moonIntensity, spaceMinIntensity, spaceWeight);
        }

        private static float CalculateMoonIntensity(float elevationAngleDegrees, double illuminationFraction)
        {
            const float maxIntensity = 0.5f; // Full moon intensity in lux
            return (float)(illuminationFraction * maxIntensity);
        }

        // Base color for a full moon high in the sky (cool bluish-white)
        private static Color _fullMoonColor = new Color(0.75f, 0.80f, 1.0f, 1.0f);
        // Dimmer, more neutral color for low illumination (new moon)
        private static Color _newMoonColor = new Color(0.2f, 0.22f, 0.25f, 1.0f);
        private static Color CalculateMoonColor(float moonElevationAngle, double moonIllumination)
        {
            // Blend between new moon and full moon color based on illumination
            var illuminationColor = Color.Lerp(_newMoonColor, _fullMoonColor, (float)moonIllumination);
            // Apply a slight desaturation and darkening when the moon is near the horizon
            var elevationFactor = Mathf.InverseLerp(0f, 30f, moonElevationAngle); // 0 at horizon, 1 at 30°+
            return Color.Lerp(Color.gray, illuminationColor, elevationFactor);
        }
    }
}