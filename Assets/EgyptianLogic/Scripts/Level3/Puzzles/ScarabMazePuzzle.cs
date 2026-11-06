using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Reto 3.4, "El escarabajo mecánico". El jugador diseña la tabla de transiciones de una máquina de estados:
///   Los estados son rumbos (↑ norte, → este, ↓ sur, ← oeste). En cada pulso el escarabajo lee sus sensores
///   (muro delante y, en V3, muro a su derecha), la tabla le dice su nuevo rumbo y avanza una losa hacia allá.
///   Si avanza contra un muro, se estrella. Si llega al altar, gana. Si da demasiados pasos, se perdió.
///
/// La tabla es una tablilla de cubos (los de 1.3, con CubeRotator): fila = rumbo actual, columna = lo que
/// leen los sensores, y la cara visible del cubo = rumbo siguiente (0° ↑, 90° →, 180° ↓, 270° ←).
/// El botón Probar hace correr al escarabajo. En V3 la misma tabla debe funcionar en dos laberintos seguidos.
/// </summary>
public class ScarabMazePuzzle : Level3Puzzle
{
    /// <summary>Rumbos en el orden de giro de los cubos: N, E, S, O.</summary>
    public const string Headings = "NESW";
    public const string Arrows = "↑→↓←";
    private static readonly int[] DR = { -1, 0, 1, 0 };
    private static readonly int[] DC = { 0, 1, 0, -1 };

    public enum Outcome { None, Running, Reached, Crashed, UnknownHeading, Lost, Solved }

    [Serializable]
    public class Maze
    {
        [Tooltip("Filas del laberinto, de norte (lejos del jugador) a sur. # = muro, S = salida, A = altar.")]
        public string[] rows = new string[0];
        [Tooltip("Rumbo inicial del escarabajo: N, E, S o W.")]
        public string start = "E";
        [Tooltip("Muros de este laberinto (los crea el builder). Se muestran solo cuando el laberinto está activo.")]
        public GameObject walls;
    }

    [Serializable]
    public class VersionSetup
    {
        public string name = "V1";
        [Tooltip("Rumbos que tienen fila en la tablilla, en orden (letras de NESW).")]
        public string headings = "NESW";
        [Tooltip("Desmarcado: solo el sensor de muro delante (2 columnas). Marcado: también muro a la derecha (4 columnas).")]
        public bool twoSensors;
        public Maze[] mazes = new Maze[0];
    }

    // ---------- Tableros (verificados con un simulador: la solución llega y las estrategias ingenuas fallan) ----------

    public static readonly string[] Stairs =
    {
        "#########",
        "#S..#####",
        "###.#####",
        "###...###",
        "#####.###",
        "#####...#",
        "#######A#",
        "#########",
        "#########",
    };

    public static readonly string[] Spiral =
    {
        "#########",
        "#S......#",
        "#######.#",
        "#.....#.#",
        "#.###.#.#",
        "#.#A..#.#",
        "#.#####.#",
        "#.......#",
        "#########",
    };

    public static readonly string[] LabyrinthA =
    {
        "#########",
        "#...#...#",
        "#.#.#.#.#",
        "#...#.#.#",
        "#.###.#.#",
        "#....A..#",
        "#.#####.#",
        "#S......#",
        "#########",
    };

    public static readonly string[] LabyrinthB =
    {
        "#########",
        "#...#A..#",
        "#.#.#.#.#",
        "#.#...#.#",
        "#.###.#.#",
        "#...#.#.#",
        "#.#.#.#.#",
        "#S......#",
        "#########",
    };

    [SerializeField]
    private VersionSetup[] versions =
    {
        new VersionSetup { name = "V1", headings = "ES",   twoSensors = false, mazes = new[] { new Maze { rows = (string[])Stairs.Clone(), start = "E" } } },
        new VersionSetup { name = "V2", headings = "NESW", twoSensors = false, mazes = new[] { new Maze { rows = (string[])Spiral.Clone(), start = "E" } } },
        new VersionSetup { name = "V3", headings = "NESW", twoSensors = true,  mazes = new[]
        {
            new Maze { rows = (string[])LabyrinthA.Clone(), start = "N" },
            new Maze { rows = (string[])LabyrinthB.Clone(), start = "N" },
        } },
    };

