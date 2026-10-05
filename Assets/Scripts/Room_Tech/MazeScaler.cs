using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeScaler : MonoBehaviour
{
    public float xScale;
    public float yScale;
    public float zScale;

    public float xPos;
    public float yPos;
    public float zPos;

    public float xRot;
    public float yRot;
    public float zRot;

    public void Update()
    {
        transform.localScale = new Vector3(xScale, yScale, zScale);
        transform.localPosition = new Vector3(xPos, yPos, zPos);
        transform.localRotation = new Quaternion(xRot, yRot, zRot, 1);
    }
}
