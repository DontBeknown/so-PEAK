using UnityEngine;
using UnityEditor;

namespace Game.Environment.DayNight.Editor
{
    /// <summary>
    /// Custom editor for DayNightCycleManager providing inspector controls and debugging.
    /// </summary>
    [CustomEditor(typeof(DayNightCycleManager))]
    public class DayNightCycleManagerEditor : UnityEditor.Editor
    {
        private DayNightCycleManager _manager;
        private bool _showDebugInfo = true;

        private void OnEnable()
        {
            _manager = (DayNightCycleManager)target;
        }

        public override void OnInspectorGUI()
        {
            // Draw default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);

            // Only show controls in play mode
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Debug controls are only available in Play Mode.", MessageType.Info);
                return;
            }

            // Debug info display
            _showDebugInfo = EditorGUILayout.Foldout(_showDebugInfo, "Runtime Information", true);
            if (_showDebugInfo)
            {
                EditorGUI.indentLevel++;
                
                // Time info
                int hours = Mathf.FloorToInt(_manager.CurrentTime);
                int minutes = Mathf.FloorToInt((_manager.CurrentTime - hours) * 60f);
                EditorGUILayout.LabelField("Current Time", $"{hours:00}:{minutes:00} ({_manager.CurrentTime:F2}h)");
                EditorGUILayout.LabelField("Time of Day", _manager.CurrentTimeOfDay.ToString());
                EditorGUILayout.LabelField("Day Progress", $"{_manager.DayProgress:P1}");
                EditorGUILayout.LabelField("Paused", _manager.IsPaused.ToString());
                EditorGUILayout.LabelField("Light Intensity", $"{_manager.GetLightIntensity():F2}");
                
                var ambientColor = _manager.GetAmbientColor();
                EditorGUILayout.ColorField("Ambient Color", ambientColor);
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Time control slider
            EditorGUILayout.LabelField("Set Time", EditorStyles.boldLabel);
            float newTime = EditorGUILayout.Slider("Hour", _manager.CurrentTime, 0f, 24f);
            if (!Mathf.Approximately(newTime, _manager.CurrentTime))
            {
                _manager.SetTime(newTime);
            }

            EditorGUILayout.Space(5);

            // Pause/Resume button
            if (GUILayout.Button(_manager.IsPaused ? "Resume Cycle" : "Pause Cycle", GUILayout.Height(30)))
            {
                _manager.SetPaused(!_manager.IsPaused);
            }

            EditorGUILayout.Space(5);

            // Quick time jump buttons
            EditorGUILayout.LabelField("Quick Jump", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Morning\n(06:00)", GUILayout.Height(40)))
            {
                _manager.SetTimeOfDay(TimeOfDay.Morning);
            }
            
            if (GUILayout.Button("Day\n(12:00)", GUILayout.Height(40)))
            {
                _manager.SetTimeOfDay(TimeOfDay.Day);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Evening\n(18:00)", GUILayout.Height(40)))
            {
                _manager.SetTimeOfDay(TimeOfDay.Evening);
            }
            
            if (GUILayout.Button("Night\n(21:00)", GUILayout.Height(40)))
            {
                _manager.SetTimeOfDay(TimeOfDay.Night);
            }
            
            EditorGUILayout.EndHorizontal();

            // Repaint continuously in play mode to update values
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
}
