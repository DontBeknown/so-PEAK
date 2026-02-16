using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSeed : MonoBehaviour
{
    public int newSeed;


    //attached automatically


    // generate seed button
    public void GenerateRandomSeed()
    {
        newSeed = Random.Range(0, 1000000);
       
    }

}
