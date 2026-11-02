using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// El gran reloj de piedra del Nivel 3. Da tics a ritmo fijo y sigue al último reto cuyo cable
/// se conectó (al inicio, 3.1). Del reto que sigue toma el periodo (por versión) y si mueve el horario.
///
/// Manecillas: el minutero da una vuelta completa por periodo (al llegar a las 12 hay flanco)
/// y el horario avanza una hora por flanco.
///
/// Los retos escuchan <see cref="Ticked"/> y se fijan en el índice del reto que sigue el reloj.
/// </summary>
public class GameClock : MonoBehaviour
{
    [Serializable]
    public class ClockProfile
    {
        public string name = "3.1";
        [Tooltip("Índice del reto en el PerformanceTracker (0 = 3.1 … 4 = 3.5).")]
        public int stageIndex;
        [Tooltip("Segundos entre flancos para V1, V2 y V3.")]
        public float[] periodByVersion = { 4f, 4f, 4f };
        [Tooltip("Márcalo para que el horario NO avance mientras el reloj sigue a este reto (3.5: el jugador cuenta de memoria).")]
        public bool freezeHourHand;
        [Tooltip("Renderers del cable de este reto: brillan un instante en cada flanco. El material debe tener Emission activado.")]
        public Renderer[] cableRenderers = new Renderer[0];
    }

    [Header("Referencias")]
    [SerializeField] private PerformanceTracker tracker;
    [Tooltip("De aquí se lee la versión activa de cada reto (para el periodo).")]
    [SerializeField] private HintManager hintManager;

    [Header("Retos que usan el reloj")]
    [SerializeField] private ClockProfile[] profiles = new ClockProfile[0];
    [Tooltip("Reto al que está conectado el reloj al iniciar (3.1).")]
    [SerializeField] private int initialStage = 0;

    [Header("Manecillas (hijos del modelo)")]
    [SerializeField] private Transform minuteHand;
    [SerializeField] private Transform hourHand;
    [Tooltip("Eje local sobre el que giran las manecillas. Pruébalo en el Inspector: Z suele ser el correcto si las manecillas miran al frente.")]
    [SerializeField] private Vector3 handAxis = Vector3.forward;
    [Tooltip("Invierte el sentido si las manecillas giran al revés.")]
    [SerializeField] private bool invertDirection = false;
    [Tooltip("Duración de la animación del horario al avanzar una hora en cada tic (para que no salte de golpe).")]
    [SerializeField] private float hourStepTime = 0.35f;

