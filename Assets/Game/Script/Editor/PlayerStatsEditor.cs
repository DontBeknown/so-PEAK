#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector for PlayerStats.
/// Shows a live Temperature debug panel during Play Mode with a visual thermometer bar.
/// All other default fields are still drawn below.
/// </summary>
[CustomEditor(typeof(PlayerStats))]
public class PlayerStatsEditor : Editor
{
    // ── Foldout state ─────────────────────────────────────────────────
    private bool _tempFoldout = true;

    // ── Thermometer bar colours ────────────────────────────────────────
    private static readonly Color ColFreezing    = new Color(0.40f, 0.75f, 1.00f); // ice blue
    private static readonly Color ColCold        = new Color(0.60f, 0.85f, 1.00f); // cool blue
    private static readonly Color ColComfort     = new Color(0.35f, 0.80f, 0.45f); // healthy green
    private static readonly Color ColHot         = new Color(1.00f, 0.65f, 0.10f); // warm orange
    private static readonly Color ColOverheating = new Color(1.00f, 0.25f, 0.10f); // danger red
    private static readonly Color ColBarBg       = new Color(0.15f, 0.15f, 0.15f);

    public override void OnInspectorGUI()
    {
        // Draw the default fields (config, equipmentManager, stats, etc.)
        DrawDefaultInspector();

        EditorGUILayout.Space(6);

        PlayerStats stats = (PlayerStats)target;

        // ── Temperature debug panel ────────────────────────────────────
        _tempFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_tempFoldout, "🌡  Temperature Debug");
        if (_tempFoldout)
        {
            DrawTemperaturePanel(stats);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Repaint every frame during play so the bar animates
        if (Application.isPlaying)
            Repaint();
    }

    private void DrawTemperaturePanel(PlayerStats stats)
    {
        TemperatureStat temp = stats.TemperatureStat;

        if (temp == null || !Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Temperature debug is only available in Play Mode.",
                MessageType.Info);
            return;
        }

        if (!temp.IsInitialized)
        {
            EditorGUILayout.HelpBox(
                "TemperatureStat not yet initialized — waiting for PlayerStats.Awake().",
                MessageType.Warning);
            return;
        }

        float current   = temp.Current;
        float max       = temp.Max;           // 100
        float coldThresh = temp.DebugColdThreshold;
        float hotThresh  = temp.DebugHotThreshold;

        EditorGUI.indentLevel++;

        // ── Status badge ──────────────────────────────────────────────
        string statusLabel;
        Color  statusColor;
        if (temp.IsFreezing)
        {
            statusLabel = "❄  FREEZING";
            statusColor = ColFreezing;
        }
        else if (temp.IsOverheating)
        {
            statusLabel = "🔥  OVERHEATING";
            statusColor = ColOverheating;
        }
        else if (current < coldThresh + 8f)
        {
            statusLabel = "🥶  Cold";
            statusColor = ColCold;
        }
        else if (current > hotThresh - 5f)
        {
            statusLabel = "🌞  Hot";
            statusColor = ColHot;
        }
        else
        {
            statusLabel = "✅  Comfortable";
            statusColor = ColComfort;
        }

        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = statusColor;
        GUILayout.Label(statusLabel, EditorStyles.helpBox);
        GUI.backgroundColor = prevBg;

        EditorGUILayout.Space(4);

        // ── Thermometer bar ───────────────────────────────────────────
        Rect barRect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
        barRect.x     += EditorGUI.indentLevel * 15;
        barRect.width -= EditorGUI.indentLevel * 15;

        // Background
        EditorGUI.DrawRect(barRect, ColBarBg);

        // Filled portion (normalized 0→1)
        float t = Mathf.Clamp01(current / max);
        Rect filled = new Rect(barRect.x, barRect.y, barRect.width * t, barRect.height);
        EditorGUI.DrawRect(filled, TemperatureColor(current, coldThresh, hotThresh));

