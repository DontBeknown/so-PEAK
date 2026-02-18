﻿using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof (MapGenerator))]
public class MapGeneratorEditor : Editor {

	public override void OnInspectorGUI() {

        DrawDefaultInspector();

        //Unused: will delete it later

        //MapGenerator mapGen = (MapGenerator)target;

        //if (DrawDefaultInspector ()) {
        //	if (mapGen.autoUpdate) {
        //		mapGen.GenerateMap ();
        //	}
        //}

        //if (GUILayout.Button ("Generate")) {
        //	mapGen.GenerateMap ();
        //}
    }
}