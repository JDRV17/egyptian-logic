using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRBaseInteractable))]
public class XRPhysicalButton : MonoBehaviour
{
    [Header("Configuración del Hundimiento")]
    [Tooltip("Objeto visual del botón que se moverá hacia abajo.")]
    [SerializeField] private Transform buttonMesh;

    [Tooltip("Distancia hacia abajo en el eje Y local que se hundirá el botón.")]
    [SerializeField] private float pressDistance = 0.02f;

    [Tooltip("Velocidad del movimiento al presionarse y regresar.")]
    [SerializeField] private float pressSpeed = 15f;

    [Header("Eventos")]
    public UnityEvent onPressed;

    private XRBaseInteractable interactable;
    private Vector3 initialLocalPosition;
    private Vector3 pressedLocalPosition;
    private Coroutine moveCoroutine;
    private bool isPressing = false;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();

        if (buttonMesh == null)
            buttonMesh = transform;

        initialLocalPosition = buttonMesh.localPosition;
        pressedLocalPosition = initialLocalPosition - new Vector3(0f, pressDistance, 0f);
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelectEntered);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (!isPressing)
        {
            PressButton();
        }
    }

    public void PressButton()
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(AnimateButtonPress());
        onPressed?.Invoke();
    }

    private IEnumerator AnimateButtonPress()
    {
        isPressing = true;

        // Mover hacia abajo (hundir)
        while (Vector3.Distance(buttonMesh.localPosition, pressedLocalPosition) > 0.001f)
        {
            buttonMesh.localPosition = Vector3.MoveTowards(buttonMesh.localPosition, pressedLocalPosition, Time.deltaTime * pressSpeed);
            yield return null;
        }

        buttonMesh.localPosition = pressedLocalPosition;

        // Pequeña pausa antes de regresar
        yield return new WaitForSeconds(0.1f);

        // Regresar a la posición inicial
        while (Vector3.Distance(buttonMesh.localPosition, initialLocalPosition) > 0.001f)
        {
            buttonMesh.localPosition = Vector3.MoveTowards(buttonMesh.localPosition, initialLocalPosition, Time.deltaTime * pressSpeed);
            yield return null;
        }

        buttonMesh.localPosition = initialLocalPosition;
        isPressing = false;
    }
}