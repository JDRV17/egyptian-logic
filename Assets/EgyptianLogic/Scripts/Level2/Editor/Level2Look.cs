using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

/// <summary>
/// Menú "Egyptian Logic > Nivel 2 > Aplicar estética del juego".
/// Transforma el Nivel 2 ya construido para que se vea y suene como los niveles 1 y 3, sin reconstruirlo:
///  - Cambia primitivas por los modelos del proyecto (antorchas con fuego, bombillas, llaves, cerraduras,
///    switches, botones, compuertas, copas y pilares).
///  - Cambia los materiales L2_* de color plano por los materiales del proyecto (Walls, Wood, Gold, Rust…).
///  - Agrega música y sonidos de acierto y fallo, y manda todos los sonidos al MainAudioMixer.
///  - Pone la fuente Eczar en todos los textos.
/// Las referencias de los scripts (CircuitInput, Level2Puzzle, Level2Flow, Level2Torch…) se reconectan solas.
/// Se puede correr varias veces: lo que ya está convertido se salta.
/// </summary>
public static class Level2Look
{
    private const string Menu = "Egyptian Logic/Nivel 2/";
    private const string Root = "Assets/EgyptianLogic/";

    // ---- Medidas de los modelos (en metros)
    private const float WallTorchLength = 0.6f;
    private const float WallTorchTilt = 30f;       // como en el Nivel 1: inclinadas hacia la sala
    private const float PathTorchLength = 1.4f;
    private const float HandTorchLength = 0.55f;
    private const float KeyLength = 0.28f;
    private const float KeyholeSize = 0.14f;
    private const float SwitchSize = 0.08f;
    private const float GateSize = 0.13f;
    private const float GobletHeight = 0.18f;
    private const float BrazierFireScale = 1.6f;

    // ---- Rotaciones (locales, como se escriben en el Inspector) con las que las piezas quedan bien de una vez
    private static readonly Vector3 BulbRotation = new Vector3(0f, 180f, 0f);
    private static readonly Vector3 SwitchRotation = new Vector3(0f, 270f, 90f);

    /// <summary>Color de una bombilla apagada (vidrio), en vez del café oscuro que usaba el builder.</summary>
    private static readonly Color UnlitBulb = new Color(0.55f, 0.52f, 0.45f);

    // Material L2_* → material del proyecto. Lo que no aparece aquí (comida, barro, alfombras, sillas, pisos y muros
    // con textura) se deja como está.
    private static readonly Dictionary<string, string> MaterialMap = new Dictionary<string, string>
    {
        { "L2_Pilar", "Walls" },
        { "L2_Bronce", "Rust" },
        { "L2_Oro", "Gold" },
        { "L2_LlaveOro", "Gold" },
        { "L2_Pin", "Gold" },
        { "L2_SolPlaca", "Gold" },
        { "L2_Palanca", "Gold" },
        { "L2_Madera", "Wood" },
        { "L2_Mesa", "Wood" },
        { "L2_Panel", "Wood" },
        { "L2_Puerta", "Hiero 1" },
        { "L2_Marco", "Gates/Gate Cable" },
        { "L2_Conector", "Gates/Gate Cable" },
        { "L2_Enchufe", "Cable" },
        { "L2_Boton", "Red Light" },
        { "L2_Lampara", "Light Bulb" },
    };

    // ---- Recursos (se cargan en Apply)
    private static GameObject torchModel, fireFx, bulbPrefab, keyModel, keyholePrefab, gobletModel, pillarModel, switchPrefab, buttonPrefab;
    private static Mesh bulbMesh;
    private static Quaternion bulbRotation = Quaternion.identity;
    private static TMP_FontAsset font;
    private static AudioMixerGroup sfxGroup, musicGroup, ambienceGroup;
    private static AudioClip fireClip;

    // Viejo → nuevo, para reconectar las referencias de todos los scripts al final
    private static Dictionary<Object, Object> remap;
    private static List<GameObject> toDelete;
    private static Dictionary<string, int> counts;

    // ================================================================ Menú

    [MenuItem(Menu + "Aplicar estética del juego (modelos, materiales, sonido)")]
    private static void ApplyMenu()
    {
        var level = GameObject.Find("Nivel2");
        if (level == null)
        {
            EditorUtility.DisplayDialog("Estética del Nivel 2", "No encontré el objeto \"Nivel2\" en la escena abierta.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Estética del Nivel 2",
                "Se van a cambiar primitivas por modelos, materiales, fuentes y sonidos dentro de \"Nivel2\".\n\n" +
                "Se puede deshacer con Ctrl+Z, pero es un cambio grande: guarda la escena (o haz commit) antes.",
                "Aplicar", "Cancelar"))
            return;

        string report = Apply(level);
        EditorUtility.DisplayDialog("Estética del Nivel 2", report, "OK");
    }

