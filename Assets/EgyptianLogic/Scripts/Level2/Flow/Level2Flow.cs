using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Recorrido del Nivel 2: un gran comedor egipcio con los 6 retos contra las paredes. Al iniciar solo está el primero;
/// los demás esperan escondidos bajo el piso. Al resolver un reto aparece un instrumento (palanca, placa, llave…).
/// Al usarlo se encienden las antorchas que llevan al reto siguiente y este sube del piso. El instrumento del último
/// reto abre la salida hacia el Nivel 3.
///
/// La dificultad de cada reto la pone el AdaptationEngine (a través del PuzzleVersionManager del reto)
/// justo al resolver el anterior, así que cuando el reto sube ya está en su versión.
/// </summary>
public class Level2Flow : MonoBehaviour
{
    [Serializable]
    public class Station
    {
        public string name = "El Detective de Verdades";
        [Tooltip("Las 3 versiones del reto (V1, V2, V3). Solo la activa puede resolverse.")]
        public Level2Puzzle[] puzzles = new Level2Puzzle[3];
        [Tooltip("Zona de pistas del reto. Se activa al abrirlo, para que el reloj no arranque antes.")]
        public GameObject zone;
        [Tooltip("Luz sobre el reto: apagada = bloqueado, ámbar = disponible, verde = resuelto.")]
        public Renderer indicator;
        [Tooltip("Aparece al resolver el reto. Al usarlo se abre el siguiente. Vacío = se abre solo.")]
        public UnlockInstrument instrument;
        [Tooltip("Puerta que se abre al usar el instrumento (en el último reto, la salida). Opcional.")]
        public DoorOpener gate;
        [Tooltip("Lo que sube del piso cuando el reto se abre (el reto entero). Vacío = siempre visible.")]
        public Transform rising;
        public AudioSource riseSound;
        [Tooltip("Antorchas que se encienden en orden al usar el instrumento, marcando el camino.")]
        public Level2Torch[] torches = new Level2Torch[0];

        [NonSerialized] public bool solved;
        [NonSerialized] public bool unlocked;
        [NonSerialized] public bool opened;
    }

    [Header("Retos en orden")]
    public Station[] stations = new Station[6];

    [Header("Sistemas (opcionales)")]
    [Tooltip("Para que las pistas dejen de darse en un reto ya resuelto.")]
    public HintManager hintManager;

    [Header("Carteles de progreso")]
    public TextMeshPro[] progressTexts = new TextMeshPro[0];

    [Header("Antorchas")]
    [Tooltip("Segundos entre una antorcha y la siguiente al marcar el camino.")]
    public float torchInterval = 0.35f;

    [Header("Retos que suben del piso")]
    [Tooltip("Metros bajo el piso a los que esperan los retos cerrados.")]
    public float riseDepth = 3.4f;
    public float riseSeconds = 3f;

    [Header("Colores de los indicadores")]
    public Color lockedColor = new Color(0.15f, 0.12f, 0.1f);
    public Color availableColor = new Color(1f, 0.65f, 0.15f);
    public Color solvedColor = new Color(0.25f, 1f, 0.35f);

    [Header("Eventos")]
    [Tooltip("Se dispara al resolver cada reto, con su índice (0 = el primer reto).")]
    public UnityEvent<int> onStationSolved;
    [Tooltip("Se dispara al abrir el reto siguiente, con el índice del reto que se dejó atrás.")]
    public UnityEvent<int> onStationOpened;
    public UnityEvent onLevelCompleted;

    public int SolvedCount { get; private set; }
    public bool LevelCompleted { get; private set; }

    private void Awake()
    {
        for (int i = 0; i < stations.Length; i++)
        {
            int index = i;
            foreach (var p in stations[i].puzzles)
                if (p != null) p.onSolved.AddListener(() => OnPuzzleSolved(index));
            if (stations[i].instrument != null)
                stations[i].instrument.onActivated.AddListener(() => OpenNext(index));

            // Los retos cerrados esperan escondidos bajo el piso (el primero ya está arriba)
            if (i > 0 && stations[i].rising != null)
                stations[i].rising.localPosition -= Vector3.up * riseDepth;
        }
    }

