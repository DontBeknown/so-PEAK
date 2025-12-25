using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NoiseTranslator))]
public class NoiseTranslatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draws all normal inspector fields

        NoiseTranslator nt = (NoiseTranslator)target;

        if (GUILayout.Button("Generate Depth Map"))
        {
            

            if (nt.ContinentalNoise == null || nt.ErosionNoise == null || nt.WeirdnessNoise == null)
            {
                Debug.LogError("Assign all 3 noise maps first!");
                return;
            }

            nt.TerrainDrawing();
            Debug.Log("Depth map generated!");
        }
    }
}
