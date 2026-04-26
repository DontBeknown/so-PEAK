using UnityEngine;
using System;
using Game.Environment.DayNight;
using Game.Sound;
using Game.Core.DI;

[ExecuteAlways]
public class TitleDynamicEnvironment : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Drag the DayNightConfig ScriptableObject here!")]
    public DayNightConfig config;

    [Header("Scene References")]
    public Light mainLight;

    [Header("Modifiers (Scene Overrides)")]
    [Tooltip("Multiplies the light intensity from the config file. 1 = default config value.")]
    [Range(0f, 10f)]
    public float globalLightMultiplier = 1f;

    [Header("Audio (Ambient Sounds)")]
    [Tooltip("Make sure these match the IDs in your SoundLibrary!")]
    public string morningAmbientId = "ambient_morning";
    public string dayAmbientId = "ambient_day";
    public string eveningAmbientId = "ambient_evening";
    public string nightAmbientId = "ambient_night";

    [Header("Testing")]
    public bool useTestTime = false;
    [Range(0, 23)] public int testHour = 12;

    private int _lastHour = -1;
    private bool _lastTestState = false;
    private float _lastMultiplier = -1f;

    // --- Audio State ---
    private SoundService _soundService;
    private TimeOfDay _lastPlayedPeriod = (TimeOfDay)(-1); // Forces an update on the first frame

    void Start()
    {
        if (Application.isPlaying)
        {
            Debug.Log("[TitleEnvironment] Start() called. Attempting to fetch SoundService...");

            // Fetch the sound service from your friend's architecture
            _soundService = FindAnyObjectByType<SoundService>();

            if (_soundService == null)
            {
                Debug.LogWarning("[TitleEnvironment] Start(): FAILED to find SoundService in ServiceContainer. Will try again when environment applies.");
            }
            else
            {
                Debug.Log("[TitleEnvironment] Start(): Successfully found SoundService.");
            }

            if (!useTestTime) ApplyEnvironment();
        }
    }

    void Update()
    {
        if (useTestTime)
        {
            if (_lastHour != testHour || _lastTestState != useTestTime || _lastMultiplier != globalLightMultiplier)
            {
                ApplyEnvironment();
                _lastHour = testHour;
                _lastTestState = useTestTime;
                _lastMultiplier = globalLightMultiplier;
            }
        }
        else if (_lastTestState == true)
        {
            ApplyEnvironment();
            _lastTestState = false;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (useTestTime)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) ApplyEnvironment();
            };
        }
    }
