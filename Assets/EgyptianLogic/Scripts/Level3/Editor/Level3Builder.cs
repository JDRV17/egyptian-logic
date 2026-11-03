using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
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
        tmp.text = "Guarda: " + string.Join(" ", code.ToCharArray());
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

        var bit = go.AddComponent<BitCube>();
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

    // ================================================================ Conexiones con el sistema

    private static void ConfigureTracker(PerformanceTracker tracker, int stage, string name, float[] t, int[] i, StringBuilder report)
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

    private static void MakeHintZone(Transform root, HintManager hints, PerformanceTracker tracker, int stage)
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