    [Header("Maqueta")]
    [Tooltip("Centro del tablero. Su eje Z (adelante) es el norte; X es el este.")]
    [SerializeField] private Transform board;
    [SerializeField] private float cellSize = 0.07f;
    [SerializeField] private Transform scarab;
    [SerializeField] private Transform altar;

    [Header("Tablilla (4 × 4, por filas)")]
    [Tooltip("16 cubos: fila = rumbo actual, columna = sensores. Se usan las filas y columnas de la versión.")]
    [SerializeField] private CubeRotator[] cubes = new CubeRotator[16];
    [SerializeField] private TMP_Text[] rowLabels = new TMP_Text[4];
    [SerializeField] private TMP_Text[] columnLabels = new TMP_Text[4];
    [SerializeField] private XRPhysicalButton testButton;

    [Header("Sensores")]
    [SerializeField] private BitBulb frontBulb;
    [SerializeField] private BitBulb rightBulb;
    [Tooltip("Lo que se oculta en V1 y V2 (la bombilla de la derecha y su letrero).")]
    [SerializeField] private GameObject rightSensorRoot;

    [Header("Ritmo")]
    [SerializeField] private float pulseInterval = 0.5f;
    [SerializeField] private float moveDuration = 0.3f;
    [SerializeField] private float pauseAfterFail = 1.5f;
    [Tooltip("Pasos máximos por laberinto antes de dar al escarabajo por perdido.")]
    [SerializeField] private int stepLimit = 60;

    [Header("Retroalimentación")]
    [SerializeField] private Color activeTint = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color culpritTint = new Color(1f, 0.15f, 0.1f);
    [SerializeField] private ParticleSystem appearEffect;
    [SerializeField] private ParticleSystem crashEffect;

    [Header("Transiciones")]
    [Tooltip("Segundos que tarda cada muro en subir o hundirse.")]
    [SerializeField] private float riseDuration = 0.45f;
    [Tooltip("Retraso entre un muro y el siguiente (de norte a sur), para que no se muevan todos a la vez.")]
    [SerializeField] private float riseStagger = 0.015f;
    [Tooltip("Cuánto se meten en el panel los cubos que la versión no usa.")]
    [SerializeField] private float retractDistance = 0.15f;
    [SerializeField] private float retractDuration = 0.4f;
    [SerializeField] private float labelFade = 0.4f;
    [Tooltip("Lo que tarda el escarabajo (o el altar) en encogerse o aparecer al cambiar de sitio.")]
    [SerializeField] private float popDuration = 0.2f;

    // ---------- Estado ----------

    public int Row => row;
    public int Col => col;
    public int Heading => heading;
    public int MazeIndex => mazeIndex;
    public bool IsRunning => running;
    public Outcome LastOutcome => lastOutcome;
    /// <summary>Casilla de la tablilla (0..15) que causó el último fallo, o -1.</summary>
    public int Culprit => culprit;

    private VersionSetup Setup => versions != null && CurrentVersion < versions.Length ? versions[CurrentVersion] : null;

    private int row, col, heading, mazeIndex;
    private bool running;
    private Outcome lastOutcome;
    private int culprit = -1;
    private UnityAction[] cubeListeners;
    private TMP_Text[][] cubeTexts;
    private Color[][] cubeColors;

    private readonly Dictionary<GameObject, List<Transform>> wallParts = new Dictionary<GameObject, List<Transform>>();
    private readonly Dictionary<Transform, Vector3> restPos = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Vector3> restScale = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<GameObject, Coroutine> wallAnims = new Dictionary<GameObject, Coroutine>();
    private Vector3[] cubeRest;
    private Vector3 scarabScale = Vector3.one, altarScale = Vector3.one;
    private bool scarabShown, altarShown;
    private Coroutine scarabAnim, altarAnim;

