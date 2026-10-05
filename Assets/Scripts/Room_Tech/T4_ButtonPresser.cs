using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class T4_ButtonPresser : MonoBehaviour
{
    [Tooltip("Id del puzzle para el sistema de tracking. Dejar como 'llave' salvo que este script se reutilice en otro tablero.")]
    public string puzzleId = "llave";

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
        color = Color.black;
        intensity = 0f;
        buttonMaterial.SetColor("_EmissionColor", color * intensity);
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

            // --- TRACKING: cada toque de un botón del tablero cuenta como interacción,
            // esto es lo que hace arrancar/mantener corriendo el cronómetro del puzzle.
            PuzzleTracker.Instance?.RegisterInteraction(puzzleId);
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

    [SerializeField] private Material buttonMaterial;
    private Color color;
    private float intensity;

    private void LightActivation()
    {
        if (LightActivated == true)
        {
            LightActivated = false;
            ButtonLightObject.enabled = LightActivated;
        }
        else
        {
            LightActivated = true;
            ButtonLightObject.enabled = LightActivated;
        }
    }
}