using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Menú "Egyptian Logic > Voces": deja la voz y los subtítulos listos en la escena abierta, y crea o actualiza
/// el asset con los guiones (asignando los clips por nombre: un archivo "N2-03..." es el guion N2-03).
/// </summary>
public static class VoiceSetupTools
{
    private const string Menu = "Egyptian Logic/Voces/";
    private const string Folder = "Assets/EgyptianLogic/Audio/Voces";
    private const string LinesPath = Folder + "/VoiceLines.asset";
    private const string MaterialPath = Folder + "/Subtitulos_Overlay.mat";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Eczar-VariableFont_wght SDF.asset";

    /// <summary>Guiones del documento "Guiones de voz – Egyptian Logic" (19 nov 2026).</summary>
    public static readonly (string code, string text)[] Scripts =
    {
            ("M-01", @"En un inhóspito rincón del Sahara se ha hallado una pirámide que parece ser la tumba del legendario faraón Tut-AND-Kamón. Por sus amplios conocimientos de arqueología, pero sobre todo de lógica, se le ha encomendado la misión de explorar sus ruinas. Tenga cuidado: se dice que el faraón amaba los acertijos tanto como el oro, y que llenó su tumba de ellos. Si se queda atascado, cuentan que presionar Y podría orientarlo en su búsqueda... pero no abuse: cada vez que lo hace, crece la ira del espíritu del faraón. Antes de partir, responderá dos preguntas para verificar su experticia. Mucho cuidado, y mucha suerte. La necesitará."),
            ("N1-01", @"¿Quién se atreve a perturbar mi descanso? Ah... un arqueólogo. Hace tres mil años que nadie pisaba mi tumba. Bienvenido, intruso: soy Tut-AND-Kamón, señor de las dos tierras... y de las dos entradas. Mi sarcófago descansa al fondo de esta sala. Acércate. Y pisa la baldosa que hay frente a él... si te atreves."),
            ("N1-02", @"¿Escuchaste eso? Fue la puerta. Entraste Y pisaste la baldosa: verdadero Y verdadero da verdadero... y ahora estás atrapado. La única salida de mi tumba es la lógica. Resuelve mis acertijos, uno tras otro, y quizá vuelvas a ver el sol. Junto a la baldosa te dejé un pergamino. Considéralo un regalo. El único."),
            ("N1-03", @"Nada mal. Mis sacerdotes tardaban semanas en resolver eso. Ahí tienes tu recompensa: la palanca del flujo sagrado. Mis ingenieros la quitaron de su pedestal para que ningún saqueador despertara la energía de la tumba. Devuélvela a su lugar y acciónala... y veremos si sabes llevar esa energía hasta donde debe llegar."),
            ("N1-04", @"Un cofre... y una antorcha. Qué tentador. Te advierto algo, arqueólogo: a la entrada de mi tumba cuelga mi tapiz favorito, tejido en honor al gran dios Ra. Cada hilo, cada color... perfecto. Sería una verdadera tragedia que alguien le acercara una llama. Aunque, ahora que lo pienso, nunca supe qué escondieron mis constructores detrás de él."),
            ("N1-05", @"Vaya, vaya. Tienes cabeza para esto, lo admito. Pero no te emociones: lo que acabas de cruzar es apenas mi antesala. Más adentro está mi gran comedor, donde celebré mi último banquete. La puerta está abierta, arqueólogo. Por ahora."),
            ("N2-01", @"Bienvenido a mi gran comedor. Perdona el desorden: nadie ha recogido desde mi último banquete, y de eso hace tres mil años. Esas frutas por el suelo... yo no me las comería. Mira las paredes: en ellas dejé unos tableros con mis acertijos, aunque por ahora solo puedes ver uno. Y vigila el techo: ahí arriba guardo mis compuertas lógicas, y tienen la mala costumbre de caer justo cuando alguien las necesita. Una cosa más. Esa noche, cinco de mis inventos, uno detrás de otro, acabaron con mi vida. Resuelve mis acertijos y los irás encontrando. Así sabrás cómo terminó el faraón más brillante de Egipto."),
            ("N2-02", @"La palanca. Mi primer invento de esa noche. Abría el gran depósito de vino para llenar todas las copas a la vez. Tiré de ella con tanto entusiasmo que el vino no llenó las copas... llenó el comedor entero. Adelante, acciónala. Hoy el depósito está seco."),
            ("N2-03", @"Con el piso cubierto de vino, caminé hacia mi segundo invento: una baldosa que, al pisarla, hacía sonar las trompetas de la cena. Sonaron, sí... mientras el faraón resbalaba y caía de espaldas frente a todos sus invitados. Písala tú. Con cuidado."),
            ("N2-04", @"Mis invitados se rieron. ¡Se rieron de mí! Así que tomé esta llave y cerré la puerta del comedor: nadie saldría de ahí hasta que dejaran de burlarse. Fue mi tercer error, aunque todavía no lo sabía. Métela en su cerradura."),
            ("N2-05", @"Para recuperar mi dignidad, presenté mi cuarto invento: este botón, que hacía descender el gran candelabro sobre la mesa. Descendió, sí... mucho más rápido de lo que esperaba. Se estrelló contra la mesa y apagó todas las velas. Oprímelo. Esta vez, el candelabro se queda donde está."),
            ("N2-06", @"Y llegamos al final. A oscuras, con la puerta cerrada y el aire cargado de vino, tomé esta antorcha para encender el gran brasero. Fue mi último invento... y mi última idea. El humo se encargó del resto. Llévala hasta el brasero de la pared de enfrente y enciéndelo. Tranquilo: a ti solo te abrirá una puerta."),
            ("N2-07", @"Así terminó mi último banquete. Cinco inventos, uno tras otro, como un circuito en el que cada salida fue la entrada del siguiente. Pero mi espíritu no descansa aquí abajo. Sube, arqueólogo. En la cúspide de mi pirámide te espera mi mayor creación. Y allá arriba, el tiempo corre."),
            ("N3-01", @"Has llegado a la cúspide de mi pirámide: la sala del reloj. Ningún mortal había subido hasta aquí. Este reloj es el corazón de mi tumba. Cada vez que el minutero llega a las doce, envía un pulso, y con ese pulso todo en esta sala avanza un paso. O así era... hasta que alguien cortó sus cables. Hoy solo mi primer acertijo sigue conectado. Resuélvelo, y los cables irán apareciendo. Y escucha bien el tictac: aquí arriba, el tiempo también es parte del acertijo."),
            ("N3-02", @"Tu memoria es mejor que la de mis escribas. Ahí tienes el primer cable. Frente al reloj hay dos conexiones rotas, y cada una despierta un par de acertijos: uno que obedece al reloj, y otro que solo te obedece a ti. Tú eliges por dónde empezar. Pero recuerda: si comienzas uno, su compañero esperará hasta que lo termines."),
            ("N3-03", @"Un par menos. Aquí tienes el último cable, y ya sabes dónde va: en la conexión que quedó vacía. Cuando el reloj vuelva a latir de ambos lados, solo un par de acertijos te separará de la salida."),
            ("N3-04", @"El reloj late completo otra vez... y lo lograste. En tres mil años, nadie había llegado tan lejos. Aposté conmigo mismo a que no saldrías... y perdí. Es la primera apuesta que pierdo en milenios. La puerta está abierta, arqueólogo. Sal, y vuelve a ver el sol. Y cuando cuentes esta historia, di que Tut-AND-Kamón te puso a prueba... y que fuiste digno. Una última cosa: la lógica nunca se olvida, pero sí se oxida. Sigue practicando."),
            ("F-01", @"Felicitaciones arqueólogo, recorriste toda la tumba del faraón Tut-AND-Kamón. Al frente tuyo verás tus resultados en las distintas pruebas. Si quieres, puedes recibir el informe en formato PDF a tu correo electrónico si seleccionas ""Enviar informe"". Si decides hacerlo, ¡no olvides revisar la carpeta de spam! El faraón Tut-AND-Kamón no era tan hábil con esa tecnología...")
    };

