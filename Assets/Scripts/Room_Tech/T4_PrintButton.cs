using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class T4_PrintButton : MonoBehaviour
{
    public GameObject button;
    public UnityEvent onPressed, onReleased;
    public Light ButtonLightObject;
    public bool LightActivated = false;

    GameObject presser;
    AudioSource sound;
    bool isPressed;

    public T4_GridChecker gridChecker;

    void Start()
    {
        sound = GetComponent<AudioSource>();
        isPressed = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isPressed && other.CompareTag("PlayerHand"))
        {
            button.transform.localPosition = new Vector3(0, 0.003f, 0);
            presser = other.gameObject;

            onPressed.Invoke();
            sound.Play();

            isPressed = true;
            LightActivation();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == presser)
        {
            button.transform.localPosition = new Vector3(0, 0.015f, 0);

            onReleased.Invoke();
            isPressed = false;

            if (gridChecker != null)
            {
                gridChecker.CubesChecker();
            }
        }
    }

    private void LightActivation()
    {
        LightActivated = !LightActivated;
        ButtonLightObject.enabled = LightActivated;
    }
}
