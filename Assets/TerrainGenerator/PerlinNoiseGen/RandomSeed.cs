using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSeed : MonoBehaviour
{
    private MapGenerator mapGenerator;
    public int newSeed;


    //attached automatically


    // generate seed button
    public void GenerateRandomSeed()
    {
        if (mapGenerator == null)
            mapGenerator = GetComponent<MapGenerator>();

        newSeed = Random.Range(0, 1000000);
        mapGenerator.seed = newSeed;
        mapGenerator.GenerateMap();
    }

}
