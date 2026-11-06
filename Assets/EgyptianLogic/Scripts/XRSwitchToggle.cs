using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRBaseInteractable))]
public class XRSwitchToggle : MonoBehaviour
{
    public enum ToggleTrigger
    {
        Select,   // al agarrar con el gatillo lateral (grip) o con el rayo — recomendado para VR
        Activate  // al apretar el gatillo frontal mientras el objeto ya está agarrado
    }

    [Header("Configuración de Rotación")]
    [SerializeField] private Vector3 offRotation = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 onRotation = new Vector3(0f, 0f, 45f);
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Malla a Rotar")]
    [SerializeField] private Transform switchMesh;

    [Header("Estado Inicial")]
    [SerializeField] private bool isOn = false;

    [Header("Interacción")]
    [Tooltip("Select: cambia al agarrarlo (grip). Activate: cambia con el gatillo frontal estando agarrado.")]
    [SerializeField] private ToggleTrigger toggleTrigger = ToggleTrigger.Select;

    [Header("Palanca desmontable")]
    [Tooltip("Actívalo SOLO en la palanca que hay que encajar. No responde hasta que se llame EnableToggle().")]
    [SerializeField] private bool startLocked = false;

    [Header("Sonido")]
    [SerializeField] private AudioSource leverSound;

    [Header("Eventos")]
    public UnityEvent<bool> onStateChanged;

    private XRBaseInteractable[] interactables;
    private Coroutine rotationCoroutine;
    private Quaternion baseRotation = Quaternion.identity; // identidad = comportamiento de siempre
    private bool canToggle = true;

    public bool IsOn => isOn;

    private void Awake()
    {
        if (switchMesh == null) switchMesh = transform;
        canToggle = !startLocked;
        interactables = GetComponents<XRBaseInteractable>();
    }

    private void OnEnable() => Subscribe();
    private void OnDisable() => Unsubscribe();

    private void Subscribe()
    {
        if (interactables == null) return;
        foreach (var i in interactables)
        {
            if (i == null) continue;
            if (toggleTrigger == ToggleTrigger.Select)
                i.selectEntered.AddListener(OnInteractSelected);
            else
                i.activated.AddListener(OnInteractActivated);
        }
    }

    private void Unsubscribe()
    {
        if (interactables == null) return;
        foreach (var i in interactables)
        {
            if (i == null) continue;
            i.selectEntered.RemoveListener(OnInteractSelected);
            i.activated.RemoveListener(OnInteractActivated);
        }
    }

    private void Start()
    {
        // La palanca desmontable conserva su pose hasta que se encaje
        if (!startLocked)
            switchMesh.localRotation = baseRotation * Quaternion.Euler(isOn ? onRotation : offRotation);
    }

    /// <summary>
    /// La llama SocketLockHelper cuando la palanca ya está encajada.
    /// La pose actual pasa a ser "apagado" y desde ahí se aplican las rotaciones.
    /// </summary>
    public void EnableToggle()
    {
        Unsubscribe();
        interactables = GetComponents<XRBaseInteractable>(); // incluye el Simple añadido después
        Subscribe();

        baseRotation = switchMesh.localRotation;
        isOn = false;
        canToggle = true;
    }

    private void OnInteractSelected(SelectEnterEventArgs args)
    {
        // Un socket "selecciona" la palanca al encajarla: eso no es el jugador accionándola
        if (args.interactorObject is XRSocketInteractor) return;
        ToggleSwitch();
    }

    private void OnInteractActivated(ActivateEventArgs args) => ToggleSwitch();

    public void ToggleSwitch()
    {
        if (!canToggle) return;
        if (leverSound != null) leverSound.Play();

        isOn = !isOn;
        Vector3 targetEuler = isOn ? onRotation : offRotation;

        if (rotationCoroutine != null) StopCoroutine(rotationCoroutine);
        rotationCoroutine = StartCoroutine(AnimateRotation(targetEuler));

        onStateChanged?.Invoke(isOn);
    }

    private IEnumerator AnimateRotation(Vector3 targetEuler)
    {
        Quaternion target = baseRotation * Quaternion.Euler(targetEuler);

        while (Quaternion.Angle(switchMesh.localRotation, target) > 0.01f)
        {
            switchMesh.localRotation = Quaternion.Slerp(switchMesh.localRotation, target, Time.deltaTime * rotationSpeed);
            yield return null;
        }

        switchMesh.localRotation = target;
    }
}