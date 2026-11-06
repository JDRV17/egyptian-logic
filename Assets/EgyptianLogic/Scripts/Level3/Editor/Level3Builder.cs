using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Menú "Egyptian Logic > Nivel 3". Arma en la escena abierta los retos del Nivel 3 usando los prefabs
/// de Assets/EgyptianLogic/Prefabs, y los conecta con el tracker, el motor, las pistas y el reloj.
/// Después solo hay que mover el conjunto a su lugar en la sala.
/// </summary>
public static class Level3Builder
{
    private const string Menu = "Egyptian Logic/Nivel 3/";
    private const string PrefabFolder = "Assets/EgyptianLogic/Prefabs/";
    private const string CubePrefab = PrefabFolder + "Cube.prefab";
    private const string BulbPrefab = PrefabFolder + "Low_Poly_Light_Bulb.prefab";
    private const string KeyPrefab = PrefabFolder + "key_low.prefab";
    private const string KeyholePrefab = PrefabFolder + "keyhole (2).prefab";
    private const string LeverPrefab = PrefabFolder + "switch.prefab";
    private const string ButtonPrefab = PrefabFolder + "Button (1).prefab";
    private const string GlowMaterial = "Assets/EgyptianLogic/Materials/Indicador.mat";
    private const string BoxMaterial = "Assets/EgyptianLogic/Materials/Walls.mat";
    private const string MagicPrefab = PrefabFolder + "Magia.prefab";
    private const string DustPrefab = PrefabFolder + "Polvo.prefab";
    private const string ClockModel = "Assets/EgyptianLogic/Models/clock.fbx";
    private const string TorchModel = "Assets/EgyptianLogic/Models/torch.fbx";
    private const string TablePrefab = PrefabFolder + "Table.prefab";
    private const string MaterialFolder = "Assets/EgyptianLogic/Materials/";

    // ================================================================ 3.1

    private static readonly string[][] Hints31 =
    {
        new[]
        {
            "Junto al gran reloj hay un cubo, una bombilla y una llave colgada. Ahí te espera el primer reto.",
            "La bombilla debe quedar encendida guardando un 1, aunque el cubo siga cambiando.",
            "Mientras la llave está en la cerradura, la bombilla copia lo que muestra el cubo. Al quitarla, se queda con el último valor. Así funciona un latch D: la llave es su señal Enable.",
            "Pon la llave, espera a que el cubo muestre 1 y quita la llave antes del siguiente tic."
        },
        new[]
        {
            "Junto al gran reloj hay cuatro cubos, cuatro bombillas y una sola cerradura.",
            "Las bombillas deben guardar el código 1011, aunque los cubos sigan cambiando.",
            "Una sola llave controla las cuatro bombillas a la vez: mientras está puesta, todas copian a los cubos, y al quitarla todas guardan lo que había. Varios latches con un mismo Enable forman un registro.",
            "Pon la llave y quítala justo cuando los cubos muestren 1 0 1 1."
        },
        new[]
        {
            "Junto al gran reloj hay cuatro cubos, cuatro bombillas y cuatro cerraduras, una debajo de cada bombilla.",
            "Las bombillas deben guardar 1011, pero los cubos nunca muestran ese código completo.",
            "Cada llave controla solo su bombilla. Puedes guardar cada bit en un momento distinto: quita la llave de un bit cuando el cubo de ese bit muestre el valor que necesitas.",
            "Pon las cuatro llaves. Cuando los cubos muestren 1 0 0 1, quita la primera, la segunda y la cuarta. Cuando muestren 0 0 1 1 o 0 1 1 0, quita la tercera."
        }
    };

