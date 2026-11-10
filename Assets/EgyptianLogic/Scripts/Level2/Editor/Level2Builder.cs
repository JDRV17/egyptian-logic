using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Menú "Egyptian Logic > Nivel 2" que arma en la escena abierta los retos del Nivel 2
/// (5 juegos × 3 versiones; el 2.6 La Balanza Digital se eliminó) con piezas básicas: switches, ranuras, cables, compuertas, botones y carteles.
/// Cada reto queda con un PuzzleVersionManager que activa una versión al iniciar (aleatoria por defecto)
/// y que el AdaptationEngine puede controlar igual que en el Nivel 1.
/// Luego se pueden mover, reemplazar los modelos o cambiar materiales sin tocar la lógica.
/// </summary>
public static partial class Level2Builder
{
    private const string MaterialFolder = "Assets/EgyptianLogic/Materials/Level2";
    private const string Menu = "Egyptian Logic/Nivel 2/";

    // ================================================================ Menú

    [MenuItem(Menu + "Construir los 5 retos")]
    public static void BuildAll()
    {
        var root = new GameObject("Nivel2_Retos");
        Undo.RegisterCreatedObjectUndo(root, "Construir retos del Nivel 2");

        float x = 0f;
        foreach (var build in new System.Func<Transform, float>[] { Build21, Build22, Build23, Build24, Build25 })
        {
            var holder = new GameObject("tmp");
            holder.transform.SetParent(root.transform, false);
            float width = build(holder.transform);
            var challenge = holder.transform.GetChild(0);
            challenge.SetParent(root.transform, false);
            Object.DestroyImmediate(holder);

            // Cada reto reparte sus piezas unos 4 m a cada lado: se dejan al menos 9 m entre retos
            float slot = Mathf.Max(width, 9f);
            challenge.localPosition = new Vector3(x + slot * 0.5f, 0f, 0f);
            x += slot + 1.5f;
        }

        Selection.activeGameObject = root;
        Debug.Log("[Level2Builder] Retos del Nivel 2 creados. Revisa Assets/EgyptianLogic/Scripts/Level2/LEEME_Nivel2.md");
    }

    [MenuItem(Menu + "Construir solo 2.1 El Detective de Verdades")] private static void Menu21() => BuildOne(Build21);
    [MenuItem(Menu + "Construir solo 2.2 Solo Tengo Esto (NAND)")] private static void Menu22() => BuildOne(Build22);
    [MenuItem(Menu + "Construir solo 2.3 El Ahorro de Compuertas")] private static void Menu23() => BuildOne(Build23);
    [MenuItem(Menu + "Construir solo 2.4 El Selector (MUX)")] private static void Menu24() => BuildOne(Build24);
    [MenuItem(Menu + "Construir solo 2.5 El Repartidor (Decodificador)")] private static void Menu25() => BuildOne(Build25);

    private static void BuildOne(System.Func<Transform, float> build)
    {
        var holder = new GameObject("tmp");
        build(holder.transform);
        var challenge = holder.transform.GetChild(0);
        challenge.SetParent(null, true);
        Object.DestroyImmediate(holder);
        Undo.RegisterCreatedObjectUndo(challenge.gameObject, "Construir reto");
        Selection.activeGameObject = challenge.gameObject;
    }

    /// <summary>Construye un reto dentro de <paramref name="parent"/> y devuelve su raíz y su ancho.</summary>
    private static Transform BuildInto(Transform parent, System.Func<Transform, float> build, out float width)
    {
        var holder = new GameObject("tmp");
        holder.transform.SetParent(parent, false);
        width = build(holder.transform);
        var challenge = holder.transform.GetChild(0);
        challenge.SetParent(parent, false);
        Object.DestroyImmediate(holder);
        return challenge;
    }

    // ================================================================ Nivel completo

    // 2.6 "La Balanza Digital" (sumadores) se eliminó: el nivel queda con 5 retos, como los niveles 1 y 3
    private const int ChallengeCount = 5;
    private static readonly string[] ChallengeIds = { "2.1", "2.2", "2.3", "2.4", "2.5" };

    /// <summary>Nombres que ve el jugador (sin número ni dificultad).</summary>
    private static readonly string[] ChallengeNames =
    {
        "El Detective de Verdades", "Solo Tengo Esto", "El Ahorro de Compuertas",
        "El Selector", "El Repartidor"
    };

    // Valores ideales del documento: (segundos, interacciones) por versión, y si el reto tiene botón de comprobar
    private static readonly (float t, int i)[][] Ideals =
    {
        new[] { (25f, 4), (60f, 8), (120f, 16) },
        new[] { (25f, 7), (60f, 10), (150f, 17) },
        new[] { (30f, 2), (60f, 6), (90f, 7) },
        new[] { (30f, 8), (90f, 20), (180f, 48) },
        new[] { (30f, 8), (90f, 24), (180f, 33) },
    };
    private static readonly bool[] HasSubmit = { true, true, true, false, false };

    // Medidas del comedor (en metros): 40 × 28, la mesa de banquete al centro y los retos en los muros largos
    // (3 en el norte y 2 en el sur; el puesto sureste quedó libre al quitar el 2.6).
    private const float HallHalfX = 20f;          // entrada al oeste (x = −20), salida al este (x = 20)
    private const float HallHalfZ = 14f;          // muro norte en z = 14, muro sur en z = −14
    private const float HallHeight = 8f;
    private const float WallThick = 0.4f;
    private const float ColumnSpacing = 11f;      // centros de los retos en x = −11, 0 y 11
    private const float TableLength = 8f;         // una mesa por columna, con pasos de 3 m entre ellas
    private const float PanelInset = 0.6f;        // distancia del reto al muro
    private const float AisleZ = 3.2f;            // pasillo entre las sillas y las piezas: ahí aparecen los instrumentos
    private const float PathTorchZ = 2f;          // antorchas del camino, junto a las sillas
    private const float DoorWidth = 2.4f;
    private const float DoorHeight = 3f;
    private const float CorridorLength = 6f;

    // Ubicación de cada reto (columna, muro norte o sur), como en el plano del comedor
    private static readonly (int col, bool north)[] Cells =
    {
        (0, true), (1, true), (0, false), (1, false), (2, true)
    };

    // Instrumento que aparece al resolver cada reto para abrir el siguiente. El del último (el brasero) abre la salida.
    private enum Instrument { Lever, PressurePlate, Key, Button, TorchBrazier, ExitLever }
    private static readonly Instrument[] Instruments =
    {
        Instrument.Lever, Instrument.PressurePlate, Instrument.Key,
        Instrument.Button, Instrument.TorchBrazier
    };

    private static float ColumnX(int k) => (Cells[k].col - 1) * ColumnSpacing;
    private static float Side(int k) => Cells[k].north ? 1f : -1f;

    /// <summary>Donde aparece el instrumento del reto k: en el pasillo frente a sus piezas, a un lado.</summary>
    private static Vector3 InstrumentPosition(int k) => new Vector3(ColumnX(k) + 3f, 0f, Side(k) * AisleZ);

    [MenuItem(Menu + "Construir Nivel 2 completo (salón, retos y sistemas)", false, 0)]
    public static void BuildLevel()
    {
        var level = new GameObject("Nivel2");
        Undo.RegisterCreatedObjectUndo(level, "Construir Nivel 2");

        // ---- Sistemas (los mismos del Nivel 1)
        var sys = new GameObject("Sistemas_Nivel2");
        sys.transform.SetParent(level.transform, false);
        var tracker = sys.AddComponent<PerformanceTracker>();
        var hints = sys.AddComponent<HintManager>();
        var spawner = sys.AddComponent<HintScrollSpawner>();
        var logger = sys.AddComponent<CsvStageLogger>();
        var engine = sys.AddComponent<AdaptationEngine>();
        var flow = sys.AddComponent<Level2Flow>();

        var hall = new GameObject("Salon").transform;
        hall.SetParent(level.transform, false);

        // ---- Retos
        var builders = new System.Func<Transform, float>[] { Build21, Build22, Build23, Build24, Build25 };
        var challenges = new Transform[ChallengeCount];
        for (int k = 0; k < ChallengeCount; k++)
            challenges[k] = BuildInto(hall, builders[k], out _);

        // ---- El comedor
        Architecture(hall);
        var progress = new List<TextMeshPro>();
        Entrance(hall, progress);

        var decor = new GameObject("Ambiente").transform;
        decor.SetParent(hall, false);
        for (int c = 0; c < 3; c++)
        {
            var table = new GameObject("MesaBanquete_" + (c + 1)).transform;
            table.SetParent(decor, false);
            table.localPosition = new Vector3((c - 1) * ColumnSpacing, 0f, 0f);
            BanquetTable(table, TableLength);
        }
        Pillars(decor);
        foreach (float x in new[] { -1f, 1f })
            foreach (float z in new[] { -1f, 1f })
                Brazier(decor, new Vector3(x * (HallHalfX - 1.6f), 0f, z * (HallHalfZ - 2.5f)));

        // ---- Retos contra los muros
        var managers = new PuzzleVersionManager[ChallengeCount];
        var panelTorches = new Level2Torch[ChallengeCount][];
        flow.stations = new Level2Flow.Station[ChallengeCount];

        for (int k = 0; k < ChallengeCount; k++)
        {
            var area = new GameObject($"Reto_{k + 1} {ChallengeNames[k]}").transform;
            area.SetParent(hall, false);
            area.localPosition = new Vector3(ColumnX(k), 0f, Side(k) * HallHalfZ);
            area.localRotation = Quaternion.Euler(0f, Cells[k].north ? 0f : 180f, 0f);   // +Z local hacia el muro

            challenges[k].SetParent(area, false);
            challenges[k].localPosition = new Vector3(0f, 0f, -PanelInset);
            challenges[k].localRotation = Quaternion.identity;
            Rug(area, new Vector3(0f, 0.006f, -PanelInset - 1.6f), new Vector2(3.2f, 1.6f));

            managers[k] = challenges[k].GetComponent<PuzzleVersionManager>();
            var pvm = new SerializedObject(managers[k]);
            pvm.FindProperty("randomizeOnStart").boolValue = false; // la versión la decide el AdaptationEngine
            pvm.ApplyModifiedPropertiesWithoutUndo();

            foreach (var p in challenges[k].GetComponentsInChildren<Level2Puzzle>(true))
            {
                p.tracker = tracker;
                p.stageIndex = k;
            }

            // Cartel con el nombre, luz indicadora, luz para leer el panel y antorchas a los lados
            Level2Visuals.CreateLabel(area, "Cartel", ChallengeNames[k], new Vector3(0f, 4.3f, -0.03f), new Vector2(7f, 0.8f), 3f);
            var lamp = MakeLamp(area, "Indicador", new Vector3(0f, 5.1f, -0.2f), 0.3f);
            PointLight(area, "LuzPanel", new Vector3(0f, 3.2f, -1.8f), 4.5f, 0.9f, new Color(1f, 0.85f, 0.65f));
            panelTorches[k] = new[]
            {
                WallTorch(area, "AntorchaPanelIzq", new Vector3(-3.4f, 2.4f, 0f), Vector3.back, k == 0),
                WallTorch(area, "AntorchaPanelDer", new Vector3(3.4f, 2.4f, 0f), Vector3.back, k == 0),
            };

            // Zona de pistas: desde el muro hasta el pasillo, frente al reto
            float depth = HallHalfZ - AisleZ - 1f;
            var zone = TriggerBox(area, "ZonaPistas", new Vector3(0f, 1.5f, -depth * 0.5f), new Vector3(9.5f, 3f, depth));
            var hz = zone.AddComponent<HintZone>();
            var hzo = new SerializedObject(hz);
            hzo.FindProperty("manager").objectReferenceValue = hints;
            hzo.FindProperty("tracker").objectReferenceValue = tracker;
            hzo.FindProperty("stageIndex").intValue = k;
            hzo.ApplyModifiedPropertiesWithoutUndo();

            flow.stations[k] = new Level2Flow.Station
            {
                name = ChallengeNames[k],
                puzzles = challenges[k].GetComponentsInChildren<Level2Puzzle>(true),
                zone = zone,
                indicator = lamp,
                rising = challenges[k],
                riseSound = k > 0 ? Sound(challenges[k], "SFX/sand_emerge (1).wav", false) : null,
            };
        }

        // ---- Instrumentos y camino de antorchas hacia el reto siguiente
        for (int k = 0; k < ChallengeCount; k++)
        {
            var station = flow.stations[k];
            var holder = new GameObject("Paso_" + (k + 1)).transform;
            holder.SetParent(hall, false);

            var torches = new List<Level2Torch>();
            station.instrument = MakeInstrument(holder, k, torches);
            if (k + 1 < ChallengeCount)
            {
                torches.AddRange(PathTorches(holder, k));
                torches.AddRange(panelTorches[k + 1]);
            }
            else
            {
                torches.AddRange(PathTorches(holder, k));
                station.gate = ExitDoor(holder, torches, progress);
            }
            station.torches = torches.ToArray();
        }
        ExitCorridor(hall, flow);

        flow.hintManager = hints;
        flow.progressTexts = progress.ToArray();
        ConfigureTracker(tracker, hints);
        ConfigureHints(hints, spawner, tracker, managers);
        ConfigureSpawner(spawner, tracker);
        ConfigureEngine(engine, tracker, managers);
        var lo = new SerializedObject(logger);
        lo.FindProperty("tracker").objectReferenceValue = tracker;
        lo.ApplyModifiedPropertiesWithoutUndo();

        ConfigureSceneLighting();

        // Todo lo que se agarra va en la capa Interactable: no choca con el jugador (capa Body).
        // Si chocara, al agarrar algo por debajo el jugador se subiría encima y saldría volando.
        int layer = LayerMask.NameToLayer("Interactable");
        if (layer < 0) Debug.LogWarning("[Level2Builder] No existe la capa Interactable: los objetos agarrables pueden empujar al jugador.");
        else
            foreach (var g in level.GetComponentsInChildren<XRGrabInteractable>(true))
                SetLayer(g.transform, layer);

        // Modelos, materiales, fuentes y sonidos del resto del juego (Level2Look.cs)
        Level2Look.Apply(level);

        Selection.activeGameObject = level;
        Debug.Log($"[Level2Builder] Nivel 2 construido: comedor de {HallHalfX * 2f:F0} × {HallHalfZ * 2f:F0} m. " +
                  "Mueve tu jugador a 'PuntoDeInicio' y revisa LEEME_Nivel2.md.");
    }

