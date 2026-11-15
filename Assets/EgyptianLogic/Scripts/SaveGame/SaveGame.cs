using System;
using System.Collections.Generic;

/// <summary>
/// Partida guardada (un archivo JSON por dispositivo). Se guarda al entrar a cada nivel y al menú final,
/// con lo que había en SessionData en ese momento: al reanudar, el nivel empieza de cero con los datos
/// de los niveles anteriores.
/// </summary>
[Serializable]
public class SaveGame
{
    public int formatVersion = SaveSystem.FormatVersion;

    /// <summary>Escena a la que se vuelve al reanudar.</summary>
    public string scene;
    public string savedAt;

    public string playerName;
    public int diagnosticCorrect = -1;
    public float initialTheta;
    public int initialVersion = 1;
    public float musicVolume = 0.8f, sfxVolume = 0.8f;

    public List<StageResult> results = new List<StageResult>();
    public List<float> thetaHistory = new List<float>();
    public float finalTheta;
    public int decisionsMade;
    public int lastPlayedVersion = -1;

    /// <summary>Los CSV de la partida: al reanudar se sigue escribiendo en los mismos archivos.</summary>
    public string stageCsvPath, decisionCsvPath;

    /// <summary>Sesión en la que se guardó (1 = la primera). Al reanudar, la nueva sesión es esta + 1.</summary>
    public int session = 1;
}