    /// <summary>Aplica la estética a un Nivel 2 ya construido. Devuelve un resumen. También lo llama Level2Builder.BuildLevel.</summary>
    public static string Apply(GameObject level)
    {
        var report = new StringBuilder();
        remap = new Dictionary<Object, Object>();
        toDelete = new List<GameObject>();
        counts = new Dictionary<string, int>();

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Estética del Nivel 2");
        int group = Undo.GetCurrentGroup();

        LoadAssets(report);

        // Primero los objetos que se reemplazan enteros (las referencias se arreglan con el remap)
        foreach (var t in level.GetComponentsInChildren<XRSwitchToggle>(true)) Switch(t);
        foreach (var b in level.GetComponentsInChildren<XRPhysicalButton>(true)) SubmitButton(b);

        // Luego los que conservan su objeto y solo cambian la parte visible
        foreach (var t in level.GetComponentsInChildren<Level2Torch>(true)) Torch(t);
        foreach (var item in level.GetComponentsInChildren<UnlockItem>(true)) Item(item);
        foreach (var g in level.GetComponentsInChildren<LogicGateItem>(true)) Gate(g);
        foreach (var tr in level.GetComponentsInChildren<Transform>(true).ToArray())
        {
            if (tr == null) continue;
            switch (tr.name)
            {
                case "PosteCerradura": LockPost(tr); break;
                case "Copa": if (tr.GetComponent<Rigidbody>() != null) Goblet(tr); break;
                case "Pilar": Pillar(tr); break;
                case "Indicador":
                case "Bombilla":
                case "LuzResultado": Bulb(tr); break;
            }
        }
        BulbColors(level);
        CableAnchors(level);

        Remap(level.scene);
        foreach (var go in toDelete) if (go != null) Undo.DestroyObjectImmediate(go);

        Materials(level);
        Fonts(level);
        Audio(level, report);
        StoneStele(level);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(level.scene);

        report.Insert(0, "Estética aplicada al Nivel 2.\n\n" +
                         string.Join("\n", counts.Where(c => c.Value > 0).Select(c => $"• {c.Key}: {c.Value}")) + "\n\n");
        report.AppendLine("\nRevisa en la escena el tamaño de antorchas, switches, compuertas y pilares: " +
                          "las medidas están al inicio de Level2Look.cs.");
        Debug.Log("[Level2Look] " + report);
        return report.ToString();
    }

    private static void Count(string what) => counts[what] = counts.TryGetValue(what, out int n) ? n + 1 : 1;

    // ================================================================ Recursos

    private static void LoadAssets(StringBuilder report)
    {
        torchModel = Need<GameObject>("Models/torch.fbx", report);
        fireFx = Need<GameObject>("Prefabs/Fuego.prefab", report);
        bulbPrefab = Need<GameObject>("Prefabs/Low_Poly_Light_Bulb.prefab", report);
        keyModel = Need<GameObject>("Models/key_low.fbx", report);
        keyholePrefab = Need<GameObject>("Prefabs/keyhole (2).prefab", report);
        gobletModel = Need<GameObject>("Models/goblet.fbx", report);
        pillarModel = Need<GameObject>("Models/pillar.fbx", report);
        switchPrefab = Need<GameObject>("Prefabs/switch.prefab", report);
        buttonPrefab = Need<GameObject>("Prefabs/Button (1).prefab", report);
        fireClip = Need<AudioClip>("Audio/Ambience/fire.wav", report);

        bulbMesh = null;
        if (bulbPrefab != null)
        {
            // La bombilla de los niveles 1 y 3: la malla con la orientación que tiene en su prefab
            var mf = bulbPrefab.GetComponentInChildren<MeshFilter>(true);
            if (mf != null)
            {
                bulbMesh = mf.sharedMesh;
                bulbRotation = Quaternion.Inverse(bulbPrefab.transform.rotation) * mf.transform.rotation;
            }
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Eczar-VariableFont_wght SDF.asset");
        if (font == null)
            font = AssetDatabase.FindAssets("Eczar t:TMP_FontAsset")
                .Select(g => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(f => f != null);
        if (font == null) report.AppendLine("• No encontré la fuente Eczar SDF: los textos quedan con su fuente.");

        sfxGroup = musicGroup = ambienceGroup = null;
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Root + "Audio/MainAudioMixer.mixer"))
            if (o is AudioMixerGroup g)
            {
                if (g.name == "SFX") sfxGroup = g;
                else if (g.name == "Music") musicGroup = g;
                else if (g.name == "Ambience") ambienceGroup = g;
            }
        if (sfxGroup == null) report.AppendLine("• No encontré los grupos del MainAudioMixer: los sonidos no se enrutan.");
    }

