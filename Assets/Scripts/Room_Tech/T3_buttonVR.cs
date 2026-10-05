using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class T3_buttonVR : MonoBehaviour
{
    public GameObject button;
    public UnityEvent onPressed, onReleased;
    public Light ButtonLightObject;
    public bool LightActivated = false;

    GameObject presser;
    AudioSource sound;
    bool isPressed;

    void Start()
    {
        sound = GetComponent<AudioSource>();
        isPressed = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((!isPressed) && (other.tag == "PlayerHand"))
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
        }
    }

    // [SerializeField] private Material chipMaterial;
    // private Color color;
    // private float intensity;

    private void LightActivation()
    {
        if (LightActivated == true)
        {
            LightActivated = false;
            ButtonLightObject.enabled = LightActivated;
            // color = Color.red;
            // intensity = 2.4f;
            // chipMaterial.SetColor("_EmissionColor", color * intensity);
        }
        else
        {
            LightActivated = true;
            ButtonLightObject.enabled = LightActivated;
            // color = Color.green;
            // intensity = 2.4f;
            // chipMaterial.SetColor("_EmissionColor", color * intensity);
        }
    }
}
