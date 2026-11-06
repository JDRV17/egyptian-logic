using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Reto 3.5, "Contrarreloj binaria". Un contador que avanza con el reloj:
///   Empezar (botón): el reloj grande se calla; en cada flanco suena beep (o clock_tock si cuenta hacia atrás)
///     y el contador suma o resta 1. Los bits se ven solo durante los primeros flancos (ayuda);
///     la luz roja marca la cuenta hacia atrás durante todo el conteo.
///   Al terminar los flancos, el reloj se detiene: cada switch enciende su bombilla y el botón envía la respuesta.
///   Correcto → resuelto. Incorrecto → intento fallido y vuelta al inicio (hay que escuchar el conteo otra vez).
///
/// Columnas de izquierda a derecha = del bit más significativo al menos.
/// Con 2 bits (V1) se hunden las dos columnas de la izquierda y la de la luz roja.
/// </summary>
public class CountdownPuzzle : Level3Puzzle
{
    [Serializable]
    public class VersionSetup
    {
        public string name = "V1";
        [Tooltip("2 o 4 bits.")]
        public int bits = 2;
        [Tooltip("Tramos del conteo: positivo = hacia adelante, negativo = hacia atrás. Ej: 4, -2, 2, -3, 4.")]
        public int[] steps = { 6 };
        [Tooltip("Hasta qué flanco se ven los bits (ayuda visual).")]
        public int visibleTicks = 3;
    }

    public enum Phase { Idle, Counting, Answer, Solved }

    [SerializeField]
    private VersionSetup[] versions =
    {
        new VersionSetup { name = "V1", bits = 2, steps = new[] { 6 },               visibleTicks = 3 },
        new VersionSetup { name = "V2", bits = 4, steps = new[] { 13 },              visibleTicks = 3 },
        new VersionSetup { name = "V3", bits = 4, steps = new[] { 4, -2, 2, -3, 4 }, visibleTicks = 7 },
    };

    [Header("Reloj")]
    [SerializeField] private GameClock clock;

    [Header("Columnas (izquierda → derecha)")]
    [SerializeField] private BitBulb[] bitBulbs = new BitBulb[4];
    [SerializeField] private XRSwitchToggle[] switches = new XRSwitchToggle[4];
    [Tooltip("Raíz de cada columna (barra + bombilla + switch): las dos primeras se hunden en V1.")]
    [SerializeField] private Transform[] bitColumns = new Transform[4];
    [SerializeField] private BitBulb redBulb;
    [SerializeField] private Transform redColumn;
    [SerializeField] private XRPhysicalButton button;

    [Header("Sonidos del conteo")]
    [Tooltip("Sale del reloj mini, junto al reto.")]
    [SerializeField] private AudioSource countSource;
    [SerializeField] private AudioClip beepClip;
    [SerializeField] private AudioClip tockClip;

    [Header("Hundirse (V1)")]
    [SerializeField] private float sinkDepth = 1.3f;
    [SerializeField] private float sinkTime = 1.5f;
    [SerializeField] private ParticleSystem sinkEffect;

    public Phase CurrentPhase => phase;
    public int Count => count;
    public int Bits => Setup != null ? Setup.bits : 4;
    public int TotalTicks => Setup != null ? Total(Setup) : 0;

    /// <summary>Respuesta correcta en binario (con los bits de la versión).</summary>
    public string Answer
    {
        get
        {
            var s = Setup;
            int n = s.bits, mod = 1 << n, v = 0;
            foreach (int st in s.steps) v += st;
            v = ((v % mod) + mod) % mod;
            return Convert.ToString(v, 2).PadLeft(n, '0');
        }
    }

    private VersionSetup Setup => versions != null && CurrentVersion < versions.Length ? versions[CurrentVersion] : null;
    private int Offset => 4 - Bits;   // columnas que no se usan (a la izquierda)

    private Phase phase = Phase.Idle;
    private UnityAction<bool>[] switchHandlers;
    private int count;
    private int tick;

    // ---------- Ciclo de vida ----------

    private void OnEnable()
    {
        if (button != null) button.onPressed.AddListener(Press);
        switchHandlers = new UnityAction<bool>[switches.Length];
        for (int i = 0; i < switches.Length; i++)
        {
            int k = i;
            switchHandlers[i] = on => OnSwitch(k, on);
            if (switches[i] != null) switches[i].onStateChanged.AddListener(switchHandlers[i]);
        }
        if (clock != null) { clock.Ticked += OnTick; clock.TicksFinished += OnFinished; }
    }

    private void OnDisable()
    {
        if (button != null) button.onPressed.RemoveListener(Press);
        if (switchHandlers != null)
            for (int i = 0; i < switches.Length && i < switchHandlers.Length; i++)
                if (switches[i] != null) switches[i].onStateChanged.RemoveListener(switchHandlers[i]);
        if (clock != null) { clock.Ticked -= OnTick; clock.TicksFinished -= OnFinished; }
    }

