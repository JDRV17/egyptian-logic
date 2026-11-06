using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Reto 3.3, "El guardián de estados". Una máquina de estados de 4 colores que avanza con el reloj:
///   En cada flanco lee los dos switches (izquierda = bit más significativo) y pasa al estado que indica la tabla.
///   La evaluación se "arma" con el primer movimiento de un switch: desde ahí, cada flanco debe llevar
///   al siguiente color de la secuencia (incluidos los pasos en que el color se mantiene).
///   Si un flanco da otro color: intento fallido, vuelve a rojo, los switches a 00 y se desarma.
///
/// Colores: R = rojo, Y = amarillo, G = verde, B = azul (en ese orden en las bombillas).
/// </summary>
public class StateMachinePuzzle : Level3Puzzle
{
    public const string Colors = "RYGB";

    [Serializable]
    public class VersionSetup
    {
        public string name = "V1";
        [Tooltip("Estado siguiente para las entradas 00, 01, 10, 11. Una línea por estado en el orden R, Y, G, B.")]
        public string[] transitions = new string[4];
        [Tooltip("Colores que debe recorrer la máquina después del rojo inicial (R, Y, G, B).")]
        public string sequence = "";
        [Tooltip("Imagen de la máquina de estados que muestra el cuadro.")]
        public Material diagram;
        [Tooltip("Fila de cuadros de colores con la secuencia (solo se muestra la de la versión activa).")]
        public GameObject sequenceRow;
    }

    private static readonly string[] Regular = { "RGYB", "YBGR", "GRBY", "BYRG" };
    private static readonly string[] Irregular = { "RBGY", "YGRB", "GYBR", "BRYG" };

    [SerializeField]
    private VersionSetup[] versions =
    {
        new VersionSetup { name = "V1", transitions = (string[])Regular.Clone(),   sequence = "BYGY" },
        new VersionSetup { name = "V2", transitions = (string[])Regular.Clone(),   sequence = "GRYRBB" },
        new VersionSetup { name = "V3", transitions = (string[])Irregular.Clone(), sequence = "YGGBRY" },
    };

    [Header("Reloj")]
    [SerializeField] private GameClock clock;

    [Header("Mecanismo")]
    [Tooltip("Bombillas en el orden rojo, amarillo, verde, azul.")]
    [SerializeField] private BitBulb[] stateBulbs = new BitBulb[4];
    [Tooltip("Izquierda = bit más significativo.")]
    [SerializeField] private XRSwitchToggle leftSwitch;
    [SerializeField] private XRSwitchToggle rightSwitch;

    [Header("Cuadro")]
    [SerializeField] private Renderer painting;
    [Tooltip("Lo que muestra el cuadro antes de que el jugador llegue (el tapiz).")]
    [SerializeField] private Material tapestry;
    [SerializeField] private ParticleSystem revealEffect;
    [Tooltip("Lo que gira como panel de tumba al revelar el diagrama. Vacío = el marco del cuadro y el cuadro (se buscan solos).")]
    [SerializeField] private Transform[] spinParts = new Transform[0];
    [Tooltip("Eje del giro, en las coordenadas del cuadro (el objeto Painting). Debe quedar sobre la superficie del cuadro: " +
             "X sirve tanto para un Plane como para un Quad. Prueba Z si prefieres que voltee hacia el otro lado.")]
    [SerializeField] private Vector3 spinAxis = Vector3.right;
    [Tooltip("Segundos de la vuelta completa. 0 = cambio instantáneo, como antes.")]
    [SerializeField] private float spinDuration = 1.2f;

    public int State => state;
    public char StateColor => Colors[state];
    public int Step => step;
    public bool Armed => armed;

    private VersionSetup Setup => versions != null && CurrentVersion < versions.Length ? versions[CurrentVersion] : null;

    private int state;      // 0 = rojo
    private int step;       // pasos de la secuencia ya logrados
    private bool armed;
    private bool resetting;

    protected override void Awake()
    {
        base.Awake();
        if (painting != null && tapestry != null) painting.sharedMaterial = tapestry;
        foreach (var v in versions) if (v?.sequenceRow != null) v.sequenceRow.SetActive(false);
        ShowState(false);
    }

