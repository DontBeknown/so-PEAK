using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{

    public override void OnInspectorGUI()
    {

        MapGenerator mapGen = (MapGenerator)target;

        // Draw the Inspector exactly ONCE. 
        if (DrawDefaultInspector())
        {
            // If they tweak a slider and autoUpdate is on, use the exact seed in the box.
            if (mapGen.autoUpdate)
            {
                mapGen.GenerateMap(mapGen.seed);
            }
        }

        // If they click Generate, just use the exact seed in the box. No random overrides!
        if (GUILayout.Button("Generate"))
        {
            mapGen.GenerateMap(mapGen.seed);
        }
    }
}