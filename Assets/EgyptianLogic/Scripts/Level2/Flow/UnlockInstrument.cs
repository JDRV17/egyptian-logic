using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Instrumento que aparece al resolver un reto y que el jugador usa para abrir la sección siguiente.
/// Al aparecer sube desde el piso; al usarlo se mueve (la palanca baja, el botón se hunde…) y dispara onActivated.
///
/// Tipos:
///  - Lever / Button: se acciona con la mano o con el rayo (XRSimpleInteractable en este objeto).
///  - PressurePlate: se acciona al pisarla (collider trigger en este objeto, jugador con tag "Player").
///  - ItemSocket: hay que encajar un objeto (llave, antorcha…) en <see cref="socket"/>; <see cref="item"/> aparece al revelar.
/// </summary>
public class UnlockInstrument : MonoBehaviour
{
    public enum Kind { Lever, Button, PressurePlate, ItemSocket }

    public Kind kind = Kind.Lever;

    [Header("Aparición")]
    [Tooltip("Parte que sube desde el piso al revelar. Vacío = no se mueve.")]
    public Transform body;
    public float riseHeight = 1.3f;
    public float riseSeconds = 1.8f;
    [Tooltip("Objetos que se muestran al revelar (p. ej., la llave sobre su pedestal).")]
    public GameObject[] showOnReveal = new GameObject[0];

    [Header("Movimiento al usarlo")]
    [Tooltip("Lo que se mueve al usarlo: el mango de la palanca, la tapa del botón o la placa.")]
    public Transform moving;
    public Vector3 activatedEuler = new Vector3(70f, 0f, 0f);
    public Vector3 activatedOffset = new Vector3(0f, -0.04f, 0f);

    [Header("Cerradura (solo ItemSocket)")]
    public Level2Socket socket;

    [Header("Sonidos (opcionales)")]
    public AudioSource revealSound;
    public AudioSource activateSound;

    [Header("Eventos")]
    public UnityEvent onRevealed;
    public UnityEvent onActivated;

    public bool Revealed { get; private set; }
    public bool Activated { get; private set; }

    private XRBaseInteractable interactable;
    private Vector3 bodyRest;
    private Vector3 movingRestPos;
    private Quaternion movingRestRot;

    private void Awake()
    {
        if (body != null)
        {
            bodyRest = body.localPosition;
            // Queda escondido bajo el piso (activo, para que el interactable encuentre sus colliders)
            body.localPosition = bodyRest - new Vector3(0f, riseHeight, 0f);
        }
        if (moving != null)
        {
            movingRestPos = moving.localPosition;
            movingRestRot = moving.localRotation;
        }
        foreach (var go in showOnReveal) if (go != null) go.SetActive(false);
        if (socket != null)
        {
            socket.enabled = false;
            socket.selectEntered.AddListener(_ => Activate());
        }

        interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null) interactable.selectEntered.AddListener(_ => { if (kind == Kind.Lever || kind == Kind.Button) Activate(); });
    }

    /// <summary>Hace aparecer el instrumento (lo llama Level2Flow al resolver el reto).</summary>
    public void Reveal()
    {
        if (Revealed) return;
        Revealed = true;
        foreach (var go in showOnReveal) if (go != null) go.SetActive(true);
        if (socket != null) socket.enabled = true;
        if (revealSound != null) revealSound.Play();
        if (body != null && isActiveAndEnabled) StartCoroutine(Rise());
        onRevealed?.Invoke();
    }

    private IEnumerator Rise()
    {
        Vector3 from = body.localPosition;
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, riseSeconds))
        {
            body.localPosition = Vector3.Lerp(from, bodyRest, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        body.localPosition = bodyRest;
    }

    // Stay y no Enter: si el jugador ya estaba parado ahí cuando la placa aparece, también cuenta
    private void OnTriggerStay(Collider other)
    {
        if (kind == Kind.PressurePlate && other.CompareTag("Player")) Activate();
    }

    public void Activate()
    {
        if (!Revealed || Activated) return;
        Activated = true;
        if (activateSound != null) activateSound.Play();
        if (moving != null && isActiveAndEnabled) StartCoroutine(Move());
        onActivated?.Invoke();
    }

    private IEnumerator Move()
    {
        Vector3 toPos = movingRestPos + activatedOffset;
        Quaternion toRot = movingRestRot * Quaternion.Euler(activatedEuler);
        for (float t = 0f; t < 1f; t += Time.deltaTime * 2.5f)
        {
            float k = Mathf.SmoothStep(0f, 1f, t);
            moving.localPosition = Vector3.Lerp(movingRestPos, toPos, k);
            moving.localRotation = Quaternion.Slerp(movingRestRot, toRot, k);
            yield return null;
        }
        moving.localPosition = toPos;
        moving.localRotation = toRot;
    }

    [ContextMenu("Revelar (prueba)")]
    private void TestReveal() => Reveal();

    [ContextMenu("Usar (prueba)")]
    private void TestActivate() { Reveal(); Activate(); }
}
