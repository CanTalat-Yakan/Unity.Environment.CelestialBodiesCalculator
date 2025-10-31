# Unity Essentials

This module is part of the Unity Essentials ecosystem and follows the same lightweight, editor-first approach.
Unity Essentials is a lightweight, modular set of editor utilities and helpers that streamline Unity development. It focuses on clean, dependency-free tools that work well together.

All utilities are under the `UnityEssentials` namespace.

```csharp
using UnityEssentials;
```

## Installation

Install the Unity Essentials entry package via Unity's Package Manager, then install modules from the Tools menu.

- Add the entry package (via Git URL)
    - Window → Package Manager
    - "+" → "Add package from git URL…"
    - Paste: `https://github.com/CanTalat-Yakan/UnityEssentials.git`

- Install or update Unity Essentials packages
    - Tools → Install & Update UnityEssentials
    - Install all or select individual modules; run again anytime to update

---

# Celestial Bodies Calculator

> Quick overview: Lightweight utilities for sun/moon astronomy (RA/Dec, distance, sidereal time, refraction) and a lighting controller that drives Unity sun/moon lights in lux with color temperature, shadows, and lens flare.

Use this module to compute basic solar/lunar parameters and translate them into believable scene lighting. The runtime helpers expose common astronomical formulas and a simple API that updates sun/moon `Light` components based on elevation, illumination, cloud coverage, and a space/atmosphere blend.

![screenshot](Documentation/Screenshot.png)

## Features
- Lighting controller for sun and moon
  - Sets `Light.lightUnit = Lux` and computes realistic intensities
  - Sun intensity scales with elevation; color temperature blends with cloud coverage
  - Moon intensity/color vary with phase (illumination fraction) and elevation
  - Automatic soft shadow enable/disable based on horizon tests
  - Optional SRP lens flare intensity tied to sun altitude
  - Flags: `IsSunLightAboveHorizon`, `IsMoonLightAboveHorizon`
- Astronomy utilities
  - Julian day conversion, sun mean anomaly, ecliptic longitude
  - Sun coordinates (declination and right ascension)
  - Moon coordinates (declination, right ascension, approximate distance)
  - Sidereal time (Greenwich and local), astronomical refraction approximation
  - Convert azimuth/altitude to a unit vector for aiming scene objects
- Practical inputs and blending
  - `spaceWeight` fades to "space lighting" (e.g., planet from orbit) to soften sun/moon
  - `cloudCoverage` cools sun color temperature as clouds increase

## Requirements
- Unity 6000.0+ (Runtime + Editor)
- SRP recommended
  - Uses `UnityEngine.Rendering.LightUnit` (Lux)
  - Optional: `LensFlareComponentSRP` on the sun light for flare intensity control

## Usage

Update your directional lights each frame or on a schedule using the computed sun/moon properties you maintain elsewhere (time/location/phase). The controller expects:
- Sun: Elevation angle in degrees
- Moon: Elevation angle in degrees and illumination fraction [0..1]
- Space weight [0..1]: 0 = full atmosphere, 1 = space
- Cloud coverage [0..1]: 0 = clear sky, 1 = overcast

```csharp
using UnityEngine;
using UnityEssentials;

public class CelestialLightingExample : MonoBehaviour
{
    public Light Sun;   // Directional
    public Light Moon;  // Directional

    // Your own model should populate these each frame/tick
    public double SunElevationDeg;   // degrees above horizon
    public double MoonElevationDeg;  // degrees above horizon
    public double MoonIllumination;  // 0=new moon, 1=full moon

    [Range(0,1)] public float SpaceWeight = 0f;
    [Range(0,1)] public float CloudCoverage = 0.2f;

    void LateUpdate()
    {
        var sunProps = new SunProperties { ElevationAngle = SunElevationDeg };
        var moonProps = new MoonProperties { ElevationAngle = MoonElevationDeg, Illumination = MoonIllumination };

        CelestialLightingController.UpdateLightProperties(
            Sun, Moon,
            sunProps, moonProps,
            SpaceWeight, CloudCoverage);

        // Aim lights using your azimuth/altitude solution (see below)
    }
}

// Minimal shape the controller expects (define these in your codebase)
public struct SunProperties { public double ElevationAngle; }
public struct MoonProperties { public double ElevationAngle; public double Illumination; }
```

### Computing directions and angles
Use the utilities to help derive directions/angles for aiming your lights or sky.

- Get days since J2000 for a UTC time:
  - `double d = CelestialCalculationUtilities.ToJulianDays(dateUtc);`
- Compute sun/ moon equatorial coordinates:
  - `var (sunDec, sunRA) = CelestialCalculationUtilities.SunCoordinates(d);`
  - `var (moonDec, moonRA, moonDistKm) = CelestialCalculationUtilities.MoonCoordinates(d);`
- Get local sidereal time for a longitude (degrees, east positive):
  - `double lstDeg = CelestialCalculationUtilities.GetLocalSiderealTime(dateUtc, longitudeDeg);`
- Convert azimuth/altitude to a Unity vector (radians):
  - `var v = CelestialCalculationUtilities.AzimuthAltitudeToVector(azimuthRad, altitudeRad);`
  - Convention: Y is up. Azimuth 0 points toward +Z (north); increases clockwise toward +X (east).

From RA/Dec to azimuth/altitude you’ll need the usual hour-angle and latitude transforms; compose those with the helpers above in your own system.

### What the lighting controller does
- Sun
  - Intensity (Lux): `sin(elevationDeg) * 120000`, clamped to a minimum of 5000 Lux; blended toward 300 Lux at `spaceWeight=1`
  - Color temperature (Kelvin): 5500 (clear) → 6000 (overcast), lerp by `cloudCoverage`
  - Lens flare intensity: scales with altitude and fades out with `spaceWeight`
- Moon
  - Intensity (Lux): `illumination * 0.5`, blended toward 0 at `spaceWeight=1`
  - Color: blends from a dim neutral at new moon to cool bluish‑white at full moon, then darkens near horizon
- Shadows
  - Enables soft shadows if a light’s forward points above a nautical‑twilight threshold and the other body is below

## Notes and Limitations
- Units and angles
  - Controller expects elevation angles in degrees; utilities mostly operate in radians unless noted
  - `GetLocalSiderealTime` returns degrees in [0..360)
  - `AzimuthAltitudeToVector` expects radians
- Approximations
  - Moon distance and colors are approximate and intended for lighting, not precise ephemerides
  - Astronomical refraction uses a common approximation and clamps at the horizon
- Coordinate conventions
  - The azimuth convention used assumes +Z = north, +X = east, +Y = up in world space
  - Adapt to your project’s world axes if they differ
- Dependencies
  - Lens flare control requires `LensFlareComponentSRP` on the sun light; otherwise safely ignored

## Files in This Package
- `Runtime/CelestialCalculationUtilities.cs` – Julian days, RA/Dec for sun/moon, distance, sidereal time, refraction, az/alt→vector
- `Runtime/CelestialLightingController.cs` – Sun/Moon lighting in Lux, color temperature, shadows, lens flare, horizon flags
- `Runtime/UnityEssentials.CelestialBodiesCalculator.asmdef` – Runtime assembly definition

## Tags
unity, runtime, environment, sky, sun, moon, astronomy, ephemeris, lighting, lux, color-temperature, sidereal-time, refraction, azimuth, altitude
