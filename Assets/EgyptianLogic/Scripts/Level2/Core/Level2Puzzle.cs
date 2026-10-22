using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Base de los retos del Nivel 2: botón de comprobar, cartel de estado, luz de resultado,
/// eventos y conexión opcional con el PerformanceTracker del Nivel 1.
/// </summary>
public abstract class Level2Puzzle : MonoBehaviour
{
    [Header("Identificación")]
    [Tooltip("Código del reto según el documento, ej. 2.1.1")]
    public string puzzleId = "2.x.x";

    [Header("Seguimiento de desempeño (opcional)")]
    public PerformanceTracker tracker;
    [Tooltip("Índice del reto en el PerformanceTracker. -1 = no se reporta.")]
    public int stageIndex = -1;
    [Tooltip("La primera interacción arranca el reloj del reto.")]
    public bool startStageOnFirstInteraction = true;

    [Header("Comprobación")]
    [Tooltip("Botón físico que valida la respuesta. Algunos retos se validan solos al probarlos.")]
    public XRPhysicalButton submitButton;

    [Header("Retroalimentación")]
    public TextMeshPro statusText;
    public Renderer resultLight;
    public Color idleColor = new Color(0.35f, 0.3f, 0.2f);
    public Color correctColor = new Color(0.2f, 1f, 0.3f);
    public Color incorrectColor = new Color(1f, 0.2f, 0.15f);
    [SerializeField] private float incorrectFlashSeconds = 1.5f;

    [Header("Sonidos (opcionales)")]
    public SfxPlayer successSfx;
    public SfxPlayer failSfx;

    [Header("Eventos")]
    public UnityEvent onSolved;
    public UnityEvent onIncorrect;
    [Tooltip("Cada interacción del jugador con el reto (útil para sonidos o métricas propias).")]
    public UnityEvent onInteraction;

    public bool IsSolved { get; private set; }
    public int Interactions { get; private set; }
    public int IncorrectAttempts { get; private set; }

    private readonly List<Action> unsubscribe = new List<Action>();
    private Coroutine flash;

    protected virtual void OnEnable()
    {
        if (submitButton != null) submitButton.onPressed.AddListener(HandleSubmit);
    }

    protected virtual void OnDisable()
    {
        if (submitButton != null) submitButton.onPressed.RemoveListener(HandleSubmit);
        foreach (var u in unsubscribe) u();
        unsubscribe.Clear();
    }

    protected virtual void Start()
    {
        Level2Visuals.SetColor(resultLight, idleColor, false);
    }

    private void HandleSubmit()
    {
        if (IsSolved) return;
        Submit();
    }

    /// <summary>Valida la respuesta actual (lo llama el botón; también se puede llamar desde eventos).</summary>
    public abstract void Submit();

    /// <summary>Cuenta switches y piezas encajadas dentro de <paramref name="root"/> como interacciones.</summary>
    protected void HookStandardInteractions(Transform root)
    {
        foreach (var s in root.GetComponentsInChildren<XRSwitchToggle>(true))
        {
            var sw = s;
            UnityAction<bool> a = _ => ReportInteraction();
            sw.onStateChanged.AddListener(a);
            unsubscribe.Add(() => sw.onStateChanged.RemoveListener(a));
        }

        foreach (var s in root.GetComponentsInChildren<XRSocketInteractor>(true))
        {
            var sk = s;
            UnityAction<SelectEnterEventArgs> a = args =>
            {
                // Un socket que agarra algo al iniciar (piezas precolocadas) no es una interacción del jugador
                if (Time.timeSinceLevelLoad > 0.5f) ReportInteraction();
            };
            sk.selectEntered.AddListener(a);
            unsubscribe.Add(() => sk.selectEntered.RemoveListener(a));
        }
    }

    public void ReportInteraction()
    {
        if (IsSolved) return;
        Interactions++;

        if (tracker != null && stageIndex >= 0)
        {
            if (startStageOnFirstInteraction) tracker.StartStage(stageIndex);
            tracker.RegisterInteraction(stageIndex);
        }

        onInteraction?.Invoke();
    }

    protected void MarkSolved(string message)
    {
        if (IsSolved) return;
        IsSolved = true;

        if (flash != null) StopCoroutine(flash);
        Level2Visuals.SetColor(resultLight, correctColor, true);
        SetStatus(message, correctColor);
        if (successSfx != null) successSfx.Play();

        Debug.Log($"[{puzzleId}] Resuelto. Interacciones: {Interactions}, intentos fallidos: {IncorrectAttempts}");

        if (tracker != null && stageIndex >= 0)
        {
            tracker.StartStage(stageIndex); // por si se resolvió sin interacciones previas
            tracker.CompleteStage(stageIndex);
        }

        onSolved?.Invoke();
    }

    protected void MarkIncorrect(string message)
    {
        if (IsSolved) return;
        IncorrectAttempts++;

        SetStatus(message, incorrectColor);
        if (failSfx != null) failSfx.Play();
        if (flash != null) StopCoroutine(flash);
        flash = StartCoroutine(FlashIncorrect());

        Debug.Log($"[{puzzleId}] Incorrecto: {message}");

        if (tracker != null && stageIndex >= 0)
        {
            tracker.StartStage(stageIndex);
            tracker.RegisterIncorrect(stageIndex);
        }

        onIncorrect?.Invoke();
    }

    protected void SetStatus(string message, Color? color = null)
    {
        if (statusText == null) return;
        statusText.text = message;
        if (color.HasValue) statusText.color = Color.Lerp(color.Value, Color.white, 0.35f);
    }

    private IEnumerator FlashIncorrect()
    {
        Level2Visuals.SetColor(resultLight, incorrectColor, true);
        yield return new WaitForSeconds(incorrectFlashSeconds);
        if (!IsSolved) Level2Visuals.SetColor(resultLight, idleColor, false);
        flash = null;
    }

    /// <summary>Saltar el reto desde el editor para probar el flujo del nivel.</summary>
    [ContextMenu("Forzar resuelto (prueba)")]
    private void ForceSolve() => MarkSolved("Resuelto (forzado desde el editor)");
}