    private void OnEnable()
    {
        if (leftSwitch != null) leftSwitch.onStateChanged.AddListener(OnSwitch);
        if (rightSwitch != null) rightSwitch.onStateChanged.AddListener(OnSwitch);
        if (clock != null) clock.Ticked += OnTick;
    }

    private void OnDisable()
    {
        if (leftSwitch != null) leftSwitch.onStateChanged.RemoveListener(OnSwitch);
        if (rightSwitch != null) rightSwitch.onStateChanged.RemoveListener(OnSwitch);
        if (clock != null) clock.Ticked -= OnTick;
    }

    // ---------- Al llegar el jugador ----------

    protected override void OnEngage()
    {
        var s = Setup;
        if (painting != null && s.diagram != null)
        {
            Fx.Play(revealEffect, painting.transform.position);
            if (spinDuration > 0f)
                StartCoroutine(Motion.SpinSwap(SpinParts(), painting.bounds.center, SpinAxisWorld(), spinDuration, () => painting.sharedMaterial = s.diagram));
            else
                painting.sharedMaterial = s.diagram;
        }
        for (int i = 0; i < versions.Length; i++)
            if (versions[i]?.sequenceRow != null) versions[i].sequenceRow.SetActive(i == CurrentVersion);

        state = 0;
        step = 0;
        armed = false;
        ShowState(true);
        StartTracking();   // el reto empieza al llegar (el tiempo no se puntúa: lo marca el reloj)
    }

    private Vector3 SpinAxisWorld() => painting.transform.TransformDirection(spinAxis == Vector3.zero ? Vector3.right : spinAxis);

    /// <summary>El marco y el cuadro: si el cuadro está dentro del marco basta con el marco; si son hermanos, giran los dos.</summary>
    private Transform[] SpinParts()
    {
        if (spinParts != null && spinParts.Length > 0) return spinParts;
        var p = painting.transform;
        var parent = p.parent;
        if (parent != null && parent.gameObject.name.ToLower().StartsWith("frame")) return new[] { parent };
        if (parent != null)
            for (int i = 0; i < parent.childCount; i++)
            {
                var sib = parent.GetChild(i);
                if (sib != p && sib.gameObject.name.ToLower().StartsWith("frame")) return new[] { sib, p };
            }
        return new[] { p };
    }

    [ContextMenu("Probar el giro del cuadro")]
    private void TestSpin()
    {
        if (painting == null) return;
        var target = Setup?.diagram != null && painting.sharedMaterial != Setup.diagram ? Setup.diagram : tapestry;
        StartCoroutine(Motion.SpinSwap(SpinParts(), painting.bounds.center, SpinAxisWorld(), Mathf.Max(0.2f, spinDuration), () => painting.sharedMaterial = target));
    }

    // ---------- Entradas ----------

    private void OnSwitch(bool on)
    {
        if (resetting || !CanPlay) return;
        CountInteraction();
        armed = true;
    }

    /// <summary>Entrada actual: 0..3 (izquierda = bit más significativo).</summary>
    public int Input => (leftSwitch != null && leftSwitch.IsOn ? 2 : 0) + (rightSwitch != null && rightSwitch.IsOn ? 1 : 0);

    // ---------- Reloj ----------

    private void OnTick(int stage, int n)
    {
        if (stage != stageIndex || !CanPlay) return;
        var s = Setup;

        char next = s.transitions[state][Input];
        state = Colors.IndexOf(next);
        ShowState(true);

        if (!armed) return;

        if (next == s.sequence[step])
        {
            step++;
            if (step >= s.sequence.Length)
            {
                Debug.Log("[StateMachinePuzzle] ¡Secuencia completa!");
                Complete();
            }
            return;
        }

        Debug.Log($"[StateMachinePuzzle] Paso {step + 1}: llegó a {next}, se esperaba {s.sequence[step]}. Reinicio.");
        RegisterIncorrect();
        Restart();
    }

    private void Restart()
    {
        resetting = true;
        if (leftSwitch != null && leftSwitch.IsOn) leftSwitch.ToggleSwitch();
        if (rightSwitch != null && rightSwitch.IsOn) rightSwitch.ToggleSwitch();
        resetting = false;

        state = 0;
        step = 0;
        armed = false;
        ShowState(true);
    }

    private void ShowState(bool on)
    {
        for (int i = 0; i < stateBulbs.Length; i++)
            if (stateBulbs[i] != null) stateBulbs[i].SetOn(on && i == state);
    }
}
