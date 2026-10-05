using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Companion : MonoBehaviour
{
    public GameObject CompanionBot;
    public Transform target;
    public float followSpeed;
    public float distanceFromCamera;
    public float heightFromCamera;
    public float centerOffset;

    void Start()
    {
        CompanionBot.SetActive(false);
    }

    void Update()
    {
        // PRESIONAR TECLA Y
        if (Input.GetKeyDown(KeyCode.Y))
        {
            CompanionBot.SetActive(!CompanionBot.activeSelf);
        }
    }

    void LateUpdate()
    {
        if (!CompanionBot.activeSelf) return;

        Vector3 cameraForward = target.forward;
        Vector3 cameraLeft = -target.right;

        Vector3 desiredPosition =
            target.position
            + cameraForward * distanceFromCamera
            + cameraLeft * distanceFromCamera / 2f
            + Vector3.up * heightFromCamera
            + cameraLeft * centerOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            Time.deltaTime * followSpeed
        );

        transform.LookAt(target);
    }
}