    private static void SetLayer(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        foreach (Transform child in t) SetLayer(child, layer);
    }

    // ================================================================ Arquitectura

    private static void Architecture(Transform hall)
    {
        var root = new GameObject("Muros").transform;
        root.SetParent(hall, false);
        float h = HallHeight, t = WallThick * 0.5f, x = HallHalfX, z = HallHalfZ;

        Floor(hall, "Piso", Vector3.zero, x * 2f, z * 2f);
        Ceiling(hall, Vector3.zero, x * 2f, z * 2f);

        // Muros largos en tramos de 10 m (así cada pieza recibe solo las luces cercanas)
        for (int i = 0; i < 4; i++)
        {
            float x0 = -x + i * x * 0.5f, x1 = x0 + x * 0.5f;
            WallRun(root, "MuroNorte_" + (i + 1), new Vector3(x0, 0f, z + t), new Vector3(x1, 0f, z + t), h);
            WallRun(root, "MuroSur_" + (i + 1), new Vector3(x0, 0f, -z - t), new Vector3(x1, 0f, -z - t), h);
        }
        WallRun(root, "MuroEntrada_N", new Vector3(-x - t, 0f, 0f), new Vector3(-x - t, 0f, z + 2f * t), h);
        WallRun(root, "MuroEntrada_S", new Vector3(-x - t, 0f, -z - 2f * t), new Vector3(-x - t, 0f, 0f), h);
        // Muro este con el vano de la salida en el centro
        WallRun(root, "MuroSalida", new Vector3(x + t, 0f, -z - 2f * t), new Vector3(x + t, 0f, z + 2f * t), h, z + 2f * t, DoorWidth, DoorHeight);
        Floor(root, "Umbral", new Vector3(x + t, 0f, 0f), WallThick, DoorWidth);
    }

    /// <summary>
    /// Muro recto de <paramref name="a"/> a <paramref name="b"/> (centrado en esa línea), con un vano opcional
    /// centrado a <paramref name="openAt"/> metros de <paramref name="a"/> y un dintel encima.
    /// </summary>
    private static Transform WallRun(Transform parent, string name, Vector3 a, Vector3 b, float height,
                                     float openAt = -1f, float openW = 0f, float openH = 0f)
    {
        var frame = new GameObject(name).transform;
        frame.SetParent(parent, false);
        frame.localPosition = (a + b) * 0.5f;
        frame.localRotation = Quaternion.FromToRotation(Vector3.right, (b - a).normalized);
        float span = (b - a).magnitude;
        float start = -span * 0.5f, end = span * 0.5f;

        if (openAt < 0f)
        {
            Wall(frame, "Tramo", new Vector3(0f, height * 0.5f, 0f), new Vector3(span, height, WallThick));
            return frame;
        }

        float oc = start + openAt;
        float l = oc - openW * 0.5f, r = oc + openW * 0.5f;
        if (l - start > 0.01f) Wall(frame, "Tramo1", new Vector3((start + l) * 0.5f, height * 0.5f, 0f), new Vector3(l - start, height, WallThick));
        if (end - r > 0.01f) Wall(frame, "Tramo2", new Vector3((r + end) * 0.5f, height * 0.5f, 0f), new Vector3(end - r, height, WallThick));
        if (height - openH > 0.01f)
            Wall(frame, "Dintel", new Vector3(oc, (openH + height) * 0.5f, 0f), new Vector3(openW, height - openH, WallThick));
        return frame;
    }

