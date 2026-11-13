using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Limpieza del juego base (menú Egyptian Logic → Limpieza del juego base).
/// Paso 1: mueve a Assets/EgyptianLogic/Heredado los archivos del juego base que sí se usan. Mover con AssetDatabase
///         conserva el GUID (.meta), así que las escenas y prefabs siguen apuntando a ellos.
/// Paso 2: manda a la papelera del sistema las carpetas y archivos del juego base que quedaron sin uso.
/// La lista sale de un análisis de dependencias del repo (13 nov 2026): GUIDs desde las escenas, prefabs, materiales,
/// controladores y .meta de todo Assets/EgyptianLogic, más las clases que usan sus scripts.
/// </summary>
public static class LimpiezaJuegoBase
{
    private const string Menu = "Egyptian Logic/Limpieza del juego base/";
    private const string Destino = "Assets/EgyptianLogic/Heredado";

    /// <summary>Archivos fuera de EgyptianLogic que el juego sí usa.</summary>
    public static readonly string[] Usados =
    {
        "Assets/Audio/popselect.wav",
        "Assets/Blue.cs",
        "Assets/CharacterPushObjects.cs",
        "Assets/Fonts/Animations/FadeIDLE.anim",
        "Assets/Fonts/Animations/FadeIn.anim",
        "Assets/Fonts/Animations/FadeOut.anim",
        "Assets/Fonts/Animations/FadeScreen.controller",
        "Assets/Fonts/Animations/FadeTime.anim",
        "Assets/HandBlock.cs",
        "Assets/LoafbrrAssets/DesertMoonSurface/Material/ORM_Shader_Vertex v2.shadergraph",
        "Assets/LoafbrrAssets/DesertMoonSurface/Material/Surf_Cliff_Desert_A.mat",
        "Assets/LoafbrrAssets/DesertMoonSurface/Material/Surf_Cliff_Moon_A.mat",
        "Assets/LoafbrrAssets/DesertMoonSurface/fbx/MoonDesertSurface.fbx",
        "Assets/LoafbrrAssets/DesertMoonSurface/prefab/MoundRocks/Mound_F.prefab",
        "Assets/LoafbrrAssets/DesertMoonSurface/prefab/MoundRocks/Rock_Cone_A.prefab",
        "Assets/LoafbrrAssets/DesertMoonSurface/prefab/MoundRocks/Rock_Cone_B.prefab",
        "Assets/LoafbrrAssets/DesertMoonSurface/prefab/MoundRocks/Rock_Cone_D.prefab",
        "Assets/LoafbrrAssets/DesertMoonSurface/prefab/MoundRocks/Rock_Formation_Big_A.prefab",
        "Assets/LoafbrrAssets/DesertMoonSurface/prefab/MoundRocks/Rock_Formation_Big_C.prefab",
        "Assets/LoafbrrAssets/DesertMoonSurface/prefab/MoundRocks/Rock_Formation_Big_D.prefab",
        "Assets/LoafbrrAssets/DesertMoonSurface/prefab/MoundRocks/Rock_Formation_Big_H.prefab",
        "Assets/LoafbrrAssets/DesertMoonSurface/prefab/MoundRocks/Rock_Formation_Med_D.prefab",
        "Assets/LoafbrrAssets/DesertMoonSurface/prefab/MoundRocks/Rock_Formation_Med_G.prefab",
        "Assets/LoafbrrAssets/DesertMoonSurface/textures/Surf_Cliffs/Surf_Cliffs_Diffuse_2K_A.png",
        "Assets/LoafbrrAssets/DesertMoonSurface/textures/Surf_Cliffs/Surf_Cliffs_Diffuse_2K_B.png",
        "Assets/LoafbrrAssets/DesertMoonSurface/textures/Surf_Cliffs/Surf_Cliffs_Normal_2K.png",
        "Assets/LoafbrrAssets/DesertMoonSurface/textures/Surf_Cliffs/Surf_Cliffs_ORM_2K.png",
        "Assets/LoafbrrAssets/DesertMoonSurface/textures/Surf_Desert/Surf_Desert_Diffuse_2K_A.png",
        "Assets/LoafbrrAssets/DesertMoonSurface/textures/Surf_Desert/Surf_Desert_Normal_2K.png",
        "Assets/LoafbrrAssets/DesertMoonSurface/textures/Surf_Desert/Surf_Desert_ORM_2K.png",
        "Assets/LoafbrrAssets/DesertMoonSurface/textures/Surf_Moon/Surf_Moon_Diffuse_2K_A.png",
        "Assets/LoafbrrAssets/DesertMoonSurface/textures/Surf_Moon/Surf_Moon_Normal_2K.png",
        "Assets/LoafbrrAssets/DesertMoonSurface/textures/Surf_Moon/Surf_Moon_ORM_2K.png",
        "Assets/Materials/FadeScreen_mat.mat",
        "Assets/Materials/Kenney/Light/Materials/texture_01.mat",
        "Assets/Materials/Light_WhitePanel.mat",
        "Assets/Materials/Metal Materials/M_YFMM_16.mat",
        "Assets/Materials/Metal Materials/Textures/T_16_Emissive.tga",
        "Assets/Materials/Metal Materials/Textures/T_16_Normal.tga",
        "Assets/Materials/Metal Materials/Textures/T_16_Specular.tga",
        "Assets/Materials/Textures/Lights/OneSidedVLight.png",
        "Assets/Objects/Room_CompSci/MainMenuButtons/Reiniciar.png",
        "Assets/Objects/Room_CompSci/MainMenuButtons/Salir.png",
        "Assets/Oculus Hands/Animations/LeftHand Controller.controller",
        "Assets/Oculus Hands/Animations/LeftHand_Default.anim",
        "Assets/Oculus Hands/Animations/LeftHand_Fist.anim",
        "Assets/Oculus Hands/Animations/LeftHand_Pinch.anim",
        "Assets/Oculus Hands/Models/l_hand_skeletal_lowres.fbx",
        "Assets/Oculus Hands/Models/r_hand_skeletal_lowres.fbx",
        "Assets/Prefabs/10 Buttons/Left Hand With Colliders.prefab",
        "Assets/Prefabs/10 Buttons/Right Hand With Colliders.prefab",
        "Assets/Random Junk/Bloop/RightHand Controller.controller",
        "Assets/Random Junk/Bloop/RightHand_Default.anim",
        "Assets/Random Junk/Bloop/RightHand_Fist.anim",
        "Assets/Random Junk/Bloop/RightHand_Pinch.anim",
        "Assets/Red.cs",
        "Assets/Scripts/5 XR Ray Interactors/HandComplete.cs",
        "Assets/Scripts/CameraFade.cs",
        "Assets/Scripts/DoorAccess.cs",
        "Assets/Scripts/MenuController.cs",
        "Assets/Scripts/PCport/ObjectZoomPC.cs",
        "Assets/Scripts/PCport/PC_crouch.cs",
        "Assets/Scripts/PCport/PC_inputActions.inputactions",
        "Assets/Scripts/PCport/PC_playerController.cs",
        "Assets/Scripts/PCport/PC_wristUI.cs",
        "Assets/Scripts/PuzzleTracking/GameCompletionChecker.cs",
        "Assets/Scripts/Random Useful Scripts/CharacterControllerHMDUpdater.cs",
        "Assets/Scripts/Timer.cs",
        "Assets/SkySeries Freebie/AmbienceExposure.mat",
        "Assets/SkySeries Freebie/FreebieHdri/AmbienceExposure4k.hdr"
    };

