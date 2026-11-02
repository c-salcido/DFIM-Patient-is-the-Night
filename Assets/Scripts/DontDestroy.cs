using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this was made using this tutorial: https://www.youtube.com/watch?v=hzdADY2LkJU
public class DontDestroy : MonoBehaviour
{
    private static GameObject[] persistentObjects = new GameObject[3] ;
    public int objectIndex;


    void Awake()
    {

        if(persistentObjects[objectIndex] == null)
        {
            persistentObjects[objectIndex] = gameObject;
            DontDestroyOnLoad(gameObject);
        }
        else if(persistentObjects[objectIndex]!= gameObject)
        {
            Destroy(gameObject);
        }
        
    }

    
}
