using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

[RequireComponent(typeof(XRSimpleInteractable))]
public class CubeRotator : MonoBehaviour
{
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float stepAngle = 90f;
    [SerializeField] private float duration = 0.35f;
    [Header("Caras con valor (sin el '?')")]
    [Tooltip("Índice 0 = cara a 90° (V3), 1 = cara a 180° (V2), 2 = cara a 270° (V1).")]
    [SerializeField] private TMP_Text[] faceTexts = new TMP_Text[3];

    public int CorrectStep { get; private set; }          // 1..3
    public bool IsCorrect => steps == CorrectStep;

    public UnityEvent onRotated;

    private XRSimpleInteractable interactable;
    private Quaternion baseRotation;
    private int steps;
    private bool rotating;

    public int CurrentAngle => (steps * Mathf.RoundToInt(stepAngle)) % 360;

    /// <summary>
    /// versionStep = paso (1..3) de la cara que contiene el valor de la versión activa.
    /// Mueve ese valor a una cara al azar y deja el resto de textos intercambiados.
    /// </summary>
    public void ShuffleFaces(int versionStep)
    {
        int target = Random.Range(1, 4);

        if (target != versionStep && faceTexts.Length >= 3
            && faceTexts[versionStep - 1] != null && faceTexts[target - 1] != null)
        {
            string correct = faceTexts[versionStep - 1].text;
            faceTexts[versionStep - 1].text = faceTexts[target - 1].text;
            faceTexts[target - 1].text = correct;
        }
        Debug.Log($"[CubeRotator] {name}: versión paso {versionStep}, respuesta movida al paso {target}. " +
          $"Face Texts: {faceTexts.Length}, cara versión: {(faceTexts.Length >= versionStep ? faceTexts[versionStep - 1] : null)}, cara destino: {(faceTexts.Length >= target ? faceTexts[target - 1] : null)}");
        CorrectStep = target;
    }

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        baseRotation = transform.localRotation;
    }

    private void OnEnable() => interactable.selectEntered.AddListener(OnSelected);
    private void OnDisable() => interactable.selectEntered.RemoveListener(OnSelected);

    private void OnSelected(SelectEnterEventArgs args)
    {
        if (!rotating) StartCoroutine(RotateTo((steps + 1) % 4, duration, true));
    }

    public void SetInteractable(bool value) => interactable.enabled = value;

    /// <summary>Gira animado hasta un paso concreto (0..3). Lo usa el manager para el paso inicial aleatorio.</summary>
    public void AnimateToStep(int targetStep, float dur)
    {
        if (!rotating) StartCoroutine(RotateTo(targetStep % 4, dur, false));
    }

    private IEnumerator RotateTo(int targetStep, float dur, bool notify)
    {
        rotating = true;

        Quaternion start = transform.localRotation;
        steps = targetStep;
        Quaternion end = baseRotation * Quaternion.AngleAxis(steps * stepAngle, rotationAxis);

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(start, end, Mathf.SmoothStep(0, 1, t / dur));
            yield return null;
        }

        transform.localRotation = end;
        rotating = false;
        if (notify) onRotated?.Invoke();
    }
}