    /// <summary>Lo que se borra en el paso 2 (después de mover lo usado).</summary>
    public static readonly string[] ABorrar =
    {
        "Assets/Audio",
        "Assets/Fonts",
        "Assets/LoafbrrAssets",
        "Assets/Materials",
        "Assets/Models",
        "Assets/Objects",
        "Assets/Oculus Hands",
        "Assets/Prefabs",
        "Assets/Random Junk",
        "Assets/Real-Time Procedural Cable Simple",
        "Assets/Scenes",
        "Assets/Scripts",
        "Assets/SkySeries Freebie",
        "Assets/Tubes",
        "Assets/HanoiManager.cs",
        "Assets/HanoiPiece.cs",
        "Assets/HanoiSocket.cs",
        "Assets/MenuControllerVR.cs",
        "Assets/MouseVerticalRotation.cs",
        "Assets/ReturnToOrigin.cs",
        "Assets/returnSocket.cs",
        "Assets/VRCrouch.cs",
        "Assets/Practice.inputactions",
        "Assets/DEV_lyt.wlt",
        "Assets/RECORD_lyt.wlt"
    };

    public static string NuevaRuta(string ruta) => Destino + "/" + ruta.Substring("Assets/".Length);

    [MenuItem(Menu + "1. Mover lo que se usa a EgyptianLogic/Heredado")]
    public static void Mover()
    {
        var pendientes = Usados.Where(p => File.Exists(p)).ToList();
        if (pendientes.Count == 0)
        {
            EditorUtility.DisplayDialog("Limpieza", "No hay nada por mover: el paso 1 ya se hizo.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Limpieza: paso 1",
                $"Se van a mover {pendientes.Count} archivos del juego base a {Destino}, conservando sus referencias.\n\n" +
                "Antes de seguir, haz commit en git (o una copia del proyecto).", "Mover", "Cancelar"))
            return;

        int ok = 0;
        var errores = new List<string>();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var ruta in pendientes)
            {
                string nueva = NuevaRuta(ruta);
                CrearCarpetas(Path.GetDirectoryName(nueva).Replace('\\', '/'));
                string error = AssetDatabase.MoveAsset(ruta, nueva);
                if (string.IsNullOrEmpty(error)) ok++;
                else errores.Add($"{ruta}: {error}");
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        foreach (var e in errores) Debug.LogError("[Limpieza] " + e);
        Debug.Log($"[Limpieza] Movidos {ok} archivos a {Destino}. Errores: {errores.Count}.");
        EditorUtility.DisplayDialog("Limpieza: paso 1",
            $"Movidos: {ok}. Errores: {errores.Count} (ver consola).\n\n" +
            "Ahora abre las 5 escenas y dale Play para comprobar que todo sigue igual antes del paso 2.", "OK");
    }

