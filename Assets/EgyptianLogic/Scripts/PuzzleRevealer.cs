using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[DefaultExecutionOrder(0)]
public class PuzzleRevealer : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float duration = 4f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Extras (opcionales)")]
    [SerializeField] private AudioSource rumbleAudio;
    public UnityEvent onRevealStarted;
    public UnityEvent onRevealed;

    private RevealPart[] parts;
    private bool revealed;
    private readonly List<Behaviour> disabledInteractors = new List<Behaviour>();

    private readonly List<(Rigidbody rb, bool wasKinematic, bool hadGravity)> bodies =
    new List<(Rigidbody, bool, bool)>();

    public bool IsRevealed => revealed;

    private void Awake()
    {
        // El PuzzleVersionManager (orden -100) ya desactivó las versiones no elegidas,
        // así que aquí solo entran las piezas de la versión activa
        parts = GetComponentsInChildren<RevealPart>(false);
        Debug.Log($"[PuzzleRevealer] {name}: {parts.Length} pieza(s) activas capturadas.");
        parts = GetComponentsInChildren<RevealPart>(false);

        foreach (var rb in GetComponentsInChildren<Rigidbody>(false))
        {
            bodies.Add((rb, rb.isKinematic, rb.useGravity));
            rb.isKinematic = true;
        }

        foreach (var p in parts)
        {
            p.Capture();
            p.Hide();
        }
    }

    private void Start()
    {
        // Start corre después del Awake de PuzzleVersionManager, así que solo
        // se bloquean los elementos de la versión activa
        DisableInteractions();
    }

    /// <summary>Conéctalo al evento On State Changed (bool) del XRSwitchToggle.</summary>
    public void OnSwitchChanged(bool isOn)
    {
        Debug.Log($"[PuzzleRevealer] {name}: OnSwitchChanged({isOn})");
        if (isOn) Reveal();
    }

    public void Reveal()
    {
        if (revealed) return;
        revealed = true;
        StartCoroutine(RevealRoutine());
    }

    private IEnumerator RevealRoutine()
    {
        onRevealStarted?.Invoke();
        if (rumbleAudio != null) rumbleAudio.Play();

        float maxDelay = 0f;
        foreach (var p in parts) maxDelay = Mathf.Max(maxDelay, p.delay);

        float total = duration + maxDelay;
        float t = 0f;

        while (t < total)
        {
            t += Time.deltaTime;

            foreach (var p in parts)
            {
                float local = Mathf.Clamp01((t - p.delay) / duration);
                p.SetProgress(curve.Evaluate(local));
            }

            yield return null;
        }

        foreach (var p in parts) p.SetProgress(1f);
        if (rumbleAudio != null) rumbleAudio.Stop();

        foreach (var b in bodies)
        {
            if (b.rb == null) continue;
            b.rb.isKinematic = b.wasKinematic;
            b.rb.useGravity = b.hadGravity;
        }
        bodies.Clear();

        EnableInteractions();
        onRevealed?.Invoke();
    }

    private void DisableInteractions()
    {
        foreach (var i in GetComponentsInChildren<XRBaseInteractable>(false))
            if (i.enabled) { i.enabled = false; disabledInteractors.Add(i); }

        foreach (var s in GetComponentsInChildren<XRSocketInteractor>(false))
            if (s.enabled) { s.enabled = false; disabledInteractors.Add(s); }
    }

    private void EnableInteractions()
    {
        foreach (var b in disabledInteractors)
            if (b != null) b.enabled = true;
        disabledInteractors.Clear();
    }
}