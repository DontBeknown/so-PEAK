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
            

            if (nt.noiseMap1 == null || nt.noiseMap2 == null || nt.noiseMap3 == null)
            {
                Debug.LogError("Assign all 3 noise maps first!");
                return;
            }

            nt.DepthNoise();
            Debug.Log("Depth map generated!");
        }
    }
}