    // ---------- Ciclo de vida ----------

    protected override void Awake()
    {
        base.Awake();
        CacheCubeTexts();
        cubeRest = new Vector3[cubes.Length];
        for (int i = 0; i < cubes.Length; i++) if (cubes[i] != null) cubeRest[i] = cubes[i].transform.localPosition;

        // Los muros empiezan hundidos (aplastados contra el tablero) y ocultos
        foreach (var v in versions)
            if (v?.mazes != null)
                foreach (var m in v.mazes)
                {
                    if (m?.walls == null) continue;
                    var parts = new List<Transform>();
                    var wt = m.walls.transform;
                    for (int i = 0; i < wt.childCount; i++)
                    {
                        var part = wt.GetChild(i);
                        parts.Add(part);
                        restPos[part] = part.localPosition;
                        restScale[part] = part.localScale;
                        Motion.SetRise(part, part.localPosition, part.localScale, 0f, 0f);
                    }
                    parts.Sort((a, b) => restPos[b].z.CompareTo(restPos[a].z));   // de norte a sur
                    wallParts[m.walls] = parts;
                    m.walls.SetActive(false);
                }

        // El escarabajo y el altar aparecen cuando llega el jugador
        if (scarab != null) { scarabScale = scarab.localScale; scarab.localScale = Vector3.one * 0.001f; }
        if (altar != null) { altarScale = altar.localScale; SetAltarHeight(0f); }
        SetSensors(false, false);
    }

    private void OnEnable()
    {
        if (testButton != null) testButton.onPressed.AddListener(Test);
        cubeListeners = new UnityAction[cubes.Length];
        for (int i = 0; i < cubes.Length; i++)
        {
            if (cubes[i] == null) continue;
            cubeListeners[i] = OnCubeRotated;
            cubes[i].onRotated.AddListener(cubeListeners[i]);
        }
    }

    private void OnDisable()
    {
        if (testButton != null) testButton.onPressed.RemoveListener(Test);
        if (cubeListeners == null) return;
        for (int i = 0; i < cubes.Length; i++)
            if (cubes[i] != null && cubeListeners[i] != null) cubes[i].onRotated.RemoveListener(cubeListeners[i]);
    }

    // ---------- Al llegar el jugador ----------

    protected override void OnEngage()
    {
        var s = Setup;
        int rows = s.headings.Length, cols = Columns(s);

        for (int r = 0; r < 4; r++)
        {
            bool rowOn = r < rows;
            if (rowLabels.Length > r && rowLabels[r] != null)
                ShowLabel(rowLabels[r], rowOn, rowOn ? "Yendo " + Arrows[Headings.IndexOf(s.headings[r])] : null);
            for (int c = 0; c < 4; c++) ShowCube(r * 4 + c, rowOn && c < cols);
        }
        string[] labels = s.twoSensors
            ? new[] { "Delante libre\nDerecha libre", "Delante libre\nMuro a la derecha", "Muro delante\nDerecha libre", "Muro delante\nMuro a la derecha" }
            : new[] { "Delante libre", "Muro delante" };
        for (int c = 0; c < 4; c++)
        {
            if (columnLabels.Length <= c || columnLabels[c] == null) continue;
            ShowLabel(columnLabels[c], c < cols, c < cols ? labels[c] : null);
        }
        if (rightSensorRoot != null) rightSensorRoot.SetActive(s.twoSensors);

        ShowMaze(0, true);
        StartTracking();   // el reto empieza al llegar
    }

    private static int Columns(VersionSetup s) => s.twoSensors ? 4 : 2;

