using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Baldosa suelta del piso: se hunde, suena y brilla cuando el jugador (tag "Player") o un objeto con
/// Rigidbody la pisa, y vuelve a subir cuando queda libre. Es solo para explorar: no afecta los retos.
/// Necesita un collider "trigger" (en este objeto o en un hijo) que cubra la parte de arriba de la baldosa.
/// </summary>
public class FloorTile : MonoBehaviour
{
    [Tooltip("Parte visible que se hunde. Vacío = este objeto.")]
    public Transform tile;
    public float sinkDepth = 0.03f;
    public float speed = 0.25f;

    [Header("Brillo")]
    public Renderer glowRenderer;
    public Color restColor = new Color(0.55f, 0.45f, 0.3f);
    public Color pressedColor = new Color(1f, 0.7f, 0.25f);

    [Header("Sonido")]
    public AudioSource pressSound;

    public UnityEvent onPressed;

    private Vector3 restPos;
    private int occupants;
    private Coroutine move;

    private void Awake()
    {
        if (tile == null) tile = transform;
        restPos = tile.localPosition;
    }

    private void Start() => Level2Visuals.SetColor(glowRenderer, restColor, false);

    private bool Counts(Collider c) => c.CompareTag("Player") || c.attachedRigidbody != null;

    private void OnTriggerEnter(Collider other)
    {
        if (!Counts(other)) return;
        occupants++;
        if (occupants == 1) Press(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!Counts(other)) return;
        occupants = Mathf.Max(0, occupants - 1);
        if (occupants == 0) Press(false);
    }

    private void Press(bool down)
    {
        if (down)
        {
            if (pressSound != null) { pressSound.pitch = Random.Range(0.9f, 1.1f); pressSound.Play(); }
            onPressed?.Invoke();
        }

        Level2Visuals.SetColor(glowRenderer, down ? pressedColor : restColor, down, 0.8f);
        if (move != null) StopCoroutine(move);
        move = StartCoroutine(MoveTo(restPos - new Vector3(0f, down ? sinkDepth : 0f, 0f)));
    }

    private IEnumerator MoveTo(Vector3 target)
    {
        while ((tile.localPosition - target).sqrMagnitude > 0.000001f)
        {
            tile.localPosition = Vector3.MoveTowards(tile.localPosition, target, speed * Time.deltaTime);
            yield return null;
        }
        tile.localPosition = target;
        move = null;
    }
}