    /// <summary>Voz de bienvenida de cada escena.</summary>
    private static readonly Dictionary<string, string> StartCode = new Dictionary<string, string>
    {
        { "EgyptLevel1", "N1-01" }, { "EgyptLevel2", "N2-01" }, { "EgyptLevel3", "N3-01" }, { "EgyptFinalMenu", "F-01" }
    };

    [MenuItem(Menu + "Configurar voz y subtítulos en esta escena")]
    public static void SetupScene()
    {
        var lines = EnsureLines();
        var material = EnsureMaterial();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        var player = Object.FindObjectOfType<VoicePlayer>();
        if (player == null)
        {
            var go = new GameObject("Voz");
            Undo.RegisterCreatedObjectUndo(go, "Voz");
            go.AddComponent<AudioSource>();
            player = go.AddComponent<VoicePlayer>();
        }
        Undo.RecordObject(player, "Configurar voz");

        var subs = player.GetComponent<SubtitleDisplay>();
        if (subs == null) subs = Undo.AddComponent<SubtitleDisplay>(player.gameObject);
        Undo.RecordObject(subs, "Configurar subtítulos");
        if (subs.font == null) subs.font = font;
        if (subs.textMaterial == null) subs.textMaterial = material;

        player.lines = lines;
        player.subtitles = subs;
        if (player.output == null) player.output = FindVoicesGroup();

        string scene = EditorSceneManager.GetActiveScene().name;
        if (player.playOnStart == null && StartCode.TryGetValue(scene, out var code))
            player.playOnStart = ClipFor(lines, code);

        var source = player.GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        if (player.output != null) source.outputAudioMixerGroup = player.output;

        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(subs);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        Selection.activeObject = player.gameObject;

        int sinClip = 0;
        foreach (var l in lines.lines) if (l.clip == null) sinClip++;
        Debug.Log($"[Voces] Escena {scene} lista. Output: {(player.output != null ? player.output.name : "SIN GRUPO (asígnalo a mano)")}. " +
                  $"Bienvenida: {(player.playOnStart != null ? player.playOnStart.name : "ninguna")}. Guiones sin clip: {sinClip}.");
    }

