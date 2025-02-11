using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateCube : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Rotate the cube
        transform.Rotate(new Vector3(15, 30, 45) * Time.deltaTime);
    }
    
    // show a button to load Home Scene
    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 150, 100), "Load Home Scene"))
        {
            // Load the Home Scene
            Application.LoadLevel("HomeScene");
        }
    }
}
