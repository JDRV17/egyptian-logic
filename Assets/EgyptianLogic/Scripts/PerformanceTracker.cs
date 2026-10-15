using System;
using System.Collections;
using UnityEngine;

public class PerformanceTracker : MonoBehaviour
{
    public enum State { NotStarted, Active, Paused, Solved }

    [Header("Referencias")]
    [SerializeField] private HintManager hintManager;
    [SerializeField] private StageConfig[] stages;

    [Header("Pesos (deben sumar 1)")]
    [SerializeField] private float wTime = 0.15f;
    [SerializeField] private float wAttempts = 0.40f;
    [SerializeField] private float wInteractions = 0.15f;
    [SerializeField] private float wHints = 0.30f;

    [Header("Parámetros de las fórmulas")]
    [SerializeField] private float tRefFactor = 2f;       // T_ref = 2 · T_ideal
    [SerializeField] private float tMaxFactor = 5f;       // T_max = 5 · T_ideal
    [SerializeField] private int attemptsForZero = 4;     // 4 intentos o más → S_A = 0
    [SerializeField] private int hintsForZero = 3;        // 3 pistas → S_H = 0
    [Range(0f, 1f)] [SerializeField] private float solutionCap = 0.30f;

    public event Action<StageResult> onStageCompleted;

    private int readingCount;

    public void BeginHintReading() => readingCount++;
    public void EndHintReading() => readingCount = Mathf.Max(0, readingCount - 1);

    private class Runtime
    {
        public State state;
        public float time, hintPause;
        public int incorrect, interactions, idealOverride = -1;
        public bool completing;
        public StageResult result;
    }

    private Runtime[] rt;
    private int active = -1;

    public int ActiveStage => active;
    public State GetState(int i) => Valid(i) ? rt[i].state : State.NotStarted;
    public StageResult GetResult(int i) => Valid(i) ? rt[i].result : null;

    private bool Valid(int i) => rt != null && i >= 0 && i < rt.Length;
    private bool InProgress(int i) => Valid(i) && (rt[i].state == State.Active || rt[i].state == State.Paused);

    private void Awake()
    {
        rt = new Runtime[stages.Length];
        for (int i = 0; i < rt.Length; i++) rt[i] = new Runtime();

        float sum = wTime + wAttempts + wInteractions + wHints;
        if (Mathf.Abs(sum - 1f) > 0.001f)
            Debug.LogWarning($"[PerformanceTracker] Los pesos suman {sum:F2}, deberían sumar 1.");
    }


    private void Update()
    {
        if (active < 0) return;
        if (readingCount > 0) rt[active].hintPause += Time.deltaTime;
        else rt[active].time += Time.deltaTime;
    }

    // ---------- API (se llama desde eventos del Inspector) ----------

    /// <summary>Arranca el reloj del reto (o lo reanuda). Pausa el reto que estaba activo.</summary>
    public void StartStage(int i)
    {
        if (!Valid(i) || rt[i].state == State.Solved) return;
        Activate(i);
    }

    /// <summary>Lo llama HintZone cuando el jugador entra a la zona de un reto.</summary>
    public void EnterZone(int i)
    {
        if (!Valid(i) || rt[i].state == State.Solved) return;

        bool resume = rt[i].state == State.Paused ||
                      (rt[i].state == State.NotStarted && stages[i].startOnZoneEnter);

        if (resume) Activate(i);
        else if (active >= 0 && active != i) PauseCurrent();   // se fue a otro reto
    }

    public void RegisterInteraction(int i)
    {
        if (InProgress(i)) rt[i].interactions++;
    }

    public void RegisterIncorrect(int i)
    {
        if (!InProgress(i)) return;
        rt[i].incorrect++;
        if (stages[i].hasSubmit) rt[i].interactions++;   // la pulsación del submit
    }

    /// <summary>Para retos con interacciones ideales dinámicas (1.3).</summary>
    public void SetIdealInteractions(int i, int value)
    {
        if (Valid(i)) rt[i].idealOverride = value;
    }

    public void CompleteStage(int i)
    {
        if (!InProgress(i) || rt[i].completing) return;
        rt[i].completing = true;
        StartCoroutine(FinishRoutine(i));
    }

    // ---------- Internos ----------

    private void Activate(int i)
    {
        if (active == i) return;
        PauseCurrent();
        active = i;
        rt[i].state = State.Active;
    }

    private void PauseCurrent()
    {
        if (active >= 0 && rt[active].state == State.Active) rt[active].state = State.Paused;
        active = -1;
    }

    private IEnumerator FinishRoutine(int i)
    {
        // Espera al final del frame para que las interacciones del mismo evento ya estén contadas
        yield return new WaitForEndOfFrame();

        var r = rt[i];
        if (stages[i].hasSubmit) r.interactions++;       // el submit correcto

        var cfg = stages[i];
        int version = hintManager != null ? hintManager.StageVersion(i) : 0;
        var ideals = cfg.byVersion[Mathf.Clamp(version, 0, cfg.byVersion.Length - 1)];

        float tIdeal = ideals.tIdeal;
        int iIdeal = r.idealOverride > 0 ? r.idealOverride : ideals.iIdeal;
        int attempts = r.incorrect + 1;
        int hints = hintManager != null ? hintManager.HintsScored(i) : 0;

        float tRef = tIdeal * tRefFactor;
        float tMax = tIdeal * tMaxFactor;

        float sT = r.time <= tRef ? 1f : Mathf.Clamp01(1f - (r.time - tRef) / (tMax - tRef));
        float sA = Mathf.Clamp01(1f - (attempts - 1f) / (attemptsForZero - 1f));
        float sI = Mathf.Clamp01(iIdeal / (float)Mathf.Max(r.interactions, 1));
        float sH = Mathf.Clamp01(1f - hints / (float)hintsForZero);

        float p = wTime * sT + wAttempts * sA + wInteractions * sI + wHints * sH;
        bool capped = hints >= hintsForZero;
        if (capped) p = Mathf.Min(p, solutionCap);

        r.result = new StageResult
        {
            stageName = cfg.name,
            stageIndex = i,
            version = version,
            attempts = attempts,
            interactions = r.interactions,
            idealInteractions = iIdeal,
            hints = hints,
            time = r.time,
            hintPauseTime = r.hintPause,
            idealTime = tIdeal,
            sT = sT,
            sA = sA,
            sI = sI,
            sH = sH,
            P = p,
            cappedBySolution = capped
        };

        r.state = State.Solved;
        if (active == i) active = -1;

        Debug.Log($"[PerformanceTracker] Reto {cfg.name} V{version + 1}: P={p:F2} " +
                  $"(T={r.time:F1}s, A={attempts}, I={r.interactions}/{iIdeal}, H={hints})");
        onStageCompleted?.Invoke(r.result);
    }
}