    [MenuItem(Menu + "2. Mandar a la papelera lo que no se usa")]
    public static void Borrar()
    {
        var sinMover = Usados.Where(p => File.Exists(p)).ToList();
        if (sinMover.Count > 0)
        {
            EditorUtility.DisplayDialog("Limpieza: paso 2",
                $"Faltan {sinMover.Count} archivos usados por mover (por ejemplo {sinMover[0]}). Corre primero el paso 1.", "OK");
            return;
        }

        var existentes = ABorrar.Where(p => AssetDatabase.IsValidFolder(p) || File.Exists(p)).ToList();
        if (existentes.Count == 0)
        {
            EditorUtility.DisplayDialog("Limpieza", "No queda nada del juego base por borrar.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Limpieza: paso 2",
                $"Se van a mandar a la papelera del sistema {existentes.Count} carpetas y archivos del juego base:\n\n" +
                string.Join("\n", existentes.Take(30)) +
                "\n\nSe pueden recuperar desde la papelera o con git.", "Borrar", "Cancelar"))
            return;

        int ok = 0;
        foreach (var p in existentes)
        {
            if (AssetDatabase.MoveAssetToTrash(p)) ok++;
            else Debug.LogError("[Limpieza] No se pudo borrar " + p);
        }
        AssetDatabase.Refresh();
        Debug.Log($"[Limpieza] Enviados a la papelera: {ok} de {existentes.Count}.");
    }

    [MenuItem(Menu + "Ver qué falta (solo informa)")]
    public static void Informe()
    {
        int porMover = Usados.Count(p => File.Exists(p));
        int movidos = Usados.Count(p => File.Exists(NuevaRuta(p)));
        int porBorrar = ABorrar.Count(p => AssetDatabase.IsValidFolder(p) || File.Exists(p));
        Debug.Log($"[Limpieza] Usados por mover: {porMover}. Ya movidos: {movidos}/{Usados.Length}. Elementos del juego base por borrar: {porBorrar}.");
    }

    private static void CrearCarpetas(string carpeta)
    {
        if (AssetDatabase.IsValidFolder(carpeta)) return;
        string padre = Path.GetDirectoryName(carpeta).Replace('\\', '/');
        CrearCarpetas(padre);
        AssetDatabase.CreateFolder(padre, Path.GetFileName(carpeta));
    }
}
