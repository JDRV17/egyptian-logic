using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base de los retos del Nivel 3 que se abren después de 3.1 (3.2–3.5).
///
/// Ciclo de un reto:
///   Bloqueado   → hasta que se conecta el cable de su pareja (Unlock, desde el ClockCableSocket).
///   Desbloqueado → el motor todavía puede cambiar su versión.
///   Comprometido → el jugador entró a su HintZone: la versión se fija y aparece lo que depende de ella.
///   Resuelto.
/// Además, la PairExclusion puede "pausarlo" (Blocked) mientras el otro reto de la pareja está comprometido.
/// </summary>
public abstract class Level3Puzzle : MonoBehaviour, IVersioned
{
    [Header("Sistema")]
    [SerializeField] protected PerformanceTracker tracker;
    [SerializeField] protected HintManager hintManager;
    [SerializeField] protected int stageIndex;

    [Header("Versión")]
    [SerializeField] private bool randomizeOnStart = false;
    [Range(0, 2)] [SerializeField] private int forcedVersion = 1;

    [Header("Disponibilidad")]
    [Tooltip("Desmarcado: el reto espera a que se conecte el cable de su pareja (Unlock).")]
    [SerializeField] private bool startsUnlocked = false;

    [Header("Eventos")]
    public UnityEvent onEngaged;
    public UnityEvent onSolved;
    public UnityEvent onIncorrect;

    public event Action<Level3Puzzle> Engaged;
    public event Action<Level3Puzzle> Solved;

    public int StageIndex => stageIndex;
    public int CurrentVersion { get; private set; }
    public bool IsUnlocked { get; private set; }
    public bool IsEngaged { get; private set; }
    public bool IsBlocked { get; private set; }
    public bool IsSolved { get; private set; }

    /// <summary>Se puede jugar ahora mismo.</summary>
    public bool CanPlay => IsUnlocked && IsEngaged && !IsBlocked && !IsSolved;
    /// <summary>La HintZone cuenta la llegada del jugador (pistas y reloj del tracker).</summary>
    public bool AcceptsPlayer => IsUnlocked && !IsBlocked && !IsSolved;

    private bool playerInside;
    private bool started;

    protected virtual void Awake()
    {
        CurrentVersion = randomizeOnStart ? UnityEngine.Random.Range(0, 3) : forcedVersion;
        IsUnlocked = startsUnlocked;
    }

    // ---------- IVersioned ----------

    /// <summary>Lo llama el AdaptationEngine. Se ignora una vez que el jugador llegó al reto.</summary>
    public void SelectVersion(int version)
    {
        if (IsEngaged || IsSolved) return;
        CurrentVersion = Mathf.Clamp(version, 0, 2);
    }

    // ---------- Disponibilidad ----------

    /// <summary>Conéctalo al On Connected del ClockCableSocket de la pareja.</summary>
    public void Unlock()
    {
        if (IsUnlocked) return;
        IsUnlocked = true;
        TryEngage();
    }

    /// <summary>Lo llama la HintZone (On Player Enter).</summary>
    public void PlayerEntered()
    {
        playerInside = true;
        TryEngage();
    }

    /// <summary>Lo llama la HintZone (On Player Exit).</summary>
    public void PlayerExited() => playerInside = false;

    /// <summary>Lo usa la PairExclusion.</summary>
    public void SetBlocked(bool value)
    {
        if (IsBlocked == value) return;
        IsBlocked = value;
        OnBlockedChanged(value);
        if (!value) TryEngage();   // si el jugador está esperando dentro de la zona
    }

    private void TryEngage()
    {
        if (IsEngaged || !playerInside || !IsUnlocked || IsBlocked || IsSolved) return;
        IsEngaged = true;
        Debug.Log($"[{GetType().Name}] Jugador en el reto: versión V{CurrentVersion + 1} fijada.");
        OnEngage();
        onEngaged?.Invoke();
        Engaged?.Invoke(this);
    }

    // ---------- Para las clases hijas ----------

    /// <summary>La versión ya es definitiva: mostrar lo que depende de ella.</summary>
    protected abstract void OnEngage();

    protected virtual void OnBlockedChanged(bool blocked) { }

    /// <summary>Arranca el reloj del tracker sin contar una interacción (p. ej., al llegar al reto).</summary>
    protected void StartTracking()
    {
        if (tracker == null || started) return;
        started = true;
        tracker.StartStage(stageIndex);
    }

    /// <summary>Cuenta una interacción (y arranca el reloj del tracker con la primera).</summary>
    protected void CountInteraction()
    {
        if (tracker == null) return;
        StartTracking();
        tracker.RegisterInteraction(stageIndex);
    }

    protected void RegisterIncorrect()
    {
        if (tracker != null) tracker.RegisterIncorrect(stageIndex);
        onIncorrect?.Invoke();
    }

    protected void Complete()
    {
        if (IsSolved) return;
        IsSolved = true;
        if (hintManager != null) hintManager.MarkSolved(stageIndex);
        if (tracker != null) tracker.CompleteStage(stageIndex);
        Debug.Log($"[{GetType().Name}] ¡Reto resuelto!");
        onSolved?.Invoke();
        Solved?.Invoke(this);
    }

    // ---------- Pruebas en el editor ----------

    [ContextMenu("Desbloquear y entrar (prueba)")]
    private void TestEnter() { Unlock(); PlayerEntered(); }
}