    /// <summary>Los cubos que la versión no usa se meten en el panel; los que sí, salen si estaban adentro.</summary>
    private void ShowCube(int cell, bool on)
    {
        var cube = CubeAt(cell);
        if (cube == null) return;
        var t = cube.transform;
        Vector3 inside = cubeRest[cell] + Vector3.forward * retractDistance;
        if (on)
        {
            if (cube.gameObject.activeSelf) return;
            t.localPosition = inside;
            cube.gameObject.SetActive(true);
            StartCoroutine(Motion.Slide(t, cubeRest[cell], retractDuration));
        }
        else if (cube.gameObject.activeSelf)
            StartCoroutine(RetractRoutine(cube.gameObject, t, inside));
    }

    private IEnumerator RetractRoutine(GameObject go, Transform t, Vector3 inside)
    {
        yield return Motion.Run(Motion.Slide(t, inside, retractDuration));
        go.SetActive(false);
    }

    /// <summary>Letreros: cambian de texto con un desvanecido, o se desvanecen y se apagan.</summary>
    private void ShowLabel(TMP_Text label, bool on, string text)
    {
        if (on)
        {
            label.gameObject.SetActive(true);
            StartCoroutine(Motion.SwapText(label, text, labelFade));
        }
        else if (label.gameObject.activeSelf)
            StartCoroutine(HideLabelRoutine(label));
    }

    private IEnumerator HideLabelRoutine(TMP_Text label)
    {
        yield return Motion.Run(Motion.Fade(label, 0f, labelFade * 0.5f));
        label.gameObject.SetActive(false);
    }

    // ---------- Tablilla ----------

    private CubeRotator CubeAt(int cell) => cell >= 0 && cell < cubes.Length ? cubes[cell] : null;

    /// <summary>Rumbo (0..3) que muestra el cubo de una casilla.</summary>
    public int CellValue(int cell)
    {
        var c = CubeAt(cell);
        return c == null ? 0 : ((c.CurrentAngle / 90) % 4 + 4) % 4;
    }

    private void OnCubeRotated()
    {
        if (!CanPlay || running) return;
        CountInteraction();
        ClearTints();
    }

    private void SetCubesInteractable(bool value)
    {
        foreach (var c in cubes) if (c != null) c.SetInteractable(value);
    }

    private void CacheCubeTexts()
    {
        cubeTexts = new TMP_Text[cubes.Length][];
        cubeColors = new Color[cubes.Length][];
        for (int i = 0; i < cubes.Length; i++)
        {
            cubeTexts[i] = cubes[i] != null ? cubes[i].GetComponentsInChildren<TMP_Text>(true) : new TMP_Text[0];
            cubeColors[i] = new Color[cubeTexts[i].Length];
            for (int k = 0; k < cubeTexts[i].Length; k++) cubeColors[i][k] = cubeTexts[i][k].color;
        }
    }

    private void Tint(int cell, Color color)
    {
        if (cubeTexts == null || cell < 0 || cell >= cubeTexts.Length) return;
        foreach (var t in cubeTexts[cell]) t.color = color;
    }

    private void ClearTints()
    {
        if (cubeTexts == null) return;
        for (int i = 0; i < cubeTexts.Length; i++)
            for (int k = 0; k < cubeTexts[i].Length; k++) cubeTexts[i][k].color = cubeColors[i][k];
    }

    // ---------- Maqueta ----------

    private void ShowMaze(int index, bool withEffect)
    {
        var s = Setup;
        for (int v = 0; v < versions.Length; v++)
            for (int m = 0; m < versions[v].mazes.Length; m++)
            {
                var w = versions[v].mazes[m]?.walls;
                if (w == null) continue;
                if (v == CurrentVersion && m == index) AnimateWalls(w, true);
                else if (w.activeSelf) AnimateWalls(w, false);
            }

        mazeIndex = index;
        var maze = s.mazes[index];
        Find(maze, 'A', out int ar, out int ac);
        if (altar != null)
        {
            if (altarAnim != null) StopCoroutine(altarAnim);
            altarAnim = StartCoroutine(AltarRoutine(CellWorld(maze, ar, ac)));
        }
        PlaceAtStart();
        if (withEffect && board != null) Fx.Play(appearEffect, board.position);
    }

