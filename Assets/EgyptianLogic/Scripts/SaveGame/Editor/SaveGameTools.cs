using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Menú "Egyptian Logic > Partida guardada": configurar el menú de inicio y manejar las partidas de prueba.</summary>
public static class SaveGameTools
{
    private const string Menu = "Egyptian Logic/Partida guardada/";

    [MenuItem(Menu + "Configurar menú de inicio (escena abierta)")]
    public static void SetupMainMenu()
    {
        var mainMenu = Find("Menu/MainMenu") ?? Find("MainMenu");
        if (mainMenu == null)
        {
            EditorUtility.DisplayDialog("Partida guardada",
                "No encontré el objeto \"Menu/MainMenu\". Abre EgyptMainMenu, o agrega SaveMenu a mano y asigna sus campos.", "OK");
            return;
        }

        var menu = mainMenu.GetComponent<SaveMenu>();
        if (menu == null) menu = Undo.AddComponent<SaveMenu>(mainMenu.gameObject);
        Undo.RecordObject(menu, "Configurar SaveMenu");

        menu.playButton = Button(mainMenu, "Cube (1)", out menu.playLabel);
        menu.exitButton = Button(mainMenu, "Cube (3)", out menu.exitLabel);
        menu.nameKeyboard = Object.FindObjectOfType<CubeKeyboard>();
        EditorUtility.SetDirty(menu);
        EditorSceneManager.MarkSceneDirty(mainMenu.gameObject.scene);

        string faltan = (menu.playButton == null ? "\n- Botón Play (Cube (1))" : "") +
                        (menu.exitButton == null ? "\n- Botón Exit (Cube (3))" : "") +
                        (menu.nameKeyboard == null ? "\n- CubeKeyboard (teclado del nombre)" : "");
        Selection.activeObject = menu;
        EditorUtility.DisplayDialog("Partida guardada",
            faltan.Length == 0
                ? "SaveMenu quedó configurado en MainMenu. Guarda la escena."
                : "SaveMenu se agregó, pero faltan por asignar a mano:" + faltan, "OK");
    }

    [MenuItem(Menu + "Ver partida guardada del editor")]
    public static void Show()
    {
        if (!SaveSystem.TryLoad(out var g))
        {
            Debug.Log($"[SaveSystem] No hay partida guardada en {SaveSystem.FilePath}");
            return;
        }
        Debug.Log($"[SaveSystem] Partida de \"{g.playerName}\" en {g.scene}, guardada el {g.savedAt} (sesión {g.session}). " +
                  $"Retos con resultado: {g.results.Count}. θ: {g.finalTheta:F2}. Archivo: {SaveSystem.FilePath}");
    }

    [MenuItem(Menu + "Borrar partida guardada del editor")]
    public static void DeleteEditorSave()
    {
        SaveSystem.Delete();
        Debug.Log("[SaveSystem] Partida del editor borrada.");
    }

    [MenuItem(Menu + "Borrar partida guardada del build de PC")]
    public static void DeleteBuildSave()
    {
        string path = Path.Combine(Application.persistentDataPath, "partida.json");
        if (File.Exists(path)) File.Delete(path);
        Debug.Log("[SaveSystem] Partida del build de PC borrada (si existía): " + path);
    }

    [MenuItem(Menu + "Abrir carpeta de partidas y CSV")]
    public static void OpenFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath + "/");

    private static Transform Find(string path)
    {
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (path == root.name) return root.transform;
            if (path.StartsWith(root.name + "/"))
            {
                var t = root.transform.Find(path.Substring(root.name.Length + 1));
                if (t != null) return t;
            }
        }
        return null;
    }

    private static XRPhysicalButton Button(Transform parent, string name, out TMP_Text label)
    {
        label = null;
        var t = parent.Find(name);
        if (t == null) return null;
        label = t.GetComponentInChildren<TMP_Text>(true);
        return t.GetComponent<XRPhysicalButton>();
    }
}
