using System;
using System.Collections;
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

    // ---------- Ciclo de vida ----------

    protected override void Awake()
    {
        base.Awake();
        CacheCubeTexts();
        foreach (var v in versions)
            if (v?.mazes != null)
                foreach (var m in v.mazes)
                    if (m?.walls != null) m.walls.SetActive(false);
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
            {
                rowLabels[r].gameObject.SetActive(rowOn);
                if (rowOn) rowLabels[r].text = "Yendo " + Arrows[Headings.IndexOf(s.headings[r])];
            }
            for (int c = 0; c < 4; c++)
            {
                var cube = CubeAt(r * 4 + c);
                if (cube != null) cube.gameObject.SetActive(rowOn && c < cols);
            }
        }
        string[] labels = s.twoSensors
            ? new[] { "Delante libre\nDerecha libre", "Delante libre\nMuro a la derecha", "Muro delante\nDerecha libre", "Muro delante\nMuro a la derecha" }
            : new[] { "Delante libre", "Muro delante" };
        for (int c = 0; c < 4; c++)
        {
            if (columnLabels.Length <= c || columnLabels[c] == null) continue;
            columnLabels[c].gameObject.SetActive(c < cols);
            if (c < cols) columnLabels[c].text = labels[c];
        }
        if (rightSensorRoot != null) rightSensorRoot.SetActive(s.twoSensors);

        ShowMaze(0, true);
        StartTracking();   // el reto empieza al llegar
    }

    private static int Columns(VersionSetup s) => s.twoSensors ? 4 : 2;

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
                if (versions[v].mazes[m]?.walls != null)
                    versions[v].mazes[m].walls.SetActive(v == CurrentVersion && m == index);

        mazeIndex = index;
        var maze = s.mazes[index];
        Find(maze, 'A', out int ar, out int ac);
        if (altar != null) altar.position = CellWorld(maze, ar, ac);
        PlaceAtStart();
        if (withEffect && board != null) Fx.Play(appearEffect, board.position);
    }

    private void PlaceAtStart()
    {
        var maze = Setup.mazes[mazeIndex];
        Find(maze, 'S', out row, out col);
        heading = string.IsNullOrEmpty(maze.start) ? 1 : Math.Max(0, Headings.IndexOf(maze.start[0]));
        if (scarab != null)
        {
            scarab.position = CellWorld(maze, row, col);
            scarab.rotation = HeadingRotation(heading);
        }
        SetSensors(false, false);
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
