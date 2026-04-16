using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NoiseTranslator))]
public class NoiseTranslatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draws all normal inspector fields

        //unused will delete later

        NoiseTranslator nt = (NoiseTranslator)target;

        if (GUILayout.Button("Generate Depth Map Seed 1234"))
        {


            if (nt.ContinentalNoise == null || nt.ErosionNoise_1 == null || nt.WeirdnessNoise == null)
            {
                Debug.LogError("Assign all 3 noise maps first!");
                return;
            }

            nt.TerrainDrawing(12345);
            Debug.Log("Depth map generated!");
        }
    }
}