    [Header("Sonido")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tickClip;
    [Tooltip("Sonido alterno (3.5.3: cuenta hacia atrás).")]
    [SerializeField] private AudioClip altTickClip;
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;

    [Header("Pulso en el cable")]
    [ColorUsage(true, true)] [SerializeField] private Color pulseColor = new Color(1f, 0.6f, 0.2f) * 2f;
    [SerializeField] private float pulseTime = 0.4f;

    [Header("Eventos")]
    public UnityEvent onTick;

    /// <summary>(índice del reto que sigue el reloj, número de tic desde que lo sigue: 1, 2, 3…)</summary>
    public event Action<int, int> Ticked;
    /// <summary>Se dispara al cambiar el reto que sigue el reloj (índice del reto).</summary>
    public event Action<int> Connected;
    /// <summary>Se dispara cuando termina una cuenta pedida con <see cref="RunTicks"/> (índice del reto).</summary>
    public event Action<int> TicksFinished;

    private ClockProfile current;
    private float phase;            // 0..1 dentro del periodo
    private bool running = true;
    private int ticksLeft = -1;     // −1 = sin límite
    private int tickCount;
    private bool altSound;

    private Quaternion minuteRest, hourRest;
    private float hourAngle, hourTarget;
    private float pulse;
    private MaterialPropertyBlock block;

    public int FollowedStage => current != null ? current.stageIndex : -1;
    public bool IsFollowing(int stage) => current != null && current.stageIndex == stage;
    public bool IsRunning => running;
    public int TickCount => tickCount;
    public float Phase => phase;
    public float CurrentPeriod => current != null ? PeriodOf(current) : 0f;

    private void Awake()
    {
        if (minuteHand != null) minuteRest = minuteHand.localRotation;
        if (hourHand != null) hourRest = hourHand.localRotation;
        block = new MaterialPropertyBlock();
    }

    private void Start()
    {
        // Todos los cables empiezan apagados (anula la emisión que traiga el material)
        if (profiles != null)
            foreach (var p in profiles) SetPulse(p, 0f);
        Connect(initialStage);
    }

    // ---------- API ----------

    /// <summary>El reloj pasa a seguir a este reto. Lo llama el ClockCableSocket al reparar un cable.</summary>
    public void Connect(int stage)
    {
        var p = Find(stage);
        if (p == null)
        {
            Debug.LogWarning($"[GameClock] No hay perfil para el reto {stage}.");
            return;
        }

        SetPulse(current, 0f);
        current = p;
        phase = 0f;
        tickCount = 0;
        ticksLeft = -1;
        running = true;
        altSound = false;

        Debug.Log($"[GameClock] Siguiendo a {p.name} (periodo {PeriodOf(p):F1} s).");
        Connected?.Invoke(stage);
    }

    /// <summary>
    /// Reinicia el periodo y da exactamente n tics más; luego se detiene y dispara TicksFinished (3.5).
    /// </summary>
    public void RunTicks(int n)
    {
        phase = 0f;
        ticksLeft = Mathf.Max(0, n);
        running = ticksLeft > 0;
        if (!running) TicksFinished?.Invoke(FollowedStage);
    }

    /// <summary>Vuelve a correr sin límite de tics.</summary>
    public void Resume()
    {
        ticksLeft = -1;
        running = true;
    }

    public void Stop() => running = false;

    /// <summary>Usa el sonido alterno a partir del siguiente tic (3.5.3).</summary>
    public void SetAltSound(bool on) => altSound = on;

    // ---------- Internos ----------

    private void Update()
    {
        Step(Time.deltaTime);
        UpdateHands(Time.deltaTime);
        UpdatePulse(Time.deltaTime);
    }

    /// <summary>Avanza el reloj dt segundos (público para pruebas).</summary>
    public void Step(float dt)
    {
        if (current == null || !running) return;
        if (tracker != null && tracker.IsReadingHint) return;   // se congela mientras se lee una pista

        float period = Mathf.Max(0.1f, PeriodOf(current));
        phase += dt / period;

        while (phase >= 1f && running)
        {
            phase -= 1f;
            Tick();
        }
        if (!running) phase = 0f;
    }

    private void Tick()
    {
        tickCount++;
        if (!current.freezeHourHand) hourTarget += 30f;

        var clip = altSound && altTickClip != null ? altTickClip : tickClip;
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, volume * SessionData.Instance.SfxVolume01);

        pulse = 1f;

        if (ticksLeft > 0)
        {
            ticksLeft--;
            if (ticksLeft == 0) running = false;
        }

        int stage = current.stageIndex;
        Ticked?.Invoke(stage, tickCount);
        onTick?.Invoke();

        if (!running && ticksLeft == 0)
        {
            ticksLeft = -1;
            TicksFinished?.Invoke(stage);
        }
    }

    private void UpdateHands(float dt)
    {
        float sign = invertDirection ? 1f : -1f;   // en Unity, un ángulo negativo sobre Z gira en sentido horario
        Vector3 axis = handAxis.sqrMagnitude > 0f ? handAxis.normalized : Vector3.forward;

        if (minuteHand != null)
            minuteHand.localRotation = minuteRest * Quaternion.AngleAxis(sign * 360f * phase, axis);

        if (hourHand != null)
        {
            float speed = 30f / Mathf.Max(0.01f, hourStepTime);
            hourAngle = Mathf.MoveTowards(hourAngle, hourTarget, speed * dt);
            hourHand.localRotation = hourRest * Quaternion.AngleAxis(sign * hourAngle, axis);
        }
    }

    private void UpdatePulse(float dt)
    {
        if (current == null) return;
        pulse = Mathf.Max(0f, pulse - dt / Mathf.Max(0.01f, pulseTime));
        SetPulse(current, pulse);
    }

    private void SetPulse(ClockProfile p, float amount)
    {
        if (p == null || p.cableRenderers == null || block == null) return;
        foreach (var r in p.cableRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", pulseColor * amount);
            r.SetPropertyBlock(block);
        }
    }

    private float PeriodOf(ClockProfile p)
    {
        int v = hintManager != null ? hintManager.StageVersion(p.stageIndex) : 1;
        if (p.periodByVersion == null || p.periodByVersion.Length == 0) return 4f;
        return p.periodByVersion[Mathf.Clamp(v, 0, p.periodByVersion.Length - 1)];
    }

    private ClockProfile Find(int stage)
    {
        if (profiles == null) return null;
        foreach (var p in profiles)
            if (p != null && p.stageIndex == stage) return p;
        return null;
    }
}
