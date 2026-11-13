using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Menú "Egyptian Logic > Menú final". Arma en la escena abierta la tablet del menú final (resultados y envío
/// del informe) con su marco, y cuatro antorchas decorativas. Después solo hay que ajustar posiciones.
/// </summary>
public static class FinalMenuBuilder
{
    private const string Menu = "Egyptian Logic/Menú final/";
    private const string TabletRootName = "Tablet_MenuFinal";
    private const string TorchRootName = "Antorchas_MenuFinal";
    private const string FinalSceneName = "EgyptFinalMenu";

    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Eczar-VariableFont_wght SDF.asset";
    private const string PrefabFolder = "Assets/EgyptianLogic/Prefabs";
    private const string BaseDoorFrame = "Assets/Prefabs/9 Doors and Drawers/DoorFrame.prefab";
    private const string TorchPrefab = PrefabFolder + "/Antorcha.prefab";
    private const string TorchModel = "Assets/EgyptianLogic/Models/torch.fbx";
    private const string FlameMaterial = "Assets/EgyptianLogic/Materials/Flame.mat";

    // Pantalla: 800 × 1150 px a 1.3 mm por px ≈ 1.04 × 1.5 m.
    private const float W = 800f, H = 1150f, MetersPerPixel = 0.0013f;
    private const float CW = 720f;              // ancho útil del contenido
    private const float FrameBorder = 0.07f;    // cuánto sobresale el marco alrededor de la pantalla (m)
    private const float FrameDepth = 0.05f;

    // Paleta: pantalla oscura con dorado arena.
    private static readonly Color ScreenBg = Hex(0x17120C);
    private static readonly Color CardBg = Hex(0x241C13);
    private static readonly Color Gold = Hex(0xD9B25F);
    private static readonly Color Body = Hex(0xF1E8D6);
    private static readonly Color Muted = Hex(0xA8987C);
    private static readonly Color KeyBg = Hex(0x2F2518);
    private static readonly Color BarBg = Hex(0x3A2E1E);
    private static readonly Color DarkText = Hex(0x1B140C);

    private static TMP_FontAsset font;

    // ================================================================ Menús

    [MenuItem(Menu + "Construir tablet y antorchas")]
    public static void BuildAll()
    {
        BuildTablet();
        BuildTorches();
    }

    [MenuItem(Menu + "Construir solo la tablet")]
    public static void BuildTablet()
    {
        if (!ConfirmReplace(TabletRootName, "Construir tablet")) return;
        DisableOldMenu();

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) font = TMP_Settings.defaultFontAsset;

        var root = new GameObject(TabletRootName);
        Undo.RegisterCreatedObjectUndo(root, "Construir tablet");

        // Se arma en el origen y sin rotación; al final se mueve a su lugar.
        BuildFrame(root.transform);
        var canvas = BuildCanvas(root.transform);

        var controller = root.AddComponent<FinalMenuController>();
        var mailer = root.AddComponent<ReportMailer>();
        CopyBaseEmailSettings(mailer);
        controller.mailer = mailer;
        controller.pcPlayer = Object.FindObjectOfType<PC_playerController>();

        var bg = Img(Stretch(Node(canvas, "Fondo")), ScreenBg);
        bg.raycastTarget = true;   // que el rayo choque con la pantalla y no la atraviese

        controller.resultsScreen = BuildResults(canvas, controller);
        controller.sendScreen = BuildSend(canvas, controller);

        EnsureEventSystem();
        EnsureInBuildSettings(SceneManager.GetActiveScene().path);

        bool finalRoom = SceneManager.GetActiveScene().name == FinalSceneName;
        root.transform.position = finalRoom ? new Vector3(0f, 1.55f, 2.86f) : SpawnPoint() + new Vector3(0f, 1.55f, 0f);
        root.transform.rotation = Quaternion.identity;   // se lee desde -Z (mirando hacia +Z)

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("[FinalMenuBuilder] Tablet construida. Revisa el marco (\"Marco\") y el correo remitente en ReportMailer.");
    }

    [MenuItem(Menu + "Construir solo las antorchas")]
    public static void BuildTorches()
    {
        if (!ConfirmReplace(TorchRootName, "Construir antorchas")) return;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TorchPrefab);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(TorchModel);
        if (prefab == null && model == null)
        {
            EditorUtility.DisplayDialog("Antorchas", $"No se encontró {TorchPrefab} ni {TorchModel}.", "OK");
            return;
        }

        var root = new GameObject(TorchRootName);
        Undo.RegisterCreatedObjectUndo(root, "Construir antorchas");

        bool finalRoom = SceneManager.GetActiveScene().name == FinalSceneName;
        Vector3 origin = finalRoom ? Vector3.zero : SpawnPoint();

        // Dos a los lados de la tablet (pared +Z) y dos en la pared opuesta (−Z), inclinadas 30° hacia la sala.
        var spots = new[]
        {
            (pos: new Vector3(-1.3f, 1.9f, 2.85f), yaw: 0f),
            (pos: new Vector3(1.3f, 1.9f, 2.85f), yaw: 0f),
            (pos: new Vector3(-1.3f, 1.9f, -2.85f), yaw: 180f),
            (pos: new Vector3(1.3f, 1.9f, -2.85f), yaw: 180f)
        };

        for (int i = 0; i < spots.Length; i++)
        {
            var lean = Quaternion.Euler(0f, spots[i].yaw, 0f) * Quaternion.Euler(-30f, 0f, 0f);
            GameObject torch = prefab != null ? FromPrefab(prefab, root.transform) : FromModel(model, root.transform);
            torch.name = $"Antorcha_{i + 1}";
            torch.transform.position = origin + spots[i].pos;
            torch.transform.rotation = lean * torch.transform.rotation;   // la rotación base la deja vertical
        }

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log(prefab != null
            ? "[FinalMenuBuilder] Antorchas creadas desde Antorcha.prefab."
            : "[FinalMenuBuilder] Antorchas creadas desde torch.fbx (fuego, luz y parpadeo agregados).");
    }

    // ================================================================ Marco y canvas

    private static void BuildFrame(Transform root)
    {
        var prefab = FindFramePrefab();
        if (prefab == null)
        {
            Debug.LogWarning("[FinalMenuBuilder] No se encontró un prefab de marco; la tablet queda sin marco.");
            return;
        }

        var frame = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
        frame.name = "Marco";
        frame.transform.localPosition = Vector3.zero;

        // Escala el marco para que rodee la pantalla y queda justo detrás de ella (lado +Z).
        if (!TryBounds(frame, out var b)) return;
        var target = new Vector3(W * MetersPerPixel + 2f * FrameBorder, H * MetersPerPixel + 2f * FrameBorder, FrameDepth);
        var s = frame.transform.localScale;
        frame.transform.localScale = new Vector3(
            s.x * target.x / Mathf.Max(b.size.x, 1e-4f),
            s.y * target.y / Mathf.Max(b.size.y, 1e-4f),
            s.z * target.z / Mathf.Max(b.size.z, 1e-4f));

        TryBounds(frame, out b);
        frame.transform.localPosition += new Vector3(-b.center.x, -b.center.y, FrameDepth * 0.5f - b.center.z);
        PrefabUtility.RecordPrefabInstancePropertyModifications(frame.transform);
    }

    private static GameObject FindFramePrefab()
    {
        // 1) Un prefab con "frame" en el nombre dentro de EgyptianLogic/Prefabs; 2) el DoorFrame del proyecto base.
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Contains("frame"))
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return AssetDatabase.LoadAssetAtPath<GameObject>(BaseDoorFrame);
    }

    private static RectTransform BuildCanvas(Transform root)
    {
        var go = new GameObject("Pantalla", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(root, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        go.AddComponent<GraphicRaycaster>();
        go.AddComponent<TrackedDeviceGraphicRaycaster>();   // rayos de VR y el rayo del First Person

        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(W, H);
        rt.localScale = Vector3.one * MetersPerPixel;
        rt.localPosition = new Vector3(0f, 0f, -0.003f);
        rt.localRotation = Quaternion.identity;
        return rt;
    }

    // ================================================================ Pantalla de resultados

    private static CanvasGroup BuildResults(RectTransform canvas, FinalMenuController controller)
    {
        var screen = Stretch(Node(canvas, "Resultados"));
        var group = screen.gameObject.AddComponent<CanvasGroup>();
        var view = screen.gameObject.AddComponent<FinalResultsView>();
        controller.results = view;

        Txt(Top(Node(screen, "Titulo"), 0, 36, CW, 60), "Has salido de la pirámide", 46, Gold, TextAlignmentOptions.Center, FontStyles.Bold);
        view.greeting = Txt(Top(Node(screen, "Saludo"), 0, 100, CW, 40), "Informe del Dr.", 28, Body, TextAlignmentOptions.Center);
        Img(Top(Node(screen, "Separador"), 0, 152, CW, 2), new Color(Gold.r, Gold.g, Gold.b, 0.4f));

        // ---- Resumen
        var card = Top(Node(screen, "Resumen"), 0, 172, CW, 160);
        Img(card, CardBg);
        view.overallScore = Txt(Top(Node(card, "Puntaje"), -250, 14, 200, 92), "--", 88, Gold, TextAlignmentOptions.Center, FontStyles.Bold);
        Txt(Top(Node(card, "De100"), -250, 106, 200, 30), "de 100", 22, Muted, TextAlignmentOptions.Center);
        view.overallLabel = Txt(Top(Node(card, "Dominio"), 110, 22, 460, 44), "", 36, Gold, TextAlignmentOptions.Left, FontStyles.Bold);
        view.suggestedLevel = Txt(Top(Node(card, "NivelSugerido"), 110, 70, 460, 34), "", 26, Body, TextAlignmentOptions.Left);
        view.summaryLine = Txt(Top(Node(card, "Detalle"), 110, 108, 460, 30), "", 22, Muted, TextAlignmentOptions.Left);

        // ---- Por nivel
        Txt(Top(Node(screen, "TituloNiveles"), 0, 352, CW, 36), "Por nivel", 28, Gold, TextAlignmentOptions.Left, FontStyles.Bold);
        var levels = List(Top(Node(screen, "Niveles"), 0, 392, CW, 150), 6f);
        view.levelRowTemplate = LevelRow(levels);

        // ---- Lo que más costó
        Txt(Top(Node(screen, "TituloDificiles"), 0, 560, CW, 36), "Lo que más te costó", 28, Gold, TextAlignmentOptions.Left, FontStyles.Bold);
        var hard = List(Top(Node(screen, "Dificiles"), 0, 600, CW, 234), 6f);
        view.hardRowTemplate = HardRow(hard);
        view.emptyHardText = Txt(Top(Node(screen, "SinDificiles"), 0, 600, CW, 60), "", 24, Body, TextAlignmentOptions.Left);

        // ---- Temas
        Txt(Top(Node(screen, "TituloTemas"), 0, 850, CW, 36), "Temas para repasar", 28, Gold, TextAlignmentOptions.Left, FontStyles.Bold);
        view.topicsText = Txt(Top(Node(screen, "Temas"), 0, 890, CW, 145), "", 24, Body, TextAlignmentOptions.TopLeft);

        // ---- Botones
        var send = Btn(screen, "BotonEnviar", "Enviar informe", -220, 1056, 280, 66, Gold, DarkText, 28);
        var menu = Btn(screen, "BotonMenu", "Volver al menú", 60, 1056, 240, 66, KeyBg, Gold, 26);
        var quit = Btn(screen, "BotonSalir", "Salir", 280, 1056, 160, 66, KeyBg, Gold, 26);
        UnityEventTools.AddPersistentListener(send.onClick, controller.ShowSend);
        UnityEventTools.AddPersistentListener(menu.onClick, controller.BackToMainMenu);
        UnityEventTools.AddPersistentListener(quit.onClick, controller.Quit);

        return group;
    }

    private static RectTransform LevelRow(RectTransform list)
    {
        var row = Node(list, "Plantilla_Nivel");
        Row(row, 46f);
        Txt(Anchor(Node(row, "Nombre"), 0f, 0f, 0.62f, 1f), "Nivel", 24, Body, TextAlignmentOptions.Left);
        var bar = Anchor(Node(row, "Barra"), 0.64f, 0.3f, 0.9f, 0.7f);
        Img(bar, BarBg);
        Img(Anchor(Node(bar, "Relleno"), 0f, 0f, 0.5f, 1f), Gold);
        Txt(Anchor(Node(row, "Valor"), 0.91f, 0f, 1f, 1f), "0", 26, Body, TextAlignmentOptions.Right, FontStyles.Bold);
        row.gameObject.SetActive(false);
        return row;
    }

    private static RectTransform HardRow(RectTransform list)
    {
        var row = Node(list, "Plantilla_Dificil");
        Row(row, 74f);
        Img(Anchor(Node(row, "Acento"), 0f, 0.08f, 0f, 0.92f), Gold).rectTransform.sizeDelta = new Vector2(4f, 0f);
        var title = Txt(Anchor(Node(row, "Titulo"), 0f, 0.5f, 1f, 1f), "Reto", 25, Body, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        title.margin = new Vector4(16f, 0f, 0f, 0f);
        var why = Txt(Anchor(Node(row, "Motivo"), 0f, 0f, 1f, 0.5f), "", 21, Muted, TextAlignmentOptions.TopLeft);
        why.margin = new Vector4(16f, 2f, 0f, 0f);
        row.gameObject.SetActive(false);
        return row;
    }

    // ================================================================ Pantalla de envío

    private static CanvasGroup BuildSend(RectTransform canvas, FinalMenuController controller)
    {
        var screen = Stretch(Node(canvas, "EnviarInforme"));
        var group = screen.gameObject.AddComponent<CanvasGroup>();
        var keyboard = screen.gameObject.AddComponent<TabletKeyboard>();
        controller.keyboard = keyboard;

        Txt(Top(Node(screen, "Titulo"), 0, 36, CW, 60), "Enviar informe", 46, Gold, TextAlignmentOptions.Center, FontStyles.Bold);
        Txt(Top(Node(screen, "Descripcion"), 0, 100, CW, 64),
            "Te enviaremos este informe en PDF. Tu correo solo se usa para este envío y no se guarda.",
            23, Muted, TextAlignmentOptions.Center);

        Txt(Top(Node(screen, "EtiquetaNombre"), 0, 180, CW, 30), "Nombre", 22, Muted, TextAlignmentOptions.Left);
        controller.nameField = Field(screen, keyboard, "CampoNombre", 212, TabletField.Kind.Name, "Toca aquí para escribir tu nombre", 40);
        Txt(Top(Node(screen, "EtiquetaCorreo"), 0, 290, CW, 30), "Correo", 22, Muted, TextAlignmentOptions.Left);
        controller.emailField = Field(screen, keyboard, "CampoCorreo", 322, TabletField.Kind.Email, "Toca aquí para escribir tu correo", 64);
        keyboard.fields = new List<TabletField> { controller.nameField, controller.emailField };

        Keys(Top(Node(screen, "Teclado"), 0, 410, CW, 392), keyboard);

        controller.statusText = Txt(Top(Node(screen, "Estado"), 0, 820, CW, 70), "", 24, Muted, TextAlignmentOptions.Center);

        var send = Btn(screen, "BotonEnviar", "Enviar", -185, 910, 350, 70, Gold, DarkText, 30);
        var back = Btn(screen, "BotonVolver", "Volver", 185, 910, 350, 70, KeyBg, Gold, 28);
        controller.sendButton = send;
        UnityEventTools.AddPersistentListener(send.onClick, controller.Send);
        UnityEventTools.AddPersistentListener(back.onClick, controller.ShowResults);

        screen.gameObject.SetActive(false);
        return group;
    }

    private static TabletField Field(RectTransform parent, TabletKeyboard keyboard, string name, float y,
                                     TabletField.Kind kind, string placeholder, int maxLength)
    {
        var rt = Top(Node(parent, name), 0, y, CW, 64);
        var bg = Img(rt, new Color(0.06f, 0.045f, 0.03f, 1f));
        bg.raycastTarget = true;
        var button = rt.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        var label = Txt(Stretch(Node(rt, "Texto")), "", 28, Body, TextAlignmentOptions.Left);
        label.margin = new Vector4(18f, 0f, 18f, 0f);
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;

        var field = rt.gameObject.AddComponent<TabletField>();
        field.keyboard = keyboard;
        field.kind = kind;
        field.label = label;
        field.background = bg;
        field.placeholder = placeholder;
        field.maxLength = maxLength;
        return field;
    }

    private static void Keys(RectTransform area, TabletKeyboard keyboard)
    {
        const float keyW = 64f, keyH = 72f, gap = 8f;
        string[] rows = { "1234567890", "QWERTYUIOP", "ASDFGHJKLÑ", "ZXCVBNM_-." };

        for (int r = 0; r < rows.Length; r++)
        {
            string row = rows[r];
            float total = row.Length * keyW + (row.Length - 1) * gap;
            for (int i = 0; i < row.Length; i++)
            {
                float x = -total * 0.5f + keyW * 0.5f + i * (keyW + gap);
                Key(area, keyboard, row[i].ToString(), TabletKey.Action.Character, row[i].ToString(),
                    x, r * (keyH + gap), keyW, keyH, 30);
            }
        }

        // Última fila: teclas especiales.
        var special = new[]
        {
            (label: "@", action: TabletKey.Action.Character, value: "@", w: 64f),
            (label: ".com", action: TabletKey.Action.Text, value: ".com", w: 110f),
            (label: "Espacio", action: TabletKey.Action.Space, value: " ", w: 230f),
            (label: "Borrar", action: TabletKey.Action.Backspace, value: "", w: 150f),
            (label: "Limpiar", action: TabletKey.Action.Clear, value: "", w: 134f)
        };
        float sum = -gap;
        foreach (var k in special) sum += k.w + gap;
        float cursor = -sum * 0.5f;
        float y = rows.Length * (keyH + gap);
        foreach (var k in special)
        {
            Key(area, keyboard, k.label, k.action, k.value, cursor + k.w * 0.5f, y, k.w, keyH, 24);
            cursor += k.w + gap;
        }
    }

    private static void Key(RectTransform parent, TabletKeyboard keyboard, string label, TabletKey.Action action,
                            string value, float x, float y, float w, float h, float fontSize)
    {
        string name = "Tecla_" + (label == "." ? "Punto" : label == "-" ? "Guion" : label == "_" ? "GuionBajo" : label);
        var button = Btn(parent, name, label, x, y, w, h, KeyBg, Body, fontSize);
        var key = button.gameObject.AddComponent<TabletKey>();
        key.keyboard = keyboard;
        key.action = action;
        key.value = value;
    }

    // ================================================================ Antorchas

    private static GameObject FromPrefab(GameObject prefab, Transform parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        return go;
    }

    /// <summary>torch.fbx con fuego, luz y parpadeo, igual que las antorchas del Nivel 1 (a escala 100).</summary>
    private static GameObject FromModel(GameObject model, Transform parent)
    {
        const float scale = 100f;
        var torch = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
        torch.transform.localScale = Vector3.one * scale;
        torch.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // vertical (el modelo apunta en +Z)

        var tip = new Vector3(0f, 0f, 0.0067f);   // punta de la antorcha en unidades del modelo

        // ---- Fuego (mismos valores que el "Fire" del Nivel 1, ajustado a la escala)
        var fire = new GameObject("Fuego");
        fire.transform.SetParent(torch.transform, false);
        fire.transform.localPosition = tip;
        var ps = fire.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = 0.5f;
        main.startSpeed = 0.5f;
        main.startSize = scale / 150f;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(0.934f, 0.653f, 0.294f));
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 40;
        var emission = ps.emission;
        emission.rateOverTime = 10f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.05f;
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.566f, 0f), 0.168f), new GradientColorKey(new Color(1f, 0.904f, 0.108f), 0.397f) },
            new[] { new GradientAlphaKey(0f, 0.006f), new GradientAlphaKey(1f, 0.174f), new GradientAlphaKey(1f, 0.397f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.333f, 1f, 1f));
        var pr = fire.GetComponent<ParticleSystemRenderer>();
        var flame = AssetDatabase.LoadAssetAtPath<Material>(FlameMaterial);
        if (flame != null) pr.sharedMaterial = flame;
        pr.renderMode = ParticleSystemRenderMode.Billboard;
        pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pr.receiveShadows = false;

        // ---- Luz con parpadeo (sin sombras: es una sala pequeña y en el visor cuestan mucho)
        var lightGo = new GameObject("Luz");
        lightGo.transform.SetParent(torch.transform, false);
        lightGo.transform.localPosition = tip;
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.712f, 0.023f);
        light.range = 4f;
        light.intensity = 1.3f;
        light.shadows = LightShadows.None;
        light.lightmapBakeType = LightmapBakeType.Realtime;
        var flicker = lightGo.AddComponent<TorchFlicker>();
        flicker.minIntensity = 1.0f;
        flicker.maxIntensity = 1.6f;

        return torch;
    }

    // ================================================================ Escena

    private static bool ConfirmReplace(string rootName, string title)
    {
        var old = GameObject.Find(rootName);
        if (old == null) return true;
        if (!EditorUtility.DisplayDialog(title, $"Ya existe \"{rootName}\" en la escena. ¿Reemplazarlo?\n\n" +
                                                 "Se pierden los cambios hechos a mano dentro de ese objeto.", "Reemplazar", "Cancelar"))
            return false;
        Undo.DestroyObjectImmediate(old);
        return true;
    }

    /// <summary>Apaga (no borra) el menú copiado del juego base, si sigue activo.</summary>
    private static void DisableOldMenu()
    {
        var old = new List<GameObject>();
        foreach (var name in new[] { "Menu", "MenuManager" })
        {
            var go = GameObject.Find(name);
            if (go != null) old.Add(go);
        }
        if (old.Count == 0) return;
        if (!EditorUtility.DisplayDialog("Menú del juego base",
                "La escena tiene el menú del juego base (\"Menu\" y \"MenuManager\"). ¿Desactivarlo?\n\n" +
                "No se borra: lo puedes eliminar a mano cuando la tablet funcione.", "Desactivar", "Dejarlo"))
            return;
        foreach (var go in old)
        {
            Undo.RecordObject(go, "Desactivar menú viejo");
            go.SetActive(false);
        }
    }

    /// <summary>Si la escena tiene el EmailSender del juego base configurado, copia su cuenta al ReportMailer.</summary>
    private static void CopyBaseEmailSettings(ReportMailer mailer)
    {
        foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>())
        {
            if (mb == null || mb.GetType().Name != "EmailSender") continue;
            var so = new SerializedObject(mb);
            string from = so.FindProperty("fromEmail")?.stringValue;
            if (string.IsNullOrWhiteSpace(from) || from.Contains("tu.correo")) return;
            mailer.fromEmail = from;
            mailer.fromPassword = so.FindProperty("fromPassword")?.stringValue ?? "";
            mailer.smtpHost = so.FindProperty("smtpHost")?.stringValue ?? mailer.smtpHost;
            var port = so.FindProperty("smtpPort");
            if (port != null) mailer.smtpPort = port.intValue;
            Debug.Log("[FinalMenuBuilder] Se copió la cuenta de correo del EmailSender del juego base.");
            return;
        }
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
        Undo.RegisterCreatedObjectUndo(go, "EventSystem");
    }

    /// <summary>Agrega la escena a Build Settings si falta (LevelPortal y LoadScene solo cargan escenas que estén ahí).</summary>
    private static void EnsureInBuildSettings(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath)) return;
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var sc in scenes) if (sc.path == scenePath) return;
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[FinalMenuBuilder] Se agregó {scenePath} a Build Settings.");
    }

    private static Vector3 SpawnPoint()
    {
        var view = SceneView.lastActiveSceneView;
        if (view == null) return Vector3.zero;
        var p = view.pivot;
        return new Vector3(p.x, 0f, p.z);
    }

    private static bool TryBounds(GameObject go, out Bounds b)
    {
        b = default;
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return false;
        b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return true;
    }

    // ================================================================ UI

    private static RectTransform Node(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    /// <summary>Anclado arriba al centro: x desde el centro, y hacia abajo desde el borde superior.</summary>
    private static RectTransform Top(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    private static RectTransform Stretch(RectTransform rt) => Anchor(rt, 0f, 0f, 1f, 1f);

    private static RectTransform Anchor(RectTransform rt, float minX, float minY, float maxX, float maxY)
    {
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static RectTransform List(RectTransform rt, float spacing)
    {
        var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = spacing;
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        return rt;
    }

    private static void Row(RectTransform row, float height)
    {
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
    }

    private static Image Img(RectTransform rt, Color c)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI Txt(RectTransform rt, string text, float size, Color color,
                                       TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
    {
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.enableWordWrapping = true;
        t.raycastTarget = false;
        t.richText = true;
        return t;
    }

    private static Button Btn(RectTransform parent, string name, string label, float x, float y, float w, float h,
                              Color bg, Color fg, float fontSize)
    {
        var rt = Top(Node(parent, name), x, y, w, h);
        var img = Img(rt, Color.white);
        img.raycastTarget = true;
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        var cb = ColorBlock.defaultColorBlock;
        cb.normalColor = bg;
        cb.highlightedColor = Color.Lerp(bg, Color.white, 0.18f);
        cb.pressedColor = Color.Lerp(bg, Color.black, 0.25f);
        cb.selectedColor = bg;
        cb.disabledColor = Color.Lerp(bg, Color.gray, 0.6f);
        cb.fadeDuration = 0.08f;
        b.colors = cb;
        Txt(Stretch(Node(rt, "Texto")), label, fontSize, fg, TextAlignmentOptions.Center, FontStyles.Bold);
        return b;
    }

    private static Color Hex(int rgb) =>
        new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
}
