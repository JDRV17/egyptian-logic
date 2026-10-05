using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectFloater : MonoBehaviour
{
    [Range(0.0f, 0.5f)]
    public float amplitude;
    public float speed;                  //Set in Inspector 
    private float tempVal1;
    private Vector3 tempPos1;
    private float tempVal2;
    private Vector3 tempPos2;
    void Start () 
    {
        tempPos1 = transform.position;
        tempVal1 = transform.position.y;
        tempPos2 = transform.position;
        tempVal2 = transform.position.x;
    }

    void Update () 
    {        
        tempPos1.y = tempVal1 + amplitude * Mathf.Sin(speed * Time.time);
        transform.position = tempPos1;

        tempPos2.x = tempVal2 + amplitude * Mathf.Sin(speed * Time.time);
        transform.position = tempPos2;
    }
}
