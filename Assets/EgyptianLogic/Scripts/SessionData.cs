using System;
using System.Collections.Generic;
using UnityEngine;

public class SessionData : MonoBehaviour
{
    // ---------- Singleton que se crea solo ----------
    private static SessionData instance;

    public static SessionData Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("SessionData");
                instance = go.AddComponent<SessionData>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---------- Identidad ----------
    public string PlayerName { get; private set; } = "";

    /// <summary>Alias limpio para usar en nombres de archivo y en los CSV.</summary>
    public string ParticipantId =>
        string.IsNullOrWhiteSpace(PlayerName) ? "anon" : Sanitize(PlayerName);

    public void SetPlayerName(string name) => PlayerName = (name ?? "").Trim();

    // ---------- Diagnóstico inicial ----------
    public int DiagnosticCorrect { get; private set; } = -1;   // -1 = no hizo el test
    public float InitialTheta { get; private set; } = 0f;
    public int InitialVersion { get; private set; } = 1;       // V2 por defecto

    /// <summary>correct: 0, 1 o 2 aciertos en el test de sondeo.</summary>
    public void SetDiagnostic(int correct)
    {
        DiagnosticCorrect = Mathf.Clamp(correct, 0, 2);
        switch (DiagnosticCorrect)
        {
            case 0: InitialVersion = 0; InitialTheta = -0.6f; break;
            case 1: InitialVersion = 1; InitialTheta = 0f; break;
            default: InitialVersion = 2; InitialTheta = 0.6f; break;
        }
    }

    // ---------- Opciones ----------
    public float MusicVolume01 { get; set; } = 0.8f;
    public float SfxVolume01 { get; set; } = 0.8f;

    // ---------- Resultados de la partida ----------
    private readonly List<StageResult> results = new List<StageResult>();
    private readonly List<float> thetaHistory = new List<float>();

    public IReadOnlyList<StageResult> Results => results;
    public IReadOnlyList<float> ThetaHistory => thetaHistory;
    public float FinalTheta { get; private set; }
    public bool HasResults => results.Count > 0;

    public void AddResult(StageResult r)
    {
        // Si el reto ya estaba, se reemplaza (por si se reinicia una partida)
        results.RemoveAll(x => x.stageIndex == r.stageIndex);
        results.Add(r);
        results.Sort((a, b) => a.stageIndex.CompareTo(b.stageIndex));
    }

    public void RecordTheta(float theta)
    {
        FinalTheta = theta;
        thetaHistory.Add(theta);
    }

    /// <summary>Borra los datos de una partida, conserva nombre, opciones y diagnóstico.</summary>
    public void ClearRun()
    {
        results.Clear();
        thetaHistory.Clear();
        FinalTheta = 0f;
    }

    /// <summary>Borra todo (volver al menú para que juegue otra persona).</summary>
    public void ClearAll()
    {
        ClearRun();
        PlayerName = "";
        DiagnosticCorrect = -1;
        InitialTheta = 0f;
        InitialVersion = 1;
    }

    private static string Sanitize(string s)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var chars = new List<char>();
        foreach (char c in s.Trim())
        {
            if (Array.IndexOf(invalid, c) >= 0) continue;
            chars.Add(char.IsWhiteSpace(c) ? '_' : c);
        }
        return chars.Count == 0 ? "anon" : new string(chars.ToArray());
    }
}