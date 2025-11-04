using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(RandomSeed))]
public class RandomSeedButton : Editor
{
    public override void OnInspectorGUI()
    {
        RandomSeed randomSeed = (RandomSeed)target;

        if (GUILayout.Button("Generate Random Seed"))
        {
            randomSeed.GenerateRandomSeed();
        }
    }

}
