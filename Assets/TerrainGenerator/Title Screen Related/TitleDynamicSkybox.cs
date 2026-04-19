using UnityEngine;
using System;
using Game.Environment.DayNight;

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

    [Header("Testing")]
    public bool useTestTime = false;
    [Range(0, 23)] public int testHour = 12;

    private int _lastHour = -1;
    private bool _lastTestState = false;
    private float _lastMultiplier = -1f;

    void Start()
    {
        if (Application.isPlaying && !useTestTime) ApplyEnvironment();
    }

    void Update()
    {
        if (useTestTime)
        {
            // We now also check if the multiplier slider changed so it updates in real-time
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
        if (config == null) return;

        int hour = useTestTime ? testHour : DateTime.Now.Hour;
        TimeOfDay currentPeriod = config.GetTimeOfDay(hour);

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

        // 3. Apply Skybox (This triggers Unity's automatic override)
        Material skyboxMat = config.GetSkyboxForTime(currentPeriod);
        if (skyboxMat != null)
        {
            RenderSettings.skybox = skyboxMat;
        }

        // 4. Force global illumination to update
        DynamicGI.UpdateEnvironment();

        // 5. THE FIX: Force Ambient Mode to Flat LAST
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

        // Multiply the base color by your intensity slider to force Unity to respect it!
        RenderSettings.ambientLight = ambColor * ambIntens;
        RenderSettings.ambientIntensity = ambIntens;
    }
}