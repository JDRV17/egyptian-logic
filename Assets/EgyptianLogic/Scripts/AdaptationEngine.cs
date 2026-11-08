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
        [Tooltip("Quien guarda la versión de este reto: PuzzleVersionManager (1.2), SpinningCubes (1.3), TextSelector (1.4, 1.5) o el script del reto en el Nivel 3. Vacío = el motor no lo controla.")]
        public MonoBehaviour versionTarget;
        [Tooltip("Se dispara justo después de aplicar la versión (aquí van Rebuild, Reconfigure o Reveal).")]
        public UnityEvent onVersionApplied;
    }

    [Serializable]
    public class StageLinks
    {
        public string name = "3.1";
        [Tooltip("Retos (índices locales) que reciben la nueva versión cuando se resuelve este reto.")]
        public int[] stages = new int[0];
    }

    public enum ThetaStart { Diagnostic, CarryOver }

    [Header("Referencias")]
    [SerializeField] private PerformanceTracker tracker;
    [Tooltip("Índice = reto que RECIBE la versión (0 = 1.1, 1 = 1.2, ... 4 = 1.5).")]
    [SerializeField] private StageBinding[] bindings = new StageBinding[5];

    [Header("Inicio del nivel")]
    [Tooltip("Diagnostic: θ del diagnóstico y K desde 1.0 (Nivel 1). CarryOver: sigue con el θ y el K de los niveles anteriores.")]
    [SerializeField] private ThetaStart thetaStart = ThetaStart.Diagnostic;
    [Tooltip("Retos que reciben versión al iniciar el nivel según el θ actual (p. ej., la primera pareja del Nivel 3). " +
             "Vacío en el Nivel 1: la versión de 1.1 la pone el diagnóstico.")]
    [SerializeField] private int[] initialStages = new int[0];
    [Tooltip("Solo con CarryOver. Si el nivel se abre sin datos de los anteriores (p. ej., probándolo directo), " +
             "sigue desde el θ de la última partida guardada en disco (último CSV de decisiones).")]
    [SerializeField] private bool loadLastSaveIfEmpty = false;
    [Tooltip("Versión de los retos iniciales si no hay datos en memoria ni partida guardada (0 = V1).")]
    [Range(0, 2)] [SerializeField] private int versionWithoutData = 0;

    [Header("Sucesores")]
    [Tooltip("Desactivado: al resolver el reto i se ajusta el reto i + 1 (Nivel 1). Activado: se usa la lista de abajo.")]
    [SerializeField] private bool useCustomSuccessors = false;
    [Tooltip("Índice = reto resuelto. Solo se ajustan los retos que todavía no han empezado.")]
    [SerializeField] private StageLinks[] successors = new StageLinks[0];

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
    private int decisionsAtLevelStart;   // en modo Diagnostic, el K se cuenta desde este nivel
    private bool startedWithoutData;     // ni partida en memoria ni guardada: los retos iniciales van en versionWithoutData

    private void Reset() => rules = DefaultRules.Build();

    [ContextMenu("Cargar reglas por defecto")]
    private void LoadDefaults() => rules = DefaultRules.Build();

    private void OnEnable() { if (tracker != null) tracker.onStageCompleted += OnStageCompleted; }
    private void OnDisable() { if (tracker != null) tracker.onStageCompleted -= OnStageCompleted; }

    private void OnStageCompleted(StageResult r) => Decide(r, true);

    private void Awake()
    {
        var s = SessionData.Instance;

        if (thetaStart == ThetaStart.CarryOver && loadLastSaveIfEmpty && s.DecisionsMade == 0 && s.DiagnosticCorrect < 0)
        {
            if (TryLoadLastSave(out float savedTheta, out int savedVersion, out int savedDecisions, out string file))
            {
                s.RestoreFromSave(savedTheta, savedVersion, savedDecisions);
                Debug.Log($"[AdaptationEngine] Sin datos en memoria: se sigue la partida guardada en {file} " +
                          $"(θ = {savedTheta:F2}, última versión V{savedVersion + 1}).");
            }
            else
            {
                startedWithoutData = true;
                Debug.Log($"[AdaptationEngine] Sin datos de niveles anteriores ni partidas guardadas: los retos iniciales van en V{versionWithoutData + 1}.");
            }
        }

        decisionsAtLevelStart = s.DecisionsMade;

        if (thetaStart == ThetaStart.CarryOver && s.DecisionsMade > 0)
        {
            theta = s.FinalTheta;   // ya está en el historial: no se vuelve a registrar
            decisionsAtLevelStart = 0;
            Debug.Log($"[AdaptationEngine] Nivel con θ heredado: {theta:F2} ({s.DecisionsMade} decisiones previas).");
            return;
        }

        if (thetaStart == ThetaStart.CarryOver) decisionsAtLevelStart = 0;
        if (s.DiagnosticCorrect >= 0)
            theta = s.InitialTheta;
        s.RecordTheta(theta);
    }

    private void Start()
    {
        // Después de los Awake: los managers ya eligieron su versión por defecto y aquí se sobrescribe
        if (initialStages != null && initialStages.Length > 0) DecideInitial();
    }

    /// <summary>Número de reto (desde 1) que alimenta las reglas de K.</summary>
    private int StagesCompleted() => SessionData.Instance.DecisionsMade - decisionsAtLevelStart + 1;

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
        if (!HasRules()) return;

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
        f.Set("stagesCompleted", StagesCompleted());
        f.Set("K", 0);
        f.Set("thetaUpdated", 0);
        f.Set("decided", 0);
        f.Set("nextVersion", played);

        // 2. Inferencia
        var trace = engine.Run(rules, f);

        float thetaAfter = f.Get("theta");
        int next = Mathf.Clamp(Mathf.RoundToInt(f.Get("nextVersion")), 0, 2);
        var targets = Successors(r.stageIndex);

        Debug.Log($"[AdaptationEngine]{(commit ? "" : " (SIMULACIÓN)")} Reto {r.stageName} V{played + 1}: P={r.P:F2} " +
                  $"(esperado {pExp:F2}) | θ {theta:F2} → {thetaAfter:F2} | siguiente versión: V{next + 1} → {TargetNames(targets)}\n" +
                  $"Reglas: {string.Join(" → ", trace)}");

        if (!commit) return;

        // 3. Acción
        float before = theta;
        theta = thetaAfter;
        SessionData.Instance.RegisterDecision(played);
        SessionData.Instance.RecordTheta(theta);

        if (writeCsv) WriteCsv(r.stageName, "V" + (played + 1), r.P, before, f, targets, next, trace);
        foreach (int t in targets) ApplyVersion(t, next);
    }

    /// <summary>
    /// Versión de los primeros retos de un nivel a partir del θ actual. Usa las mismas reglas:
    /// θ ya está actualizado, así que solo disparan las de selección y las de seguridad.
    /// </summary>
    private void DecideInitial()
    {
        if (startedWithoutData)
        {
            foreach (int t in initialStages) ApplyVersion(t, versionWithoutData);
            return;
        }
        if (!HasRules()) return;

        int last = SessionData.Instance.LastPlayedVersion;
        int reference = last >= 0 ? last : 1;   // sin historial, se compara contra V2

        var f = new FactBase();
        f.Set("stage", -1);
        f.Set("version", reference);
        f.Set("P", 0);
        f.Set("attempts", 1);
        f.Set("hints", 0);
        f.Set("usedSolution", 0);
        f.Set("theta", theta);
        f.Set("pExpected", 0);
        f.Set("delta", 0);
        f.Set("stagesCompleted", StagesCompleted());
        f.Set("K", 0);
        f.Set("thetaUpdated", 1);
        f.Set("decided", 0);
        f.Set("nextVersion", reference);

        var trace = engine.Run(rules, f);
        int next = Mathf.Clamp(Mathf.RoundToInt(f.Get("nextVersion")), 0, 2);
        var targets = new List<int>(initialStages);

        Debug.Log($"[AdaptationEngine] Inicio del nivel: θ {theta:F2} | versión inicial: V{next + 1} → {TargetNames(targets)}\n" +
                  $"Reglas: {string.Join(" → ", trace)}");

        if (writeCsv) WriteCsv("inicio_nivel", last >= 0 ? "V" + (last + 1) : "-", -1f, theta, f, targets, next, trace);
        foreach (int t in targets) ApplyVersion(t, next);
    }

    private bool HasRules()
    {
        if (rules != null && rules.Count > 0) return true;
        Debug.LogWarning("[AdaptationEngine] No hay reglas. Clic derecho → Cargar reglas por defecto.");
        return false;
    }

    private List<int> Successors(int solved)
    {
        var list = new List<int>();
        if (!useCustomSuccessors)
        {
            list.Add(solved + 1);   // Nivel 1: lineal
            return list;
        }

        if (successors == null || solved < 0 || solved >= successors.Length || successors[solved] == null)
            return list;

        foreach (int s in successors[solved].stages)
        {
            if (list.Contains(s)) continue;
            // No se cambia un reto que el jugador ya empezó (o resolvió)
            if (tracker != null && tracker.GetState(s) != PerformanceTracker.State.NotStarted) continue;
            list.Add(s);
        }
        return list;
    }

    private string BindingName(int stage) =>
        bindings != null && stage >= 0 && stage < bindings.Length && bindings[stage] != null ? bindings[stage].name : "-";

    private string TargetNames(List<int> targets)
    {
        if (targets.Count == 0) return "-";
        var names = new List<string>();
        foreach (int t in targets) names.Add(BindingName(t));
        return string.Join("+", names);
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
            case IVersioned v: v.SelectVersion(version); break;   // retos del Nivel 3
            default:
                if (b.versionTarget != null)
                    Debug.LogWarning($"[AdaptationEngine] Tipo no soportado en el reto {b.name}: {b.versionTarget.GetType().Name}");
                break;
        }

        b.onVersionApplied?.Invoke();
    }

    private void WriteCsv(string solvedName, string playedVersion, float p, float thetaBefore, FactBase f,
                          List<int> targets, int next, List<string> trace)
    {
        var session = SessionData.Instance;
        string pid = session.PlayerName != ""
        ? session.ParticipantId
        : participantId;

        // Un solo archivo de decisiones por partida, aunque se cambie de nivel
        string csvPath = session.DecisionCsvPath;
        if (csvPath == null)
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            csvPath = Path.Combine(Application.persistentDataPath, $"gatekeeper_decisiones_{pid}_{stamp}.csv");
            File.WriteAllText(csvPath,
                "participante,reto_resuelto,version_jugada,P,theta_antes,K,delta,theta_despues,reto_siguiente,version_siguiente,reglas\n",
                Encoding.UTF8);
            session.DecisionCsvPath = csvPath;
            Debug.Log($"[AdaptationEngine] Decisiones en: {csvPath}");
        }

        var c = CultureInfo.InvariantCulture;
        string line = string.Join(",",
            pid, solvedName, playedVersion, p >= 0f ? p.ToString("F3", c) : "-",
            thetaBefore.ToString("F3", c), f.Get("K").ToString("F2", c), f.Get("delta").ToString("F3", c),
            f.Get("theta").ToString("F3", c), TargetNames(targets), "V" + (next + 1), "\"" + string.Join(" > ", trace) + "\"");
        File.AppendAllText(csvPath, line + "\n", Encoding.UTF8);
    }

    /// <summary>
    /// Lee el CSV de decisiones más reciente (el que escribe <see cref="WriteCsv"/>) y devuelve el θ final,
    /// la última versión jugada y cuántas decisiones hubo.
    /// </summary>
    private static bool TryLoadLastSave(out float savedTheta, out int lastVersion, out int decisions, out string file)
    {
        savedTheta = 0f; lastVersion = -1; decisions = 0; file = null;
        try
        {
            var dir = new DirectoryInfo(Application.persistentDataPath);
            if (!dir.Exists) return false;

            FileInfo latest = null;
            foreach (var f in dir.GetFiles("gatekeeper_decisiones_*.csv"))
                if (latest == null || f.LastWriteTime > latest.LastWriteTime) latest = f;
            if (latest == null) return false;

            var c = CultureInfo.InvariantCulture;
            bool any = false;
            foreach (var line in File.ReadAllLines(latest.FullName))
            {
                // participante,reto_resuelto,version_jugada,P,theta_antes,K,delta,theta_despues,... (las reglas van al final, entre comillas)
                var col = line.Split(',');
                if (col.Length < 8 || !float.TryParse(col[7], NumberStyles.Float, c, out float t)) continue;
                savedTheta = t;
                any = true;
                if (col[1] == "inicio_nivel") continue;
                decisions++;
                if (col[2].Length == 2 && col[2][0] == 'V' && char.IsDigit(col[2][1])) lastVersion = col[2][1] - '1';
            }

            file = latest.Name;
            return any;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AdaptationEngine] No se pudo leer la partida guardada: " + e.Message);
            return false;
        }
    }

    private static float Sigmoid(float x) => 1f / (1f + Mathf.Exp(-x));
}
