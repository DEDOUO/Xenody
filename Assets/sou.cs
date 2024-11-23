using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sou : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        string tempPath = System.IO.Path.GetTempPath();
        Debug.Log("Temporary Path: " + tempPath);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
