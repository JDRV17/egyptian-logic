using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Reto 3.1, "El cofre con memoria". Un latch D por bit:
///   D (cubos) = lo que manda el reloj; cambia en cada tic recorriendo un ciclo de patrones.
///   Enable (sockets de llave) = mientras hay llave, el bit copia a D.
///   Q (bombillas) = lo guardado; al quitar la llave se queda con el valor de ese instante.
/// Se aprueba cuando no queda ninguna llave puesta y las bombillas muestran el código.
/// Quitar la última llave con otro código cuenta como intento incorrecto.
///
/// Orden de los arreglos: de izquierda a derecha = del bit más significativo al menos
/// (igual que se lee el código: "1011" → cubo 0 = 1, cubo 1 = 0…).
/// </summary>
[DefaultExecutionOrder(-100)]   // elige versión antes que los demás (como los managers del Nivel 1)
public class MemoryLatchPuzzle : MonoBehaviour, IVersioned
{
    [Serializable]
    public class VersionSetup
    {
        public string name = "V1";
        [Tooltip("Padre de los objetos de esta versión. Solo el de la versión activa queda encendido.")]
        public GameObject root;
        [Tooltip("Patrones que recorre D, uno por tic. Todos con tantos dígitos como cubos.")]
        public string[] cycle = { "0", "1" };
        [Tooltip("Código que deben guardar las bombillas.")]
        public string target = "1";
        [Tooltip("Un cubo por bit, de izquierda a derecha.")]
        public BitCube[] dCubes = new BitCube[0];
        [Tooltip("Una bombilla por bit, de izquierda a derecha.")]
        public BitBulb[] qBulbs = new BitBulb[0];
        [Tooltip("1 socket = una llave maestra para todos los bits. Uno por bit = una llave por bit.")]
        public XRSocketInteractor[] keySockets = new XRSocketInteractor[0];
    }

    [Header("Referencias")]
    [SerializeField] private GameClock clock;
    [SerializeField] private PerformanceTracker tracker;
    [SerializeField] private HintManager hintManager;
    [SerializeField] private int stageIndex = 0;

    [Header("Versión")]
    [SerializeField] private bool randomizeOnStart = false;
    [Range(0, 2)] [SerializeField] private int forcedVersion = 1;

    [SerializeField]
    private VersionSetup[] versions =
    {
        new VersionSetup { name = "V1", cycle = new[] { "0", "1" }, target = "1" },
        new VersionSetup { name = "V2", cycle = new[] { "0110", "1011", "0001", "1100" }, target = "1011" },
        new VersionSetup { name = "V3", cycle = new[] { "1100", "0011", "1001", "0110" }, target = "1011" },
    };

    [Header("Eventos")]
    public UnityEvent onSolved;
    public UnityEvent onIncorrect;

    public int CurrentVersion { get; private set; }
    public bool IsSolved { get; private set; }
    public string StoredCode => new string(Array.ConvertAll(q ?? new int[0], b => b == 1 ? '1' : '0'));
    public string CurrentD => Setup != null && Setup.cycle.Length > 0 ? Setup.cycle[cycleIndex] : "";

    private VersionSetup Setup => versions != null && CurrentVersion < versions.Length ? versions[CurrentVersion] : null;

    private int[] q;            // bits guardados
    private bool[] keyIn;       // una entrada por socket de la versión activa
    private int cycleIndex;
    private bool started;
    private bool usedKey;

    // ---------- Ciclo de vida ----------

    private void Awake()
    {
        CurrentVersion = randomizeOnStart ? UnityEngine.Random.Range(0, 3) : forcedVersion;
        ApplyVersion();
    }

    private void OnEnable()
    {
        if (clock != null) clock.Ticked += OnTick;
        if (versions == null) return;
        foreach (var v in versions)
            if (v?.keySockets != null)
                foreach (var s in v.keySockets)
                {
                    if (s == null) continue;
                    s.selectEntered.AddListener(OnKeyIn);
                    s.selectExited.AddListener(OnKeyOut);
                }
    }

    private void OnDisable()
    {
        if (clock != null) clock.Ticked -= OnTick;
        if (versions == null) return;
        foreach (var v in versions)
            if (v?.keySockets != null)
                foreach (var s in v.keySockets)
                {
                    if (s == null) continue;
                    s.selectEntered.RemoveListener(OnKeyIn);
                    s.selectExited.RemoveListener(OnKeyOut);
                }
    }

    // ---------- Versiones (IVersioned) ----------

    /// <summary>Lo llama el AdaptationEngine. Solo cambia si el jugador aún no empezó el reto.</summary>
    public void SelectVersion(int version)
    {
        if (started || IsSolved) return;
        CurrentVersion = Mathf.Clamp(version, 0, 2);
        ApplyVersion();
    }

