using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Reto 3.2, "El registro de desplazamiento".
///   Cubos en una cinta (el registro): en cada pulso todos avanzan una posición, entra un cubo
///   con el bit de la palanca de entrada y sale uno por el otro extremo.
///   Pulsos limitados: si se acaban sin formar el código, cuenta como intento incorrecto y el
///   registro vuelve (girando los cubos) a su estado inicial.
///   V2 y V3 tienen palanca de dirección (encendida = derecha: los bits entran por la izquierda).
///
/// La cinta usa N + 1 cubos: N en la ventana y uno escondido tras la tapa del lado de entrada.
/// En cada pulso el escondido entra, el del otro extremo sale y queda escondido para el próximo.
/// Posiciones: eje X local de "Track"; índice 0 = izquierda = bit más significativo.
/// </summary>
public class ShiftRegisterPuzzle : Level3Puzzle
{
    [Serializable]
    public class VersionSetup
    {
        public string name = "V1";
        [Tooltip("Cantidad de bits del registro (4 o 6).")]
        public int bits = 4;
        public string start = "0000";
        public string target = "1011";
        [Tooltip("Pulsos disponibles antes de que el registro se reinicie.")]
        public int pulses = 4;
        [Tooltip("Aparece la palanca de dirección.")]
        public bool hasDirection = false;
    }

    [SerializeField]
    private VersionSetup[] versions =
    {
        new VersionSetup { name = "V1", bits = 4, start = "0000",   target = "1011",   pulses = 4, hasDirection = false },
        new VersionSetup { name = "V2", bits = 4, start = "0010",   target = "1011",   pulses = 3, hasDirection = true },
        new VersionSetup { name = "V3", bits = 6, start = "001010", target = "110011", pulses = 4, hasDirection = true },
    };

    [Header("Cinta")]
    [Tooltip("Objeto cuyo eje X es la dirección de la cinta; los cubos son sus hijos.")]
    [SerializeField] private Transform track;
    [Tooltip("Ancho de la ventana visible del cajón, en unidades locales de Track.")]
    [SerializeField] private float windowWidth = 1.2f;
    [Tooltip("Cubos de la cinta (7 = el máximo de 6 bits + 1).")]
    [SerializeField] private BitCube[] cubes = new BitCube[0];
    [SerializeField] private float moveTime = 0.4f;
    [SerializeField] private float growTime = 0.6f;

    [Header("Controles")]
    [SerializeField] private XRSwitchToggle inputLever;
    [Tooltip("Encendida = derecha.")]
    [SerializeField] private XRSwitchToggle directionLever;
    [SerializeField] private XRPhysicalButton pulseButton;
    [Tooltip("Raíz de la palanca de dirección: emerge del suelo en V2 y V3.")]
    [SerializeField] private Transform directionLeverRoot;
    [SerializeField] private float riseDepth = 1f;
    [SerializeField] private float riseTime = 1.5f;

    [Header("Indicadores")]
    [SerializeField] private GlowGroup zeroGlyph;
    [SerializeField] private GlowGroup oneGlyph;
    [SerializeField] private GlowGroup leftArrow;
    [SerializeField] private GlowGroup rightArrow;
    [Tooltip("Bombillas del contador de pulsos (de izquierda a derecha).")]
    [SerializeField] private BitBulb[] pulseBulbs = new BitBulb[0];
    [SerializeField] private TMP_Text codeText;

    [Header("Sonido (opcional)")]
    [SerializeField] private AudioSource moveSound;
    [SerializeField] private AudioSource resetSound;
    [Tooltip("Suena al aparecer los cubos.")]
    [SerializeField] private AudioSource appearSound;

    [Header("Efectos de aparición (opcional)")]
    [Tooltip("Prefab de partículas que sale en cada cubo al aparecer (p. ej. destellos dorados).")]
    [SerializeField] private ParticleSystem appearEffect;
    [Tooltip("Prefab de partículas al pie de la palanca de dirección cuando emerge (p. ej. polvo).")]
    [SerializeField] private ParticleSystem riseEffect;
    [Tooltip("Pausa entre un cubo y el siguiente al aparecer (de izquierda a derecha).")]
    [SerializeField] private float appearStagger = 0.12f;

    public string Register => new string(reg.ToArray());
    public int PulsesLeft => Setup != null ? Setup.pulses - used : 0;
    public bool MovesRight => Setup == null || !Setup.hasDirection || directionLever == null || directionLever.IsOn;
    public int InputBit => inputLever != null && inputLever.IsOn ? 1 : 0;
    public bool IsAnimating => busy;

    private VersionSetup Setup => versions != null && CurrentVersion < versions.Length ? versions[CurrentVersion] : null;

    private readonly List<char> reg = new List<char>();
    private readonly List<BitCube> slots = new List<BitCube>();
    private BitCube spare;
    private Vector3[] baseScale;
    private Vector3 leverHome;
    private int used;
    private bool busy;

    // ---------- Ciclo de vida ----------

