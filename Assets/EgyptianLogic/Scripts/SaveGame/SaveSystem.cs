using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Guarda la partida al entrar a cada nivel (y al menú final) y la reanuda desde el menú de inicio.
/// Un solo archivo en Application.persistentDataPath: una partida por dispositivo. En el editor usa otro
/// nombre de archivo, para que las pruebas en el Game view no pisen la partida del build de PC.
/// No hace falta poner nada en las escenas: se engancha solo al cargar escenas.
/// </summary>
public static class SaveSystem
{
    public const int FormatVersion = 1;

    /// <summary>Escenas en las que se guarda la partida al entrar.</summary>
    public static readonly string[] CheckpointScenes =
    {
        "EgyptLevel1", "EgyptLevel2", "EgyptLevel3", "EgyptFinalMenu"
    };

    public static string FileName => Application.isEditor ? "partida_editor.json" : "partida.json";
    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSave => TryLoad(out _);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>sceneLoaded llega después de los Awake de la escena y antes de los Start.</summary>
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || !IsCheckpoint(scene.name)) return;

        var session = SessionData.Instance;
        if (!session.RunActive)
        {
            // Escena abierta directo en el editor: no hay partida que guardar.
            Debug.Log($"[SaveSystem] {scene.name}: no se guarda (la partida no empezó desde el menú de inicio).");
            return;
        }
        Save(session.Capture(scene.name));
    }

    public static bool IsCheckpoint(string sceneName) => Array.IndexOf(CheckpointScenes, sceneName) >= 0;

    public static bool Save(SaveGame game)
    {
        try
        {
            string path = FilePath, tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(game, true));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);   // escribir aparte y mover: si se apaga a mitad, no queda un archivo cortado
            Debug.Log($"[SaveSystem] Partida guardada en {game.scene} (sesión {game.session}): {path}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SaveSystem] No se pudo guardar la partida: " + e.Message);
            return false;
        }
    }

    public static bool TryLoad(out SaveGame game)
    {
        game = null;
        try
        {
            if (!File.Exists(FilePath)) return false;
            game = JsonUtility.FromJson<SaveGame>(File.ReadAllText(FilePath));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SaveSystem] La partida guardada no se pudo leer y se ignora: " + e.Message);
            game = null;
            return false;
        }

        if (game == null || game.formatVersion != FormatVersion || !IsCheckpoint(game.scene))
        {
            game = null;
            return false;
        }
        return true;
    }

    public static void Delete()
    {
        try
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            if (File.Exists(FilePath + ".tmp")) File.Delete(FilePath + ".tmp");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SaveSystem] No se pudo borrar la partida guardada: " + e.Message);
        }
    }

    /// <summary>Carga la partida en SessionData y abre su escena. Devuelve false si no hay partida válida.</summary>
    public static bool Resume()
    {
        if (!TryLoad(out var game)) return false;
        if (!Application.CanStreamedLevelBeLoaded(game.scene))
        {
            Debug.LogError($"[SaveSystem] La escena {game.scene} no está en Build Settings: no se puede reanudar.");
            return false;
        }

        SessionData.Instance.Restore(game);
        Debug.Log($"[SaveSystem] Reanudando en {game.scene} (guardada el {game.savedAt}, sesión {SessionData.Instance.Session}).");
        SceneManager.LoadScene(game.scene);
        return true;
    }
}
