using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeySpawner : MonoBehaviour
{
    public GameObject GraphChecker;
    public GameObject Key;
    public bool isCreated;
    // Update is called once per frame
    void Update()
    {
        if((!isCreated) && (GraphChecker.GetComponent<T7_GraphChecker>().GraphFinished == true)){
            this.transform.position = new Vector3(-4.65775442f,1.33201265f,4.39677763f);
            Debug.Log("Checked");
            isCreated = true;
        }
    }
}