    protected override void Awake()
    {
        base.Awake();
        baseScale = new Vector3[cubes.Length];
        for (int i = 0; i < cubes.Length; i++)
        {
            if (cubes[i] == null) continue;
            baseScale[i] = cubes[i].transform.localScale;
            cubes[i].gameObject.SetActive(false);   // aparecen al llegar el jugador
        }
        if (directionLeverRoot != null)
        {
            leverHome = directionLeverRoot.localPosition;
            directionLeverRoot.gameObject.SetActive(false);
        }
        if (codeText != null) codeText.text = "? ? ? ?";
    }

    private void OnEnable()
    {
        if (inputLever != null) inputLever.onStateChanged.AddListener(OnInputLever);
        if (directionLever != null) directionLever.onStateChanged.AddListener(OnDirectionLever);
        if (pulseButton != null) pulseButton.onPressed.AddListener(Pulse);
    }

    private void OnDisable()
    {
        if (inputLever != null) inputLever.onStateChanged.RemoveListener(OnInputLever);
        if (directionLever != null) directionLever.onStateChanged.RemoveListener(OnDirectionLever);
        if (pulseButton != null) pulseButton.onPressed.RemoveListener(Pulse);
    }

    // ---------- Al llegar el jugador (versión fijada) ----------

    protected override void OnEngage()
    {
        var s = Setup;
        int n = s.bits;

        reg.Clear();
        reg.AddRange(s.start.ToCharArray());
        slots.Clear();
        for (int i = 0; i < n && i < cubes.Length; i++) slots.Add(cubes[i]);
        spare = n < cubes.Length ? cubes[n] : null;

        if (appearSound != null) appearSound.Play();
        for (int i = 0; i < slots.Count; i++)
        {
            Place(slots[i], SlotX(i));
            slots[i].SetBitInstant(reg[i] == '1' ? 1 : 0);
            slots[i].transform.localScale = Vector3.zero;
            slots[i].gameObject.SetActive(true);
            StartCoroutine(Grow(slots[i], i * appearStagger));
        }
        PrepareSpare();

        if (s.hasDirection && directionLeverRoot != null) StartCoroutine(Rise(directionLeverRoot));
        if (codeText != null) codeText.text = string.Join(" ", s.target.ToCharArray());

        used = 0;
        RefreshIndicators();
    }

    protected override void OnBlockedChanged(bool blocked) => RefreshIndicators();

    // ---------- Controles ----------

    private void OnInputLever(bool on)
    {
        if (!CanPlay) return;
        CountInteraction();
        PrepareSpare();
        RefreshIndicators();
    }

    private void OnDirectionLever(bool on)
    {
        if (!CanPlay) return;
        CountInteraction();
        PrepareSpare();   // el cubo escondido pasa al otro extremo
        RefreshIndicators();
    }

    /// <summary>Lo llama el botón (On Pressed). Público para pruebas.</summary>
    public void Pulse()
    {
        if (!CanPlay || busy || PulsesLeft <= 0) return;
        CountInteraction();
        StartCoroutine(Shift());
    }

    // ---------- La cinta ----------

    private float Pitch => windowWidth / Mathf.Max(1, Setup.bits);
    private float SlotX(int i) => (i - (Setup.bits - 1) * 0.5f) * Pitch;
    private float HiddenX(bool leftSide) => leftSide ? SlotX(-1) : SlotX(Setup.bits);

    /// <summary>El cubo de reserva espera escondido en el lado de entrada, mostrando el bit de la palanca.</summary>
    private void PrepareSpare()
    {
        if (spare == null || busy) return;
        if (!spare.gameObject.activeSelf)
        {
            spare.gameObject.SetActive(true);
            spare.transform.localScale = ScaleOf(spare);
        }
        Place(spare, HiddenX(MovesRight));   // a la derecha entran por la izquierda
        spare.SetBitInstant(InputBit);
    }