#endif

    public void ApplyEnvironment()
    {
        if (config == null)
        {
            Debug.LogWarning("[TitleEnvironment] Config is missing! Cannot apply environment.");
            return;
        }

        int hour = useTestTime ? testHour : DateTime.Now.Hour;
        TimeOfDay currentPeriod = config.GetTimeOfDay(hour);

        // --- AMBIENT SOUND LOGIC (WITH OPTION 2 FAILSAFE) ---
        if (Application.isPlaying && currentPeriod != _lastPlayedPeriod)
        {
            Debug.Log($"[TitleEnvironment] Time period changed to: {currentPeriod}. Processing audio...");

            // Failsafe 1: Try the Service Container if we still don't have it
            if (_soundService == null && ServiceContainer.Instance != null)
            {
                _soundService = ServiceContainer.Instance.TryGet<SoundService>();
                if (_soundService != null) Debug.Log("[TitleEnvironment] Found SoundService via ServiceContainer late-fetch!");
            }

            // Failsafe 2: Your friend's brute-force method
            if (_soundService == null)
            {
                _soundService = FindFirstObjectByType<SoundService>();
                if (_soundService != null) Debug.Log("[TitleEnvironment] Found SoundService via FindFirstObjectByType fallback!");
            }

            // Now attempt to play the sound
            if (_soundService != null)
            {
                string targetAmbientId = "";

                switch (currentPeriod)
                {
                    case TimeOfDay.Morning: targetAmbientId = morningAmbientId; break;
                    case TimeOfDay.Day: targetAmbientId = dayAmbientId; break;
                    case TimeOfDay.Evening: targetAmbientId = eveningAmbientId; break;
                    case TimeOfDay.Night: targetAmbientId = nightAmbientId; break;
                }

                Debug.Log($"[TitleEnvironment] Target Ambient ID resolved to: '{targetAmbientId}'");

                if (!string.IsNullOrEmpty(targetAmbientId))
                {
                    Debug.Log($"[TitleEnvironment] Calling _soundService.PlayAmbient({targetAmbientId})");
                    _soundService.PlayAmbient(targetAmbientId);
                }
                else
                {
                    Debug.LogWarning($"[TitleEnvironment] The string ID for {currentPeriod} is empty in the Inspector!");
                }
            }
            else
            {
                // If it hits this point, the SoundService literally does not exist in the active scene at all.
                Debug.LogError("[TitleEnvironment] FATAL: SoundService is completely missing from the scene. Check if it was destroyed or never loaded.");
            }

            _lastPlayedPeriod = currentPeriod;
        }
        // ----------------------------------------------------

        Color lightColor = Color.white;
        float lightIntensity = 1f;
        Vector3 sunRot = Vector3.zero;
        Color ambColor = Color.white;
        float ambIntens = 1f;
        Color fogCol = Color.white;
        float fogDens = 0f;

        switch (currentPeriod)
        {
            case TimeOfDay.Morning:
                lightColor = config.morningLightColor;
                lightIntensity = config.morningLightIntensity;
                sunRot = config.morningSunRotation;
                ambColor = config.morningAmbientColor;
                ambIntens = config.morningAmbientIntensity;
                fogCol = config.morningFogColor;
                fogDens = config.morningFogDensity;
                break;
            case TimeOfDay.Day:
                lightColor = config.dayLightColor;
                lightIntensity = config.dayLightIntensity;
                sunRot = config.daySunRotation;
                ambColor = config.dayAmbientColor;
                ambIntens = config.dayAmbientIntensity;
                fogCol = config.dayFogColor;
                fogDens = config.dayFogDensity;
                break;
            case TimeOfDay.Evening:
                lightColor = config.eveningLightColor;
                lightIntensity = config.eveningLightIntensity;
                sunRot = config.eveningSunRotation;
                ambColor = config.eveningAmbientColor;
                ambIntens = config.eveningAmbientIntensity;
                fogCol = config.eveningFogColor;
                fogDens = config.eveningFogDensity;
                break;
            case TimeOfDay.Night:
                lightColor = config.nightLightColor;
                lightIntensity = config.nightLightIntensity;
                sunRot = config.nightMoonRotation;
                ambColor = config.nightAmbientColor;
                ambIntens = config.nightAmbientIntensity;
                fogCol = config.nightFogColor;
                fogDens = config.nightFogDensity;
                break;
        }

        // 1. Apply Directional Light (Sun/Moon)
        if (mainLight != null)
        {
            mainLight.color = lightColor;
            mainLight.intensity = lightIntensity * globalLightMultiplier;
            mainLight.transform.rotation = Quaternion.Euler(sunRot);
        }

        // 2. Apply Fog
        RenderSettings.fog = config.useFog;
        if (config.useFog)
        {
            RenderSettings.fogColor = fogCol;
            RenderSettings.fogDensity = fogDens;
        }

        // 3. Apply Skybox
        Material skyboxMat = config.GetSkyboxForTime(currentPeriod);
        if (skyboxMat != null)
        {
            RenderSettings.skybox = skyboxMat;
        }

        // 4. Force global illumination to update
        DynamicGI.UpdateEnvironment();

        // 5. Force Ambient Mode to Flat
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambColor * ambIntens;
        RenderSettings.ambientIntensity = ambIntens;
    }
}