    private void ApplyVersion()
    {
        if (versions == null) return;
        for (int i = 0; i < versions.Length; i++)
            if (versions[i]?.root != null) versions[i].root.SetActive(i == CurrentVersion);

        var s = Setup;
        if (s == null) return;

        int bits = s.target != null ? s.target.Length : 0;
        q = new int[bits];
        keyIn = new bool[s.keySockets != null ? s.keySockets.Length : 0];
        cycleIndex = 0;
        usedKey = false;

        if (s.dCubes != null) foreach (var c in s.dCubes) if (c != null) c.ResetToZero();
        ShowD();
        ShowQ();
        Debug.Log($"[MemoryLatchPuzzle] Versión V{CurrentVersion + 1}: código {s.target}, {keyIn.Length} llave(s).");
    }

    // ---------- Reloj ----------

    private void OnTick(int stage, int tick)
    {
        if (stage != stageIndex || IsSolved) return;
        var s = Setup;
        if (s == null || s.cycle == null || s.cycle.Length == 0) return;

        cycleIndex = (cycleIndex + 1) % s.cycle.Length;
        ShowD();
        Follow();
    }

    // ---------- Llaves ----------

    private void OnKeyIn(SelectEnterEventArgs args) => SetKey(args.interactorObject as XRSocketInteractor, args.interactableObject, true);
    private void OnKeyOut(SelectExitEventArgs args) => SetKey(args.interactorObject as XRSocketInteractor, args.interactableObject, false);

    private void SetKey(XRSocketInteractor socket, IXRSelectInteractable item, bool inserted)
    {
        if (IsSolved || socket == null) return;
        if (item == null || item.transform.GetComponent<HangingKey>() == null) return;   // solo llaves

        int k = Array.IndexOf(Setup.keySockets, socket);
        if (k < 0) return;                          // socket de otra versión
        KeyChanged(k, inserted);
    }

    /// <summary>Lógica de una llave que entra o sale (pública para pruebas).</summary>
    public void KeyChanged(int k, bool inserted)
    {
        if (IsSolved || keyIn == null || k < 0 || k >= keyIn.Length || keyIn[k] == inserted) return;

        if (!started)
        {
            started = true;
            if (tracker != null) tracker.StartStage(stageIndex);
        }
        if (tracker != null) tracker.RegisterInteraction(stageIndex);

        keyIn[k] = inserted;
        if (inserted) usedKey = true;
        Follow();

        if (!inserted && AllKeysOut() && usedKey) Check();
    }

    private bool Enabled(int bit) => keyIn.Length == 1 ? keyIn[0] : bit < keyIn.Length && keyIn[bit];

    private bool AllKeysOut()
    {
        foreach (bool b in keyIn) if (b) return false;
        return true;
    }

    /// <summary>Los bits con llave copian a D.</summary>
    private void Follow()
    {
        string d = CurrentD;
        for (int b = 0; b < q.Length && b < d.Length; b++)
            if (Enabled(b)) q[b] = d[b] == '1' ? 1 : 0;
        ShowQ();
    }

    private void Check()
    {
        if (StoredCode == Setup.target)
        {
            IsSolved = true;
            foreach (var s in Setup.keySockets) if (s != null) s.socketActive = false;
            if (hintManager != null) hintManager.MarkSolved(stageIndex);
            if (tracker != null) tracker.CompleteStage(stageIndex);
            Debug.Log($"[MemoryLatchPuzzle] ¡Reto resuelto! Código guardado: {StoredCode}");
            onSolved?.Invoke();
        }
        else
        {
            if (tracker != null) tracker.RegisterIncorrect(stageIndex);
            Debug.Log($"[MemoryLatchPuzzle] Código guardado {StoredCode}, se esperaba {Setup.target}.");
            onIncorrect?.Invoke();
        }
    }

    // ---------- Visual ----------

    private void ShowD()
    {
        var s = Setup;
        if (s?.dCubes == null) return;
        string d = CurrentD;
        for (int b = 0; b < s.dCubes.Length && b < d.Length; b++)
            if (s.dCubes[b] != null) s.dCubes[b].SetBit(d[b] == '1' ? 1 : 0);
    }

    private void ShowQ()
    {
        var s = Setup;
        if (s?.qBulbs == null || q == null) return;
        for (int b = 0; b < s.qBulbs.Length && b < q.Length; b++)
            if (s.qBulbs[b] != null) s.qBulbs[b].SetOn(q[b] == 1);
    }

    [ContextMenu("Previsualizar V1")] private void PreviewV1() { CurrentVersion = 0; ApplyVersion(); }
    [ContextMenu("Previsualizar V2")] private void PreviewV2() { CurrentVersion = 1; ApplyVersion(); }
    [ContextMenu("Previsualizar V3")] private void PreviewV3() { CurrentVersion = 2; ApplyVersion(); }
}