    private void Start()
    {
        foreach (var s in stations)
        {
            Level2Visuals.SetColor(s.indicator, lockedColor, false);
            if (s.zone != null) s.zone.SetActive(false);
        }

        if (stations.Length > 0) Unlock(0);
        UpdateProgress();
    }

    private void OnPuzzleSolved(int i)
    {
        if (i < 0 || i >= stations.Length || stations[i].solved) return;

        var s = stations[i];
        s.solved = true;
        SolvedCount++;
        Level2Visuals.SetColor(s.indicator, solvedColor, true);
        if (hintManager != null) hintManager.MarkSolved(i);

        Debug.Log($"[Level2Flow] Reto «{s.name}» resuelto ({SolvedCount}/{stations.Length}).");
        onStationSolved?.Invoke(i);

        if (s.instrument != null) s.instrument.Reveal();
        else OpenNext(i);

        UpdateProgress();
    }

    /// <summary>Enciende el camino desde el reto <paramref name="i"/> y abre el siguiente (lo dispara su instrumento).</summary>
    private void OpenNext(int i)
    {
        var s = stations[i];
        if (!s.solved || s.opened) return;
        s.opened = true;

        StartCoroutine(LightPath(s.torches));
        if (s.gate != null) s.gate.Open();
        onStationOpened?.Invoke(i);

        if (i + 1 < stations.Length) Unlock(i + 1);
        else CompleteLevel();

        UpdateProgress();
    }

    private IEnumerator LightPath(Level2Torch[] torches)
    {
        foreach (var t in torches)
        {
            if (t == null) continue;
            t.Ignite();
            yield return new WaitForSeconds(torchInterval);
        }
    }

    private int NextUnsolved()
    {
        for (int i = 0; i < stations.Length; i++)
            if (!stations[i].solved) return i;
        return -1;
    }

    private void Unlock(int i)
    {
        var s = stations[i];
        if (s.unlocked) return;
        s.unlocked = true;
        Level2Visuals.SetColor(s.indicator, availableColor, true);
        if (s.zone != null) s.zone.SetActive(true);
        if (i > 0 && s.rising != null) StartCoroutine(Rise(s));
    }

    private IEnumerator Rise(Station s)
    {
        if (s.riseSound != null) s.riseSound.Play();
        Vector3 from = s.rising.localPosition, to = from + Vector3.up * riseDepth;
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, riseSeconds))
        {
            s.rising.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        s.rising.localPosition = to;
    }

    private void CompleteLevel()
    {
        if (LevelCompleted) return;
        LevelCompleted = true;
        Debug.Log("[Level2Flow] ¡Nivel 2 completado! Se abre la salida.");
        onLevelCompleted?.Invoke();
    }

    private void UpdateProgress()
    {
        string text;
        if (LevelCompleted) text = "¡Superaste las 6 pruebas!\nLa salida está abierta.";
        else
        {
            int pending = Array.FindIndex(stations, s => s.solved && !s.opened);
            text = $"Pruebas superadas: {SolvedCount} / {stations.Length}" +
                   (pending >= 0 ? "\nUsa lo que apareció junto al reto para seguir." : "");
        }

        foreach (var t in progressTexts)
            if (t != null) t.text = text;
    }

    // Atajos para probar el flujo desde el editor (clic derecho sobre el componente)
    [ContextMenu("Prueba: resolver el reto actual")]
    private void DebugSolveCurrent()
    {
        int next = NextUnsolved();
        if (next >= 0) OnPuzzleSolved(next);
    }

    [ContextMenu("Prueba: resolver y abrir el siguiente")]
    private void DebugSolveAndOpen()
    {
        int next = NextUnsolved();
        if (next < 0) return;
        OnPuzzleSolved(next);
        if (stations[next].instrument != null) stations[next].instrument.Activate();
    }
}