    [MenuItem(Menu + "Volver a buscar los clips de los guiones")]
    public static void RefreshClips()
    {
        var lines = EnsureLines();
        AssignClips(lines, overwrite: true);
    }

    [MenuItem(Menu + "Seleccionar el asset de guiones")]
    public static void SelectLines() => Selection.activeObject = EnsureLines();

    // ------------------------------------------------------------------------------------------------

    private static VoiceLines EnsureLines()
    {
        var lines = AssetDatabase.LoadAssetAtPath<VoiceLines>(LinesPath);
        if (lines == null)
        {
            EnsureFolder(Folder);
            lines = ScriptableObject.CreateInstance<VoiceLines>();
            AssetDatabase.CreateAsset(lines, LinesPath);
        }

        // Agrega los guiones que falten; no toca los textos que ya estén (pudieron editarse a mano).
        bool changed = false;
        foreach (var (code, text) in Scripts)
        {
            if (lines.lines.Exists(l => l.code == code)) continue;
            lines.lines.Add(new VoiceLines.Line { code = code, text = text });
            changed = true;
        }
        if (AssignClips(lines, overwrite: false)) changed = true;
        if (changed)
        {
            EditorUtility.SetDirty(lines);
            AssetDatabase.SaveAssets();
        }
        return lines;
    }

    private static bool AssignClips(VoiceLines lines, bool overwrite)
    {
        var clips = new List<AudioClip>();
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" }))
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
            if (c != null) clips.Add(c);
        }

        bool changed = false;
        foreach (var l in lines.lines)
        {
            if (l.clip != null && !overwrite) continue;
            var found = clips.Find(c => VoiceLines.NameMatches(c.name, l.code));
            if (found != null && found != l.clip) { l.clip = found; changed = true; }
        }
        if (changed)
        {
            EditorUtility.SetDirty(lines);
            AssetDatabase.SaveAssets();
        }
        return changed;
    }

    private static AudioClip ClipFor(VoiceLines lines, string code)
    {
        var l = lines.lines.Find(x => x.code == code);
        return l != null ? l.clip : null;
    }

    private static Material EnsureMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat != null) return mat;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        var shader = Shader.Find("TextMeshPro/Distance Field Overlay");
        if (font == null || shader == null)
        {
            Debug.LogWarning("[Voces] No encontré la fuente Eczar o el shader Distance Field Overlay; los subtítulos usarán el material de la fuente (las paredes podrían taparlos).");
            return null;
        }

        EnsureFolder(Folder);
        mat = new Material(font.material) { shader = shader, name = "Subtitulos_Overlay" };
        // Contorno oscuro para que se lea sobre cualquier fondo.
        mat.EnableKeyword("OUTLINE_ON");
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.25f);
        mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.9f));
        mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.1f);
        AssetDatabase.CreateAsset(mat, MaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static AudioMixerGroup FindVoicesGroup()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:AudioMixer"))
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(AssetDatabase.GUIDToAssetPath(guid));
            if (mixer == null) continue;
            foreach (var name in new[] { "Voices", "Voice", "Voces", "Voz" })
            {
                var groups = mixer.FindMatchingGroups(name);
                foreach (var g in groups) if (g.name == name) return g;
            }
        }
        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