        // Cold / comfort / hot threshold tick marks
        DrawThresholdTick(barRect, coldThresh / max, ColFreezing);
        DrawThresholdTick(barRect, 37f       / max, ColComfort);
        DrawThresholdTick(barRect, hotThresh  / max, ColOverheating);

        // Value label centred on bar
        GUI.Label(barRect, $"  {current:F1} °C / {max:F0} °C",
            new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleLeft
            });

        EditorGUILayout.Space(4);

        // ── Numeric breakdown ─────────────────────────────────────────
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Runtime Values", EditorStyles.boldLabel);

            DrawReadOnly("Body Temperature",
                $"{current:F2} °C");

            DrawReadOnly("Env. Ambient Target",
                $"{temp.DebugEnvironmentTarget:F2} °C");

            float heatBonus = temp.DebugHeatBonus;
            DrawReadOnly("Heat Source Bonus",
                heatBonus > 0 ? $"+{heatBonus:F2} °C  🔥" : "none");

            float weatherOff = temp.DebugWeatherOffset;
            DrawReadOnly("Weather Offset",
                weatherOff != 0 ? $"{weatherOff:+0.##;-0.##;0} °C" : "none");

            float insulation = temp.DebugInsulation;
            DrawReadOnly("Insulation",
                $"{insulation * 100f:F0} %");

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Thresholds", EditorStyles.boldLabel);
            DrawReadOnly("Cold Damage Below", $"{coldThresh:F1} °C");
            DrawReadOnly("Heat Damage Above", $"{hotThresh:F1}  °C");

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Active Penalties", EditorStyles.boldLabel);

            float speedPenalty = temp.GetColdSpeedPenalty();
            DrawReadOnly("Speed Multiplier",
                speedPenalty < 1f
                    ? $"{speedPenalty * 100f:F0} %  ⚠"
                    : $"{speedPenalty * 100f:F0} %");

            float hungerMult = temp.GetHungerDrainMultiplier();
            DrawReadOnly("Hunger Drain ×",
                hungerMult > 1f
                    ? $"{hungerMult:F2}×  ⚠"
                    : $"{hungerMult:F2}×");

            float thirstMult = temp.GetThirstDrainMultiplier();
            DrawReadOnly("Thirst Drain ×",
                thirstMult > 1f
                    ? $"{thirstMult:F2}×  ⚠"
                    : $"{thirstMult:F2}×");

            if (temp.IsFreezing)
            {
                EditorGUILayout.HelpBox(
                    $"Dealing {temp.ColdDPS:F1} HP/s cold damage  →  DeathCause.Freezing",
                    MessageType.Error);
            }
            else if (temp.IsOverheating)
            {
                EditorGUILayout.HelpBox(
                    $"Dealing {temp.HotDPS:F1} HP/s heat damage  →  DeathCause.Heatstroke",
                    MessageType.Error);
            }
        }

        // ── Runtime controls (Play Mode only) ─────────────────────────
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("❄  Set to 5°C (Cold)"))
                stats.ModifyTemperature(5f - current);

            if (GUILayout.Button("✅  Reset to 37°C"))
                stats.ModifyTemperature(37f - current);

            if (GUILayout.Button("🔥  Set to 45°C (Hot)"))
                stats.ModifyTemperature(45f - current);
        }

        EditorGUI.indentLevel--;
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static void DrawReadOnly(string label, string value)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(label, GUILayout.Width(160));
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
        }
    }

    private static void DrawThresholdTick(Rect bar, float normalizedPos, Color color)
    {
        float x = bar.x + bar.width * normalizedPos;
        EditorGUI.DrawRect(new Rect(x - 1, bar.y, 2, bar.height), color * 1.4f);
    }

    private static Color TemperatureColor(float t, float cold, float hot)
    {
        if (t <= cold)        return ColFreezing;
        if (t <= cold + 8f)   return Color.Lerp(ColFreezing, ColComfort, (t - cold) / 8f);
        if (t >= hot)         return ColOverheating;
        if (t >= hot - 5f)    return Color.Lerp(ColComfort, ColOverheating, (t - (hot - 5f)) / 5f);
        return ColComfort;
    }
}
#endif