    protected override void OnEngage()
    {
        if (Bits < 4)
        {
            for (int i = 0; i < Offset && i < bitColumns.Length; i++) Sink(bitColumns[i]);
            Sink(redColumn);
        }
        AllOff();
        phase = Phase.Idle;
    }

    // ---------- Botón ----------

    /// <summary>Lo llama el botón (On Pressed). Público para pruebas.</summary>
    public void Press()
    {
        if (!CanPlay) return;
        switch (phase)
        {
            case Phase.Idle: CountInteraction(); StartCount(); break;
            case Phase.Answer: CountInteraction(); Submit(); break;
            // Contando o resuelto: no hace nada
        }
    }

    private void StartCount()
    {
        phase = Phase.Counting;
        count = 0;
        tick = 0;
        AllOff();
        ShowCount(true);   // 0000 visible al empezar (todas apagadas)
        if (clock != null)
        {
            if (!clock.IsFollowing(stageIndex)) clock.Connect(stageIndex);
            clock.SetMuted(true);
            clock.RunTicks(TotalTicks);
        }
        Debug.Log($"[CountdownPuzzle] Conteo de {TotalTicks} flancos. Respuesta: {Answer}");
    }

    private void OnTick(int stage, int n)
    {
        if (stage != stageIndex || phase != Phase.Counting) return;
        tick++;
        int dir = DirectionAt(tick);
        int mod = 1 << Bits;
        count = ((count + dir) % mod + mod) % mod;

        if (countSource != null)
        {
            var clip = dir < 0 && tockClip != null ? tockClip : beepClip;
            if (clip != null) countSource.PlayOneShot(clip, SessionData.Instance.SfxVolume01);
        }
        if (redBulb != null) redBulb.SetOn(dir < 0);
        ShowCount(tick <= Setup.visibleTicks);
    }

    private void OnFinished(int stage)
    {
        if (stage != stageIndex || phase != Phase.Counting) return;
        phase = Phase.Answer;
        if (redBulb != null) redBulb.SetOn(false);
        for (int b = 0; b < Bits; b++) SetBulb(b, SwitchOn(b));   // ahora las bombillas muestran la respuesta
    }

    private void Submit()
    {
        string given = "";
        for (int b = 0; b < Bits; b++) given += SwitchOn(b) ? '1' : '0';
        ResumeClock();

        if (given == Answer)
        {
            phase = Phase.Solved;
            Debug.Log($"[CountdownPuzzle] Respuesta {given}: correcta.");
            Complete();
        }
        else
        {
            Debug.Log($"[CountdownPuzzle] Respuesta {given}, se esperaba {Answer}. Vuelve al inicio.");
            phase = Phase.Idle;
            AllOff();
            RegisterIncorrect();
        }
    }

    private void ResumeClock()
    {
        if (clock == null) return;
        clock.SetMuted(false);
        clock.Resume();
    }

    // ---------- Switches ----------

    private void OnSwitch(int column, bool on)
    {
        if (!CanPlay || phase != Phase.Answer || column < Offset) return;
        CountInteraction();
        SetBulb(column - Offset, on);
    }

    // ---------- Utilidades ----------

    private static int Total(VersionSetup s)
    {
        int t = 0;
        foreach (int st in s.steps) t += Mathf.Abs(st);
        return t;
    }

    /// <summary>+1 o −1 según el tramo al que pertenece el flanco (1, 2, 3…).</summary>
    private int DirectionAt(int n)
    {
        int acc = 0;
        foreach (int st in Setup.steps)
        {
            acc += Mathf.Abs(st);
            if (n <= acc) return st < 0 ? -1 : 1;
        }
        return 1;
    }

    private bool SwitchOn(int bit)
    {
        int c = bit + Offset;
        return c < switches.Length && switches[c] != null && switches[c].IsOn;
    }

    private void SetBulb(int bit, bool on)
    {
        int c = bit + Offset;
        if (c < bitBulbs.Length && bitBulbs[c] != null) bitBulbs[c].SetOn(on);
    }

    private void ShowCount(bool visible)
    {
        for (int b = 0; b < Bits; b++)
            SetBulb(b, visible && ((count >> (Bits - 1 - b)) & 1) == 1);
    }

    private void AllOff()
    {
        foreach (var b in bitBulbs) if (b != null) b.SetOn(false);
        if (redBulb != null) redBulb.SetOn(false);
    }

    private void Sink(Transform t)
    {
        if (t == null) return;
        Fx.Play(sinkEffect, t.position);
        StartCoroutine(SinkRoutine(t));
    }

    private IEnumerator SinkRoutine(Transform t)
    {
        Vector3 from = t.localPosition, to = from - new Vector3(0f, sinkDepth, 0f);
        float e = 0f;
        while (e < sinkTime)
        {
            e += Time.deltaTime;
            t.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / sinkTime)));
            yield return null;
        }
        t.localPosition = to;
        t.gameObject.SetActive(false);
    }
}