    /// <summary>Segundos que tarda en terminar de subir el laberinto activo (para esperar antes de caminar).</summary>
    private float WallsTime(GameObject walls)
        => walls != null && wallParts.TryGetValue(walls, out var parts) ? riseDuration + riseStagger * parts.Count : 0f;

    private void AnimateWalls(GameObject walls, bool up)
    {
        if (wallAnims.TryGetValue(walls, out var running) && running != null) StopCoroutine(running);
        wallAnims[walls] = StartCoroutine(WallsRoutine(walls, up ? 1f : 0f));
    }

    /// <summary>Los muros suben desde el tablero (o se hunden en él), uno tras otro de norte a sur.</summary>
    private IEnumerator WallsRoutine(GameObject walls, float target)
    {
        if (!wallParts.TryGetValue(walls, out var parts)) { walls.SetActive(target > 0f); yield break; }
        if (target > 0f) walls.SetActive(true);
        var start = new float[parts.Count];
        for (int i = 0; i < parts.Count; i++) start[i] = parts[i].localScale.y / Mathf.Max(0.0001f, restScale[parts[i]].y);

        float total = riseDuration + riseStagger * parts.Count;
        for (float e = 0f; e < total; e += Time.deltaTime)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                float k = Mathf.Lerp(start[i], target, Motion.Ease((e - i * riseStagger) / riseDuration));
                Motion.SetRise(parts[i], restPos[parts[i]], restScale[parts[i]], 0f, k);
            }
            yield return null;
        }
        foreach (var p in parts) Motion.SetRise(p, restPos[p], restScale[p], 0f, target);
        if (target <= 0f) walls.SetActive(false);
    }

    private void SetAltarHeight(float k)
    {
        k = Mathf.Max(0.001f, k);
        altar.localScale = new Vector3(altarScale.x, altarScale.y * k, altarScale.z);
    }

    /// <summary>Si el altar cambia de losa, se hunde, se mueve y vuelve a subir.</summary>
    private IEnumerator AltarRoutine(Vector3 target)
    {
        if (altarShown && (altar.position - target).sqrMagnitude < 1e-6f) yield break;
        if (altarShown)
            for (float e = 0f; e < popDuration; e += Time.deltaTime) { SetAltarHeight(1f - Motion.Ease(e / popDuration)); yield return null; }
        SetAltarHeight(0f);
        altar.position = target;
        altarShown = true;
        for (float e = 0f; e < popDuration * 1.5f; e += Time.deltaTime) { SetAltarHeight(Motion.Ease(e / (popDuration * 1.5f))); yield return null; }
        SetAltarHeight(1f);
    }

    private void PlaceAtStart()
    {
        var maze = Setup.mazes[mazeIndex];
        Find(maze, 'S', out row, out col);
        heading = string.IsNullOrEmpty(maze.start) ? 1 : Math.Max(0, Headings.IndexOf(maze.start[0]));
        SetSensors(false, false);
        if (scarab == null) return;

        Vector3 pos = CellWorld(maze, row, col);
        Quaternion rot = HeadingRotation(heading);
        if (scarabShown && (scarab.position - pos).sqrMagnitude < 1e-6f) { scarab.rotation = rot; return; }
        if (scarabAnim != null) StopCoroutine(scarabAnim);
        scarabAnim = StartCoroutine(ScarabRoutine(pos, rot));
    }

    /// <summary>El escarabajo se encoge donde está y reaparece en la salida.</summary>
    private IEnumerator ScarabRoutine(Vector3 pos, Quaternion rot)
    {
        if (scarabShown)
        {
            Vector3 from = scarab.localScale;
            for (float e = 0f; e < popDuration; e += Time.deltaTime) { scarab.localScale = Vector3.Lerp(from, Vector3.one * 0.001f, Motion.Ease(e / popDuration)); yield return null; }
        }
        scarab.localScale = Vector3.one * 0.001f;
        scarab.position = pos;
        scarab.rotation = rot;
        scarabShown = true;
        for (float e = 0f; e < popDuration; e += Time.deltaTime) { scarab.localScale = Vector3.Lerp(Vector3.one * 0.001f, scarabScale, Motion.Ease(e / popDuration)); yield return null; }
        scarab.localScale = scarabScale;
    }

    private static void Find(Maze maze, char what, out int r, out int c)
    {
        for (r = 0; r < maze.rows.Length; r++)
        {
            c = maze.rows[r].IndexOf(what);
            if (c >= 0) return;
        }
        r = c = 0;
    }

    private static bool Blocked(Maze maze, int r, int c)
        => r < 0 || r >= maze.rows.Length || c < 0 || c >= maze.rows[r].Length || maze.rows[r][c] == '#';

    private Vector3 CellWorld(Maze maze, int r, int c)
    {
        float x = (c - (maze.rows[0].Length - 1) * 0.5f) * cellSize;
        float z = ((maze.rows.Length - 1) * 0.5f - r) * cellSize;
        return board != null ? board.TransformPoint(new Vector3(x, 0f, z)) : new Vector3(x, 0f, z);
    }

    private Quaternion HeadingRotation(int h)
        => (board != null ? board.rotation : Quaternion.identity) * Quaternion.Euler(0f, h * 90f, 0f);

    private void SetSensors(bool front, bool right)
    {
        if (frontBulb != null) frontBulb.SetOn(front);
        if (rightBulb != null) rightBulb.SetOn(right);
    }

    // ---------- Probar ----------

    /// <summary>Conéctalo al botón Probar (ya lo hace el script si el campo Test Button está asignado).</summary>
    public void Test()
    {
        if (!CanPlay || running) return;
        CountInteraction();
        StartCoroutine(RunRoutine());
    }

    private IEnumerator RunRoutine()
    {
        running = true;
        lastOutcome = Outcome.Running;
        culprit = -1;
        SetCubesInteractable(false);
        ClearTints();

        var s = Setup;
        bool failed = false;
        for (int m = 0; m < s.mazes.Length && !failed; m++)
        {
            if (m > 0)
            {
                yield return new WaitForSeconds(pauseAfterFail);
                ShowMaze(m, true);
                yield return new WaitForSeconds(WallsTime(s.mazes[m].walls));
            }
            else if (mazeIndex != 0) ShowMaze(0, false);
            else PlaceAtStart();

            yield return StartCoroutine(Walk(s, s.mazes[m]));
            failed = lastOutcome != Outcome.Reached;
        }

        SetSensors(false, false);
        if (!failed)
        {
            lastOutcome = Outcome.Solved;
            ClearTints();
            running = false;
            Complete();
            yield break;
        }

        Debug.Log($"[ScarabMazePuzzle] Falló: {lastOutcome} (casilla {culprit}).");
        RegisterIncorrect();
        if (culprit >= 0) Tint(culprit, culpritTint);
        yield return new WaitForSeconds(pauseAfterFail);
        if (mazeIndex != 0) ShowMaze(0, false);
        else PlaceAtStart();
        SetCubesInteractable(true);
        running = false;
    }

    /// <summary>Un recorrido completo en un laberinto. Deja el resultado en lastOutcome.</summary>
    private IEnumerator Walk(VersionSetup s, Maze maze)
    {
        int cols = Columns(s);
        for (int steps = 0; ; steps++)
        {
            if (steps >= stepLimit) { lastOutcome = Outcome.Lost; yield break; }
            yield return new WaitForSeconds(pulseInterval);

            // 1. Sensores (relativos al rumbo actual)
            bool front = Blocked(maze, row + DR[heading], col + DC[heading]);
            int rh = (heading + 1) % 4;
            bool right = Blocked(maze, row + DR[rh], col + DC[rh]);
            SetSensors(front, s.twoSensors && right);

            // 2. La tabla: fila = rumbo actual, columna = sensores
            int rowSlot = s.headings.IndexOf(Headings[heading]);
            int input = s.twoSensors ? (front ? 2 : 0) + (right ? 1 : 0) : (front ? 1 : 0);
            int cell = rowSlot * 4 + input;
            ClearTints();
            Tint(cell, activeTint);
            if (rowSlot < 0 || input >= cols) { culprit = -1; lastOutcome = Outcome.UnknownHeading; yield break; }

            int next = CellValue(cell);
            if (s.headings.IndexOf(Headings[next]) < 0)
            {
                // un rumbo que no tiene fila en esta versión: el escarabajo no sabría qué hacer después
                culprit = cell;
                lastOutcome = Outcome.UnknownHeading;
                yield break;
            }

            // 3. Girar y avanzar
            yield return StartCoroutine(Turn(next));
            int nr = row + DR[next], nc = col + DC[next];
            if (Blocked(maze, nr, nc))
            {
                culprit = cell;
                lastOutcome = Outcome.Crashed;
                yield return StartCoroutine(Bump(maze, nr, nc));
                yield break;
            }
            yield return StartCoroutine(Move(maze, nr, nc));
            if (maze.rows[row][col] == 'A') { lastOutcome = Outcome.Reached; yield break; }
        }
    }

    private IEnumerator Turn(int to)
    {
        heading = to;
        if (scarab == null) yield break;
        Quaternion from = scarab.rotation, target = HeadingRotation(to);
        float dur = moveDuration * 0.5f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            scarab.rotation = Quaternion.Slerp(from, target, t / dur);
            yield return null;
        }
        scarab.rotation = target;
    }

    private IEnumerator Move(Maze maze, int r, int c)
    {
        Vector3 from = CellWorld(maze, row, col), to = CellWorld(maze, r, c);
        row = r;
        col = c;
        if (scarab == null) yield break;
        for (float t = 0f; t < moveDuration; t += Time.deltaTime)
        {
            scarab.position = Vector3.Lerp(from, to, t / moveDuration);
            yield return null;
        }
        scarab.position = to;
    }

    /// <summary>Choque: avanza un poco hacia el muro y rebota.</summary>
    private IEnumerator Bump(Maze maze, int r, int c)
    {
        if (scarab == null) yield break;
        Vector3 home = CellWorld(maze, row, col), wall = Vector3.Lerp(home, CellWorld(maze, r, c), 0.35f);
        float half = moveDuration * 0.5f;
        for (float t = 0f; t < half; t += Time.deltaTime) { scarab.position = Vector3.Lerp(home, wall, t / half); yield return null; }
        Fx.Play(crashEffect, wall);
        for (float t = 0f; t < half; t += Time.deltaTime) { scarab.position = Vector3.Lerp(wall, home, t / half); yield return null; }
        scarab.position = home;
    }

    // ---------- Pruebas en el editor ----------

    [ContextMenu("Llenar la tabla con una solución (prueba)")]
    private void FillSolution()
    {
        var s = Setup;
        for (int r = 0; r < s.headings.Length; r++)
        {
            int h = Headings.IndexOf(s.headings[r]);
            for (int c = 0; c < Columns(s); c++)
            {
                var cube = CubeAt(r * 4 + c);
                if (cube != null) cube.AnimateToStep(SolutionFor(CurrentVersion, h, c), 0.2f);
            }
        }
    }

    /// <summary>Una solución válida (la que usan las pistas y las pruebas).</summary>
    public static int SolutionFor(int version, int h, int input)
    {
        int right = (h + 1) % 4, left = (h + 3) % 4;
        if (version == 2)   // mano derecha en la pared
            switch (input)
            {
                case 0: return right;   // delante libre, derecha libre → doblar a la derecha
                case 1: return h;       // delante libre, muro a la derecha → seguir
                case 2: return right;   // muro delante, derecha libre → derecha
                default: return left;   // muro delante y a la derecha → izquierda
            }
        if (version == 0)   // escalera: este ↔ sur
            return input == 0 ? h : (h == 1 ? 2 : 1);
        return input == 0 ? h : right;   // espiral: al chocar, girar a la derecha
    }
}