    private static T Need<T>(string path, StringBuilder report) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(Root + path);
        if (a == null) report.AppendLine($"• No encontré {path}: esa parte se salta.");
        return a;
    }

    private static Material ProjectMat(string name) => AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/" + name + ".mat");

    // ================================================================ Utilidades de modelos

    /// <summary>Instancia un modelo (manteniendo el vínculo con su prefab/fbx) como hijo, en la capa del padre.</summary>
    private static GameObject Spawn(GameObject asset, Transform parent, string name)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        Undo.RegisterCreatedObjectUndo(go, "Estética del Nivel 2");
        go.name = name;
        SetLayer(go.transform, parent.gameObject.layer);
        return go;
    }

    /// <summary>
    /// Guarda en la escena los cambios hechos por código a una instancia de prefab o modelo (posición, escala,
    /// materiales). Sin esto, Unity los descarta al recargar la escena.
    /// </summary>
    private static void Commit(GameObject go)
    {
        if (go == null || !PrefabUtility.IsPartOfPrefabInstance(go)) return;
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        foreach (var a in go.GetComponentsInChildren<AudioSource>(true)) PrefabUtility.RecordPrefabInstancePropertyModifications(a);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go);
    }

    private static void SetLayer(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        foreach (Transform c in t) SetLayer(c, layer);
    }

    /// <summary>Caja que envuelve las mallas de <paramref name="go"/>, en coordenadas locales de <paramref name="space"/>.</summary>
    private static Bounds LocalBounds(Transform space, GameObject go)
    {
        bool any = false;
        var b = new Bounds();
        var toSpace = space.worldToLocalMatrix;
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            var mb = mf.sharedMesh.bounds;
            var m = toSpace * mf.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = m.MultiplyPoint3x4(corner);
                if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                else b.Encapsulate(p);
            }
        }
        return b;
    }

    /// <summary>Escala el modelo para que su lado más largo (o el eje dado) mida <paramref name="size"/> en el espacio del padre.</summary>
    private static void Fit(GameObject model, float size, int axis = -1)
    {
        var parent = model.transform.parent;
        var b = LocalBounds(parent, model);
        float current = axis < 0 ? Mathf.Max(b.size.x, b.size.y, b.size.z) : b.size[axis];
        if (current < 1e-6f) return;
        model.transform.localScale *= size / current;
    }

    /// <summary>Mueve el modelo para que el centro de su caja (o la base, si <paramref name="sitOnBottom"/>) quede en <paramref name="localPoint"/>.</summary>
    private static void Place(GameObject model, Vector3 localPoint, bool sitOnBottom = false)
    {
        var parent = model.transform.parent;
        var b = LocalBounds(parent, model);
        var anchor = sitOnBottom ? new Vector3(b.center.x, b.min.y, b.center.z) : b.center;
        model.transform.localPosition += localPoint - anchor;
    }

    private static void ApplyMaterial(GameObject model, Material mat)
    {
        if (mat == null) return;
        foreach (var r in model.GetComponentsInChildren<Renderer>(true))
        {
            Undo.RecordObject(r, "Estética del Nivel 2");
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
    }

    /// <summary>Ajusta un BoxCollider a la caja del modelo, en coordenadas del objeto del collider.</summary>
    private static void FitCollider(BoxCollider col, GameObject model)
    {
        if (col == null) return;
        var b = LocalBounds(col.transform, model);
        Undo.RecordObject(col, "Estética del Nivel 2");
        col.center = b.center;
        col.size = Vector3.Max(b.size, Vector3.one * 0.01f);
    }

    private static Transform Child(Transform t, string name)
    {
        foreach (Transform c in t) if (c.name == name) return c;
        return null;
    }

    private static bool IsPrimitive(Transform t)
    {
        if (t == null) return false;
        var mf = t.GetComponent<MeshFilter>();
        return mf != null && mf.sharedMesh != null && AssetDatabase.GetAssetPath(mf.sharedMesh) == "Library/unity default resources";
    }

    /// <summary>Marca para borrar los hijos con esos nombres. Las referencias a ellos se pasan a <paramref name="replacement"/>.</summary>
    private static void Drop(Transform parent, Object replacement, params string[] names)
    {
        foreach (var n in names)
        {
            var c = Child(parent, n);
            if (c == null || !IsPrimitive(c)) continue;
            if (replacement != null) MapObject(c.gameObject, replacement as GameObject);
            toDelete.Add(c.gameObject);
        }
    }

    /// <summary>Registra old → new para el GameObject, su Transform y sus componentes del mismo tipo.</summary>
    private static void MapObject(GameObject oldGo, GameObject newGo)
    {
        if (oldGo == null || newGo == null) return;
        remap[oldGo] = newGo;
        remap[oldGo.transform] = newGo.transform;
        foreach (var c in oldGo.GetComponents<Component>())
        {
            if (c == null || c is Transform) continue;
            var match = newGo.GetComponentInChildren(c.GetType(), true);
            if (match != null) remap[c] = match;
            else if (c is Renderer)
            {
                var r = newGo.GetComponentInChildren<Renderer>(true);
                if (r != null) remap[c] = r;
            }
        }
    }

    // ================================================================ Antorchas, braseros

    private static void Torch(Level2Torch torch)
    {
        var t = torch.transform;
        if (Child(t, "Modelo") != null || (torch.flame != null && torch.flame.name == "Llamas")) return;

        if (Child(t, "Soporte") != null) WallTorch(torch);
        else if (Child(t, "Brasas") != null) Brazier(torch);
        else if (Child(t, "Cuenco") != null && Child(t, "Palo") != null) PathTorch(torch);
    }

    /// <summary>Fuego del Nivel 1 en la punta del modelo de antorcha (misma posición que allá).</summary>
    private static GameObject TorchFire(GameObject model)
    {
        if (fireFx == null) return null;
        var fire = Spawn(fireFx, model.transform, "Fuego");
        fire.transform.localPosition = new Vector3(0.00007f, 0f, 0.006735f);
        fire.transform.localRotation = Quaternion.identity;
        Commit(fire);
        return fire;
    }

    /// <summary>Antorcha de pared: +Z local de la raíz sale del muro.</summary>
    private static void WallTorch(Level2Torch torch)
    {
        if (torchModel == null) return;
        var t = torch.transform;
        var model = Spawn(torchModel, t, "Modelo");
        var upright = torchModel.transform.localRotation;
        model.transform.localRotation = upright;
        model.transform.localScale = torchModel.transform.localScale;
        Fit(model, WallTorchLength, 1);
        model.transform.localRotation = Quaternion.Euler(WallTorchTilt, 0f, 0f) * upright;   // la punta hacia la sala
        Place(model, new Vector3(0f, -0.05f, 0.14f));
        Commit(model);
        var fire = TorchFire(model);

        Drop(t, fire, "Fuego");
        Drop(t, null, "Soporte", "Palo");
        SetFlame(torch, fire);
        AddFireLoop(torch);
        Count("Antorchas de pared");
    }

    /// <summary>Antorcha de pie del camino: el modelo parado sobre su base.</summary>
    private static void PathTorch(Level2Torch torch)
    {
        if (torchModel == null) return;
        var t = torch.transform;
        var model = Spawn(torchModel, t, "Modelo");
        model.transform.localRotation = torchModel.transform.localRotation;
        model.transform.localScale = torchModel.transform.localScale;
        Fit(model, PathTorchLength, 1);
        Place(model, new Vector3(0f, 0.04f, 0f), true);
        Commit(model);
        var fire = TorchFire(model);

        Drop(t, fire, "Fuego");
        Drop(t, null, "Palo", "Cuenco");
        SetFlame(torch, fire);
        Count("Antorchas del camino");
    }

    /// <summary>Brasero: se queda la forma (no hay modelo), pero lleva fuego de verdad sobre las brasas.</summary>
    private static void Brazier(Level2Torch torch)
    {
        var t = torch.transform;
        var embers = Child(t, "Brasas");
        var holder = new GameObject("Llamas");
        Undo.RegisterCreatedObjectUndo(holder, "Estética del Nivel 2");
        holder.transform.SetParent(t, false);
        holder.transform.localPosition = embers.localPosition;
        holder.layer = t.gameObject.layer;
        Undo.SetTransformParent(embers, holder.transform, "Estética del Nivel 2");

        if (fireFx != null)
        {
            var fire = Spawn(fireFx, holder.transform, "Fuego");
            fire.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            fire.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // el cono del fuego hacia arriba
            fire.transform.localScale = Vector3.one * BrazierFireScale;
            Commit(fire);
        }
        SetFlame(torch, holder);
        if (torch.fireSound == null) AddFireLoop(torch);
        Count("Braseros con fuego");
    }

    private static void SetFlame(Level2Torch torch, GameObject flame)
    {
        if (flame == null) return;
        Undo.RecordObject(torch, "Estética del Nivel 2");
        torch.flame = flame;
        EditorUtility.SetDirty(torch);
    }

    /// <summary>Sonido de fuego en bucle (como las antorchas del Nivel 1); Level2Torch solo lo reproduce encendida.</summary>
    private static void AddFireLoop(Level2Torch torch)
    {
        if (fireClip == null || torch.fireSound != null) return;
        var src = Undo.AddComponent<AudioSource>(torch.gameObject);
        src.clip = fireClip;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 1f;
        src.volume = 0.3f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.maxDistance = 6f;
        src.outputAudioMixerGroup = ambienceGroup;
        Undo.RecordObject(torch, "Estética del Nivel 2");
        torch.fireSound = src;
    }

    // ================================================================ Objetos que se llevan (llave, antorcha de mano)

    private static void Item(UnlockItem item)
    {
        var t = item.transform;
        if (Child(t, "Modelo") != null) return;

        if (Child(t, "Cana") != null && keyModel != null)
        {
            var model = Spawn(keyModel, t, "Modelo");
            Fit(model, KeyLength);
            Place(model, Vector3.zero);
            ApplyMaterial(model, ProjectMat("Gold"));
            Commit(model);
            FitCollider(t.GetComponent<BoxCollider>(), model);
            Drop(t, model, "Cana", "Ojo", "Dientes");
            Count("Llaves");
        }
        else if (Child(t, "Trapo") != null && torchModel != null)
        {
            // Antorcha apagada para llevar al brasero: el modelo sobre el eje Y del objeto (que va acostado)
            var model = Spawn(torchModel, t, "Modelo");
            model.transform.localRotation = torchModel.transform.localRotation;
            model.transform.localScale = torchModel.transform.localScale;
            Fit(model, HandTorchLength, 1);
            Place(model, Vector3.zero);
            Commit(model);
            FitCollider(t.GetComponent<BoxCollider>(), model);
            Drop(t, model, "Palo", "Trapo");
            Count("Antorchas de mano");
        }
    }

    private static void LockPost(Transform post)
    {
        var eye = Child(post, "Ojo");
        if (eye == null || !IsPrimitive(eye) || keyholePrefab == null) return;

        // La cerradura de los niveles 1 y 3, solo como adorno: el socket del Nivel 2 (Level2Socket) sigue siendo el que vale
        var model = (GameObject)PrefabUtility.InstantiatePrefab(keyholePrefab, post);
        PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (var c in model.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(c);
        foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        foreach (var c in model.GetComponentsInChildren<AudioSource>(true)) Object.DestroyImmediate(c);
        Undo.RegisterCreatedObjectUndo(model, "Estética del Nivel 2");
        model.name = "Cerradura";
        SetLayer(model.transform, post.gameObject.layer);
        Fit(model, KeyholeSize);
        Place(model, eye.localPosition);
        Drop(post, model, "Ojo");
        Count("Cerraduras");
    }

    // ================================================================ Piezas del protoboard

    /// <summary>Cambia el switch de primitivas por el prefab del proyecto (con su sonido) y reconecta CircuitInput.</summary>
    private static void Switch(XRSwitchToggle old)
    {
        if (switchPrefab == null || Child(old.transform, "Pivote") == null) return;
        var t = old.transform;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(switchPrefab, t.parent);
        Undo.RegisterCreatedObjectUndo(go, "Estética del Nivel 2");
        go.name = "Switch";
        go.transform.SetSiblingIndex(t.GetSiblingIndex());
        go.transform.localRotation = Quaternion.Euler(SwitchRotation);
        SetLayer(go.transform, t.gameObject.layer);
        Fit(go, SwitchSize);
        Place(go, t.localPosition);
        RouteSources(go, sfxGroup);
        Commit(go);

        MapObject(t.gameObject, go);
        toDelete.Add(t.gameObject);
        Count("Switches");
    }

    /// <summary>Cambia el botón "Comprobar" por el prefab del proyecto. El texto se queda encima.</summary>
    private static void SubmitButton(XRPhysicalButton old)
    {
        if (buttonPrefab == null || old.name != "BotonComprobar" || Child(old.transform, "Tapa") == null) return;
        var t = old.transform;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, t.parent);
        Undo.RegisterCreatedObjectUndo(go, "Estética del Nivel 2");
        go.name = "BotonComprobar";
        SetLayer(go.transform, t.gameObject.layer);
        // Apoyado sobre la mesa (el tablero está en y = 0 del objeto Mesa)
        Place(go, new Vector3(t.localPosition.x, 0f, t.localPosition.z), true);
        RouteSources(go, sfxGroup);
        Commit(go);

        var label = Child(t, "Texto");
        if (label != null) Undo.SetTransformParent(label, t.parent, "Estética del Nivel 2");

        MapObject(t.gameObject, go);
        toDelete.Add(t.gameObject);
        Count("Botones Comprobar");
    }

    // ================================================================ Materiales de las compuertas

    /// <summary>
    /// Copia los materiales de la compuerta seleccionada (pieza por pieza) a todas las compuertas del mismo tipo
    /// en las escenas abiertas. La compuerta de origen puede estar en otra escena abierta en modo aditivo
    /// (por ejemplo, una del Nivel 1, que ya tiene sus materiales puestos a mano).
    /// </summary>
    [MenuItem(Menu + "Copiar materiales de la compuerta seleccionada a las del mismo tipo")]
    private static void CopyGateMaterials()
    {
        var source = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<LogicGateItem>() : null;
        var sourceModel = source != null ? GateModel(source) : null;
        if (sourceModel == null)
        {
            EditorUtility.DisplayDialog("Materiales de compuertas",
                "Selecciona una compuerta que ya tenga sus materiales puestos (o cualquier pieza de ella).", "OK");
            return;
        }

        // Materiales por ruta de la pieza dentro del modelo (las piezas se llaman igual en todas las copias del .fbx)
        var byPath = new Dictionary<string, Material[]>();
        foreach (var r in sourceModel.GetComponentsInChildren<Renderer>(true))
            byPath[PathIn(sourceModel, r.transform)] = r.sharedMaterials;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Copiar materiales de compuertas");
        int gates = 0;
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            bool changed = false;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var g in root.GetComponentsInChildren<LogicGateItem>(true))
                {
                    if (g == source || g.GateType != source.GateType) continue;
                    var model = GateModel(g);
                    if (model == null) continue;
                    foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!byPath.TryGetValue(PathIn(model, r.transform), out var mats)) continue;
                        Undo.RecordObject(r, "Copiar materiales de compuertas");
                        r.sharedMaterials = mats;
                        if (PrefabUtility.IsPartOfPrefabInstance(r)) PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                    }
                    gates++;
                    changed = true;
                }
            if (changed) EditorSceneManager.MarkSceneDirty(scene);
        }

        EditorUtility.DisplayDialog("Materiales de compuertas",
            $"Materiales de {source.GateType} copiados a {gates} compuerta(s).\nGuarda la escena para conservarlos.", "OK");
    }

    /// <summary>
    /// Raíz del modelo 3D de una compuerta: en el Nivel 2 es el hijo "Modelo"; en el Nivel 1 la compuerta misma
    /// es la instancia del .fbx. Null si la compuerta es un bloque sin modelo (MUX, sumadores).
    /// </summary>
    private static Transform GateModel(LogicGateItem gate)
    {
        var model = Child(gate.transform, "Modelo");
        if (model != null) return model;
        return PrefabUtility.IsPartOfPrefabInstance(gate.gameObject) && gate.GetComponentInChildren<MeshFilter>(true) != null
            ? gate.transform : null;
    }

    private static string PathIn(Transform root, Transform t)
    {
        var parts = new List<string>();
        for (var c = t; c != null && c != root; c = c.parent) parts.Add(c.name);
        parts.Reverse();
        return string.Join("/", parts);
    }

    /// <summary>Compuertas básicas con los modelos del Nivel 1. MUX y sumadores no tienen modelo: quedan como bloques.</summary>
    private static void Gate(LogicGateItem gate)
    {
        var t = gate.transform;

        var body = Child(t, "Cuerpo");
        if (body == null || !IsPrimitive(body)) return;

        var model3d = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}Models/Gates/{gate.GateType}.fbx");
        if (model3d == null) return;   // NOR (.blend), MUX, sumadores

        var model = Spawn(model3d, t, "Modelo");
        // El modelo del Nivel 1 está acostado (símbolo hacia +Y); aquí el símbolo mira al jugador (−Z)
        model.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        model.transform.localScale = Vector3.one;
        Fit(model, GateSize);
        Place(model, Vector3.zero);
        // Los materiales no se ponen aquí: cada pieza del modelo lleva uno distinto (centro, conexiones, punto).
        // Se asignan a mano en una compuerta y se copian con "Copiar materiales de la compuerta seleccionada".
        Commit(model);
        FitCollider(t.GetComponent<BoxCollider>(), model);

        // La etiqueta queda justo delante del modelo
        var label = Child(t, "Nombre");
        if (label != null)
        {
            var b = LocalBounds(t, model);
            Undo.RecordObject(label, "Estética del Nivel 2");
            label.localPosition = new Vector3(label.localPosition.x, label.localPosition.y, b.min.z - 0.003f);
        }

        Drop(t, model, "Cuerpo");
        Count("Compuertas con modelo");
    }

    /// <summary>
    /// Bombillas (indicadores, salidas, luces de resultado): se cambia solo la malla y el material, así siguen
    /// funcionando las referencias de CircuitInput, CircuitOutput, Level2Puzzle y Level2Flow.
    /// </summary>
    private static void Bulb(Transform lamp)
    {
        var mf = lamp.GetComponent<MeshFilter>();
        var mr = lamp.GetComponent<MeshRenderer>();
        if (bulbMesh == null || mf == null || mr == null || !IsPrimitive(lamp)) return;

        float size = lamp.localScale.x;          // diámetro de la esfera
        var oldCenter = lamp.localPosition;
        Undo.RecordObjects(new Object[] { mf, mr, lamp }, "Estética del Nivel 2");
        mf.sharedMesh = bulbMesh;
        var bulbMat = ProjectMat("Light Bulb");
        if (bulbMat != null) mr.sharedMaterial = bulbMat;
        lamp.localRotation = Quaternion.Euler(BulbRotation) * bulbRotation;
        var mb = bulbMesh.bounds;
        float scale = size * 1.4f / Mathf.Max(mb.size.y, 1e-6f);   // un poco más alta que la esfera
        lamp.localScale = Vector3.one * scale;
        // Centrada donde estaba la esfera
        lamp.localPosition = oldCenter - lamp.localRotation * (mb.center * scale);
        Count("Bombillas");
    }

    /// <summary>
    /// Los cables de las entradas salen del Transform de la bombilla, que queda detrás del panel (la bombilla va
    /// medio enterrada para que no flote), así que el cable se dibuja escondido. Se agrega un hijo vacío
    /// "AnclaCable" justo delante de la cara visible de la bombilla y el cable sale de ahí. La bombilla no se mueve.
    /// Se puede volver a correr: recoloca las anclas (por si se movieron las bombillas).
    /// </summary>
    private static void CableAnchors(GameObject level)
    {
        foreach (var node in level.GetComponentsInChildren<SignalNode>(true))
        {
            var current = node.cableAnchor;
            if (current == null) continue;
            var lamp = current.name == "AnclaCable" ? current.parent : current;
            var r = lamp != null ? lamp.GetComponent<Renderer>() : null;
            if (r == null || IsPrimitive(lamp)) continue;

            var anchor = Child(lamp, "AnclaCable");
            if (anchor == null)
            {
                var go = new GameObject("AnclaCable");
                Undo.RegisterCreatedObjectUndo(go, "Estética del Nivel 2");
                anchor = go.transform;
                anchor.SetParent(lamp, false);
            }

            // Hacia el jugador = −Z del tablero (la fila de la entrada). Punto de la bombilla más adelantado + 5 mm.
            var board = node.transform;
            Vector3 toPlayer = -board.forward;
            var b = r.bounds;
            float half = Mathf.Abs(b.extents.x * toPlayer.x) + Mathf.Abs(b.extents.y * toPlayer.y) + Mathf.Abs(b.extents.z * toPlayer.z);
            Undo.RecordObject(anchor, "Estética del Nivel 2");
            anchor.position = b.center + toPlayer * (half + 0.005f);

            if (node.cableAnchor != anchor)
            {
                Undo.RecordObject(node, "Estética del Nivel 2");
                node.cableAnchor = anchor;
                EditorUtility.SetDirty(node);
            }
            Count("Cables que salen del frente de la bombilla");
        }
    }

    /// <summary>Con el modelo de bombilla, "apagado" se ve como vidrio y no como una bola café.</summary>
    private static void BulbColors(GameObject level)
    {
        foreach (var i in level.GetComponentsInChildren<CircuitInput>(true)) { Undo.RecordObject(i, "Estética del Nivel 2"); i.offColor = UnlitBulb; }
        foreach (var o in level.GetComponentsInChildren<CircuitOutput>(true)) { Undo.RecordObject(o, "Estética del Nivel 2"); o.offColor = UnlitBulb; }
        foreach (var p in level.GetComponentsInChildren<Level2Puzzle>(true)) { Undo.RecordObject(p, "Estética del Nivel 2"); p.idleColor = UnlitBulb; }
        foreach (var f in level.GetComponentsInChildren<Level2Flow>(true)) { Undo.RecordObject(f, "Estética del Nivel 2"); f.lockedColor = UnlitBulb; }
    }

    // ================================================================ Decoración

    private static void Goblet(Transform cup)
    {
        if (gobletModel == null || Child(cup, "Modelo") != null || !IsPrimitive(Child(cup, "Copa"))) return;
        var model = Spawn(gobletModel, cup, "Modelo");
        Fit(model, GobletHeight, 1);
        Place(model, Vector3.zero, true);
        Commit(model);
        var col = Undo.AddComponent<BoxCollider>(cup.gameObject);
        FitCollider(col, model);
        Drop(cup, model, "Base", "Tallo", "Copa");
        Count("Copas");
    }

    private static void Pillar(Transform pillar)
    {
        var shaft = Child(pillar, "Fuste");
        if (pillarModel == null || !IsPrimitive(shaft)) return;
        float height = shaft.localScale.y * 2f;   // cilindro de Unity: alto 2 a escala 1

        var model = Spawn(pillarModel, pillar, "Modelo");
        Fit(model, height, 1);
        Place(model, Vector3.zero, true);
        Commit(model);
        var col = Undo.AddComponent<BoxCollider>(pillar.gameObject);
        FitCollider(col, model);
        Drop(pillar, model, "Fuste", "Base", "Capitel");
        Count("Pilares");
    }

    /// <summary>La estela del progreso con la textura de estela del proyecto.</summary>
    private static void StoneStele(GameObject level)
    {
        var mat = ProjectMat("Hiero 2");
        if (mat == null) return;
        foreach (var t in level.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != "Piedra" || t.parent == null || t.parent.name != "Estela") continue;
            var r = t.GetComponent<Renderer>();
            if (r == null) continue;
            Undo.RecordObject(r, "Estética del Nivel 2");
            r.sharedMaterial = mat;
        }
    }

    // ================================================================ Materiales, fuentes, audio

    private static void Materials(GameObject level)
    {
        var cache = new Dictionary<string, Material>();
        foreach (var r in level.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer || r is LineRenderer) continue;
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null || !MaterialMap.TryGetValue(mats[i].name, out var target)) continue;
                if (!cache.TryGetValue(target, out var m)) cache[target] = m = ProjectMat(target);
                if (m == null) continue;
                mats[i] = m;
                changed = true;
            }
            if (!changed) continue;
            Undo.RecordObject(r, "Estética del Nivel 2");
            r.sharedMaterials = mats;
            Count("Piezas con material del proyecto");
        }
    }

    private static void Fonts(GameObject level)
    {
        if (font == null) return;
        foreach (var tmp in level.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp.font == font) continue;
            Undo.RecordObject(tmp, "Estética del Nivel 2");
            tmp.font = font;
            tmp.fontSharedMaterial = font.material;
            EditorUtility.SetDirty(tmp);
            Count("Textos con Eczar");
        }
    }

    /// <summary>Música, sonidos de acierto y fallo (como en los niveles 1 y 3) y todos los sonidos por el mixer.</summary>
    private static void Audio(GameObject level, StringBuilder report)
    {
        var holder = Child(level.transform, "Audio");
        if (holder == null)
        {
            var go = new GameObject("Audio");
            Undo.RegisterCreatedObjectUndo(go, "Estética del Nivel 2");
            go.transform.SetParent(level.transform, false);
            holder = go.transform;
        }

        Player2D(holder, "MusicPlayer", Root + "Audio/Music/music.mp3", musicGroup, true, 0.2f, report);
        var success = Player2D(holder, "SuccessPlayer", Root + "Audio/SFX/success.wav", sfxGroup, false, 1f, report);
        var fail = Player2D(holder, "FailurePlayer", Root + "Audio/SFX/fail.wav", sfxGroup, false, 1f, report);

        foreach (var p in level.GetComponentsInChildren<Level2Puzzle>(true))
        {
            if (success != null) Listen(p, p.onSolved, success);
            if (fail != null) Listen(p, p.onIncorrect, fail);
        }

        // Todo lo demás por el mixer: fuego en Ambience, el resto en SFX (incluye los pasos del jugador)
        foreach (var root in level.scene.GetRootGameObjects())
            foreach (var src in root.GetComponentsInChildren<AudioSource>(true))
            {
                if (src.outputAudioMixerGroup != null) continue;
                Undo.RecordObject(src, "Estética del Nivel 2");
                src.outputAudioMixerGroup = src.loop && src.clip == fireClip ? ambienceGroup : sfxGroup;
                Count("Sonidos enrutados al mixer");
            }
    }

    private static AudioSource Player2D(Transform holder, string name, string clipPath, AudioMixerGroup group, bool music, float volume,
                                        StringBuilder report)
    {
        var existing = GameObject.Find(name);
        if (existing != null && existing.scene == holder.gameObject.scene && existing.GetComponent<AudioSource>() != null)
            return existing.GetComponent<AudioSource>();

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        if (clip == null) { report.AppendLine($"• No encontré {clipPath}: no se creó {name}."); return null; }

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Estética del Nivel 2");
        go.transform.SetParent(holder, false);
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.outputAudioMixerGroup = group;
        src.spatialBlend = 0f;
        src.volume = volume;
        src.loop = music;
        src.playOnAwake = music;
        if (music) src.priority = 0;
        Count("Reproductores de audio creados");
        return src;
    }

    /// <summary>Conecta el evento a AudioSource.Play, sin duplicar si ya estaba conectado.</summary>
    private static void Listen(Object owner, UnityEvent evt, AudioSource src)
    {
        for (int i = 0; i < evt.GetPersistentEventCount(); i++)
            if (evt.GetPersistentTarget(i) == src && evt.GetPersistentMethodName(i) == "Play") return;
        Undo.RecordObject(owner, "Estética del Nivel 2");
        UnityEventTools.AddPersistentListener(evt, src.Play);
        EditorUtility.SetDirty(owner);
    }

    private static void RouteSources(GameObject go, AudioMixerGroup group)
    {
        if (group == null) return;
        foreach (var s in go.GetComponentsInChildren<AudioSource>(true)) s.outputAudioMixerGroup = group;
    }

    // ================================================================ Reconectar referencias

    /// <summary>Pasa todas las referencias de los objetos viejos a los nuevos, en todos los scripts de la escena.</summary>
    private static void Remap(UnityEngine.SceneManagement.Scene scene)
    {
        if (remap.Count == 0) return;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                if (remap.ContainsKey(mb)) continue;   // el componente viejo se borra
                var so = new SerializedObject(mb);
                var it = so.GetIterator();
                bool changed = false;
                while (it.Next(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var v = it.objectReferenceValue;
                    if (v == null || !remap.TryGetValue(v, out var nv)) continue;
                    it.objectReferenceValue = nv;
                    changed = true;
                }
                if (changed) so.ApplyModifiedProperties();
            }
    }
}