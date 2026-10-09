using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRBaseInteractable))]
public class XRSwitchToggle : MonoBehaviour
{
    [Header("Configuración de Rotación")]
    [Tooltip("Ángulo de rotación relativo cuando el switch está APAGADO.")]
    [SerializeField] private Vector3 offRotation = new Vector3(0f, 0f, 0f);

    [Tooltip("Ángulo de rotación relativo cuando el switch está ENCENDIDO.")]
    [SerializeField] private Vector3 onRotation = new Vector3(0f, 0f, 45f);

    [Tooltip("Velocidad de transición de la rotación.")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Malla a Rotar")]
    [Tooltip("El Transform de la palanca/mesh que rotará. Si se deja vacío, usará este mismo GameObject.")]
    [SerializeField] private Transform switchMesh;

    [Header("Estado Inicial")]
    [SerializeField] private bool isOn = false;

    [Header("Eventos")]
    public UnityEvent<bool> onStateChanged;

    private XRBaseInteractable interactable;
    private Coroutine rotationCoroutine;

    public bool IsOn => isOn;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();

        if (switchMesh == null)
            switchMesh = transform;
    }

    private void OnEnable()
    {
        // Nos suscribimos al evento de selección (clic/trigger del control o mano)
        interactable.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelectEntered);
    }

    private void Start()
    {
        // Aplicar la rotación inicial sin animación al iniciar
        switchMesh.localRotation = Quaternion.Euler(isOn ? onRotation : offRotation);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        ToggleSwitch();
    }

    /// <summary>
    /// Cambia el estado del interruptor y anima la rotación.
    /// </summary>
    public void ToggleSwitch()
    {
        isOn = !isOn;

        Vector3 targetEuler = isOn ? onRotation : offRotation;

        if (rotationCoroutine != null)
            StopCoroutine(rotationCoroutine);

        rotationCoroutine = StartCoroutine(AnimateRotation(targetEuler));

        // Dispara el evento por si quieres conectar puertas, luces o código
        onStateChanged?.Invoke(isOn);
    }

    private IEnumerator AnimateRotation(Vector3 targetEuler)
    {
        Quaternion targetRotation = Quaternion.Euler(targetEuler);

        while (Quaternion.Angle(switchMesh.localRotation, targetRotation) > 0.01f)
        {
            switchMesh.localRotation = Quaternion.Slerp(switchMesh.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
            yield return null;
        }

        switchMesh.localRotation = targetRotation;
    }
}