    private static void Floor(Transform parent, string name, Vector3 center, float sizeX, float sizeZ)
    {
        // En baldosas de 5 m como máximo: un piso de una sola pieza recibe solo 4 luces y parpadea al moverse
        int nx = Mathf.Max(1, Mathf.CeilToInt(sizeX / 5f)), nz = Mathf.Max(1, Mathf.CeilToInt(sizeZ / 5f));
        float tx = sizeX / nx, tz = sizeZ / nz;
        var mat = FloorMaterial(tx, tz);
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                var pos = center + new Vector3(-sizeX * 0.5f + (i + 0.5f) * tx, -0.1f, -sizeZ * 0.5f + (j + 0.5f) * tz);
                Prim(PrimitiveType.Cube, parent, name, pos, new Vector3(tx, 0.2f, tz), mat, true);
            }
    }

    private static void Ceiling(Transform parent, Vector3 center, float sizeX, float sizeZ, float height = HallHeight)
    {
        var roof = Prim(PrimitiveType.Cube, parent, "Techo", center + new Vector3(0f, height + 0.1f, 0f),
            new Vector3(sizeX, 0.2f, sizeZ), Mat("L2_Techo", new Color(0.08f, 0.12f, 0.3f)), true);
        // Sin sombra, para que la luz general entre al salón
        roof.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private static void Wall(Transform parent, string name, Vector3 pos, Vector3 size) =>
        Prim(PrimitiveType.Cube, parent, name, pos, size, WallMaterial(size.x, size.y), true);

    /// <summary>Pilares junto a los muros largos, entre los retos y en los extremos, con objetos sueltos a sus pies.</summary>
    private static void Pillars(Transform decor)
    {
        float z = HallHalfZ - 1f;
        float[] xs = { -1.5f * ColumnSpacing, -0.5f * ColumnSpacing, 0.5f * ColumnSpacing, 1.5f * ColumnSpacing };
        int n = 0;
        foreach (float x in xs)
            foreach (float side in new[] { 1f, -1f })
            {
                Pillar(decor, new Vector3(x, 0f, side * z));
                // Ánforas, vasijas y cestas que se pueden agarrar (solo entre retos, no en los extremos)
                if (Mathf.Abs(x) > ColumnSpacing) continue;
                var spot = new Vector3(x, 0f, side * (z - 1.4f));
                switch (n++ % 3)
                {
                    case 0: Amphora(decor, spot); break;
                    case 1: Pot(decor, spot); break;
                    default: Basket(decor, spot); break;
                }
            }
    }

    private static void Pillar(Transform parent, Vector3 pos)
    {
        var p = new GameObject("Pilar").transform;
        p.SetParent(parent, false);
        p.localPosition = pos;
        var stone = Mat("L2_Pilar", new Color(0.78f, 0.67f, 0.48f));
        Prim(PrimitiveType.Cylinder, p, "Fuste", new Vector3(0f, HallHeight * 0.5f, 0f), new Vector3(1f, HallHeight * 0.5f, 1f), stone, true);
        Prim(PrimitiveType.Cylinder, p, "Base", new Vector3(0f, 0.12f, 0f), new Vector3(1.3f, 0.12f, 1.3f), stone);
        Prim(PrimitiveType.Cylinder, p, "Capitel", new Vector3(0f, HallHeight - 0.25f, 0f), new Vector3(1.45f, 0.25f, 1.45f), stone);
    }

    /// <summary>Entrada al oeste: puerta (cerrada, decorativa), punto de inicio, baldosas sueltas y estela con el progreso.</summary>
    private static void Entrance(Transform hall, List<TextMeshPro> progress)
    {
        var root = new GameObject("Entrada").transform;
        root.SetParent(hall, false);
        float x = -HallHalfX;

        Prim(PrimitiveType.Cube, root, "PuertaEntrada", new Vector3(x + 0.05f, DoorHeight * 0.5f, 0f),
            new Vector3(0.15f, DoorHeight, DoorWidth), Mat("L2_Puerta", new Color(0.38f, 0.26f, 0.14f)), true);
        Rug(root, new Vector3(x + 2.2f, 0.006f, 0f), new Vector2(2.6f, 3.2f));

        var spawn = new GameObject("PuntoDeInicio").transform;
        spawn.SetParent(hall, false);
        spawn.localPosition = new Vector3(x + 2f, 0f, 0f);
        spawn.localRotation = Quaternion.Euler(0f, 90f, 0f);   // mirando a lo largo de la mesa

        foreach (var p in new[] { new Vector3(x + 3.2f, 0f, 1.6f), new Vector3(x + 4.1f, 0f, -1.4f), new Vector3(x + 2.4f, 0f, -2.3f) })
            FloorTile(root, p);

        // Estela con el progreso, junto al inicio
        var stele = new GameObject("Estela").transform;
        stele.SetParent(root, false);
        stele.localPosition = new Vector3(x + 3.2f, 0f, 3.6f);
        stele.localRotation = Quaternion.LookRotation(Vector3.right);   // se lee desde el lado de la entrada
        Prim(PrimitiveType.Cube, stele, "Piedra", new Vector3(0f, 1.1f, 0.1f), new Vector3(1.6f, 2.2f, 0.25f), Mat("L2_Pilar", new Color(0.78f, 0.67f, 0.48f)), true);
        progress.Add(Level2Visuals.CreateLabel(stele, "Progreso", $"Pruebas superadas: 0 / {ChallengeCount}", new Vector3(0f, 1.5f, -0.04f), new Vector2(1.4f, 0.6f), 1.2f));
    }

    private static void FloorTile(Transform parent, Vector3 pos)
    {
        var root = new GameObject("BaldosaSuelta").transform;
        root.SetParent(parent, false);
        root.localPosition = pos;

        var tile = Prim(PrimitiveType.Cube, root, "Losa", new Vector3(0f, 0.03f, 0f), new Vector3(0.9f, 0.06f, 0.9f),
            Mat("L2_BaldosaSuelta", new Color(0.55f, 0.45f, 0.3f)), true);
        var trigger = root.gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.35f, 0f);
        trigger.size = new Vector3(0.85f, 0.6f, 0.85f);

        var ft = root.gameObject.AddComponent<FloorTile>();
        ft.tile = tile.transform;
        ft.glowRenderer = tile.GetComponent<Renderer>();
        ft.pressSound = Sound(root, "SFX/small_stone_thud.wav", false);
    }

    /// <summary>Puerta de salida al Nivel 3 (centro del muro este): sube al usar la última palanca.</summary>
    private static DoorOpener ExitDoor(Transform holder, List<Level2Torch> torches, List<TextMeshPro> progress)
    {
        var frame = new GameObject("PuertaSalida").transform;
        frame.SetParent(holder, false);
        frame.localPosition = new Vector3(HallHalfX + WallThick * 0.5f, 0f, 0f);
        frame.localRotation = Quaternion.LookRotation(Vector3.left);   // +Z local hacia el comedor

        var door = Prim(PrimitiveType.Cube, frame, "Puerta", new Vector3(0f, DoorHeight * 0.5f, 0f),
            new Vector3(DoorWidth, DoorHeight, 0.25f), Mat("L2_Puerta", new Color(0.38f, 0.26f, 0.14f)), true);
        var opener = frame.gameObject.AddComponent<DoorOpener>();
        var so = new SerializedObject(opener);
        var doors = so.FindProperty("doors");
        doors.arraySize = 1;
        doors.GetArrayElementAtIndex(0).objectReferenceValue = door.transform;
        so.FindProperty("openOffset").vector3Value = new Vector3(0f, DoorHeight + 0.1f, 0f);
        so.FindProperty("speed").floatValue = 1f;
        so.FindProperty("openSound").objectReferenceValue = Sound(frame, "SFX/stone_slide.wav", false);
        so.ApplyModifiedPropertiesWithoutUndo();

        // Los carteles se leen desde el comedor (+Z local): se giran 180°
        float front = WallThick * 0.5f + 0.02f;
        Level2Visuals.CreateLabel(frame, "CartelSalida", "Nivel 3", new Vector3(0f, DoorHeight + 0.45f, front), new Vector2(DoorWidth, 0.6f), 2f)
            .transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var sign = Level2Visuals.CreateLabel(frame, "Progreso", $"Pruebas superadas: 0 / {ChallengeCount}", new Vector3(0f, DoorHeight + 1.5f, front),
            new Vector2(6f, 0.9f), 2f);
        sign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        progress.Add(sign);

        float side = DoorWidth * 0.5f + 0.7f;
        torches.Add(WallTorch(frame, "Antorcha", new Vector3(-side, 2.3f, WallThick * 0.5f), Vector3.forward, false));
        torches.Add(WallTorch(frame, "Antorcha", new Vector3(side, 2.3f, WallThick * 0.5f), Vector3.forward, false));
        return opener;
    }

    /// <summary>Pasillo detrás de la puerta de salida, con la zona que carga el Nivel 3.</summary>
    private static void ExitCorridor(Transform hall, Level2Flow flow)
    {
        var root = new GameObject("PasilloSalida").transform;
        root.SetParent(hall, false);
        float x0 = HallHalfX + WallThick;
        float cx = x0 + CorridorLength * 0.5f, h = DoorHeight + 0.6f, half = DoorWidth * 0.5f + WallThick * 0.5f;

        Floor(root, "Piso", new Vector3(cx, 0f, 0f), CorridorLength, DoorWidth + 2f * WallThick);
        Ceiling(root, new Vector3(cx, 0f, 0f), CorridorLength, DoorWidth + 2f * WallThick, h);
        WallRun(root, "MuroN", new Vector3(x0, 0f, half), new Vector3(x0 + CorridorLength, 0f, half), h);
        WallRun(root, "MuroS", new Vector3(x0, 0f, -half), new Vector3(x0 + CorridorLength, 0f, -half), h);
        WallRun(root, "Fondo", new Vector3(x0 + CorridorLength + WallThick * 0.5f, 0f, -half), new Vector3(x0 + CorridorLength + WallThick * 0.5f, 0f, half), h);

        var exit = TriggerBox(root, "ZonaSalida", new Vector3(x0 + CorridorLength * 0.65f, 1.5f, 0f), new Vector3(1.4f, 3f, DoorWidth));
        var levelExit = exit.AddComponent<LevelExit>();
        levelExit.nextSceneName = "EgyptLevel3";
        levelExit.flow = flow;

        // Las antorchas del pasillo se prenden con las de la puerta (al usar el último instrumento)
        var last = flow.stations[ChallengeCount - 1];
        var list = new List<Level2Torch>(last.torches)
        {
            WallTorch(root, "Antorcha", new Vector3(cx, 2.2f, half - WallThick * 0.5f), Vector3.back, false),
            WallTorch(root, "Antorcha", new Vector3(cx, 2.2f, -half + WallThick * 0.5f), Vector3.forward, false),
        };
        last.torches = list.ToArray();
    }

    // ================================================================ Luz

    private static Light PointLight(Transform parent, string name, Vector3 pos, float range, float intensity, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = range;
        light.intensity = intensity;
        light.color = color;
        light.shadows = LightShadows.None;
        return light;
    }

    /// <summary>
    /// Antorcha de pared apagada (o encendida) en <paramref name="pos"/>, con el fuego hacia <paramref name="outward"/>
    /// (dirección local que sale del muro). Rango corto para que cada pieza del salón reciba pocas luces.
    /// </summary>
    private static Level2Torch WallTorch(Transform parent, string name, Vector3 pos, Vector3 outward, bool lit)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        root.localPosition = pos;
        root.localRotation = Quaternion.LookRotation(outward);

        var bronze = Mat("L2_Bronce", new Color(0.35f, 0.24f, 0.12f));
        Prim(PrimitiveType.Cube, root, "Soporte", new Vector3(0f, -0.25f, 0.08f), new Vector3(0.08f, 0.08f, 0.16f), bronze);
        Prim(PrimitiveType.Cylinder, root, "Palo", new Vector3(0f, -0.1f, 0.16f), new Vector3(0.06f, 0.2f, 0.06f), Mat("L2_Madera", new Color(0.32f, 0.2f, 0.11f)));
        var flame = Prim(PrimitiveType.Sphere, root, "Fuego", new Vector3(0f, 0.16f, 0.16f), new Vector3(0.13f, 0.22f, 0.13f),
            Mat("L2_Llama", new Color(1f, 0.6f, 0.2f), true));

        var light = PointLight(root, "Luz", new Vector3(0f, 0.3f, 0.35f), 5f, 1.4f, new Color(1f, 0.62f, 0.28f));
        var flicker = light.gameObject.AddComponent<TorchFlicker>();
        flicker.minIntensity = 1.25f;   // parpadeo suave de fuego, no un apagón
        flicker.maxIntensity = 1.55f;
        flicker.flickerSpeed = 3f;

        var torch = root.gameObject.AddComponent<Level2Torch>();
        torch.flameLight = light;
        torch.flame = flame;
        torch.litOnStart = lit;
        torch.igniteSound = Sound(root, "SFX/torch_ignite.wav", false);
        return torch;
    }

    /// <summary>Antorcha de pie (palo con un cuenco de fuego) para marcar el camino. Sin luz propia, solo brilla, salvo que se pida.</summary>
    private static Level2Torch StandingTorch(Transform parent, Vector3 pos, bool withLight)
    {
        var root = new GameObject("AntorchaCamino").transform;
        root.SetParent(parent, false);
        root.localPosition = pos;

        var bronze = Mat("L2_Bronce", new Color(0.35f, 0.24f, 0.12f));
        Prim(PrimitiveType.Cylinder, root, "Palo", new Vector3(0f, 0.65f, 0f), new Vector3(0.07f, 0.65f, 0.07f), Mat("L2_Madera", new Color(0.32f, 0.2f, 0.11f)), true);
        Prim(PrimitiveType.Cylinder, root, "Base", new Vector3(0f, 0.02f, 0f), new Vector3(0.35f, 0.02f, 0.35f), bronze);
        Prim(PrimitiveType.Sphere, root, "Cuenco", new Vector3(0f, 1.32f, 0f), new Vector3(0.26f, 0.1f, 0.26f), bronze);
        var flame = Prim(PrimitiveType.Sphere, root, "Fuego", new Vector3(0f, 1.48f, 0f), new Vector3(0.16f, 0.26f, 0.16f),
            Mat("L2_Llama", new Color(1f, 0.6f, 0.2f), true));

        var torch = root.gameObject.AddComponent<Level2Torch>();
        torch.flame = flame;
        torch.igniteSound = Sound(root, "SFX/torch_ignite.wav", false);
        if (withLight)
        {
            var light = PointLight(root, "Luz", new Vector3(0f, 1.8f, 0f), 4f, 1.2f, new Color(1f, 0.62f, 0.28f));
            var flicker = light.gameObject.AddComponent<TorchFlicker>();
            flicker.minIntensity = 1.05f;
            flicker.maxIntensity = 1.3f;
            flicker.flickerSpeed = 3f;
            torch.flameLight = light;
        }
        return torch;
    }

    /// <summary>
    /// Antorchas de pie desde el instrumento del reto k hasta el reto siguiente (o la salida), por el pasillo
    /// junto a las sillas. Si el siguiente está en el otro muro, el camino cruza por el paso entre mesas más cercano.
    /// </summary>
    private static List<Level2Torch> PathTorches(Transform holder, int k)
    {
        var route = new List<Vector3>();
        Vector3 start = InstrumentPosition(k);
        float zA = Side(k) * PathTorchZ;

        if (k + 1 < ChallengeCount)
        {
            float x1 = ColumnX(k + 1), zB = Side(k + 1) * PathTorchZ;
            route.Add(new Vector3(start.x, 0f, zA));
            if (!Mathf.Approximately(zA, zB))
            {
                // Cruza al otro muro por el paso más cercano: entre dos mesas o rodeando un extremo
                float tableEnd = ColumnSpacing + TableLength * 0.5f + 1.5f;
                float[] gaps = { -tableEnd, -ColumnSpacing * 0.5f, ColumnSpacing * 0.5f, tableEnd };
                float gx = gaps.OrderBy(g => Mathf.Abs(g - start.x) + Mathf.Abs(g - x1)).First();
                route.Add(new Vector3(gx, 0f, zA));
                route.Add(new Vector3(gx, 0f, zB));
            }
            route.Add(new Vector3(x1, 0f, zB));
        }
        else
        {
            // Del último reto a la salida, rodeando el extremo de la mesa
            float end = ColumnSpacing + TableLength * 0.5f + 1.8f;
            route.AddRange(new[] { new Vector3(start.x, 0f, zA), new Vector3(end, 0f, zA), new Vector3(HallHalfX - 1.5f, 0f, 0f) });
        }

        // Una antorcha cada 3.5 m a lo largo del camino (una de cada dos con luz).
        // En los tramos que cruzan entre mesas no se ponen: estorbarían el paso.
        var torches = new List<Level2Torch>();
        var path = new GameObject("CaminoAntorchas").transform;
        path.SetParent(holder, false);
        const float step = 3.5f;
        float walked = 0f, next = 1.5f;
        for (int i = 0; i + 1 < route.Count; i++)
        {
            Vector3 a = route[i], b = route[i + 1];
            float len = Vector3.Distance(a, b);
            bool crossing = Mathf.Abs(a.x - b.x) < 0.01f;
            for (; next <= walked + len; next += step)
                if (!crossing) torches.Add(StandingTorch(path, Vector3.Lerp(a, b, (next - walked) / len), torches.Count % 2 == 0));
            walked += len;
        }
        return torches;
    }

    private static void Brazier(Transform parent, Vector3 pos)
    {
        var b = new GameObject("Brasero").transform;
        b.SetParent(parent, false);
        b.localPosition = pos;
        BrazierBody(b);
        var fire = BrazierFire(b, true);
        fire.fireSound = Sound(b, "Ambience/fire.wav", true);
        fire.fireSound.volume = 0.25f;
        fire.fireSound.maxDistance = 6f;
    }

    private static void BrazierBody(Transform b)
    {
        var metal = Mat("L2_Bronce", new Color(0.35f, 0.24f, 0.12f));
        Prim(PrimitiveType.Cylinder, b, "Pie", new Vector3(0f, 0.5f, 0f), new Vector3(0.12f, 0.5f, 0.12f), metal, true);
        Prim(PrimitiveType.Cylinder, b, "Base", new Vector3(0f, 0.03f, 0f), new Vector3(0.4f, 0.03f, 0.4f), metal);
        Prim(PrimitiveType.Sphere, b, "Cuenco", new Vector3(0f, 1.05f, 0f), new Vector3(0.55f, 0.18f, 0.55f), metal);
    }

    private static Level2Torch BrazierFire(Transform b, bool lit)
    {
        var embers = Prim(PrimitiveType.Sphere, b, "Brasas", new Vector3(0f, 1.15f, 0f), new Vector3(0.42f, 0.12f, 0.42f),
            Mat("L2_Brasas", new Color(1f, 0.45f, 0.1f), true));
        var light = PointLight(b, "Fuego", new Vector3(0f, 1.6f, 0f), 5f, 1.5f, new Color(1f, 0.6f, 0.25f));
        var flicker = light.gameObject.AddComponent<TorchFlicker>();
        flicker.minIntensity = 1.35f;
        flicker.maxIntensity = 1.65f;
        flicker.flickerSpeed = 3f;

        var torch = b.gameObject.AddComponent<Level2Torch>();
        torch.flameLight = light;
        torch.flame = embers;
        torch.litOnStart = lit;
        return torch;
    }

    /// <summary>
    /// Luz general: usa la luz direccional de la escena (o crea una) y una luz ambiente cálida.
    /// La direccional no cuenta para el límite de luces por objeto de URP, así que el salón no parpadea.
    /// </summary>
    private static void ConfigureSceneLighting()
    {
        var sun = Object.FindObjectsOfType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
        if (sun == null)
        {
            sun = new GameObject("LuzGeneral").AddComponent<Light>();
            sun.type = LightType.Directional;
            Undo.RegisterCreatedObjectUndo(sun.gameObject, "Luz general");
        }
        else Undo.RecordObjects(new Object[] { sun, sun.transform }, "Luz general");

        sun.color = new Color(0.8f, 0.75f, 0.85f);
        sun.intensity = 0.45f;
        sun.shadows = LightShadows.None;
        sun.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.36f, 0.3f, 0.24f);
    }

    // ================================================================ Instrumentos

    private static UnlockInstrument MakeInstrument(Transform holder, int k, List<Level2Torch> torches)
    {
        Vector3 pos = InstrumentPosition(k);
        Vector3 toWall = Side(k) * Vector3.forward;
        var face = Quaternion.LookRotation(toWall);   // el jugador lo usa desde el lado de la mesa

        switch (Instruments[k])
        {
            case Instrument.PressurePlate:
                return MakePlate(holder, pos, face);
            case Instrument.Key:
                return MakeKeyLock(holder, k, pos, face, toWall);
            case Instrument.Button:
                return MakeButton(holder, pos, face);
            case Instrument.TorchBrazier:
                return MakeTorchBrazier(holder, k, pos, face, toWall, torches);
            case Instrument.ExitLever:
                return MakeLever(holder, pos, face, 1.25f);
            default:
                return MakeLever(holder, pos, face, 1f);
        }
    }

    private static UnlockInstrument InstrumentRoot(Transform holder, string name, Vector3 pos, Quaternion rot, UnlockInstrument.Kind kind,
                                                   out Transform body)
    {
        var root = new GameObject(name);
        root.transform.SetParent(holder, false);
        root.transform.localPosition = pos;
        root.transform.localRotation = rot;
        body = new GameObject("Cuerpo").transform;
        body.SetParent(root.transform, false);

        var inst = root.AddComponent<UnlockInstrument>();
        inst.kind = kind;
        inst.body = body;
        inst.revealSound = Sound(root.transform, "SFX/sand_emerge (1).wav", false);
        return inst;
    }

    private static void Pedestal(Transform body, float height)
    {
        Prim(PrimitiveType.Cube, body, "Pedestal", new Vector3(0f, height * 0.5f, 0f), new Vector3(0.5f, height, 0.5f),
            Mat("L2_Pilar", new Color(0.78f, 0.67f, 0.48f)), true);
        Prim(PrimitiveType.Cube, body, "Remate", new Vector3(0f, height + 0.02f, 0f), new Vector3(0.6f, 0.04f, 0.6f),
            Mat("L2_Oro", new Color(0.85f, 0.65f, 0.2f)));
    }

    private static UnlockInstrument MakeLever(Transform holder, Vector3 pos, Quaternion rot, float scale)
    {
        var inst = InstrumentRoot(holder, scale > 1f ? "PalancaSalida" : "Palanca", pos, rot, UnlockInstrument.Kind.Lever, out var body);
        body.localScale = Vector3.one * scale;
        Pedestal(body, 0.9f);

        var pivot = new GameObject("Pivote").transform;
        pivot.SetParent(body, false);
        pivot.localPosition = new Vector3(0f, 0.94f, 0f);
        pivot.localRotation = Quaternion.Euler(35f, 0f, 0f);   // inclinada hacia el muro; el jugador la tira hacia sí
        Prim(PrimitiveType.Cylinder, pivot, "Mango", new Vector3(0f, 0.25f, 0f), new Vector3(0.06f, 0.25f, 0.06f), Mat("L2_Bronce", new Color(0.35f, 0.24f, 0.12f)), true);
        Prim(PrimitiveType.Sphere, pivot, "Pomo", new Vector3(0f, 0.52f, 0f), Vector3.one * 0.13f, Mat("L2_Oro", new Color(0.85f, 0.65f, 0.2f)), true);

        inst.riseHeight = 1.6f * scale;
        inst.moving = pivot;
        inst.activatedEuler = new Vector3(-80f, 0f, 0f);
        inst.activatedOffset = Vector3.zero;
        inst.activateSound = Sound(inst.transform, "SFX/lever (1).wav", false);
        inst.gameObject.AddComponent<XRSimpleInteractable>();
        return inst;
    }

    private static UnlockInstrument MakeButton(Transform holder, Vector3 pos, Quaternion rot)
    {
        var inst = InstrumentRoot(holder, "Boton", pos, rot, UnlockInstrument.Kind.Button, out var body);
        Pedestal(body, 0.95f);
        var cap = Prim(PrimitiveType.Cylinder, body, "Tapa", new Vector3(0f, 1.02f, 0f), new Vector3(0.3f, 0.04f, 0.3f),
            Mat("L2_Boton", new Color(0.75f, 0.15f, 0.1f)), true);

        inst.riseHeight = 1.15f;
        inst.moving = cap.transform;
        inst.activatedEuler = Vector3.zero;
        inst.activatedOffset = new Vector3(0f, -0.035f, 0f);
        inst.activateSound = Sound(inst.transform, "SFX/click.mp3", false);
        inst.gameObject.AddComponent<XRSimpleInteractable>();
        return inst;
    }

    private static UnlockInstrument MakePlate(Transform holder, Vector3 pos, Quaternion rot)
    {
        var inst = InstrumentRoot(holder, "PlacaPresion", pos, rot, UnlockInstrument.Kind.PressurePlate, out var body);
        Prim(PrimitiveType.Cube, body, "Placa", new Vector3(0f, 0.03f, 0f), new Vector3(1.1f, 0.1f, 1.1f), Mat("L2_Pilar", new Color(0.78f, 0.67f, 0.48f)));
        var sun = Prim(PrimitiveType.Cylinder, body, "Sol", new Vector3(0f, 0.085f, 0f), new Vector3(0.7f, 0.01f, 0.7f),
            Mat("L2_SolPlaca", new Color(1f, 0.7f, 0.2f), true));

        var trigger = inst.gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.4f, 0f);
        trigger.size = new Vector3(1.1f, 0.8f, 1.1f);

        inst.riseHeight = 0.2f;
        inst.riseSeconds = 1f;
        inst.moving = sun.transform;
        inst.activatedEuler = Vector3.zero;
        inst.activatedOffset = new Vector3(0f, -0.05f, 0f);
        inst.activateSound = Sound(inst.transform, "SFX/stone_thud.wav", false);
        return inst;
    }

    /// <summary>Pedestal con una llave que aparece al resolver, y a su lado un poste con la cerradura.</summary>
    private static UnlockInstrument MakeKeyLock(Transform holder, int k, Vector3 pos, Quaternion rot, Vector3 toWall)
    {
        var inst = InstrumentRoot(holder, "PedestalLlave", pos, rot, UnlockInstrument.Kind.ItemSocket, out var body);
        Pedestal(body, 1f);
        inst.riseHeight = 1.15f;
        string id = "llave_" + (k + 1);

        var key = new GameObject("Llave");
        key.transform.SetParent(body, false);
        key.transform.localPosition = new Vector3(0f, 1.08f, 0f);
        var col = key.AddComponent<BoxCollider>();
        col.size = new Vector3(0.1f, 0.06f, 0.32f);
        var rb = key.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        var gold = Mat("L2_LlaveOro", new Color(1f, 0.75f, 0.25f), true);
        Prim(PrimitiveType.Cylinder, key.transform, "Cana", Vector3.zero, new Vector3(0.03f, 0.12f, 0.03f), gold, false, Quaternion.Euler(90f, 0f, 0f));
        Prim(PrimitiveType.Sphere, key.transform, "Ojo", new Vector3(0f, 0f, -0.14f), new Vector3(0.1f, 0.03f, 0.1f), gold);
        Prim(PrimitiveType.Cube, key.transform, "Dientes", new Vector3(0f, -0.025f, 0.09f), new Vector3(0.02f, 0.05f, 0.06f), gold);
        key.AddComponent<UnlockItem>().id = id;
        var grab = key.AddComponent<XRGrabInteractable>();
        grab.throwOnDetach = false;
        key.AddComponent<ReturnHome>();

        // Poste con la cerradura, al otro lado del reto (siempre visible: es parte del comedor)
        var post = new GameObject("PosteCerradura").transform;
        post.SetParent(holder, false);
        post.localPosition = new Vector3(ColumnX(k) - 3f, 0f, pos.z);
        post.localRotation = rot;   // +Z local hacia el muro: la cerradura mira a la mesa (−Z)
        Prim(PrimitiveType.Cube, post, "Poste", new Vector3(0f, 0.65f, 0.15f), new Vector3(0.45f, 1.3f, 0.3f), Mat("L2_Pilar", new Color(0.78f, 0.67f, 0.48f)), true);
        Prim(PrimitiveType.Cube, post, "Placa", new Vector3(0f, 1.05f, -0.01f), new Vector3(0.3f, 0.36f, 0.03f), Mat("L2_Bronce", new Color(0.35f, 0.24f, 0.12f)));
        Prim(PrimitiveType.Cylinder, post, "Ojo", new Vector3(0f, 1.07f, -0.03f), new Vector3(0.06f, 0.006f, 0.06f),
            Mat("L2_Conector", new Color(0.1f, 0.09f, 0.08f)), false, Quaternion.Euler(90f, 0f, 0f));
        Level2Visuals.CreateLabel(post, "Texto", "Cerradura", new Vector3(0f, 1.38f, -0.01f), new Vector2(0.45f, 0.1f), 0.5f);

        var sockGo = new GameObject("Socket");
        sockGo.transform.SetParent(post, false);
        sockGo.transform.localPosition = new Vector3(0f, 1.07f, -0.17f);
        var sc = sockGo.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.18f;
        var sock = sockGo.AddComponent<Level2Socket>();
        sock.accept = Level2Socket.Accept.UnlockItem;
        sock.requiredItemId = id;

        inst.socket = sock;
        inst.activateSound = Sound(inst.transform, "SFX/keys_falling.wav", false);
        return inst;
    }

    /// <summary>Pedestal con una antorcha apagada y, al otro lado del reto, un brasero apagado: al poner la antorcha en el brasero, se enciende.</summary>
    private static UnlockInstrument MakeTorchBrazier(Transform holder, int k, Vector3 pos, Quaternion rot, Vector3 toWall, List<Level2Torch> torches)
    {
        var inst = InstrumentRoot(holder, "PedestalAntorcha", pos, rot, UnlockInstrument.Kind.ItemSocket, out var body);
        Pedestal(body, 1f);
        inst.riseHeight = 1.15f;
        string id = "antorcha_" + (k + 1);

        var item = new GameObject("AntorchaDeMano");
        item.transform.SetParent(body, false);
        item.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        item.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);   // acostada sobre el pedestal
        var col = item.AddComponent<BoxCollider>();
        col.size = new Vector3(0.1f, 0.6f, 0.1f);
        var rb = item.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        Prim(PrimitiveType.Cylinder, item.transform, "Palo", Vector3.zero, new Vector3(0.05f, 0.27f, 0.05f), Mat("L2_Madera", new Color(0.32f, 0.2f, 0.11f)));
        Prim(PrimitiveType.Cylinder, item.transform, "Trapo", new Vector3(0f, 0.27f, 0f), new Vector3(0.09f, 0.07f, 0.09f), Mat("L2_Trapo", new Color(0.25f, 0.2f, 0.15f)));
        item.AddComponent<UnlockItem>().id = id;
        var grab = item.AddComponent<XRGrabInteractable>();
        grab.throwOnDetach = false;
        item.AddComponent<ReturnHome>();

        var b = new GameObject("BraseroApagado").transform;
        b.SetParent(holder, false);
        b.localPosition = new Vector3(ColumnX(k) - 3f, 0f, pos.z);
        BrazierBody(b);
        var fire = BrazierFire(b, false);
        fire.igniteSound = Sound(b, "SFX/torch_ignite.wav", false);
        torches.Insert(0, fire);   // es lo primero que se prende

        var sockGo = new GameObject("Socket");
        sockGo.transform.SetParent(b, false);
        sockGo.transform.localPosition = new Vector3(0f, 1.42f, 0f);
        var sc = sockGo.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.25f;
        var sock = sockGo.AddComponent<Level2Socket>();
        sock.accept = Level2Socket.Accept.UnlockItem;
        sock.requiredItemId = id;
        Level2Visuals.CreateLabel(b, "Texto", "Brasero", new Vector3(0f, 1.9f, 0f), new Vector2(0.6f, 0.12f), 0.6f)
            .transform.localRotation = Quaternion.LookRotation(toWall);   // se lee desde el lado de la mesa

        inst.socket = sock;
        return inst;
    }

    private static GameObject TriggerBox(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var col = go.AddComponent<BoxCollider>();
        col.size = size;
        col.isTrigger = true;
        return go;
    }

    private static AudioSource Sound(Transform parent, string sfx, bool loop)
    {
        var src = parent.gameObject.AddComponent<AudioSource>();   // uno por sonido: DoorOpener usa dos
        src.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/EgyptianLogic/Audio/" + sfx);
        if (src.clip == null) Debug.LogWarning("[Level2Builder] No encontré el sonido Audio/" + sfx);
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 1f;
        return src;
    }

    // ================================================================ Ambiente

    private static void Rug(Transform parent, Vector3 pos, Vector2 size)
    {
        var rug = Prim(PrimitiveType.Cube, parent, "Alfombra", pos, new Vector3(size.x, 0.01f, size.y), Mat("L2_Alfombra", new Color(0.55f, 0.32f, 0.18f)));
        Prim(PrimitiveType.Cube, rug.transform.parent, "Borde", pos + new Vector3(0f, 0.001f, 0f), new Vector3(size.x * 0.8f, 0.01f, size.y * 0.8f),
            Mat("L2_AlfombraCentro", new Color(0.78f, 0.62f, 0.38f)));
    }
    private static void BanquetTable(Transform parent, float length)
    {
        var table = new GameObject("MesaBanquete").transform;
        table.SetParent(parent, false);
        // Madera rojiza con incrustaciones de nácar (Level2Decor) y patas curvas que se abren hacia afuera
        InlaidTabletop(table, length, 1.5f);
        foreach (float lx in new[] { -length * 0.5f + 0.25f, 0f, length * 0.5f - 0.25f })
            foreach (float lz in new[] { -0.6f, 0.6f })
                CurvedLeg(table, new Vector3(lx, 0f, lz), lz > 0f ? 0f : 180f);

        // Sillas a ambos lados, mirando a la mesa
        for (float x = -length * 0.5f + 0.7f; x <= length * 0.5f - 0.7f; x += 1.3f)
        {
            Chair(table, new Vector3(x, 0f, 1.15f), 0f);
            Chair(table, new Vector3(x, 0f, -1.15f), 180f);
        }

        // Objetos del banquete: se pueden tomar, mover y tirar (solo para explorar)
        var rnd = new System.Random(7);
        int i = 0;
        for (float x = -length * 0.5f + 0.4f; x <= length * 0.5f - 0.4f; x += 0.55f, i++)
        {
            float side = i % 2 == 0 ? 0.42f : -0.42f;
            var pos = new Vector3(x, 0.78f, side);
            switch (i % 6)
            {
                case 0: Plate(table, pos); Fruit(table, pos + new Vector3(0f, 0.04f, 0f), rnd); break;
                case 1: Goblet(table, pos); break;
                case 2: Jug(table, pos); break;
                case 3: Bread(table, pos); break;
                case 4: Goblet(table, pos); Plate(table, pos + new Vector3(0.2f, 0f, -side * 0.3f)); break;
                default: Candle(table, pos, i % 12 == 5); break;
            }

            if (i % 3 == 1) // fuentes con fruta sobre el camino central
                FruitBowl(table, new Vector3(x, 0.79f, 0f), rnd);
        }
    }
    private static void Chair(Transform parent, Vector3 pos, float yaw)
    {
        var c = new GameObject("Silla").transform;
        c.SetParent(parent, false);
        c.localPosition = pos;
        c.localRotation = Quaternion.Euler(0f, yaw, 0f);
        // Silla dorada de respaldo ovalado (Level2Decor): una sola malla con oro y tela
        c.gameObject.AddComponent<MeshFilter>().sharedMesh = ChairMesh();
        c.gameObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { GoldMat(), Mat("L2_Tapiz", new Color(0.93f, 0.9f, 0.82f)) };
        var seat = c.gameObject.AddComponent<BoxCollider>();
        seat.center = new Vector3(0f, 0.26f, 0f);
        seat.size = new Vector3(0.46f, 0.52f, 0.46f);
        var back = c.gameObject.AddComponent<BoxCollider>();
        back.center = new Vector3(0f, 0.88f, 0.28f);
        back.size = new Vector3(0.46f, 0.56f, 0.08f);
    }
    /// <summary>Objeto suelto que se puede agarrar: Rigidbody + XRGrabInteractable + sonido al chocar.</summary>
    private static Transform Grabbable(Transform parent, string name, Vector3 pos, float mass)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        go.AddComponent<XRGrabInteractable>();
        go.AddComponent<AudioSource>().spatialBlend = 1f;
        var impact = go.AddComponent<ImpactSound>();
        var so = new SerializedObject(impact);
        var clips = so.FindProperty("clips");
        clips.arraySize = 1;
        clips.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/EgyptianLogic/Audio/SFX/small_stone_thud.wav");
        so.ApplyModifiedPropertiesWithoutUndo();
        return go.transform;
    }
    private static void Plate(Transform parent, Vector3 pos)
    {
        var p = Grabbable(parent, "Plato", pos + new Vector3(0f, 0.012f, 0f), 0.4f);
        Prim(PrimitiveType.Cylinder, p, "Cuerpo", Vector3.zero, new Vector3(0.26f, 0.008f, 0.26f), Mat("L2_Ceramica", new Color(0.8f, 0.68f, 0.5f)), true);
    }
    private static void Goblet(Transform parent, Vector3 pos)
    {
        var g = Grabbable(parent, "Copa", pos, 0.3f);
        var gold = Mat("L2_Oro", new Color(0.85f, 0.65f, 0.2f));
        Prim(PrimitiveType.Cylinder, g, "Base", new Vector3(0f, 0.008f, 0f), new Vector3(0.07f, 0.008f, 0.07f), gold, true);
        Prim(PrimitiveType.Cylinder, g, "Tallo", new Vector3(0f, 0.06f, 0f), new Vector3(0.02f, 0.05f, 0.02f), gold);
        Prim(PrimitiveType.Cylinder, g, "Copa", new Vector3(0f, 0.15f, 0f), new Vector3(0.08f, 0.045f, 0.08f), gold, true);
    }
    private static void Jug(Transform parent, Vector3 pos)
    {
        var j = Grabbable(parent, "Cantaro", pos, 0.8f);
        var clay = Mat("L2_Barro", new Color(0.62f, 0.33f, 0.18f));
        Prim(PrimitiveType.Sphere, j, "Cuerpo", new Vector3(0f, 0.11f, 0f), new Vector3(0.17f, 0.22f, 0.17f), clay, true);
        Prim(PrimitiveType.Cylinder, j, "Cuello", new Vector3(0f, 0.25f, 0f), new Vector3(0.07f, 0.04f, 0.07f), clay);
    }
    private static void Bread(Transform parent, Vector3 pos)
    {
        var b = Grabbable(parent, "Pan", pos + new Vector3(0f, 0.04f, 0f), 0.2f);
        Prim(PrimitiveType.Capsule, b, "Cuerpo", Vector3.zero, new Vector3(0.08f, 0.09f, 0.08f), Mat("L2_Pan", new Color(0.72f, 0.48f, 0.22f)), true,
            Quaternion.Euler(0f, 0f, 90f));
    }
    private static void Fruit(Transform parent, Vector3 pos, System.Random rnd)
    {
        var colors = new[] { new Color(0.75f, 0.1f, 0.12f), new Color(0.45f, 0.15f, 0.45f), new Color(0.9f, 0.75f, 0.2f), new Color(0.4f, 0.6f, 0.15f) };
        string[] names = { "Granada", "Uva", "Datil", "Higo" };
        int k = rnd.Next(colors.Length);
        var f = Grabbable(parent, names[k], pos + new Vector3(0f, 0.04f, 0f), 0.15f);
        Prim(PrimitiveType.Sphere, f, "Cuerpo", Vector3.zero, Vector3.one * 0.075f, Mat("L2_Fruta" + k, colors[k]), true);
    }
    private static void FruitBowl(Transform parent, Vector3 pos, System.Random rnd)
    {
        var bowl = Grabbable(parent, "Fuente", pos + new Vector3(0f, 0.03f, 0f), 0.8f);
        Prim(PrimitiveType.Sphere, bowl, "Cuenco", Vector3.zero, new Vector3(0.3f, 0.07f, 0.3f), Mat("L2_Bronce", new Color(0.35f, 0.24f, 0.12f)), true);
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            Fruit(parent, pos + new Vector3(Mathf.Cos(a) * 0.06f, 0.03f, Mathf.Sin(a) * 0.06f), rnd);
        }
    }
    private static void Candle(Transform parent, Vector3 pos, bool withLight)
    {
        var c = new GameObject("Vela").transform;
        c.SetParent(parent, false);
        c.localPosition = pos;
        Prim(PrimitiveType.Cylinder, c, "Cera", new Vector3(0f, 0.08f, 0f), new Vector3(0.045f, 0.08f, 0.045f), Mat("L2_Cera", new Color(0.95f, 0.9f, 0.78f)), true);
        Prim(PrimitiveType.Sphere, c, "Llama", new Vector3(0f, 0.185f, 0f), new Vector3(0.025f, 0.045f, 0.025f), Mat("L2_Llama", new Color(1f, 0.8f, 0.3f), true));
        if (!withLight) return;

        var lightGo = new GameObject("Luz");
        lightGo.transform.SetParent(c, false);
        lightGo.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 2f;
        light.intensity = 0.8f;
        light.color = new Color(1f, 0.75f, 0.4f);
        var candleFlicker = lightGo.AddComponent<TorchFlicker>();
        candleFlicker.minIntensity = 0.5f;
        candleFlicker.maxIntensity = 1f;
    }
    private static void Amphora(Transform parent, Vector3 pos)
    {
        var a = Grabbable(parent, "Anfora", pos, 4f);
        var clay = Mat("L2_Barro", new Color(0.62f, 0.33f, 0.18f));
        Prim(PrimitiveType.Sphere, a, "Cuerpo", new Vector3(0f, 0.32f, 0f), new Vector3(0.36f, 0.5f, 0.36f), clay, true);
        Prim(PrimitiveType.Cylinder, a, "Cuello", new Vector3(0f, 0.63f, 0f), new Vector3(0.13f, 0.07f, 0.13f), clay);
        Prim(PrimitiveType.Cylinder, a, "Boca", new Vector3(0f, 0.71f, 0f), new Vector3(0.2f, 0.015f, 0.2f), clay);
        Prim(PrimitiveType.Cylinder, a, "Pie", new Vector3(0f, 0.06f, 0f), new Vector3(0.16f, 0.05f, 0.16f), clay, true);
    }
    private static void Pot(Transform parent, Vector3 pos)
    {
        var p = Grabbable(parent, "Vasija", pos, 2f);
        Prim(PrimitiveType.Sphere, p, "Cuerpo", new Vector3(0f, 0.15f, 0f), new Vector3(0.32f, 0.3f, 0.32f), Mat("L2_BarroOscuro", new Color(0.45f, 0.25f, 0.15f)), true);
        Prim(PrimitiveType.Cylinder, p, "Borde", new Vector3(0f, 0.3f, 0f), new Vector3(0.2f, 0.02f, 0.2f), Mat("L2_BarroOscuro", new Color(0.45f, 0.25f, 0.15f)));
    }
    private static void Basket(Transform parent, Vector3 pos)
    {
        var b = Grabbable(parent, "Cesta", pos, 1f);
        Prim(PrimitiveType.Cylinder, b, "Cuerpo", new Vector3(0f, 0.12f, 0f), new Vector3(0.42f, 0.12f, 0.42f), Mat("L2_Mimbre", new Color(0.7f, 0.55f, 0.3f)), true);
    }
    private static void ConfigureTracker(PerformanceTracker tracker, HintManager hints)
    {
        var so = new SerializedObject(tracker);
        so.FindProperty("hintManager").objectReferenceValue = hints;
        so.FindProperty("level").intValue = 2;

        var stages = so.FindProperty("stages");
        stages.arraySize = ChallengeCount;
        for (int k = 0; k < ChallengeCount; k++)
        {
            var st = stages.GetArrayElementAtIndex(k);
            st.FindPropertyRelative("name").stringValue = ChallengeIds[k];
            st.FindPropertyRelative("startOnZoneEnter").boolValue = true;
            st.FindPropertyRelative("hasSubmit").boolValue = HasSubmit[k];
            var byVersion = st.FindPropertyRelative("byVersion");
            byVersion.arraySize = 3;
            for (int v = 0; v < 3; v++)
            {
                var ideal = byVersion.GetArrayElementAtIndex(v);
                ideal.FindPropertyRelative("tIdeal").floatValue = Ideals[k][v].t;
                ideal.FindPropertyRelative("iIdeal").intValue = Ideals[k][v].i;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void ConfigureHints(HintManager hints, HintScrollSpawner spawner, PerformanceTracker tracker, PuzzleVersionManager[] managers)
    {
        var so = new SerializedObject(hints);
        so.FindProperty("spawner").objectReferenceValue = spawner;
        so.FindProperty("tracker").objectReferenceValue = tracker;

        // La misma acción de pedir pista del Nivel 1
        var action = AssetDatabase.LoadAllAssetsAtPath("Assets/EgyptianLogic/HintInput.inputactions")
            .OfType<UnityEngine.InputSystem.InputActionReference>().FirstOrDefault();
        if (action != null) so.FindProperty("hintAction").objectReferenceValue = action;
        else Debug.LogWarning("[Level2Builder] No encontré HintInput.inputactions: asigna Hint Action en el HintManager.");

        var stages = so.FindProperty("stages");
        stages.arraySize = ChallengeCount;
        for (int k = 0; k < ChallengeCount; k++)
        {
            var st = stages.GetArrayElementAtIndex(k);
            st.FindPropertyRelative("name").stringValue = ChallengeIds[k];
            st.FindPropertyRelative("versionSource").objectReferenceValue = managers[k];
            st.FindPropertyRelative("skipFirstHint").boolValue = false;
            var byVersion = st.FindPropertyRelative("byVersion");
            byVersion.arraySize = 3;
            for (int v = 0; v < 3; v++)
            {
                var texts = byVersion.GetArrayElementAtIndex(v).FindPropertyRelative("hints");
                texts.arraySize = 4;
                for (int h = 0; h < 4; h++)
                    texts.GetArrayElementAtIndex(h).stringValue = Level2Hints.All[k][v][h];
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void ConfigureSpawner(HintScrollSpawner spawner, PerformanceTracker tracker)
    {
        var so = new SerializedObject(spawner);
        so.FindProperty("tracker").objectReferenceValue = tracker;

        var scroll = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EgyptianLogic/Prefabs/Scroll 1 1 1.prefab");
        if (scroll != null) so.FindProperty("scrollPrefab").objectReferenceValue = scroll;
        else Debug.LogWarning("[Level2Builder] No encontré el prefab del pergamino: asigna Scroll Prefab en el HintScrollSpawner.");

        if (Camera.main != null) so.FindProperty("head").objectReferenceValue = Camera.main.transform;
        else Debug.LogWarning("[Level2Builder] No hay Main Camera en la escena: asigna Head en el HintScrollSpawner (la cámara del XR Rig).");

        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void ConfigureEngine(AdaptationEngine engine, PerformanceTracker tracker, PuzzleVersionManager[] managers)
    {
        // Reglas por defecto del Nivel 1 (mismo método que el clic derecho "Cargar reglas por defecto")
        typeof(AdaptationEngine).GetMethod("LoadDefaults", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.Invoke(engine, null);

        var so = new SerializedObject(engine);
        so.FindProperty("tracker").objectReferenceValue = tracker;
        so.FindProperty("thetaStart").enumValueIndex = (int)AdaptationEngine.ThetaStart.CarryOver;
        so.FindProperty("useCustomSuccessors").boolValue = false; // lineal: al resolver 2.k se ajusta 2.k+1

        var initial = so.FindProperty("initialStages");
        initial.arraySize = 1;
        initial.GetArrayElementAtIndex(0).intValue = 0; // la versión de 2.1 sale del θ que trae el jugador

        // Abierto directo (sin pasar por el Nivel 1): sigue la última partida guardada; si no hay ninguna, empieza en V1
        so.FindProperty("loadLastSaveIfEmpty").boolValue = true;
        so.FindProperty("versionWithoutData").intValue = 0;

        var bindings = so.FindProperty("bindings");
        bindings.arraySize = ChallengeCount;
        for (int k = 0; k < ChallengeCount; k++)
        {
            var b = bindings.GetArrayElementAtIndex(k);
            b.FindPropertyRelative("name").stringValue = ChallengeIds[k];
            b.FindPropertyRelative("versionTarget").objectReferenceValue = managers[k];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================================================================ Definiciones de cada reto

    private static float Build21(Transform parent)
    {
        var defs = new[]
        {
            new TableDef
            {
                title = "2.1.1 El Detective de Verdades (Fácil)", inputs = new[] { "A", "B" },
                spec = "S = (A | B) & !(A & B)",
                drawing = "S = AND(OR(A, B), NAND(A, B))",
                description = "Lee el circuito y completa la tabla de verdad."
            },
            new TableDef
            {
                title = "2.1.2 El Detective de Verdades (Medio)", inputs = new[] { "A", "B", "C" },
                spec = "S = (!A & B) | C",
                drawing = "S = OR(AND(NOT(A), B), C)",
                description = "Lee el circuito y resuélvelo etapa por etapa."
            },
            new TableDef
            {
                title = "2.1.3 El Detective de Verdades (Difícil)", inputs = new[] { "A", "B", "C", "D" },
                spec = "S = (A ^ B) & (C | !D)\ndc S = A & B",
                drawing = "S = AND(XOR(A, B), OR(C, NOT(D)))",
                description = "Lee el circuito. A=1 y B=1 nunca ocurre a la vez: esas filas son X."
            },
        };
        return BuildChallenge(parent, "Reto 2.1 - El Detective de Verdades", defs.Select(d => (System.Func<Transform, float>)(p => BuildTruthTable(p, d))).ToArray());
    }

    private static float Build22(Transform parent)
    {
        var nand = new[] { LogicGateType.NAND };
        var defs = new[]
        {
            new CircuitDef
            {
                title = "2.2.1 Solo Tengo Esto (Fácil)", inputs = new[] { "A", "B" }, outputs = new[] { "S" },
                spec = "S = A & B", allowed = nand, slots = 3, ports = 2, pins = 1, inputFanout = 2, pinFanout = 2,
                gates = Gates((LogicGateType.NAND, 3)),
                drawing = "S = AND(A, B)",
                instructions = "Construye este circuito usando SOLO compuertas NAND.\nRecuerda: NOT(A) = NAND(A, A)   y   AND = NOT(NAND)."
            },
            new CircuitDef
            {
                title = "2.2.2 Solo Tengo Esto (Medio)", inputs = new[] { "A", "B", "C" }, outputs = new[] { "S" },
                spec = "S = (A & B) | C", allowed = nand, slots = 5, ports = 2, pins = 1, inputFanout = 2, pinFanout = 2,
                gates = Gates((LogicGateType.NAND, 5)),
                drawing = "S = OR(AND(A, B), C)",
                instructions = "Construye este circuito usando SOLO NAND.\nDe Morgan: A OR B = NAND( NAND(A,A), NAND(B,B) )."
            },
            new CircuitDef
            {
                title = "2.2.3 Solo Tengo Esto (Difícil)", inputs = new[] { "A", "B" }, outputs = new[] { "S", "C" },
                spec = "S = A ^ B\nC = A & B", allowed = nand, slots = 6, ports = 2, pins = 1, inputFanout = 3, pinFanout = 3,
                gates = Gates((LogicGateType.NAND, 6)),
                drawing = "S = XOR(A, B); C = AND(A, B)",
                instructions = "Construye este medio sumador usando SOLO NAND:\nla suma S y el acarreo C."
            },
        };
        return BuildChallenge(parent, "Reto 2.2 - Solo Tengo Esto", defs.Select(d => (System.Func<Transform, float>)(p => BuildCircuit(p, d))).ToArray());
    }

    private static float Build23(Transform parent)
    {
        var defs = new[]
        {
            new KMapDef
            {
                title = "2.3.1 El Ahorro de Compuertas (Fácil)", vars = new[] { "A", "B" },
                spec = "F = A&B | A&!B", given = "Simplifica este circuito con el mapa.",
                drawing = "F = OR(AND(A, B), AND(A, B'))"
            },
            new KMapDef
            {
                title = "2.3.2 El Ahorro de Compuertas (Medio)", vars = new[] { "A", "B", "C" },
                spec = "F = !A&!B&!C | !A&!B&C | A&!B&!C | A&!B&C | A&B&!C",
                given = "Simplifica este circuito con el mapa.",
                drawing = "F = OR(AND(A', B', C'), AND(A', B', C), AND(A, B', C'), AND(A, B', C), AND(A, B, C'))"
            },
            new KMapDef
            {
                title = "2.3.3 El Ahorro de Compuertas (Difícil)", vars = new[] { "A", "B", "C", "D" },
                spec = "F = !A&!B&!C&D | !A&!B&C&D | !A&B&C&D | A&!B&C&D | A&B&C&D\n" +
                       "dc F = !A&!B&!C&!D | !A&!B&C&!D | !A&B&!C&D",
                given = "Simplifica este circuito. Las celdas 0, 2 y 5 son X:\naprovéchalas para agrupar más.",
                drawing = "F = OR(AND(A', B', C', D), AND(A', B', C, D), AND(A', B, C, D), AND(A, B', C, D), AND(A, B, C, D))"
            },
        };
        return BuildChallenge(parent, "Reto 2.3 - El Ahorro de Compuertas", defs.Select(d => (System.Func<Transform, float>)(p => BuildKMap(p, d))).ToArray());
    }

    private static float Build24(Transform parent)
    {
        var defs = new[]
        {
            new CircuitDef
            {
                title = "2.4.1 El Selector (Fácil)", inputs = new[] { "I0", "I1", "S" }, outputs = new[] { "Y" },
                spec = MuxSpec(new[] { "S" }, 2), slots = 4, ports = 2, pins = 1, inputFanout = 2, pinFanout = 2,
                gates = Gates((LogicGateType.NOT, 1), (LogicGateType.AND, 2), (LogicGateType.OR, 1), (LogicGateType.XOR, 1)),
                liveTest = new[] { "S" },
                drawing = "[MUX] MUX 2:1 | I0, I1 | S | Y",
                instructions = "Arma un MUX 2:1:  Y = S'·I0 + S·I1\nPruébalo con S = 0 y S = 1."
            },
            new CircuitDef
            {
                title = "2.4.2 El Selector (Medio)", inputs = new[] { "I0", "I1", "I2", "I3", "S1", "S0" }, outputs = new[] { "Y" },
                spec = MuxSpec(new[] { "S1", "S0" }, 4), slots = 3, ports = 3, pins = 1, inputFanout = 2, pinFanout = 1,
                gates = Gates((LogicGateType.MUX2, 3), (LogicGateType.OR, 1)),
                liveTest = new[] { "S1", "S0" },
                drawing = "[MUX] MUX 4:1 | I0, I1, I2, I3 | S1, S0 | Y",
                instructions = "Arma un MUX 4:1 con módulos MUX 2:1 (I0, I1, S).\nS1S0 = 00→I0, 01→I1, 10→I2, 11→I3. Prueba las 4."
            },
            new CircuitDef
            {
                title = "2.4.3 El Selector (Difícil)",
                inputs = new[] { "I0", "I1", "I2", "I3", "I4", "I5", "I6", "I7", "S2", "S1", "S0" }, outputs = new[] { "Y" },
                spec = MuxSpec(new[] { "S2", "S1", "S0" }, 8), slots = 3, ports = 6, pins = 1, inputFanout = 2, pinFanout = 1,
                gates = Gates((LogicGateType.MUX4, 2), (LogicGateType.MUX2, 2)),
                liveTest = new[] { "S2", "S1", "S0" },
                drawing = "[MUX] MUX 8:1 | I0, I1, I2, I3, I4, I5, I6, I7 | S2, S1, S0 | Y",
                instructions = "Arma un MUX 8:1 en cascada: dos MUX 4:1 (I0-I3, S0, S1) y un MUX 2:1 final con S2.\nPrueba las 8 combinaciones."
            },
        };
        return BuildChallenge(parent, "Reto 2.4 - El Selector", defs.Select(d => (System.Func<Transform, float>)(p => BuildCircuit(p, d))).ToArray());
    }

    private static float Build25(Transform parent)
    {
        var defs = new[]
        {
            new CircuitDef
            {
                title = "2.5.1 El Repartidor (Fácil)", inputs = new[] { "A1", "A0" }, outputs = Names("Y", 4),
                spec = DecoderSpec(new[] { "A1", "A0" }, null), slots = 6, ports = 2, pins = 1, inputFanout = 3, pinFanout = 3,
                gates = Gates((LogicGateType.NOT, 2), (LogicGateType.AND, 4), (LogicGateType.OR, 1)),
                liveTest = new[] { "A1", "A0" },
                drawing = "[BOX] DECODIFICADOR 2 a 4 | A1, A0 | | Y0, Y1, Y2, Y3",
                instructions = "Decodificador 2→4: para cada dirección A1A0 debe encenderse SOLO la puerta Y correspondiente.\nPrueba las 4 direcciones."
            },
            new CircuitDef
            {
                title = "2.5.2 El Repartidor (Medio)", inputs = new[] { "A2", "A1", "A0" }, outputs = Names("Y", 8),
                spec = DecoderSpec(new[] { "A2", "A1", "A0" }, null), slots = 11, ports = 3, pins = 1, inputFanout = 5, pinFanout = 4,
                gates = Gates((LogicGateType.NOT, 3), (LogicGateType.AND, 8)),
                liveTest = new[] { "A2", "A1", "A0" },
                drawing = "[BOX] DECODIFICADOR 3 a 8 | A2, A1, A0 | | Y0, Y1, Y2, Y3, Y4, Y5, Y6, Y7",
                instructions = "Decodificador 3→8: cada dirección A2A1A0 abre UNA sola bóveda (Y0-Y7).\nLas AND aceptan 3 cables. Prueba las 8."
            },
            new CircuitDef
            {
                title = "2.5.3 El Repartidor (Difícil)", inputs = new[] { "E", "A1", "A0" }, outputs = Names("Y", 4),
                spec = DecoderSpec(new[] { "A1", "A0" }, "E"), slots = 6, ports = 3, pins = 1, inputFanout = 4, pinFanout = 3,
                gates = Gates((LogicGateType.NOT, 2), (LogicGateType.AND, 4)),
                liveTest = new[] { "E", "A1", "A0" },
                drawing = "[BOX] DECODIFICADOR 2 a 4 | A1, A0 | E | Y0, Y1, Y2, Y3",
                instructions = "Decodificador 2→4 con Enable: si E = 0 ninguna salida se enciende; si E = 1 funciona normal.\nPrueba las 8 combinaciones de E, A1, A0."
            },
        };
        return BuildChallenge(parent, "Reto 2.5 - El Repartidor", defs.Select(d => (System.Func<Transform, float>)(p => BuildCircuit(p, d))).ToArray());
    }

    // ================================================================ Especificaciones generadas

    private static string MuxSpec(string[] selects, int count)
    {
        var terms = new List<string>();
        int k = selects.Length;
        for (int i = 0; i < count; i++)
        {
            var lits = new List<string>();
            for (int j = 0; j < k; j++)
            {
                bool bit = ((i >> (k - 1 - j)) & 1) == 1;
                lits.Add(bit ? selects[j] : "!" + selects[j]);
            }
            lits.Add("I" + i);
            terms.Add(string.Join(" & ", lits));
        }
        return "Y = " + string.Join(" | ", terms);
    }

    private static string DecoderSpec(string[] address, string enable)
    {
        var sb = new StringBuilder();
        int k = address.Length;
        for (int i = 0; i < (1 << k); i++)
        {
            var lits = new List<string>();
            if (enable != null) lits.Add(enable);
            for (int j = 0; j < k; j++)
            {
                bool bit = ((i >> (k - 1 - j)) & 1) == 1;
                lits.Add(bit ? address[j] : "!" + address[j]);
            }
            sb.Append("Y").Append(i).Append(" = ").Append(string.Join(" & ", lits)).Append('\n');
        }
        return sb.ToString();
    }

    private static string AdderSpec(bool subtract)
    {
        var sb = new StringBuilder();
        if (subtract)
        {
            sb.AppendLine("# M = 1 resta: se invierte B y se suma 1 con el acarreo de entrada");
            for (int i = 0; i < 4; i++) sb.AppendLine($"X{i} = B{i} ^ M");
            sb.AppendLine("c0 = M");
        }
        else
        {
            for (int i = 0; i < 4; i++) sb.AppendLine($"X{i} = B{i}");
            sb.AppendLine("c0 = 0");
        }

        for (int i = 0; i < 4; i++)
        {
            sb.AppendLine($"S{i} = A{i} ^ X{i} ^ c{i}");
            sb.AppendLine($"c{i + 1} = A{i} & X{i} | c{i} & (A{i} ^ X{i})");
        }
        sb.AppendLine("C4 = c4");

        if (subtract)
        {
            sb.AppendLine("EQ = !(S0 | S1 | S2 | S3)");
            sb.AppendLine("LT = !C4");
            sb.AppendLine("GT = C4 & !EQ");
            sb.AppendLine("# El comparador solo se revisa en modo resta");
            sb.AppendLine("dc GT = !M");
            sb.AppendLine("dc EQ = !M");
            sb.AppendLine("dc LT = !M");
        }
        return sb.ToString();
    }

    private static string[] Names(string prefix, int count) => Enumerable.Range(0, count).Select(i => prefix + i).ToArray();

    private static List<LogicGateType> Gates(params (LogicGateType type, int count)[] items)
    {
        var list = new List<LogicGateType>();
        foreach (var (type, count) in items)
            for (int i = 0; i < count; i++) list.Add(type);
        return list;
    }

    // ================================================================ Tipos de definición

    private class CircuitDef
    {
        public string title, spec, instructions;
        /// <summary>Dibujo de explicación (ver Level2Schematic). Vacío = sin dibujo.</summary>
        public string drawing;
        public string[] inputs, outputs;
        public string[] constants = new string[0];
        public string[] liveTest = new string[0];
        public LogicGateType[] allowed = new LogicGateType[0];
        public List<LogicGateType> gates;
        public int slots, ports, pins, inputFanout, pinFanout;
        public Dictionary<string, int> fanoutOverrides = new Dictionary<string, int>();
    }

    private class TableDef
    {
        public string title, spec, description;
        /// <summary>Dibujo de explicación (ver Level2Schematic): "S = AND(OR(A, B), NOT(C))". Vacío = sin dibujo.</summary>
        public string drawing;
        public string[] inputs;
    }

    private class KMapDef
    {
        public string title, spec, given;
        public string[] vars;
        /// <summary>Dibujo de explicación (ver Level2Schematic). Vacío = sin dibujo.</summary>
        public string drawing;
    }

    // ================================================================ Reto (3 versiones)

    private static float BuildChallenge(Transform parent, string name, System.Func<Transform, float>[] versions)
    {
        var challenge = new GameObject(name);
        challenge.transform.SetParent(parent, false);

        var roots = new GameObject[versions.Length];
        float width = 0f;
        for (int i = 0; i < versions.Length; i++)
        {
            var v = new GameObject("V" + (i + 1));
            v.transform.SetParent(challenge.transform, false);
            width = Mathf.Max(width, versions[i](v.transform));
            v.SetActive(i == 0);
            roots[i] = v;
        }

        var manager = challenge.AddComponent<PuzzleVersionManager>();
        var so = new SerializedObject(manager);
        var arr = so.FindProperty("puzzleRoots");
        arr.arraySize = roots.Length;
        for (int i = 0; i < roots.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = roots[i];
        so.FindProperty("randomizeOnStart").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        return width;
    }

    // ================================================================ Constructores por tipo

    private const float TableTop = 0.85f;
    private const float PanelBottom = 0.95f;
    private const float ContentBottom = 1.02f;

    private static float BuildCircuit(Transform root, CircuitDef d)
    {
        const float rowH = 0.11f, plugStep = 0.055f, portStep = 0.065f;

        int maxInFanout = Mathf.Max(d.inputFanout, d.fanoutOverrides.Count > 0 ? d.fanoutOverrides.Values.Max() : 0);
        float inputsW = 0.3f + maxInFanout * plugStep;
        float slotW = 0.4f + d.pinFanout * plugStep;
        float slotH = Mathf.Max(d.ports * portStep, d.pins * 0.1f) + 0.12f;
        int cols = Mathf.Min(d.slots, Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(d.slots * 1.6f))));
        int rows = Mathf.CeilToInt(d.slots / (float)cols);
        float outputsW = 0.34f;
        float panelW = inputsW + cols * slotW + outputsW + 0.1f;

        int allInputs = d.inputs.Length + d.constants.Length;
        float contentH = Mathf.Max(allInputs * rowH, rows * slotH, d.outputs.Length * rowH) + 0.04f;
        float top = ContentBottom + contentH;
        float left = -panelW * 0.5f;

        var puzzle = root.gameObject.AddComponent<CircuitSpecPuzzle>();
        puzzle.puzzleId = d.title.Split(' ')[0];
        puzzle.spec = d.spec;
        puzzle.allowedGates = d.allowed;
        puzzle.liveTestInputs = d.liveTest;
        puzzle.instructions = d.instructions;

        Backdrop(root, panelW, PanelBottom, top + 0.5f);
        Texts(root, puzzle, d.title, panelW, top, out puzzle.instructionsText, out puzzle.progressText);

        var boardGo = new GameObject("Protoboard");
        boardGo.transform.SetParent(root, false);
        var board = boardGo.AddComponent<Breadboard>();
        puzzle.board = board;

        // ---- Entradas
        var inputs = new List<CircuitInput>();
        var allNames = d.inputs.Concat(d.constants).ToArray();
        for (int i = 0; i < allNames.Length; i++)
        {
            bool constant = i >= d.inputs.Length;
            float y = top - 0.06f - i * rowH;
            int fanout = d.fanoutOverrides.TryGetValue(allNames[i], out int f) ? f : d.inputFanout;
            inputs.Add(MakeInput(board.transform, allNames[i], constant, new Vector3(left + 0.06f, y, 0f), fanout, plugStep));
        }
        board.inputs = inputs.ToArray();

        // ---- Ranuras
        for (int s = 0; s < d.slots; s++)
        {
            int c = s % cols, r = s / cols;
            var pos = new Vector3(left + inputsW + c * slotW + 0.06f, top - 0.04f - r * slotH, 0f);
            MakeSlot(board.transform, s, pos, d.ports, d.pins, d.pinFanout, d.allowed, plugStep, portStep, slotH);
        }

        // ---- Salidas
        var outputs = new List<CircuitOutput>();
        for (int o = 0; o < d.outputs.Length; o++)
        {
            float y = top - 0.06f - o * rowH;
            outputs.Add(MakeOutput(board.transform, d.outputs[o], new Vector3(left + panelW - outputsW + 0.04f, y, 0f)));
        }
        board.outputs = outputs.ToArray();

        // ---- Mesa con el botón; las compuertas quedan repartidas por el suelo de la sala
        var table = Table(root, panelW);
        var floor = FloorPieces(root);
        var spots = ScatterSpots(d.gates.Count, d.title);
        for (int g = 0; g < d.gates.Count; g++)
            MakeGate(floor, d.gates[g], spots[g].pos, Quaternion.Euler(90f, spots[g].yaw, 0f));

        if (d.liveTest.Length == 0)
            puzzle.submitButton = MakeSubmit(table, new Vector3(panelW * 0.5f - 0.15f, 0.02f, 0f));
        puzzle.resultLight = MakeLamp(table, "LuzResultado", new Vector3(panelW * 0.5f - 0.32f, 0.04f, 0f), 0.05f);

        return string.IsNullOrEmpty(d.drawing) ? panelW : AddExplanation(root, d.drawing, puzzle.puzzleId, panelW, top);
    }

    private static float BuildTruthTable(Transform root, TableDef d)
    {
        int rows = 1 << d.inputs.Length;
        var cell = new Vector2(0.11f, 0.07f);
        float tableH = (rows + 1) * (cell.y + 0.012f);
        float panelW = Mathf.Max(1.0f, (d.inputs.Length + 1) * 0.122f + 0.5f);
        float top = ContentBottom + tableH;

        var puzzle = root.gameObject.AddComponent<TruthTablePuzzle>();
        puzzle.puzzleId = d.title.Split(' ')[0];
        puzzle.inputNames = d.inputs;
        puzzle.spec = d.spec;
        puzzle.outputName = "S";
        puzzle.circuitDescription = d.description;
        puzzle.cellSize = cell;

        Backdrop(root, panelW, PanelBottom, top + 0.5f);
        Texts(root, puzzle, d.title, panelW, top, out puzzle.circuitText, out _);

        var tableRoot = new GameObject("Tabla");
        tableRoot.transform.SetParent(root, false);
        tableRoot.transform.localPosition = new Vector3(0f, ContentBottom + tableH * 0.5f, -0.01f);
        puzzle.tableRoot = tableRoot.transform;

        var desk = Table(root, panelW);
        puzzle.submitButton = MakeSubmit(desk, new Vector3(0.2f, 0.02f, 0f));
        puzzle.resultLight = MakeLamp(desk, "LuzResultado", new Vector3(-0.05f, 0.04f, 0f), 0.05f);
        return string.IsNullOrEmpty(d.drawing) ? panelW : AddExplanation(root, d.drawing, puzzle.puzzleId, panelW, top);
    }

    private static float BuildKMap(Transform root, KMapDef d)
    {
        int n = d.vars.Length;
        int mapRows = 1 << (n / 2);
        var cell = new Vector2(0.12f, 0.12f);
        float mapH = (mapRows + 2) * (cell.y + 0.012f) + 0.05f;
        float panelW = 2.4f;
        float top = ContentBottom + Mathf.Max(mapH, 0.75f);

        var puzzle = root.gameObject.AddComponent<KarnaughMapPuzzle>();
        puzzle.puzzleId = d.title.Split(' ')[0];
        puzzle.variables = d.vars;
        puzzle.spec = d.spec;
        puzzle.outputName = "F";
        puzzle.givenExpression = d.given;
        puzzle.cellSize = cell;

        Backdrop(root, panelW, PanelBottom, top + 0.5f);
        Texts(root, puzzle, d.title, panelW, top, out puzzle.expressionText, out _);

        var mapRoot = new GameObject("MapaK");
        mapRoot.transform.SetParent(root, false);
        mapRoot.transform.localPosition = new Vector3(-0.35f, top - mapH * 0.5f + 0.05f, -0.01f);
        puzzle.mapRoot = mapRoot.transform;

        puzzle.groupInfoText = Level2Visuals.CreateLabel(root, "InfoGrupos", "", new Vector3(-1.0f, top - 0.35f, -0.01f), new Vector2(0.38f, 0.6f), 0.3f);
        puzzle.groupInfoText.alignment = TextAlignmentOptions.TopLeft;

        var keypadGo = new GameObject("Teclado");
        keypadGo.transform.SetParent(root, false);
        keypadGo.transform.localPosition = new Vector3(0.65f, top - 0.45f, -0.01f);
        var keypad = keypadGo.AddComponent<ExpressionKeypad>();
        keypad.keys = d.vars.Concat(new[] { "'", "+", "(", ")" }).ToArray();
        keypad.display = Level2Visuals.CreateLabel(root, "Pantalla", "_", new Vector3(0.65f, top - 0.1f, -0.01f), new Vector2(0.55f, 0.12f), 0.6f);
        Prim(PrimitiveType.Cube, root, "FondoPantalla", new Vector3(0.65f, top - 0.1f, 0.005f), new Vector3(0.6f, 0.14f, 0.01f), Mat("L2_Pantalla", new Color(0.08f, 0.07f, 0.06f)));
        puzzle.keypad = keypad;

        var desk = Table(root, panelW);
        puzzle.submitButton = MakeSubmit(desk, new Vector3(0.65f, 0.02f, 0f));
        puzzle.resultLight = MakeLamp(desk, "LuzResultado", new Vector3(0.4f, 0.04f, 0f), 0.05f);
        return string.IsNullOrEmpty(d.drawing) ? panelW : AddExplanation(root, d.drawing, puzzle.puzzleId, panelW, top);
    }

    // ================================================================ Piezas

    private static void Backdrop(Transform root, float width, float bottom, float top)
    {
        Prim(PrimitiveType.Cube, root, "Panel", new Vector3(0f, (bottom + top) * 0.5f, 0.04f),
            new Vector3(width, top - bottom, 0.04f), Mat("L2_Panel", new Color(0.42f, 0.34f, 0.22f)), true);
    }

    private static void Texts(Transform root, Level2Puzzle puzzle, string title, float width, float top,
                              out TextMeshPro info, out TextMeshPro progress)
    {
        Level2Visuals.CreateLabel(root, "Titulo", DisplayTitle(title), new Vector3(0f, top + 0.42f, -0.01f), new Vector2(width * 0.9f, 0.09f), 0.9f);
        info = Level2Visuals.CreateLabel(root, "Enunciado", "", new Vector3(0f, top + 0.27f, -0.01f), new Vector2(width * 0.9f, 0.18f), 0.45f);
        progress = Level2Visuals.CreateLabel(root, "Progreso", "", new Vector3(0f, top + 0.14f, -0.01f), new Vector2(width * 0.9f, 0.06f), 0.4f);
        puzzle.statusText = Level2Visuals.CreateLabel(root, "Estado", "", new Vector3(0f, top + 0.07f, -0.01f), new Vector2(width * 0.9f, 0.06f), 0.4f);
    }

    /// <summary>"2.1.2 El Detective de Verdades (Medio)" → "El Detective de Verdades": el jugador no ve número ni dificultad.</summary>
    private static string DisplayTitle(string title)
    {
        int space = title.IndexOf(' ');
        string name = space > 0 ? title.Substring(space + 1) : title;
        int paren = name.LastIndexOf(" (");
        return paren > 0 ? name.Substring(0, paren) : name;
    }

    private static Transform Table(Transform root, float width)
    {
        var table = new GameObject("Mesa").transform;
        table.SetParent(root, false);
        table.localPosition = new Vector3(0f, TableTop, -0.42f);
        Prim(PrimitiveType.Cube, table, "Tablero", new Vector3(0f, -0.025f, 0f), new Vector3(Mathf.Max(1.2f, width), 0.05f, 0.4f),
            Mat("L2_Mesa", new Color(0.33f, 0.24f, 0.14f)), true);
        Prim(PrimitiveType.Cube, table, "Base", new Vector3(0f, -TableTop * 0.5f - 0.025f, 0f), new Vector3(Mathf.Max(1.0f, width - 0.2f), TableTop - 0.05f, 0.3f),
            Mat("L2_Mesa", new Color(0.33f, 0.24f, 0.14f)), true);
        return table;
    }

    private static CircuitInput MakeInput(Transform parent, string name, bool constant, Vector3 pos, int fanout, float plugStep)
    {
        var go = new GameObject("Entrada_" + name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        var input = go.AddComponent<CircuitInput>();
        input.displayName = name;
        input.isConstant = constant;
        input.fixedValue = false;

        Level2Visuals.CreateLabel(go.transform, "Nombre", constant ? name + " = 0" : name, new Vector3(0.02f, 0f, -0.01f), new Vector2(0.09f, 0.06f), 0.5f);

        if (!constant)
            input.sourceSwitch = MakeSwitch(go.transform, new Vector3(0.11f, 0f, -0.01f));

        var lamp = MakeLamp(go.transform, "Indicador", new Vector3(0.19f, 0f, -0.01f), 0.035f);
        input.indicator = lamp;
        input.cableAnchor = lamp.transform;

        for (int k = 0; k < fanout; k++)
            MakePlug(go.transform, input, new Vector3(0.25f + k * plugStep, 0f, -0.035f));

        return input;
    }

    private static void MakeSlot(Transform parent, int index, Vector3 pos, int ports, int pins, int pinFanout,
                                 LogicGateType[] allowed, float plugStep, float portStep, float slotH)
    {
        var go = new GameObject("Ranura_" + (index + 1));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var slot = go.AddComponent<GateSlot>();

        float midY = -slotH * 0.5f + 0.02f;
        Prim(PrimitiveType.Cube, go.transform, "Marco", new Vector3(0.17f, midY, 0.01f), new Vector3(0.34f, slotH - 0.04f, 0.01f),
            Mat("L2_Marco", new Color(0.22f, 0.17f, 0.11f)));

        slot.label = Level2Visuals.CreateLabel(go.transform, "Nombre", "vacío", new Vector3(0.17f, -0.02f, -0.01f), new Vector2(0.3f, 0.04f), 0.3f);

        // Socket de la compuerta
        var sockGo = new GameObject("SocketCompuerta");
        sockGo.transform.SetParent(go.transform, false);
        sockGo.transform.localPosition = new Vector3(0.17f, midY, -0.03f);
        var col = sockGo.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.05f;
        var sock = sockGo.AddComponent<Level2Socket>();
        sock.accept = Level2Socket.Accept.LogicGate;
        sock.allowedGates = allowed;
        slot.gateSocket = sock;

        // Puertos de entrada (columna izquierda)
        slot.inputPorts = new WirePort[ports];
        float py0 = midY + (ports - 1) * portStep * 0.5f;
        for (int p = 0; p < ports; p++)
            slot.inputPorts[p] = MakePort(go.transform, "Entrada" + (p + 1), new Vector3(0.03f, py0 - p * portStep, -0.02f), "·");

        // Pines de salida (columna derecha) con sus cables
        slot.outputPins = new GateOutputPin[pins];
        float oy0 = midY + (pins - 1) * 0.1f * 0.5f;
        for (int o = 0; o < pins; o++)
        {
            var pinGo = new GameObject("Salida" + (o + 1));
            pinGo.transform.SetParent(go.transform, false);
            pinGo.transform.localPosition = new Vector3(0.31f, oy0 - o * 0.1f, -0.02f);
            var pin = pinGo.AddComponent<GateOutputPin>();
            pin.slot = slot;
            pin.outputIndex = o;
            pin.displayName = $"R{index + 1}.{o}";
            Prim(PrimitiveType.Sphere, pinGo.transform, "Pin", Vector3.zero, Vector3.one * 0.025f, Mat("L2_Pin", new Color(0.8f, 0.6f, 0.2f)));
            pin.label = Level2Visuals.CreateLabel(pinGo.transform, "Etiqueta", "·", new Vector3(0f, 0.03f, -0.01f), new Vector2(0.06f, 0.03f), 0.25f);

            for (int k = 0; k < pinFanout; k++)
                MakePlug(go.transform, pin, new Vector3(0.37f + k * plugStep, oy0 - o * 0.1f, -0.035f));
            slot.outputPins[o] = pin;
        }
    }

    private static WirePort MakePort(Transform parent, string name, Vector3 pos, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.022f;
        var sock = go.AddComponent<Level2Socket>();
        sock.accept = Level2Socket.Accept.WirePlug;
        var port = go.AddComponent<WirePort>();

        Prim(PrimitiveType.Cylinder, go.transform, "Conector", new Vector3(0f, 0f, 0.012f), new Vector3(0.03f, 0.008f, 0.03f),
            Mat("L2_Conector", new Color(0.1f, 0.09f, 0.08f)), false, Quaternion.Euler(90f, 0f, 0f));
        port.label = Level2Visuals.CreateLabel(go.transform, "Etiqueta", label, new Vector3(-0.045f, 0f, -0.005f), new Vector2(0.05f, 0.03f), 0.25f);
        port.portName = label;
        return port;
    }

    private static CircuitOutput MakeOutput(Transform parent, string name, Vector3 pos)
    {
        var go = new GameObject("Salida_" + name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        var output = go.AddComponent<CircuitOutput>();
        output.displayName = name;
        output.port = MakePort(go.transform, "Puerto", Vector3.zero, "");
        output.bulb = MakeLamp(go.transform, "Bombilla", new Vector3(0.09f, 0f, -0.01f), 0.06f);

        var lightGo = new GameObject("Luz");
        lightGo.transform.SetParent(go.transform, false);
        lightGo.transform.localPosition = new Vector3(0.09f, 0f, -0.08f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 0.35f;
        light.intensity = 1.5f;
        light.color = new Color(1f, 0.8f, 0.4f);
        light.enabled = false;
        output.bulbLight = light;

        Level2Visuals.CreateLabel(go.transform, "Nombre", name, new Vector3(0.2f, 0f, -0.01f), new Vector2(0.12f, 0.06f), 0.5f);
        return output;
    }

    private static XRSwitchToggle MakeSwitch(Transform parent, Vector3 pos)
    {
        var go = new GameObject("Switch");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(0.06f, 0.09f, 0.05f);
        go.AddComponent<XRSimpleInteractable>();

        Prim(PrimitiveType.Cube, go.transform, "Base", new Vector3(0f, -0.03f, 0f), new Vector3(0.05f, 0.02f, 0.04f), Mat("L2_Marco", new Color(0.22f, 0.17f, 0.11f)));
        var pivot = new GameObject("Pivote");
        pivot.transform.SetParent(go.transform, false);
        pivot.transform.localPosition = new Vector3(0f, -0.025f, 0f);
        Prim(PrimitiveType.Cube, pivot.transform, "Palanca", new Vector3(0f, 0.035f, 0f), new Vector3(0.012f, 0.07f, 0.012f), Mat("L2_Palanca", new Color(0.75f, 0.6f, 0.3f)));

        var toggle = go.AddComponent<XRSwitchToggle>();
        var so = new SerializedObject(toggle);
        so.FindProperty("switchMesh").objectReferenceValue = pivot.transform;
        so.FindProperty("offRotation").vector3Value = new Vector3(0f, 0f, 30f);
        so.FindProperty("onRotation").vector3Value = new Vector3(0f, 0f, -30f);
        so.ApplyModifiedPropertiesWithoutUndo();
        return toggle;
    }

    private static WirePlug MakePlug(Transform parent, SignalNode source, Vector3 pos)
    {
        var go = new GameObject("Cable_" + source.displayName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        var col = go.AddComponent<SphereCollider>();
        col.radius = 0.018f;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var tip = Prim(PrimitiveType.Cylinder, go.transform, "Punta", Vector3.zero, new Vector3(0.022f, 0.012f, 0.022f),
            Mat("L2_Enchufe", new Color(0.4f, 0.4f, 0.4f)), false, Quaternion.Euler(90f, 0f, 0f));

        var grab = go.AddComponent<XRGrabInteractable>();
        grab.throwOnDetach = false;

        var plug = go.AddComponent<WirePlug>();
        plug.source = source;
        plug.tip = tip.GetComponent<Renderer>();
        plug.lineMaterial = CableMaterial();
        go.AddComponent<ReturnHome>();
        return plug;
    }

    private static void MakeGate(Transform parent, LogicGateType type, Vector3 pos, Quaternion rot)
    {
        var go = new GameObject("Compuerta_" + type);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;

        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(0.11f, 0.07f, 0.04f);
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Prim(PrimitiveType.Cube, go.transform, "Cuerpo", Vector3.zero, new Vector3(0.11f, 0.07f, 0.04f), GateMaterial(type));
        Level2Visuals.CreateLabel(go.transform, "Nombre", GateSlot.Pretty(type), new Vector3(0f, 0f, -0.022f), new Vector2(0.1f, 0.06f), 0.35f);

        var item = go.AddComponent<LogicGateItem>();
        var so = new SerializedObject(item);
        so.FindProperty("gateType").intValue = (int)type;
        so.ApplyModifiedPropertiesWithoutUndo();

        var grab = go.AddComponent<XRGrabInteractable>();
        grab.throwOnDetach = false;
        go.AddComponent<ReturnHome>();
    }

    // ================================================================ Piezas en el suelo

    /// <summary>Contenedor de las compuertas que se recogen del suelo (va dentro de cada versión, así solo se ven las de la activa).</summary>
    private static Transform FloorPieces(Transform root)
    {
        var t = new GameObject("PiezasSuelo").transform;
        t.SetParent(root, false);
        return t;
    }

    /// <summary>
    /// Lugares repartidos por el suelo frente al reto (en coordenadas del reto: el jugador está hacia −Z),
    /// separados entre sí. Siempre salen iguales para el mismo reto.
    /// </summary>
    private static List<(Vector3 pos, float yaw)> ScatterSpots(int count, string seedText)
    {
        const float halfX = 4.2f, zNear = -2f, zFar = -8.5f, minDist = 0.8f;
        int seed = 17;
        foreach (char ch in seedText) seed = unchecked(seed * 31 + ch);
        var rnd = new System.Random(seed);

        var spots = new List<(Vector3 pos, float yaw)>();
        for (int attempts = 0; spots.Count < count && attempts < 4000; attempts++)
        {
            var p = new Vector3((float)(rnd.NextDouble() * 2 - 1) * halfX, 0f, zNear + (float)rnd.NextDouble() * (zFar - zNear));
            if (spots.Any(s => (s.pos - p).sqrMagnitude < minDist * minDist)) continue;
            spots.Add((p, (float)rnd.NextDouble() * 360f));
        }
        // Si no cupieron todas (muchas piezas), el resto va en fila al fondo
        for (int i = spots.Count; i < count; i++)
            spots.Add((new Vector3(-halfX + (i % 10) * 0.6f, 0f, zFar - 0.6f - (i / 10) * 0.6f), 0f));

        // Altura de una pieza acostada sobre el piso
        return spots.Select(s => (s.pos + new Vector3(0f, 0.025f, 0f), s.yaw)).ToList();
    }

    private static XRPhysicalButton MakeSubmit(Transform parent, Vector3 pos)
    {
        var go = new GameObject("BotonComprobar");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(0.1f, 0.05f, 0.1f);
        col.center = new Vector3(0f, 0.02f, 0f);
        go.AddComponent<XRSimpleInteractable>();

        Prim(PrimitiveType.Cylinder, go.transform, "Base", new Vector3(0f, 0.005f, 0f), new Vector3(0.13f, 0.01f, 0.13f), Mat("L2_Marco", new Color(0.22f, 0.17f, 0.11f)));
        var cap = Prim(PrimitiveType.Cylinder, go.transform, "Tapa", new Vector3(0f, 0.022f, 0f), new Vector3(0.09f, 0.012f, 0.09f), Mat("L2_Boton", new Color(0.75f, 0.15f, 0.1f)));
        var label = Level2Visuals.CreateLabel(go.transform, "Texto", "Comprobar", new Vector3(0f, 0.09f, 0.08f), new Vector2(0.2f, 0.05f), 0.35f);
        label.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);

        var button = go.AddComponent<XRPhysicalButton>();
        var so = new SerializedObject(button);
        so.FindProperty("buttonMesh").objectReferenceValue = cap.transform;
        so.FindProperty("pressDistance").floatValue = 0.01f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    private static Renderer MakeLamp(Transform parent, string name, Vector3 pos, float size)
    {
        return Prim(PrimitiveType.Sphere, parent, name, pos, Vector3.one * size, Mat("L2_Lampara", new Color(0.15f, 0.12f, 0.1f))).GetComponent<Renderer>();
    }

    // ================================================================ Utilidades

    private static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, Material mat,
                                   bool keepCollider = false, Quaternion? rot = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (rot.HasValue) go.transform.localRotation = rot.Value;
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        else if (type == PrimitiveType.Cylinder)
        {
            // El collider por defecto del cilindro es una cápsula (un plato rodaría como pelota)
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.AddComponent<BoxCollider>();
        }
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private static Material Mat(string name, Color color, bool glow = false)
    {
        EnsureFolder();
        string path = $"{MaterialFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        mat = new Material(shader) { name = name };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (glow && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Material CableMaterial()
    {
        EnsureFolder();
        string path = $"{MaterialFolder}/L2_Cable.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        mat = new Material(Shader.Find("Sprites/Default")) { name = "L2_Cable" };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Material GateMaterial(LogicGateType type)
    {
        // Reutiliza los materiales de compuertas del Nivel 1 cuando existen
        var existing = AssetDatabase.LoadAssetAtPath<Material>($"Assets/EgyptianLogic/Materials/Gates/{type}.mat");
        if (existing != null) return existing;

        switch (type)
        {
            case LogicGateType.NAND: return Mat("L2_NAND", new Color(0.55f, 0.2f, 0.6f));
            case LogicGateType.NOR: return Mat("L2_NOR", new Color(0.2f, 0.5f, 0.55f));
            case LogicGateType.XNOR: return Mat("L2_XNOR", new Color(0.5f, 0.5f, 0.2f));
            case LogicGateType.MUX2: return Mat("L2_MUX2", new Color(0.2f, 0.35f, 0.7f));
            case LogicGateType.MUX4: return Mat("L2_MUX4", new Color(0.15f, 0.25f, 0.55f));
            case LogicGateType.HALF_ADDER: return Mat("L2_HalfAdder", new Color(0.6f, 0.45f, 0.15f));
            case LogicGateType.FULL_ADDER: return Mat("L2_FullAdder", new Color(0.7f, 0.35f, 0.1f));
            default: return Mat("L2_Gate_" + type, new Color(0.5f, 0.5f, 0.5f));
        }
    }

    private static void EnsureFolder()
    {
        if (AssetDatabase.IsValidFolder(MaterialFolder)) return;
        if (!AssetDatabase.IsValidFolder("Assets/EgyptianLogic/Materials"))
            AssetDatabase.CreateFolder("Assets/EgyptianLogic", "Materials");
        AssetDatabase.CreateFolder("Assets/EgyptianLogic/Materials", "Level2");
    }
}
