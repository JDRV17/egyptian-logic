using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

public class AdaptationEngine : MonoBehaviour
{
    [Serializable]
    public class StageBinding
    {
        public string name = "1.2";
        [Tooltip("Quien guarda la versión de este reto: PuzzleVersionManager (1.2), SpinningCubes (1.3) o TextSelector (1.4, 1.5). Vacío = el motor no lo controla.")]
        public MonoBehaviour versionTarget;
        [Tooltip("Se dispara justo después de aplicar la versión (aquí van Rebuild, Reconfigure o Reveal).")]
        public UnityEvent onVersionApplied;
    }

    [Header("Referencias")]
    [SerializeField] private PerformanceTracker tracker;
    [Tooltip("Índice = reto que RECIBE la versión (0 = 1.1, 1 = 1.2, ... 4 = 1.5).")]
    [SerializeField] private StageBinding[] bindings = new StageBinding[5];

    [Header("Modelo de habilidad")]
    [SerializeField] private float theta = 0f;
    [Tooltip("P mínimo y máximo esperables. P_esperado = pMin + (pMax − pMin)·σ(θ − b).")]
    [SerializeField] private float pMin = 0.3f;
    [SerializeField] private float pMax = 1f;

    [Header("Base de reglas")]
    [SerializeField] private List<AdaptRule> rules = new List<AdaptRule>();

    [Header("Registro")]
    [SerializeField] private bool writeCsv = true;
    [SerializeField] private string participantId = "test";

    [Header("Simulador (clic derecho en el componente → Simular decisión)")]
    [SerializeField] private int simStage = 0;
    [Range(0, 2)] [SerializeField] private int simVersion = 1;
    [Range(0f, 1f)] [SerializeField] private float simP = 0.9f;
    [SerializeField] private int simAttempts = 1;
    [Range(0, 3)] [SerializeField] private int simHints = 0;
    [SerializeField] private bool simUsedSolution = false;

    public float Theta => theta;

    private readonly RuleEngine engine = new RuleEngine();
    private int decisions;
    private string csvPath;

    private void Reset() => rules = DefaultRules.Build();

    [ContextMenu("Cargar reglas por defecto")]
    private void LoadDefaults() => rules = DefaultRules.Build();

    private void OnEnable() { if (tracker != null) tracker.onStageCompleted += OnStageCompleted; }
    private void OnDisable() { if (tracker != null) tracker.onStageCompleted -= OnStageCompleted; }

    private void OnStageCompleted(StageResult r) => Decide(r, true);

    [ContextMenu("Simular decisión")]
    private void Simulate()
    {
        var r = new StageResult
        {
            stageName = "sim",
            stageIndex = simStage,
            version = simVersion,
            P = simP,
            attempts = simAttempts,
            hints = simHints,
            cappedBySolution = simUsedSolution
        };
        Decide(r, false);   // sin cambiar θ ni aplicar versiones
    }

    private void Decide(StageResult r, bool commit)
    {
        if (rules == null || rules.Count == 0)
        {
            Debug.LogWarning("[AdaptationEngine] No hay reglas. Clic derecho → Cargar reglas por defecto.");
            return;
        }

        int played = r.version;
        float b = played - 1f;                                   // dificultad: V1 = −1, V2 = 0, V3 = +1
        float pExp = pMin + (pMax - pMin) * Sigmoid(theta - b);

        // 1. Percepción: se cargan los hechos
        var f = new FactBase();
        f.Set("stage", r.stageIndex);
        f.Set("version", played);
        f.Set("P", r.P);
        f.Set("attempts", r.attempts);
        f.Set("hints", r.hints);
        f.Set("usedSolution", r.cappedBySolution ? 1 : 0);
        f.Set("theta", theta);
        f.Set("pExpected", pExp);
        f.Set("delta", r.P - pExp);
        f.Set("stagesCompleted", decisions + 1);
        f.Set("K", 0);
        f.Set("thetaUpdated", 0);
        f.Set("decided", 0);
        f.Set("nextVersion", played);

        // 2. Inferencia
        var trace = engine.Run(rules, f);

        float thetaAfter = f.Get("theta");
        int next = Mathf.Clamp(Mathf.RoundToInt(f.Get("nextVersion")), 0, 2);
        int nextStage = r.stageIndex + 1;

        Debug.Log($"[AdaptationEngine]{(commit ? "" : " (SIMULACIÓN)")} Reto {r.stageName} V{played + 1}: P={r.P:F2} " +
                  $"(esperado {pExp:F2}) | θ {theta:F2} → {thetaAfter:F2} | siguiente versión: V{next + 1}\n" +
                  $"Reglas: {string.Join(" → ", trace)}");

        if (!commit) return;

        // 3. Acción
        float before = theta;
        theta = thetaAfter;
        decisions++;

        if (writeCsv) WriteCsv(r, before, f, nextStage, next, trace);
        ApplyVersion(nextStage, next);
    }

    private void ApplyVersion(int stage, int version)
    {
        if (bindings == null || stage < 0 || stage >= bindings.Length || bindings[stage] == null) return;
        var b = bindings[stage];

        switch (b.versionTarget)
        {
            case PuzzleVersionManager p: p.SelectVersion((LightCombinationLock.LevelVersion)version); break;
            case SpinningCubes s: s.SelectVersion((SpinningCubes.Version)version); break;
            case TextSelector t: t.SelectVersion(version); break;
            default:
                if (b.versionTarget != null)
                    Debug.LogWarning($"[AdaptationEngine] Tipo no soportado en el reto {b.name}: {b.versionTarget.GetType().Name}");
                break;
        }

        b.onVersionApplied?.Invoke();
    }

    private void WriteCsv(StageResult r, float thetaBefore, FactBase f, int nextStage, int next, List<string> trace)
    {
        if (csvPath == null)
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            csvPath = Path.Combine(Application.persistentDataPath, $"gatekeeper_decisiones_{participantId}_{stamp}.csv");
            File.WriteAllText(csvPath,
                "participante,reto_resuelto,version_jugada,P,theta_antes,K,delta,theta_despues,reto_siguiente,version_siguiente,reglas\n",
                Encoding.UTF8);
            Debug.Log($"[AdaptationEngine] Decisiones en: {csvPath}");
        }

        var c = CultureInfo.InvariantCulture;
        string nextName = nextStage < bindings.Length && bindings[nextStage] != null ? bindings[nextStage].name : "-";
        string line = string.Join(",",
            participantId, r.stageName, "V" + (r.version + 1), r.P.ToString("F3", c),
            thetaBefore.ToString("F3", c), f.Get("K").ToString("F2", c), f.Get("delta").ToString("F3", c),
            f.Get("theta").ToString("F3", c), nextName, "V" + (next + 1), "\"" + string.Join(" > ", trace) + "\"");
        File.AppendAllText(csvPath, line + "\n", Encoding.UTF8);
    }

    private static float Sigmoid(float x) => 1f / (1f + Mathf.Exp(-x));
}