    private IEnumerator Shift()
    {
        busy = true;
        used++;
        RefreshIndicators();
        if (moveSound != null) moveSound.Play();

        bool right = MovesRight;
        int n = slots.Count;
        char input = InputBit == 1 ? '1' : '0';
        spare.SetBitInstant(InputBit);

        // Quién va a dónde
        var movers = new List<BitCube>(slots) { spare };
        var from = new List<float>();
        var to = new List<float>();
        for (int i = 0; i < n; i++)
        {
            from.Add(SlotX(i));
            to.Add(right ? SlotX(i + 1) : SlotX(i - 1));
        }
        from.Add(HiddenX(right));
        to.Add(right ? SlotX(0) : SlotX(n - 1));

        float t = 0f;
        while (t < moveTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / moveTime));
            for (int i = 0; i < movers.Count; i++) Place(movers[i], Mathf.Lerp(from[i], to[i], k));
            yield return null;
        }
        for (int i = 0; i < movers.Count; i++) Place(movers[i], to[i]);

        // Nuevo orden: el que salió queda de reserva
        BitCube leaving;
        if (right)
        {
            leaving = slots[n - 1];
            slots.RemoveAt(n - 1);
            slots.Insert(0, spare);
            reg.RemoveAt(n - 1);
            reg.Insert(0, input);
        }
        else
        {
            leaving = slots[0];
            slots.RemoveAt(0);
            slots.Add(spare);
            reg.RemoveAt(0);
            reg.Add(input);
        }
        spare = leaving;
        busy = false;
        PrepareSpare();

        if (Register == Setup.target)
        {
            Complete();
            RefreshIndicators();
            yield break;
        }

        if (PulsesLeft <= 0)
        {
            RegisterIncorrect();
            Debug.Log($"[ShiftRegisterPuzzle] Sin pulsos: quedó {Register}, se esperaba {Setup.target}. Reinicio.");
            yield return StartCoroutine(ResetRoutine());
        }
    }

    /// <summary>Los cubos giran en su lugar hasta mostrar el estado inicial y el contador se recarga.</summary>
    private IEnumerator ResetRoutine()
    {
        busy = true;
        yield return new WaitForSeconds(0.6f);
        if (resetSound != null) resetSound.Play();

        string start = Setup.start;
        for (int i = 0; i < slots.Count; i++)
        {
            reg[i] = start[i];
            slots[i].SetBit(start[i] == '1' ? 1 : 0);
        }
        yield return new WaitForSeconds(0.5f);

        used = 0;
        busy = false;
        PrepareSpare();   // por si movieron una palanca durante el reinicio
        RefreshIndicators();
    }

    // ---------- Visual ----------

    private void RefreshIndicators()
    {
        bool show = IsUnlocked && IsEngaged && !IsBlocked;
        bool right = MovesRight;
        if (zeroGlyph != null) zeroGlyph.SetOn(show && InputBit == 0);
        if (oneGlyph != null) oneGlyph.SetOn(show && InputBit == 1);
        if (rightArrow != null) rightArrow.SetOn(show && right);
        if (leftArrow != null) leftArrow.SetOn(show && !right);

        for (int i = 0; i < pulseBulbs.Length; i++)
        {
            if (pulseBulbs[i] == null) continue;
            bool exists = Setup != null && i < Setup.pulses;
            pulseBulbs[i].gameObject.SetActive(!IsEngaged || exists);   // V2 usa 3 de las 4
            pulseBulbs[i].SetOn(show && !IsSolved && exists && i < PulsesLeft);
        }
    }

    private void Place(BitCube c, float x)
    {
        if (c == null) return;
        var p = c.transform.localPosition;
        c.transform.localPosition = new Vector3(x, p.y, p.z);
    }

    private Vector3 ScaleOf(BitCube c)
    {
        int i = Array.IndexOf(cubes, c);
        return i >= 0 && baseScale != null && i < baseScale.Length ? baseScale[i] : Vector3.one;
    }

    private IEnumerator Grow(BitCube c, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        Fx.Play(appearEffect, c.transform.position);
        Vector3 full = ScaleOf(c);
        float t = 0f;
        while (t < growTime)
        {
            t += Time.deltaTime;
            c.transform.localScale = full * EaseOutBack(Mathf.Clamp01(t / growTime));
            yield return null;
        }
        c.transform.localScale = full;
    }

    /// <summary>Crece pasándose un poco del tamaño final y regresa: se siente como un "pop".</summary>
    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float u = x - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }

    private IEnumerator Rise(Transform root)
    {
        root.gameObject.SetActive(true);
        Fx.Play(riseEffect, root.parent != null ? root.parent.TransformPoint(leverHome) : leverHome);
        Vector3 down = leverHome - new Vector3(0f, riseDepth, 0f);
        float t = 0f;
        while (t < riseTime)
        {
            t += Time.deltaTime;
            root.localPosition = Vector3.Lerp(down, leverHome, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / riseTime)));
            yield return null;
        }
        root.localPosition = leverHome;
    }

    [ContextMenu("Previsualizar V1")] private void PreviewV1() => Preview(0);
    [ContextMenu("Previsualizar V2")] private void PreviewV2() => Preview(1);
    [ContextMenu("Previsualizar V3")] private void PreviewV3() => Preview(2);

    /// <summary>En el editor (sin jugar): coloca los cubos de esa versión para ver cómo queda.</summary>
    private void Preview(int v)
    {
        var s = versions[v];
        float pitch = windowWidth / s.bits;
        for (int i = 0; i < cubes.Length; i++)
        {
            if (cubes[i] == null) continue;
            bool visible = i < s.bits;
            cubes[i].gameObject.SetActive(visible);
            var p = cubes[i].transform.localPosition;
            cubes[i].transform.localPosition = new Vector3((i - (s.bits - 1) * 0.5f) * pitch, p.y, p.z);
        }
        if (directionLeverRoot != null) directionLeverRoot.gameObject.SetActive(s.hasDirection);
    }
}