    [MenuItem(Menu + "Construir 3.1 El cofre con memoria")]
    public static void Build31()
    {
        const string rootName = "Reto_3.1";
        const int stage = 0;

        var cube = Load(CubePrefab);
        var bulb = Load(BulbPrefab);
        var key = Load(KeyPrefab);
        var keyhole = Load(KeyholePrefab);
        if (cube == null || bulb == null || key == null || keyhole == null) return;

        var old = GameObject.Find(rootName);
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Construir 3.1",
                    $"Ya existe \"{rootName}\" en la escena. ¿Reemplazarlo?", "Reemplazar", "Cancelar"))
                return;
            Undo.DestroyObjectImmediate(old);
        }

        var root = new GameObject(rootName);
        Undo.RegisterCreatedObjectUndo(root, "Construir 3.1");
        root.transform.position = SpawnPoint();

        var puzzle = root.AddComponent<MemoryLatchPuzzle>();
        var so = new SerializedObject(puzzle);
        var versions = so.FindProperty("versions");

        string[] codes = { "1", "1011", "1011" };
        int[] bits = { 1, 4, 4 };
        int[] sockets = { 1, 1, 4 };

        for (int v = 0; v < 3; v++)
        {
            var vRoot = new GameObject("V" + (v + 1));
            vRoot.transform.SetParent(root.transform, false);
            var setup = versions.GetArrayElementAtIndex(v);
            setup.FindPropertyRelative("root").objectReferenceValue = vRoot;

            BuildLatchVersion(vRoot.transform, bits[v], sockets[v], codes[v], cube, bulb, key, keyhole,
                out var cubes, out var bulbs, out var holes);

            SetArray(setup.FindPropertyRelative("dCubes"), cubes);
            SetArray(setup.FindPropertyRelative("qBulbs"), bulbs);
            SetArray(setup.FindPropertyRelative("keySockets"), holes);
            vRoot.SetActive(v == 1);   // en el editor se ve V2; al jugar manda el motor
        }

        var report = new StringBuilder();
        var tracker = Object.FindObjectOfType<PerformanceTracker>();
        var hints = Object.FindObjectOfType<HintManager>();
        var clock = Object.FindObjectOfType<GameClock>();
        var engine = Object.FindObjectOfType<AdaptationEngine>();

        so.FindProperty("clock").objectReferenceValue = clock;
        so.FindProperty("tracker").objectReferenceValue = tracker;
        so.FindProperty("hintManager").objectReferenceValue = hints;
        so.FindProperty("stageIndex").intValue = stage;
        so.FindProperty("randomizeOnStart").boolValue = false;
        so.FindProperty("forcedVersion").intValue = 1;
        so.ApplyModifiedPropertiesWithoutUndo();

        MakeHintZone(root.transform, hints, tracker, stage);

        ConfigureTracker(tracker, stage, "3.1", new[] { 10f, 20f, 40f }, new[] { 2, 2, 8 }, report);
        ConfigureEngine(engine, stage, "3.1", puzzle, report);
        ConfigureHints(hints, stage, "3.1", puzzle, Hints31, report);
        if (clock == null) report.AppendLine("• No hay GameClock en la escena: asígnalo a mano en el reto.");

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);

        report.Insert(0, "Reto 3.1 construido.\n\n");
        report.AppendLine("\nMuévelo a su lugar en la sala. En la escena se ve V2; las otras versiones son V1 y V3 " +
                          "(clic derecho en MemoryLatchPuzzle → Previsualizar).");
        EditorUtility.DisplayDialog("Construir 3.1", report.ToString(), "OK");
    }

    /// <summary>
    /// Una versión de 3.1: fila de cubos (D) arriba, bombillas (Q) debajo, cerraduras más abajo
    /// y las llaves colgando en la parte de abajo. Todo de izquierda a derecha = del bit más significativo al menos.
    /// </summary>
    private static void BuildLatchVersion(Transform parent, int bitCount, int socketCount, string code,
        GameObject cubePrefab, GameObject bulbPrefab, GameObject keyPrefab, GameObject keyholePrefab,
        out BitCube[] cubes, out BitBulb[] bulbs, out XRSocketInteractor[] holes)
    {
        cubes = new BitCube[bitCount];
        bulbs = new BitBulb[bitCount];
        holes = new XRSocketInteractor[socketCount];

        // Medidas reales de los prefabs para el espaciado
        float cubeW = Width(cubePrefab), bulbW = Width(bulbPrefab);
        float spacing = Mathf.Max(0.25f, Mathf.Max(cubeW, bulbW) + 0.1f);

        for (int b = 0; b < bitCount; b++)
        {
            float x = Column(b, bitCount, spacing);

            var c = Spawn(cubePrefab, parent, $"D{bitCount - 1 - b}", new Vector3(x, 1.5f, 0f));
            cubes[b] = ToBitCube(c);

            var l = Spawn(bulbPrefab, parent, $"Q{bitCount - 1 - b}", new Vector3(x, 1.22f, 0f));
            bulbs[b] = ToBitBulb(l);
        }

        var keyTargets = new List<Transform>();
        for (int s = 0; s < socketCount; s++)
        {
            float x = Column(s, socketCount, spacing);
            string label = socketCount == 1 ? "Cerradura_Maestra" : $"Cerradura_{bitCount - 1 - s}";
            var h = Spawn(keyholePrefab, parent, label, new Vector3(x, 1.0f, 0f));
            holes[s] = h.GetComponentInChildren<XRSocketInteractor>();
            keyTargets.Add(h.transform);
        }

        for (int s = 0; s < socketCount; s++)
        {
            float x = Column(s, socketCount, spacing);
            var k = Spawn(keyPrefab, parent, socketCount == 1 ? "Llave_Maestra" : $"Llave_{bitCount - 1 - s}",
                new Vector3(x, 0.8f, -0.05f));
            var hk = k.GetComponent<HangingKey>();
            if (hk != null)
            {
                // Tocar cualquier cerradura de esta versión no la devuelve a su gancho
                var kso = new SerializedObject(hk);
                SetArray(kso.FindProperty("safeRoots"), keyTargets.ToArray());
                kso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // Código a guardar, arriba de los cubos
        var label3d = new GameObject("Codigo");
        label3d.transform.SetParent(parent, false);
        label3d.transform.localPosition = new Vector3(0f, 1.78f, 0f);
        var tmp = label3d.AddComponent<TextMeshPro>();
        tmp.text = string.Join(" ", code.ToCharArray());
        tmp.fontSize = 1.2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(2f, 0.3f);

        // Tablero de fondo (para reemplazar por un modelo de piedra)
        var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "Tablero (reemplazar)";
        board.transform.SetParent(parent, false);
        float width = Mathf.Max(bitCount, socketCount) * spacing + 0.4f;
        board.transform.localPosition = new Vector3(0f, 1.25f, 0.12f);
        board.transform.localScale = new Vector3(width, 1.2f, 0.05f);
        Object.DestroyImmediate(board.GetComponent<Collider>());
    }

    /// <summary>Convierte el cubo de 1.3 en un BitCube: caras 0/1/0/1 y sin interacción del jugador.</summary>
    private static BitCube ToBitCube(GameObject go)
    {
        Vector3 axis = Vector3.up;
        var rot = go.GetComponent<CubeRotator>();
        if (rot != null)
        {
            var rso = new SerializedObject(rot);
            axis = rso.FindProperty("rotationAxis").vector3Value;

            // faceTexts: 0 = cara a 90°, 1 = 180°, 2 = 270°. La cara que no está en la lista es la de 0° ("?")
            var faces = rso.FindProperty("faceTexts");
            var listed = new HashSet<Object>();
            for (int i = 0; i < faces.arraySize; i++)
            {
                var t = faces.GetArrayElementAtIndex(i).objectReferenceValue as TMP_Text;
                if (t == null) continue;
                listed.Add(t);
                SetText(t, i % 2 == 0 ? "1" : "0");   // 90° = 1, 180° = 0, 270° = 1
            }
            foreach (var t in go.GetComponentsInChildren<TMP_Text>(true))
                if (!listed.Contains(t)) SetText(t, "0");   // la cara que mira al jugador

            Disable(rot);   // el jugador ya no gira los cubos
        }
        var simple = go.GetComponent<XRSimpleInteractable>();
        if (simple != null) Disable(simple);

        var bit = go.GetComponent<BitCube>();
        if (bit == null) bit = go.AddComponent<BitCube>();
        var bso = new SerializedObject(bit);
        bso.FindProperty("rotationAxis").vector3Value = axis;
        bso.FindProperty("rotateSound").objectReferenceValue = go.GetComponent<AudioSource>();
        bso.ApplyModifiedPropertiesWithoutUndo();
        return bit;
    }

    /// <summary>Convierte la bombilla del Nivel 1 en un BitBulb con el mismo brillo.</summary>
    private static BitBulb ToBitBulb(GameObject go)
    {
        var bulb = go.AddComponent<BitBulb>();
        var bso = new SerializedObject(bulb);
        bso.FindProperty("bulbLight").objectReferenceValue = go.GetComponentInChildren<Light>(true);
        var rend = go.GetComponent<Renderer>();
        if (rend == null) rend = go.GetComponentInChildren<Renderer>(true);
        bso.FindProperty("bulbRenderer").objectReferenceValue = rend;

        var oldLock = go.GetComponent<LightCombinationLock>();
        if (oldLock != null)
        {
            var lso = new SerializedObject(oldLock);
            var on = lso.FindProperty("onEmissionColor");
            if (on != null) bso.FindProperty("onEmission").colorValue = on.colorValue;
            Disable(oldLock);   // era la lógica de 1.1; aquí la bombilla la controla el reto
        }
        bso.ApplyModifiedPropertiesWithoutUndo();
        return bulb;
    }


    // ================================================================ 3.2

    private static readonly string[][] Hints32 =
    {
        new[]
        {
            "Junto al reloj hay un cajón con cubos ensartados en un cilindro, una palanca y un botón. Ahí está el registro de desplazamiento.",
            "Los cubos del cajón deben formar 1 0 1 1, de izquierda a derecha, antes de que se acaben los pulsos.",
            "Cada vez que oprimes el botón (un pulso de reloj), todos los cubos avanzan una posición y entra uno nuevo con el valor de la palanca. El registro solo cambia en el instante del pulso: así funcionan los flip-flops de un registro de desplazamiento.",
            "Los bits entran por la izquierda, así que el primero que metas terminará a la derecha. Mete 1, 1, 0, 1, con un pulso para cada uno."
        },
        new[]
        {
            "Junto al reloj hay un cajón con cubos ensartados en un cilindro, dos palancas y un botón. Ahí está el registro de desplazamiento.",
            "Los cubos deben formar 1 0 1 1, y solo tienes 3 pulsos.",
            "La segunda palanca cambia la dirección: hacia la derecha, los bits entran por la izquierda; hacia la izquierda, entran por la derecha. Fíjate en los cubos que ya están en el registro: quizás no necesitas reemplazarlos todos.",
            "Pon la dirección hacia la izquierda y mete 1 y luego 1: 0010 → 0101 → 1011."
        },
        new[]
        {
            "Junto al reloj hay un cajón con seis cubos ensartados en un cilindro, dos palancas y un botón.",
            "Los cubos deben formar 1 1 0 0 1 1, y solo tienes 4 pulsos.",
            "Con 4 pulsos no alcanzas a reemplazar los 6 cubos, y en una sola dirección no hay forma de lograrlo. Desplazar hacia un lado y luego hacia el otro cambia solo el bit del extremo.",
            "Con la dirección hacia la derecha, pon la palanca en 1 y da tres pulsos (001010 → 111001). Luego cambia la dirección hacia la izquierda y da un último pulso con la palanca en 1: 110011."
        }
    };

    [MenuItem(Menu + "Construir 3.2 El registro de desplazamiento")]
    public static void Build32()
    {
        const string rootName = "Reto_3.2";
        const int stage = 1;
        const int registrosCable = 4;   // el cable de la pareja Registros va a 3.5

        var cubePrefab = Load(CubePrefab);
        var bulbPrefab = Load(BulbPrefab);
        var leverPrefab = Load(LeverPrefab);
        var buttonPrefab = Load(ButtonPrefab);
        if (cubePrefab == null || bulbPrefab == null || leverPrefab == null || buttonPrefab == null) return;
        var glow = AssetDatabase.LoadAssetAtPath<Material>(GlowMaterial);
        var stone = AssetDatabase.LoadAssetAtPath<Material>(BoxMaterial);

        var old = GameObject.Find(rootName);
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Construir 3.2",
                    $"Ya existe \"{rootName}\" en la escena. ¿Reemplazarlo?", "Reemplazar", "Cancelar"))
                return;
            Undo.DestroyObjectImmediate(old);
        }

        var root = new GameObject(rootName);
        Undo.RegisterCreatedObjectUndo(root, "Construir 3.2");
        root.transform.position = SpawnPoint();
        var puzzle = root.AddComponent<ShiftRegisterPuzzle>();

        // ---- Medidas: la ventana se calcula para 6 bits; con 4 se reparten en el mismo espacio
        float cubeW = Width(cubePrefab);
        float pitch6 = cubeW + 0.04f;
        float window = 6f * pitch6;
        float cap = window / 8f + cubeW * 0.5f + 0.04f;   // tapa que esconde el cubo de reserva (4 bits es el caso más largo)
        float total = window + 2f * cap;
        float h = cubeW + 0.1f;
        const float trackY = 1.3f;

        var track = new GameObject("Track").transform;
        track.SetParent(root.transform, false);
        track.localPosition = new Vector3(0f, trackY, 0f);

        // ---- Cajón y cilindro (primitivas: reemplázalas o cámbiales el material)
        var box = new GameObject("Cajon").transform;
        box.SetParent(track, false);
        Box(box, "Fondo", new Vector3(0f, 0f, cubeW * 0.5f + 0.03f), new Vector3(total + 0.04f, h + 0.06f, 0.03f), stone);
        Box(box, "Riel_Superior", new Vector3(0f, h * 0.5f, 0f), new Vector3(total + 0.04f, 0.03f, cubeW + 0.08f), stone);
        Box(box, "Riel_Inferior", new Vector3(0f, -h * 0.5f, 0f), new Vector3(total + 0.04f, 0.03f, cubeW + 0.08f), stone);
        Box(box, "Pared_Izquierda", new Vector3(-total * 0.5f - 0.01f, 0f, 0f), new Vector3(0.03f, h + 0.06f, cubeW + 0.08f), stone);
        Box(box, "Pared_Derecha", new Vector3(total * 0.5f + 0.01f, 0f, 0f), new Vector3(0.03f, h + 0.06f, cubeW + 0.08f), stone);
        Box(box, "Tapa_Izquierda", new Vector3(-(window + cap) * 0.5f, 0f, -cubeW * 0.5f - 0.025f), new Vector3(cap, h, 0.03f), stone);
        Box(box, "Tapa_Derecha", new Vector3((window + cap) * 0.5f, 0f, -cubeW * 0.5f - 0.025f), new Vector3(cap, h, 0.03f), stone);
        var rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rod.name = "Cilindro";
        rod.transform.SetParent(box, false);
        rod.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        rod.transform.localScale = new Vector3(0.025f, total * 0.5f, 0.025f);
        Object.DestroyImmediate(rod.GetComponent<Collider>());

        // ---- 7 cubos (6 bits + 1 de reserva)
        var cubes = new BitCube[7];
        for (int i = 0; i < cubes.Length; i++)
        {
            var c = Spawn(cubePrefab, track, "Cubo_" + (i + 1), new Vector3((i - 3f) * pitch6, 0f, 0f));
            cubes[i] = ToBitCube(c);
        }

        // ---- Código a formar
        var label = new GameObject("Codigo");
        label.transform.SetParent(root.transform, false);
        label.transform.localPosition = new Vector3(0f, trackY + h * 0.5f + 0.15f, 0f);
        var tmp = label.AddComponent<TextMeshPro>();
        tmp.text = "? ? ? ?";
        tmp.fontSize = 1.2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(2f, 0.3f);

        // ---- Controles: entrada (izquierda), dirección (centro, emerge en V2/V3), pulso (derecha)
        const float ctrlY = 0.95f, glyphY = 1.1f, ctrlZ = -0.15f;
        var input = Spawn(leverPrefab, root.transform, "Palanca_Entrada", new Vector3(-0.45f, ctrlY, ctrlZ));
        var zero = Glyph(root.transform, "Indicador_0", new Vector3(-0.52f, glyphY, ctrlZ), glow, GlyphZero);
        var one = Glyph(root.transform, "Indicador_1", new Vector3(-0.38f, glyphY, ctrlZ), glow, GlyphOne);

        var dirRoot = new GameObject("Palanca_Direccion").transform;
        dirRoot.SetParent(root.transform, false);
        dirRoot.localPosition = new Vector3(0f, ctrlY, ctrlZ);
        var dir = Spawn(leverPrefab, dirRoot, "Palanca", Vector3.zero);
        var dirToggle = dir.GetComponentInChildren<XRSwitchToggle>(true);
        if (dirToggle != null)
        {
            var dso = new SerializedObject(dirToggle);
            dso.FindProperty("isOn").boolValue = true;   // encendida = derecha, la dirección por defecto
            dso.ApplyModifiedPropertiesWithoutUndo();
        }
        var left = Glyph(root.transform, "Flecha_Izquierda", new Vector3(-0.07f, glyphY, ctrlZ), glow, (t, m) => GlyphArrow(t, m, false));
        var right = Glyph(root.transform, "Flecha_Derecha", new Vector3(0.07f, glyphY, ctrlZ), glow, (t, m) => GlyphArrow(t, m, true));

        var button = Spawn(buttonPrefab, root.transform, "Boton_Pulso", new Vector3(0.45f, ctrlY, ctrlZ));
        var pulses = new BitBulb[4];
        for (int i = 0; i < pulses.Length; i++)
        {
            var b = Spawn(bulbPrefab, root.transform, "Pulso_" + (i + 1), new Vector3(0.33f + i * 0.08f, glyphY, ctrlZ));
            b.transform.localScale = b.transform.localScale * 0.5f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(b.transform);
            pulses[i] = ToBitBulb(b);
        }

        // ---- El reto
        var report = new StringBuilder();
        var tracker = Object.FindObjectOfType<PerformanceTracker>();
        var hints = Object.FindObjectOfType<HintManager>();
        var engine = Object.FindObjectOfType<AdaptationEngine>();

        var so = new SerializedObject(puzzle);
        so.FindProperty("tracker").objectReferenceValue = tracker;
        so.FindProperty("hintManager").objectReferenceValue = hints;
        so.FindProperty("stageIndex").intValue = stage;
        so.FindProperty("track").objectReferenceValue = track;
        so.FindProperty("windowWidth").floatValue = window;
        SetArray(so.FindProperty("cubes"), cubes);
        so.FindProperty("inputLever").objectReferenceValue = input.GetComponentInChildren<XRSwitchToggle>(true);
        so.FindProperty("directionLever").objectReferenceValue = dirToggle;
        so.FindProperty("pulseButton").objectReferenceValue = button.GetComponentInChildren<XRPhysicalButton>(true);
        so.FindProperty("directionLeverRoot").objectReferenceValue = dirRoot;
        so.FindProperty("zeroGlyph").objectReferenceValue = zero;
        so.FindProperty("oneGlyph").objectReferenceValue = one;
        so.FindProperty("leftArrow").objectReferenceValue = left;
        so.FindProperty("rightArrow").objectReferenceValue = right;
        SetArray(so.FindProperty("pulseBulbs"), pulses);
        so.FindProperty("codeText").objectReferenceValue = tmp;
        var magic = AssetDatabase.LoadAssetAtPath<GameObject>(MagicPrefab);
        var dust = AssetDatabase.LoadAssetAtPath<GameObject>(DustPrefab);
        if (magic != null) so.FindProperty("appearEffect").objectReferenceValue = magic.GetComponent<ParticleSystem>();
        if (dust != null) so.FindProperty("riseEffect").objectReferenceValue = dust.GetComponent<ParticleSystem>();
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---- Zona: al entrar se fija la versión
        var zone = MakeHintZone(root.transform, hints, tracker, stage);
        var zso = new SerializedObject(zone);
        zso.FindProperty("gate").objectReferenceValue = puzzle;
        zso.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(zone.onPlayerEnter, puzzle.PlayerEntered);
        UnityEventTools.AddPersistentListener(zone.onPlayerExit, puzzle.PlayerExited);

        // ---- Se desbloquea al conectar el cable de la pareja Registros
        var cable = FindCableSocket(registrosCable);
        if (cable != null)
        {
            UnityEventTools.AddPersistentListener(cable.onConnected, puzzle.Unlock);
            report.AppendLine("• Se desbloquea al conectar el cable de la pareja Registros.");
        }
        else report.AppendLine($"• No encontré el ClockCableSocket con Stage Index {registrosCable}: conecta su On Connected a ShiftRegisterPuzzle.Unlock a mano.");

        // ---- Bloqueo mutuo con 3.5
        SetExclusion("Exclusion_Registros", puzzle, true, report);

        ConfigureTracker(tracker, stage, "3.2", new[] { 30f, 40f, 60f }, new[] { 7, 4, 6 }, report);
        ConfigureEngine(engine, stage, "3.2", puzzle, report);
        ConfigureHints(hints, stage, "3.2", puzzle, Hints32, report);
        if (glow == null) report.AppendLine($"• No encontré {GlowMaterial}: asigna un material con emisión a los indicadores.");
        SetupGuide(report, false);   // si ya hay guía, el reto nuevo queda registrado en ella

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);

        report.Insert(0, "Reto 3.2 construido.\n\n");
        report.AppendLine("\nMuévelo a su lugar en la sala. Los cubos y la palanca de dirección aparecen al jugar, " +
                          "cuando el jugador entra a la zona (clic derecho en ShiftRegisterPuzzle → Previsualizar para verlos en el editor).");
        EditorUtility.DisplayDialog("Construir 3.2", report.ToString(), "OK");
    }

    // ---- Piezas del cajón y de los indicadores

    private static void Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.name = name;
        b.transform.SetParent(parent, false);
        b.transform.localPosition = pos;
        b.transform.localScale = size;
        Object.DestroyImmediate(b.GetComponent<Collider>());
        if (mat != null) b.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void Bar(Transform parent, Vector3 pos, Vector2 size, float rotZ, Material mat)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.name = "Barra";
        b.transform.SetParent(parent, false);
        b.transform.localPosition = pos;
        b.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        b.transform.localScale = new Vector3(size.x, size.y, 0.01f);
        Object.DestroyImmediate(b.GetComponent<Collider>());
        if (mat != null) b.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static GlowGroup Glyph(Transform parent, string name, Vector3 pos, Material mat, System.Action<Transform, Material> draw)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        draw(g.transform, mat);
        return g.AddComponent<GlowGroup>();
    }

    private static void GlyphOne(Transform t, Material m)
    {
        Bar(t, Vector3.zero, new Vector2(0.012f, 0.07f), 0f, m);
        Bar(t, new Vector3(-0.01f, 0.027f, 0f), new Vector2(0.022f, 0.01f), 35f, m);
    }

    private static void GlyphZero(Transform t, Material m)
    {
        Bar(t, new Vector3(-0.02f, 0f, 0f), new Vector2(0.012f, 0.07f), 0f, m);
        Bar(t, new Vector3(0.02f, 0f, 0f), new Vector2(0.012f, 0.07f), 0f, m);
        Bar(t, new Vector3(0f, 0.029f, 0f), new Vector2(0.052f, 0.012f), 0f, m);
        Bar(t, new Vector3(0f, -0.029f, 0f), new Vector2(0.052f, 0.012f), 0f, m);
    }

    private static void GlyphArrow(Transform t, Material m, bool pointsRight)
    {
        float s = pointsRight ? 1f : -1f;
        Bar(t, Vector3.zero, new Vector2(0.07f, 0.012f), 0f, m);
        Bar(t, new Vector3(0.023f * s, 0.011f, 0f), new Vector2(0.035f, 0.012f), -45f * s, m);
        Bar(t, new Vector3(0.023f * s, -0.011f, 0f), new Vector2(0.035f, 0.012f), 45f * s, m);
    }

    private static ClockCableSocket FindCableSocket(int stage)
    {
        foreach (var s in Object.FindObjectsOfType<ClockCableSocket>())
            if (s.StageIndex == stage) return s;
        return null;
    }

    /// <summary>Crea (o completa) el objeto de bloqueo mutuo de una pareja.</summary>
    private static void SetExclusion(string name, Level3Puzzle puzzle, bool asFirst, StringBuilder report)
    {
        var go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Bloqueo de pareja");
        }
        var ex = go.GetComponent<PairExclusion>();
        if (ex == null) ex = go.AddComponent<PairExclusion>();
        var so = new SerializedObject(ex);
        so.FindProperty(asFirst ? "first" : "second").objectReferenceValue = puzzle;
        so.ApplyModifiedPropertiesWithoutUndo();
        report.AppendLine($"• Bloqueo mutuo: {name} (el otro reto se asigna al construirlo).");
    }



    // ================================================================ 3.5

    private static readonly string[][] Hints35 =
    {
        new[]
        {
            "",
            "Presiona el botón y cuenta los pitidos. Cuando se detengan, enciende con los interruptores el número que marcaba el contador y presiona el botón otra vez.",
            "Es un contador de 2 bits: suma 1 en cada flanco de reloj (cada pitido). Después de 11 vuelve a 00, porque con 2 bits solo caben los números del 0 al 3.",
            "Son 6 pitidos: 00 → 01 → 10 → 11 → 00 → 01 → 10. Enciende el interruptor de la izquierda y deja apagado el de la derecha: 10."
        },
        new[]
        {
            "",
            "Presiona el botón y cuenta los pitidos. Cuando se detengan, enciende con los interruptores el número que marcaba el contador y presiona el botón otra vez.",
            "Es un contador de 4 bits: suma 1 en cada flanco de reloj. Puedes contar los pitidos y convertir el total a binario, o seguir la cuenta en binario mientras las bombillas te ayudan.",
            "Son 13 pitidos, y 13 en binario es 1101. Enciende el primero, el segundo y el cuarto interruptor."
        },
        new[]
        {
            "",
            "Presiona el botón y sigue el conteo. Cuando se detenga, enciende con los interruptores el número que marcaba el contador y presiona el botón otra vez. Atento al sonido y a la luz roja.",
            "Es un contador ascendente/descendente: mientras suena distinto y la luz roja está encendida, resta 1 en cada flanco en lugar de sumar.",
            "Sube 4, baja 2, sube 2, baja 3 y sube 4: termina en 5, que en binario es 0101. Enciende el segundo y el cuarto interruptor."
        }
    };

    [MenuItem(Menu + "Construir 3.5 Contrarreloj binaria")]
    public static void Build35()
    {
        const string rootName = "Reto_3.5";
        const int stage = 4;

        var bulbPrefab = Load(BulbPrefab);
        var leverPrefab = Load(LeverPrefab);
        var buttonPrefab = Load(ButtonPrefab);
        var torchModel = Load(TorchModel);
        var clockModel = Load(ClockModel);
        if (bulbPrefab == null || leverPrefab == null || buttonPrefab == null || torchModel == null || clockModel == null) return;

        var old = GameObject.Find(rootName);
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Construir 3.5",
                    $"Ya existe \"{rootName}\" en la escena. ¿Reemplazarlo?", "Reemplazar", "Cancelar"))
                return;
            Undo.DestroyObjectImmediate(old);
        }

        var root = new GameObject(rootName);
        Undo.RegisterCreatedObjectUndo(root, "Construir 3.5");
        root.transform.position = SpawnPoint();
        var puzzle = root.AddComponent<CountdownPuzzle>();
        var report = new StringBuilder();
        var bigClock = Object.FindObjectOfType<GameClock>();

        // ---- Columnas: barra de antorcha, bombilla encima y switch al frente (izquierda → derecha = MSB → LSB)
        const float spacing = 0.3f, barHeight = 1.0f, switchY = 0.9f, switchZ = -0.22f;
        var bulbs = new BitBulb[4];
        var levers = new XRSwitchToggle[4];
        var columns = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            float x = (i - 1.5f) * spacing;
            columns[i] = Column(root.transform, $"Bit_{3 - i}", x, torchModel, bulbPrefab, barHeight, out bulbs[i]);
            var lv = Spawn(leverPrefab, columns[i], "Switch", new Vector3(0f, switchY, switchZ));
            levers[i] = lv.GetComponentInChildren<XRSwitchToggle>(true);
        }
        var redCol = Column(root.transform, "Luz_Atras", 2.5f * spacing + 0.1f, torchModel, bulbPrefab, barHeight, out var red);
        var rso = new SerializedObject(red);
        rso.FindProperty("onEmission").colorValue = new Color(1f, 0.08f, 0.05f) * 2.5f;
        rso.ApplyModifiedPropertiesWithoutUndo();
        var redLight = red.GetComponentInChildren<Light>(true);
        if (redLight != null) { redLight.color = Color.red; PrefabUtility.RecordPrefabInstancePropertyModifications(redLight); }

        // ---- Botón (a la izquierda)
        var button = Spawn(buttonPrefab, root.transform, "Boton", new Vector3(-2.5f * spacing - 0.1f, switchY, switchZ));

        // ---- Reloj mini: el mismo modelo del reloj grande, copiando sus manecillas
        var mini = (GameObject)PrefabUtility.InstantiatePrefab(clockModel, root.transform);
        mini.name = "Reloj_Mini";
        FitHeight(mini, 0.4f);
        mini.transform.localPosition = new Vector3(0f, barHeight + 0.55f, 0.12f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(mini.transform);
        var mirror = mini.AddComponent<ClockMirror>();
        var mso = new SerializedObject(mirror);
        mso.FindProperty("source").objectReferenceValue = bigClock;
        if (bigClock != null)
        {
            mso.FindProperty("minuteHand").objectReferenceValue = FindChild(mini.transform, bigClock.MinuteHand != null ? bigClock.MinuteHand.name : null);
            mso.FindProperty("hourHand").objectReferenceValue = FindChild(mini.transform, bigClock.HourHand != null ? bigClock.HourHand.name : null);
            if (mso.FindProperty("minuteHand").objectReferenceValue == null)
                report.AppendLine("• No encontré las manecillas en el reloj mini: asígnalas a mano en ClockMirror.");
        }
        else report.AppendLine("• No hay GameClock en la escena: el reloj mini no se moverá.");
        mso.ApplyModifiedPropertiesWithoutUndo();

        var countSource = mini.AddComponent<AudioSource>();
        countSource.playOnAwake = false;
        countSource.spatialBlend = 1f;

        // ---- El reto
        var tracker = Object.FindObjectOfType<PerformanceTracker>();
        var hints = Object.FindObjectOfType<HintManager>();
        var engine = Object.FindObjectOfType<AdaptationEngine>();
        var dust = AssetDatabase.LoadAssetAtPath<GameObject>(DustPrefab);

        var so = new SerializedObject(puzzle);
        so.FindProperty("tracker").objectReferenceValue = tracker;
        so.FindProperty("hintManager").objectReferenceValue = hints;
        so.FindProperty("stageIndex").intValue = stage;
        so.FindProperty("clock").objectReferenceValue = bigClock;
        SetArray(so.FindProperty("bitBulbs"), bulbs);
        SetArray(so.FindProperty("switches"), levers);
        SetArray(so.FindProperty("bitColumns"), columns);
        so.FindProperty("redBulb").objectReferenceValue = red;
        so.FindProperty("redColumn").objectReferenceValue = redCol;
        so.FindProperty("button").objectReferenceValue = button.GetComponentInChildren<XRPhysicalButton>(true);
        so.FindProperty("countSource").objectReferenceValue = countSource;
        so.FindProperty("beepClip").objectReferenceValue = FindClip("beep");
        so.FindProperty("tockClip").objectReferenceValue = FindClip("clock_tock");
        if (dust != null) so.FindProperty("sinkEffect").objectReferenceValue = dust.GetComponent<ParticleSystem>();
        so.ApplyModifiedPropertiesWithoutUndo();
        if (FindClip("beep") == null) report.AppendLine("• No encontré el audio \"beep\".");

        // ---- Sonidos de acierto y error
        PlayOn(puzzle.onSolved, "SuccessPlayer", report);
        PlayOn(puzzle.onIncorrect, "FailurePlayer", report);

        // ---- Zona: al entrar se fija la versión y se bloquea 3.2
        var zone = MakeHintZone(root.transform, hints, tracker, stage);
        var zso = new SerializedObject(zone);
        zso.FindProperty("gate").objectReferenceValue = puzzle;
        zso.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(zone.onPlayerEnter, puzzle.PlayerEntered);
        UnityEventTools.AddPersistentListener(zone.onPlayerExit, puzzle.PlayerExited);

        var cable = FindCableSocket(stage);
        if (cable != null)
        {
            UnityEventTools.AddPersistentListener(cable.onConnected, puzzle.Unlock);
            report.AppendLine("• Se desbloquea al conectar el cable de la pareja Registros.");
        }
        else report.AppendLine($"• No encontré el ClockCableSocket con Stage Index {stage}: conecta su On Connected a CountdownPuzzle.Unlock a mano.");

        SetExclusion("Exclusion_Registros", puzzle, false, report);
        FreezeHourHandFor(bigClock, stage, report);

        // El tiempo lo marca el reloj: pesos propios sin tiempo
        ConfigureTracker(tracker, stage, "3.5", new[] { 40f, 75f, 85f }, new[] { 3, 5, 4 }, report, new[] { 0f, 0.45f, 0.20f, 0.35f });
        ConfigureEngine(engine, stage, "3.5", puzzle, report);
        ConfigureHints(hints, stage, "3.5", puzzle, Hints35, report);
        SetupGuide(report, false);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);

        report.Insert(0, "Reto 3.5 construido.\n\n");
        report.AppendLine("\nMuévelo a su lugar en la sala. En 3.5.1 las dos columnas de la izquierda y la luz roja se hunden " +
                          "cuando el jugador entra a la zona.");
        EditorUtility.DisplayDialog("Construir 3.5", report.ToString(), "OK");
    }

    /// <summary>Una barra de antorcha (sin fuego) con una bombilla encima. Devuelve la raíz de la columna.</summary>
    private static Transform Column(Transform parent, string name, float x, GameObject torchModel, GameObject bulbPrefab, float height, out BitBulb bulb)
    {
        var col = new GameObject(name).transform;
        col.SetParent(parent, false);
        col.localPosition = new Vector3(x, 0f, 0f);

        var bar = (GameObject)PrefabUtility.InstantiatePrefab(torchModel, col);
        bar.name = "Barra";
        FitHeight(bar, height);
        SitOnFloor(bar, col);

        var b = Spawn(bulbPrefab, col, "Bombilla", new Vector3(0f, height + 0.06f, 0f));
        bulb = ToBitBulb(b);
        return col;
    }

    /// <summary>Escala un objeto para que mida esa altura (según sus renderers).</summary>
    private static void FitHeight(GameObject go, float height)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        if (b.size.y <= 0.0001f) return;
        go.transform.localScale = go.transform.localScale * (height / b.size.y);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
    }

    /// <summary>Baja o sube el objeto para que su base quede en el suelo de la columna.</summary>
    private static void SitOnFloor(GameObject go, Transform column)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        go.transform.position = go.transform.position + new Vector3(0f, column.position.y - b.min.y, 0f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
    }

    private static Transform FindChild(Transform root, string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    private static AudioClip FindClip(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " t:AudioClip"))
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
            if (clip != null && clip.name == name) return clip;
        }
        return null;
    }

    /// <summary>Conecta un evento al Play del AudioSource de un objeto de la escena (SuccessPlayer, FailurePlayer).</summary>
    private static void PlayOn(UnityEngine.Events.UnityEvent evt, string objectName, StringBuilder report)
    {
        var go = GameObject.Find(objectName);
        var src = go != null ? go.GetComponent<AudioSource>() : null;
        if (src == null) { report.AppendLine($"• No encontré {objectName} con un Audio Source."); return; }
        UnityEventTools.AddPersistentListener(evt, src.Play);
    }

    /// <summary>En el perfil del reloj para este reto, el horario queda quieto (el jugador cuenta de memoria).</summary>
    private static void FreezeHourHandFor(GameClock clock, int stage, StringBuilder report)
    {
        if (clock == null) return;
        var so = new SerializedObject(clock);
        var profiles = so.FindProperty("profiles");
        for (int i = 0; i < profiles.arraySize; i++)
        {
            var p = profiles.GetArrayElementAtIndex(i);
            if (p.FindPropertyRelative("stageIndex").intValue != stage) continue;
            p.FindPropertyRelative("freezeHourHand").boolValue = true;
            so.ApplyModifiedProperties();
            return;
        }
        report.AppendLine($"• El GameClock no tiene perfil para el reto {stage}: agrégalo (periodo 4, 4, 4 y Freeze Hour Hand).");
    }


    // ================================================================ 3.3

    private static readonly string[][] Hints33 =
    {
        new[]
        {
            "",
            "Haz que la luz encendida recorra la secuencia de colores que aparece bajo el cuadro, empezando en rojo. Los interruptores eligen el camino, pero el cambio solo ocurre en el siguiente tic del reloj.",
            "Es una máquina de estados finitos: cada color es un estado y cada combinación de los dos interruptores es una entrada. En cada flanco del reloj la máquina lee las entradas y sigue la flecha que corresponde en el diagrama del cuadro.",
            "Desde rojo: 11 (azul), luego 01 (amarillo), luego 10 (verde) y por último 11 (amarillo). Deja lista cada combinación antes del siguiente tic."
        },
        new[]
        {
            "",
            "Haz que la luz encendida recorra la secuencia de colores que aparece bajo el cuadro, empezando en rojo. El cambio solo ocurre en el siguiente tic del reloj, y esta vez el reloj va más rápido.",
            "Es una máquina de estados finitos: en cada flanco del reloj lee los interruptores y sigue la flecha que corresponde en el diagrama. La entrada 00 mantiene el estado en el que estás.",
            "Desde rojo: 01 (verde), 01 (rojo), 10 (amarillo), 11 (rojo), 11 (azul) y 00 (azul)."
        },
        new[]
        {
            "",
            "Haz que la luz encendida recorra la secuencia de colores que aparece bajo el cuadro, empezando en rojo. Ojo: esta máquina tiene otras flechas.",
            "Es una máquina de estados finitos: en cada flanco del reloj lee los interruptores y sigue la flecha que corresponde. No te guíes por un patrón: revisa el diagrama en cada paso. La entrada 00 mantiene el estado.",
            "Desde rojo: 11 (amarillo), 01 (verde), 00 (verde), 10 (azul), 01 (rojo) y 11 (amarillo)."
        }
    };

    private static readonly Color[] StateColors =
    {
        new Color(1f, 0.1f, 0.05f),    // rojo
        new Color(1f, 0.82f, 0.1f),    // amarillo
        new Color(0.15f, 1f, 0.25f),   // verde
        new Color(0.15f, 0.4f, 1f),    // azul
    };

    [MenuItem(Menu + "Construir 3.3 El guardián de estados")]
    public static void Build33()
    {
        const string rootName = "Reto_3.3";
        const int stage = 2;

        var tablePrefab = Load(TablePrefab);
        var bulbPrefab = Load(BulbPrefab);
        var leverPrefab = Load(LeverPrefab);
        var clockModel = Load(ClockModel);
        if (tablePrefab == null || bulbPrefab == null || leverPrefab == null || clockModel == null) return;

        var report = new StringBuilder();
        var tapestry = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "Painting 2.mat");
        var fsmReg = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "FSM_Reg.mat");
        if (fsmReg == null) fsmReg = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "FMS_Reg.mat");
        var fsmIrr = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "FSM_Irr.mat");
        var glow = AssetDatabase.LoadAssetAtPath<Material>(GlowMaterial);
        if (tapestry == null) report.AppendLine("• No encontré el material Painting 2.");
        if (fsmReg == null) report.AppendLine("• No encontré el material FSM_Reg (ni FMS_Reg).");
        if (fsmIrr == null) report.AppendLine("• No encontré el material FSM_Irr.");

        var old = GameObject.Find(rootName);
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Construir 3.3",
                    $"Ya existe \"{rootName}\" en la escena. ¿Reemplazarlo?", "Reemplazar", "Cancelar"))
                return;
            Undo.DestroyObjectImmediate(old);
        }

        var root = new GameObject(rootName);
        Undo.RegisterCreatedObjectUndo(root, "Construir 3.3");
        root.transform.position = SpawnPoint();
        var puzzle = root.AddComponent<StateMachinePuzzle>();
        var bigClock = Object.FindObjectOfType<GameClock>();

        // ---- La mesa de 1.2, con su cuadro
        var table = Spawn(tablePrefab, root.transform, "Mesa", Vector3.zero);
        Transform frame = null, plane = null, oldBulb = null;
        foreach (var t in table.GetComponentsInChildren<Transform>(true))
        {
            if (frame == null && t.name.ToLower().StartsWith("frame")) frame = t;
            if (plane == null && t.name == "Plane") plane = t;
            if (oldBulb == null && t.name.StartsWith("Low_Poly_Light_Bulb")) oldBulb = t;
        }
        var painting = plane != null ? plane.GetComponent<Renderer>() : null;
        if (painting == null) report.AppendLine("• No encontré el objeto Plane del cuadro: asigna Painting a mano.");
        else if (tapestry != null)
        {
            painting.sharedMaterial = tapestry;
            PrefabUtility.RecordPrefabInstancePropertyModifications(painting);
        }
        if (oldBulb != null)
        {
            oldBulb.gameObject.SetActive(false);   // la de 1.2 (con FixedInputCircuit): la reemplazan las cuatro de colores
            PrefabUtility.RecordPrefabInstancePropertyModifications(oldBulb.gameObject);
        }

        var tb = RendererBounds(table.GetComponent<Renderer>() != null ? new[] { table.GetComponent<Renderer>() } : table.GetComponentsInChildren<Renderer>());
        float top = tb.max.y, front = tb.min.z, cx = tb.center.x;
        Vector3 Local(Vector3 world) => root.transform.InverseTransformPoint(world);

        // ---- Cuatro bombillas de colores sobre la mesa (rojo, amarillo, verde, azul)
        var bulbs = new BitBulb[4];
        string[] names = { "Rojo", "Amarillo", "Verde", "Azul" };
        for (int i = 0; i < 4; i++)
        {
            var b = Spawn(bulbPrefab, root.transform, "Bombilla_" + names[i], Local(new Vector3(cx + (i - 1.5f) * 0.16f, top, tb.center.z - tb.extents.z * 0.35f)));
            SitOnFloorAt(b, top);
            bulbs[i] = ToBitBulb(b);
            var bso = new SerializedObject(bulbs[i]);
            bso.FindProperty("onEmission").colorValue = StateColors[i] * 2.5f;
            bso.ApplyModifiedPropertiesWithoutUndo();
            var l = b.GetComponentInChildren<Light>(true);
            if (l != null) { l.color = StateColors[i]; PrefabUtility.RecordPrefabInstancePropertyModifications(l); }
        }

        // ---- Dos switches al frente (izquierda = bit más significativo)
        var left = Spawn(leverPrefab, root.transform, "Switch_Izquierda", Local(new Vector3(cx - 0.12f, top, front + 0.08f)));
        var right = Spawn(leverPrefab, root.transform, "Switch_Derecha", Local(new Vector3(cx + 0.12f, top, front + 0.08f)));

        // ---- Reloj mini en una esquina de la mesa
        var mini = MiniClock(root.transform, clockModel, bigClock, 0.3f, Local(new Vector3(tb.max.x - 0.15f, top + 0.2f, tb.center.z)), report);

        // ---- Secuencias: una fila de cuadros de colores por versión, bajo el cuadro
        var fb = frame != null ? RendererBounds(frame.GetComponentsInChildren<Renderer>()) : tb;
        var seqRoot = new GameObject("Secuencia").transform;
        seqRoot.SetParent(root.transform, false);
        seqRoot.position = new Vector3(fb.center.x, fb.min.y - 0.06f, fb.min.z - 0.02f);
        string[] sequences = { "BYGY", "GRYRBB", "YGGBRY" };
        var rows = new GameObject[3];
        for (int v = 0; v < 3; v++)
        {
            rows[v] = SequenceRow(seqRoot, "V" + (v + 1), sequences[v], glow);
            rows[v].SetActive(v == 1);   // en el editor se ve V2; al jugar manda la versión
        }

        // ---- El reto
        var tracker = Object.FindObjectOfType<PerformanceTracker>();
        var hints = Object.FindObjectOfType<HintManager>();
        var engine = Object.FindObjectOfType<AdaptationEngine>();
        var magic = AssetDatabase.LoadAssetAtPath<GameObject>(MagicPrefab);

        var so = new SerializedObject(puzzle);
        so.FindProperty("tracker").objectReferenceValue = tracker;
        so.FindProperty("hintManager").objectReferenceValue = hints;
        so.FindProperty("stageIndex").intValue = stage;
        so.FindProperty("clock").objectReferenceValue = bigClock;
        SetArray(so.FindProperty("stateBulbs"), bulbs);
        so.FindProperty("leftSwitch").objectReferenceValue = left.GetComponentInChildren<XRSwitchToggle>(true);
        so.FindProperty("rightSwitch").objectReferenceValue = right.GetComponentInChildren<XRSwitchToggle>(true);
        so.FindProperty("painting").objectReferenceValue = painting;
        so.FindProperty("tapestry").objectReferenceValue = tapestry;
        if (magic != null) so.FindProperty("revealEffect").objectReferenceValue = magic.GetComponent<ParticleSystem>();
        var vers = so.FindProperty("versions");
        Material[] diagrams = { fsmReg, fsmReg, fsmIrr };
        for (int v = 0; v < 3 && v < vers.arraySize; v++)
        {
            vers.GetArrayElementAtIndex(v).FindPropertyRelative("diagram").objectReferenceValue = diagrams[v];
            vers.GetArrayElementAtIndex(v).FindPropertyRelative("sequenceRow").objectReferenceValue = rows[v];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        PlayOn(puzzle.onSolved, "SuccessPlayer", report);
        PlayOn(puzzle.onIncorrect, "FailurePlayer", report);

        // ---- Zona: al entrar se fija la versión, aparece el diagrama y se bloquea 3.4
        var zone = MakeHintZone(root.transform, hints, tracker, stage);
        var zso = new SerializedObject(zone);
        zso.FindProperty("gate").objectReferenceValue = puzzle;
        zso.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(zone.onPlayerEnter, puzzle.PlayerEntered);
        UnityEventTools.AddPersistentListener(zone.onPlayerExit, puzzle.PlayerExited);

        var cable = FindCableSocket(stage);
        if (cable != null)
        {
            UnityEventTools.AddPersistentListener(cable.onConnected, puzzle.Unlock);
            report.AppendLine("• Se desbloquea al conectar el cable de la pareja Estados.");
        }
        else report.AppendLine($"• No encontré el ClockCableSocket con Stage Index {stage}: conecta su On Connected a StateMachinePuzzle.Unlock a mano.");

        SetExclusion("Exclusion_Estados", puzzle, true, report);
        SetClockPeriods(bigClock, stage, new[] { 8f, 4f, 4f }, report);

        ConfigureTracker(tracker, stage, "3.3", new[] { 32f, 24f, 24f }, new[] { 6, 6, 8 }, report, new[] { 0f, 0.45f, 0.20f, 0.35f });
        ConfigureEngine(engine, stage, "3.3", puzzle, report);
        ConfigureHints(hints, stage, "3.3", puzzle, Hints33, report);
        SetupGuide(report, false);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);

        report.Insert(0, "Reto 3.3 construido.\n\n");
        report.AppendLine("\nMuévelo a su lugar en la sala. Revisa la posición de las bombillas, los switches, el reloj mini " +
                          "y la fila \"Secuencia\" sobre la mesa: se calcularon con el tamaño de la mesa.");
        EditorUtility.DisplayDialog("Construir 3.3", report.ToString(), "OK");
    }

    /// <summary>Fila de cuadros de colores: el rojo inicial (más pequeño, separado) y luego los pasos de la secuencia.</summary>
    private static GameObject SequenceRow(Transform parent, string name, string sequence, Material mat)
    {
        var row = new GameObject(name);
        row.transform.SetParent(parent, false);
        const float size = 0.045f, gap = 0.06f;
        float total = 0.05f + sequence.Length * gap;
        float x = -total * 0.5f;

        Swatch(row.transform, "Inicio_R", new Vector3(x, 0f, 0f), size * 0.6f, StateColors[0], mat);
        x += 0.05f + gap * 0.5f;
        for (int i = 0; i < sequence.Length; i++)
        {
            int c = StateMachinePuzzle.Colors.IndexOf(sequence[i]);
            Swatch(row.transform, $"Paso_{i + 1}_{sequence[i]}", new Vector3(x + i * gap, 0f, 0f), size, StateColors[c], mat);
        }
        return row;
    }

    private static void Swatch(Transform parent, string name, Vector3 pos, float size, Color color, Material mat)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Cube);
        q.name = name;
        q.transform.SetParent(parent, false);
        q.transform.localPosition = pos;
        q.transform.localScale = new Vector3(size, size, 0.01f);
        Object.DestroyImmediate(q.GetComponent<Collider>());
        if (mat != null) q.GetComponent<Renderer>().sharedMaterial = mat;
        q.AddComponent<ColorSwatch>().SetColor(color);
    }

    /// <summary>Reloj mini: el mismo modelo del reloj grande, copiando sus manecillas.</summary>
    private static GameObject MiniClock(Transform parent, GameObject clockModel, GameClock bigClock, float height, Vector3 localPos, StringBuilder report)
    {
        var mini = (GameObject)PrefabUtility.InstantiatePrefab(clockModel, parent);
        mini.name = "Reloj_Mini";
        FitHeight(mini, height);
        mini.transform.localPosition = localPos;
        PrefabUtility.RecordPrefabInstancePropertyModifications(mini.transform);
        var mirror = mini.AddComponent<ClockMirror>();
        var mso = new SerializedObject(mirror);
        mso.FindProperty("source").objectReferenceValue = bigClock;
        if (bigClock != null)
        {
            mso.FindProperty("minuteHand").objectReferenceValue = FindChild(mini.transform, bigClock.MinuteHand != null ? bigClock.MinuteHand.name : null);
            mso.FindProperty("hourHand").objectReferenceValue = FindChild(mini.transform, bigClock.HourHand != null ? bigClock.HourHand.name : null);
            if (mso.FindProperty("minuteHand").objectReferenceValue == null)
                report.AppendLine("• No encontré las manecillas en el reloj mini: asígnalas a mano en ClockMirror.");
        }
        mso.ApplyModifiedPropertiesWithoutUndo();
        return mini;
    }

    private static Bounds RendererBounds(Renderer[] rs)
    {
        var b = new Bounds();
        bool first = true;
        foreach (var r in rs)
        {
            if (r == null) continue;
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }
        return b;
    }

    /// <summary>Apoya el objeto sobre una altura (la base de sus renderers queda en y).</summary>
    private static void SitOnFloorAt(GameObject go, float y)
    {
        var b = RendererBounds(go.GetComponentsInChildren<Renderer>(true));
        go.transform.position = go.transform.position + new Vector3(0f, y - b.min.y, 0f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
    }

    /// <summary>Periodos del reloj para un reto (crea el perfil si no existe).</summary>
    private static void SetClockPeriods(GameClock clock, int stage, float[] periods, StringBuilder report)
    {
        if (clock == null) return;
        var so = new SerializedObject(clock);
        var profiles = so.FindProperty("profiles");
        SerializedProperty p = null;
        for (int i = 0; i < profiles.arraySize; i++)
            if (profiles.GetArrayElementAtIndex(i).FindPropertyRelative("stageIndex").intValue == stage) p = profiles.GetArrayElementAtIndex(i);
        if (p == null)
        {
            profiles.arraySize++;
            p = profiles.GetArrayElementAtIndex(profiles.arraySize - 1);
            p.FindPropertyRelative("stageIndex").intValue = stage;
            p.FindPropertyRelative("freezeHourHand").boolValue = false;
            SetArray(p.FindPropertyRelative("cableRenderers"), new Object[0]);
            report.AppendLine($"• Creé el perfil del reloj para el reto {stage}: agrégale los renderers de su cable.");
        }
        var arr = p.FindPropertyRelative("periodByVersion");
        arr.arraySize = periods.Length;
        for (int i = 0; i < periods.Length; i++) arr.GetArrayElementAtIndex(i).floatValue = periods[i];
        so.ApplyModifiedProperties();
    }

    // ================================================================ Guía del nivel

    [MenuItem(Menu + "Crear o actualizar la guía del nivel")]
    public static void BuildGuide()
    {
        var report = new StringBuilder();
        var g = SetupGuide(report, true);
        if (g != null) Selection.activeGameObject = g.gameObject;
        EditorUtility.DisplayDialog("Guía del nivel", report.ToString(), "OK");
    }

    /// <summary>
    /// Crea la guía si no existe (o solo la actualiza) y la conecta: tracker, retos 3.2–3.5, huecos del reloj
    /// y el campo Guide del HintManager. No toca los textos que ya hayas escrito.
    /// </summary>
    private static Level3Guide SetupGuide(StringBuilder report, bool create)
    {
        var guide = Object.FindObjectOfType<Level3Guide>();
        if (guide == null)
        {
            if (!create) return null;
            var go = new GameObject("Guia_Nivel3");
            Undo.RegisterCreatedObjectUndo(go, "Guía del nivel");
            guide = go.AddComponent<Level3Guide>();
            report.AppendLine("• Guía creada (objeto Guia_Nivel3). Sus textos se editan en el Inspector.");
        }

        var tracker = Object.FindObjectOfType<PerformanceTracker>();
        var hints = Object.FindObjectOfType<HintManager>();
        var puzzles = Object.FindObjectsOfType<Level3Puzzle>();
        var sockets = Object.FindObjectsOfType<ClockCableSocket>();

        var so = new SerializedObject(guide);
        so.FindProperty("tracker").objectReferenceValue = tracker;
        SetArray(so.FindProperty("puzzles"), puzzles);
        SetArray(so.FindProperty("cableSockets"), sockets);
        so.ApplyModifiedPropertiesWithoutUndo();

        if (hints != null)
        {
            var hso = new SerializedObject(hints);
            hso.FindProperty("guide").objectReferenceValue = guide;
            hso.FindProperty("tracker").objectReferenceValue = tracker;
            hso.ApplyModifiedPropertiesWithoutUndo();
        }
        else report.AppendLine("• No hay HintManager en la escena.");

        report.AppendLine($"• Guía conectada: {puzzles.Length} reto(s) y {sockets.Length} hueco(s) de cable.");
        EditorSceneManager.MarkSceneDirty(guide.gameObject.scene);
        return guide;
    }

    // ================================================================ Conexiones con el sistema

    private static void ConfigureTracker(PerformanceTracker tracker, int stage, string name, float[] t, int[] i, StringBuilder report)
        => ConfigureTracker(tracker, stage, name, t, i, report, null);

    /// <param name="weights">Pesos propios (tiempo, intentos, interacciones, pistas) o null para usar los globales.</param>
    private static void ConfigureTracker(PerformanceTracker tracker, int stage, string name, float[] t, int[] i, StringBuilder report, float[] weights)
    {
        if (tracker == null) { report.AppendLine("• No hay PerformanceTracker en la escena."); return; }
        var so = new SerializedObject(tracker);
        so.FindProperty("level").intValue = 3;
        var stages = so.FindProperty("stages");
        EnsureSize(stages, 5);
        var s = stages.GetArrayElementAtIndex(stage);
        s.FindPropertyRelative("name").stringValue = name;
        s.FindPropertyRelative("startOnZoneEnter").boolValue = false;
        s.FindPropertyRelative("hasSubmit").boolValue = false;
        var byVersion = s.FindPropertyRelative("byVersion");
        byVersion.arraySize = 3;
        for (int v = 0; v < 3; v++)
        {
            var ideal = byVersion.GetArrayElementAtIndex(v);
            ideal.FindPropertyRelative("tIdeal").floatValue = t[v];
            ideal.FindPropertyRelative("iIdeal").intValue = i[v];
        }
        s.FindPropertyRelative("overrideWeights").boolValue = weights != null;
        if (weights != null)
        {
            s.FindPropertyRelative("wTime").floatValue = weights[0];
            s.FindPropertyRelative("wAttempts").floatValue = weights[1];
            s.FindPropertyRelative("wInteractions").floatValue = weights[2];
            s.FindPropertyRelative("wHints").floatValue = weights[3];
        }
        so.ApplyModifiedProperties();
        report.AppendLine($"• Tracker: reto {stage} = {name} con sus valores ideales.");
    }

    private static void ConfigureEngine(AdaptationEngine engine, int stage, string name, MonoBehaviour target, StringBuilder report)
    {
        if (engine == null) { report.AppendLine("• No hay AdaptationEngine en la escena."); return; }
        var so = new SerializedObject(engine);
        var bindings = so.FindProperty("bindings");
        EnsureSize(bindings, 5);
        var b = bindings.GetArrayElementAtIndex(stage);
        b.FindPropertyRelative("name").stringValue = name;
        b.FindPropertyRelative("versionTarget").objectReferenceValue = target;
        so.ApplyModifiedProperties();
        report.AppendLine($"• Motor: el binding {stage} apunta a {name}.");
    }

    private static void ConfigureHints(HintManager hints, int stage, string name, MonoBehaviour source, string[][] texts, StringBuilder report)
    {
        if (hints == null) { report.AppendLine("• No hay HintManager en la escena."); return; }
        var so = new SerializedObject(hints);
        var stages = so.FindProperty("stages");
        EnsureSize(stages, 5);
        var s = stages.GetArrayElementAtIndex(stage);
        s.FindPropertyRelative("name").stringValue = name;
        s.FindPropertyRelative("versionSource").objectReferenceValue = source;
        s.FindPropertyRelative("skipFirstHint").boolValue = false;

        var byVersion = s.FindPropertyRelative("byVersion");
        byVersion.arraySize = 3;
        bool kept = false;
        for (int v = 0; v < 3; v++)
        {
            var arr = byVersion.GetArrayElementAtIndex(v).FindPropertyRelative("hints");
            arr.arraySize = 4;
            for (int h = 0; h < 4; h++)
            {
                var p = arr.GetArrayElementAtIndex(h);
                if (string.IsNullOrEmpty(p.stringValue)) p.stringValue = texts[v][h];
                else kept = true;   // no se pisan textos que ya hayas escrito
            }
        }
        so.ApplyModifiedProperties();
        report.AppendLine($"• Pistas: {name} conectado" + (kept ? " (se conservaron los textos que ya había)." : " con las 12 pistas."));
    }

    private static HintZone MakeHintZone(Transform root, HintManager hints, PerformanceTracker tracker, int stage)
    {
        var zone = new GameObject("HintZone");
        zone.transform.SetParent(root, false);
        zone.transform.localPosition = new Vector3(0f, 1f, -1f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2.5f, 2f, 2.5f);
        var hz = zone.AddComponent<HintZone>();
        var so = new SerializedObject(hz);
        so.FindProperty("manager").objectReferenceValue = hints;
        so.FindProperty("tracker").objectReferenceValue = tracker;
        so.FindProperty("stageIndex").intValue = stage;
        so.ApplyModifiedPropertiesWithoutUndo();
        return hz;
    }

    // ================================================================ Utilidades

    private static GameObject Load(string path)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go == null)
            EditorUtility.DisplayDialog("Nivel 3", $"No se encontró el prefab:\n{path}\n\nRevisa que exista con ese nombre.", "OK");
        return go;
    }

    private static GameObject Spawn(GameObject prefab, Transform parent, string name, Vector3 localPos)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = name;
        go.transform.localPosition = localPos;   // conserva la rotación y escala del prefab
        PrefabUtility.RecordPrefabInstancePropertyModifications(go);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        return go;
    }

    private static float Column(int i, int count, float spacing) => (i - (count - 1) * 0.5f) * spacing;

    /// <summary>Ancho del prefab en X según sus renderers (para espaciar).</summary>
    private static float Width(GameObject prefab)
    {
        var tmp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        float w = 0.2f;
        var rs = tmp.GetComponentsInChildren<Renderer>(true);
        if (rs.Length > 0)
        {
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            w = b.size.x;
        }
        Object.DestroyImmediate(tmp);
        return w;
    }

    private static Vector3 SpawnPoint()
    {
        var view = SceneView.lastActiveSceneView;
        if (view == null) return Vector3.zero;
        var p = view.pivot;
        return new Vector3(p.x, 0f, p.z);
    }

    private static void SetText(TMP_Text t, string value)
    {
        t.text = value;
        PrefabUtility.RecordPrefabInstancePropertyModifications(t);
    }

    /// <summary>Apaga un componente del prefab (no se borra: así el prefab original queda intacto).</summary>
    private static void Disable(Behaviour b)
    {
        b.enabled = false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(b);
    }

    private static void SetArray(SerializedProperty prop, Object[] values)
    {
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static void EnsureSize(SerializedProperty array, int size)
    {
        if (array.arraySize < size) array.arraySize = size;
    }
}
