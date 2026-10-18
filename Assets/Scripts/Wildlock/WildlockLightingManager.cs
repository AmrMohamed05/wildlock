using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Wildlock
{
    /// <summary>
    /// Assignment 2 Part 3 (Section 3): Lighting Behaviors.
    /// Implements:
    /// 1) Day/Night Cycle & Time-Based Lighting Shift (High Noon -> Sunset -> Midnight)
    /// 2) Player-Controlled Light Source (Toggleable Western Lantern + Aim Spotlight via 'F')
    /// 3) Environment- and Event-Triggered Lighting Changes (Mine/Saloon zone darkening + Torch ignition,
    ///    Generator Switch Floodlights, and 10-Gem Countdown pulsing Crimson Alarm Lights).
    /// </summary>
    public class WildlockLightingManager : MonoBehaviour
    {
        public static WildlockLightingManager Instance { get; private set; }

        [Header("1. Day / Night Cycle")]
        public Light directionalSunLight;
        [Range(0f, 1f)]
        [Tooltip("0.25 = High Noon, 0.55 = Crimson Sunset, 0.85 = Midnight Showdown")]
        public float timeOfDay = 0.25f;
        public bool autoCycleTime = false;
        public float fullDayCycleDurationSeconds = 95f;

        [Header("2. Player-Controlled Light Source (Lantern + Spotlight)")]
        public Light playerLanternPointLight;
        public Light playerAimSpotLight;
        public Renderer playerLanternBulbRenderer;
        public bool isLanternOn = true;

        [Header("3. Environment & Event-Triggered Lights")]
        public List<Light> townGeneratorFloodlights = new List<Light>();
        public List<Light> mineAndSaloonTorches = new List<Light>();
        public List<Light> countdownEmergencyLights = new List<Light>();

        public bool areGeneratorFloodlightsOn;
        public bool isPlayerInDarkZone;
        public string currentDarkZoneName = "";
        public bool isShowdownAlarmActive;

        // Verification flags for Assignment 2 HUD checklist
        public bool ObservedDayNightShift { get; private set; }
        public bool UsedPlayerLantern { get; private set; }
        public bool TriggeredEnvironmentLighting { get; private set; }

        private float _initialTimeOfDay;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (directionalSunLight == null)
            {
                GameObject dirGo = GameObject.Find("Directional Light");
                if (dirGo != null) directionalSunLight = dirGo.GetComponent<Light>();
            }

            _initialTimeOfDay = timeOfDay;

            // Enable subtle atmospheric fog for outer vista depth without obscuring the arena
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0006f;

            ApplyLanternState(isLanternOn, false);
            ApplyGeneratorFloodlights(areGeneratorFloodlightsOn);
            UpdateDayNightLighting(true);
        }

        private void Update()
        {
            HandleLightingInput();

            if (autoCycleTime)
            {
                timeOfDay = Mathf.Repeat(timeOfDay + (Time.deltaTime / Mathf.Max(10f, fullDayCycleDurationSeconds)), 1f);
                if (Mathf.Abs(timeOfDay - _initialTimeOfDay) > 0.12f)
                {
                    ObservedDayNightShift = true;
                }
            }

            UpdateDayNightLighting();
            UpdateFlickerAndEventLights();
        }

        private void HandleLightingInput()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            // 'F' toggles player-controlled Western Lantern & Aim Spotlight
            if (kb.fKey.wasPressedThisFrame)
            {
                TogglePlayerLantern();
            }

            // 'T' steps Time-of-Day Preset (High Noon -> Sunset -> Midnight)
            if (kb.tKey.wasPressedThisFrame)
            {
                StepTimeOfDayPreset();
            }

            // 'Y' toggles continuous fast Day/Night auto-cycle
            if (kb.yKey.wasPressedThisFrame)
            {
                autoCycleTime = !autoCycleTime;
                if (autoCycleTime) fullDayCycleDurationSeconds = 35f;
                ObservedDayNightShift = true;
            }
#endif
        }

        public void StepTimeOfDayPreset()
        {
            ObservedDayNightShift = true;
            if (timeOfDay < 0.42f)
            {
                timeOfDay = 0.54f; // Crimson Western Sunset
            }
            else if (timeOfDay < 0.72f)
            {
                timeOfDay = 0.85f; // Midnight Showdown
            }
            else
            {
                timeOfDay = 0.25f; // Blazing High Noon
            }
            UpdateDayNightLighting();
        }

        public void TogglePlayerLantern()
        {
            UsedPlayerLantern = true;
            ApplyLanternState(!isLanternOn, true);
        }

        public void ApplyLanternState(bool turnOn, bool playSound = true)
        {
            isLanternOn = turnOn;
            if (playerLanternPointLight != null) playerLanternPointLight.enabled = isLanternOn;
            if (playerAimSpotLight != null) playerAimSpotLight.enabled = isLanternOn;

            if (playerLanternBulbRenderer != null && playerLanternBulbRenderer.material != null)
            {
                Color bulbColor = isLanternOn ? new Color(1f, 0.78f, 0.28f) * 3f : new Color(0.2f, 0.18f, 0.15f);
                playerLanternBulbRenderer.material.color = bulbColor;
                if (playerLanternBulbRenderer.material.HasProperty("_EmissionColor"))
                {
                    playerLanternBulbRenderer.material.EnableKeyword("_EMISSION");
                    playerLanternBulbRenderer.material.SetColor("_EmissionColor", isLanternOn ? new Color(1f, 0.65f, 0.15f) * 2.5f : Color.black);
                }
            }

            if (playSound && playerLanternPointLight != null)
            {
                WildlockAudioSynth.PlaySoundAt(playerLanternPointLight.transform.position, WildlockAudioSynth.SoundType.LanternToggle, 0.6f);
            }
        }

        public void SetGeneratorFloodlights(bool turnOn)
        {
            TriggeredEnvironmentLighting = true;
            ApplyGeneratorFloodlights(turnOn);
        }

        private void ApplyGeneratorFloodlights(bool turnOn)
        {
            areGeneratorFloodlightsOn = turnOn;
            foreach (Light l in townGeneratorFloodlights)
            {
                if (l != null)
                {
                    l.enabled = turnOn;
                }
            }
        }

        public void SetDarkZoneState(bool inDarkZone, string zoneName)
        {
            if (inDarkZone) TriggeredEnvironmentLighting = true;
            isPlayerInDarkZone = inDarkZone;
            currentDarkZoneName = inDarkZone ? zoneName : "";

            // Auto-turn on lantern when entering dark cavern/saloon if it was off
            if (inDarkZone && !isLanternOn)
            {
                ApplyLanternState(true, true);
                UsedPlayerLantern = true;
            }
        }

        public void SetShowdownAlarmLights(bool active)
        {
            if (active) TriggeredEnvironmentLighting = true;
            isShowdownAlarmActive = active;
            foreach (Light l in countdownEmergencyLights)
            {
                if (l != null) l.enabled = active;
            }
        }

        private void UpdateDayNightLighting(bool immediate = false)
        {
            // Compute sun angle: 0.25 = High Noon (70 deg elevation), 0.55 = Sunset (12 deg), 0.85 = Midnight (-35 deg)
            float sunPitch;
            Color sunColor;
            float sunIntensity;
            Color skyAmbient;
            Color fogColor;

            if (timeOfDay < 0.45f)
            {
                // High Noon to Afternoon
                float t = Mathf.InverseLerp(0.0f, 0.45f, timeOfDay);
                sunPitch = Mathf.Lerp(48f, 68f, Mathf.Sin(t * Mathf.PI));
                sunColor = Color.Lerp(new Color(1f, 0.94f, 0.82f), new Color(1f, 0.97f, 0.90f), t);
                sunIntensity = 1.85f;
                skyAmbient = new Color(0.82f, 0.76f, 0.66f);
                fogColor = new Color(0.86f, 0.76f, 0.60f);
            }
            else if (timeOfDay < 0.68f)
            {
                // Crimson Western Sunset
                float t = Mathf.InverseLerp(0.45f, 0.68f, timeOfDay);
                sunPitch = Mathf.Lerp(42f, 8f, t);
                sunColor = Color.Lerp(new Color(1f, 0.75f, 0.45f), new Color(1f, 0.34f, 0.16f), t);
                sunIntensity = Mathf.Lerp(1.5f, 0.75f, t);
                skyAmbient = Color.Lerp(new Color(0.7f, 0.48f, 0.38f), new Color(0.38f, 0.22f, 0.32f), t);
                fogColor = Color.Lerp(new Color(0.82f, 0.52f, 0.35f), new Color(0.42f, 0.20f, 0.25f), t);
            }
            else
            {
                // Midnight Showdown
                float t = Mathf.InverseLerp(0.68f, 1.0f, timeOfDay);
                sunPitch = 28f; // Moon angle casting dramatic silver shadows
                sunColor = new Color(0.35f, 0.48f, 0.78f);
                sunIntensity = 0.32f;
                skyAmbient = new Color(0.16f, 0.20f, 0.32f);
                fogColor = new Color(0.08f, 0.10f, 0.18f);
            }

            // Environment Trigger Zone Modifier: Entering Gold Mine Cavern or Saloon dims exterior sun by 75%
            float zoneDimFactor = isPlayerInDarkZone ? 0.25f : 1.0f;

            if (directionalSunLight != null)
            {
                float yaw = Mathf.Lerp(-35f, 145f, timeOfDay);
                directionalSunLight.transform.rotation = Quaternion.Euler(sunPitch, yaw, 0f);
                directionalSunLight.color = sunColor;
                directionalSunLight.intensity = immediate
                    ? (sunIntensity * zoneDimFactor)
                    : Mathf.Lerp(directionalSunLight.intensity, sunIntensity * zoneDimFactor, Time.deltaTime * 5f);
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = skyAmbient * zoneDimFactor;
            RenderSettings.ambientEquatorColor = skyAmbient * 0.72f * zoneDimFactor;
            RenderSettings.ambientGroundColor = skyAmbient * 0.42f * zoneDimFactor;
            RenderSettings.fogColor = fogColor * zoneDimFactor;
        }

        private void UpdateFlickerAndEventLights()
        {
            float flicker = 0.92f + Mathf.PerlinNoise(Time.time * 9f, 1.3f) * 0.22f;

            if (isLanternOn && playerLanternPointLight != null)
            {
                playerLanternPointLight.intensity = 6.5f * flicker;
            }

            // Mine & Saloon Torches burn brighter at Night or when Player enters a Dark Zone
            bool nightOrDarkZone = (timeOfDay >= 0.5f) || isPlayerInDarkZone;
            float baseTorchIntensity = nightOrDarkZone ? 8.5f : 3.8f;

            for (int i = 0; i < mineAndSaloonTorches.Count; i++)
            {
                Light torch = mineAndSaloonTorches[i];
                if (torch == null) continue;
                float localFlicker = 0.85f + Mathf.PerlinNoise(Time.time * 8f + i * 3.1f, i * 1.7f) * 0.35f;
                torch.intensity = baseTorchIntensity * localFlicker;
            }

            // 10-Gem Showdown Emergency Beacons pulse rapidly
            if (isShowdownAlarmActive)
            {
                float pulse = (Mathf.Sin(Time.time * 10f) * 0.5f + 0.5f);
                foreach (Light alarm in countdownEmergencyLights)
                {
                    if (alarm != null)
                    {
                        alarm.enabled = true;
                        alarm.intensity = Mathf.Lerp(1.5f, 14f, pulse);
                    }
                }
            }
        }

        public string GetTimeOfDayLabel()
        {
            if (isPlayerInDarkZone) return $"Dark Zone ({currentDarkZoneName})";
            if (timeOfDay < 0.45f) return "High Noon (Bright Desert Sun)";
            if (timeOfDay < 0.68f) return "Sunset Showdown (Golden/Crimson Dusk)";
            return "Midnight Overtime (Moonlight & Lanterns)";
        }
    }
}

