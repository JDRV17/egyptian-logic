using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class CsvStageLogger : MonoBehaviour
{
    [SerializeField] private PerformanceTracker tracker;
    [Tooltip("Identificador del voluntario (sin datos personales).")]
    [SerializeField] private string participantId = "test";

    private string path;

    private void OnEnable() { if (tracker != null) tracker.onStageCompleted += Write; }
    private void OnDisable() { if (tracker != null) tracker.onStageCompleted -= Write; }

    private void Write(StageResult r)
    {
        SessionData.Instance.AddResult(r);

        string pid = SessionData.Instance.PlayerName != ""
        ? SessionData.Instance.ParticipantId
        : participantId;

        if (path == null)
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            path = Path.Combine(Application.persistentDataPath, $"gatekeeper_{pid}_{stamp}.csv");
            File.WriteAllText(path,
                "participante,reto,version,tiempo_s,tiempo_pausado_pistas_s,intentos,interacciones,interacciones_ideal,pistas,tope_solucion,S_T,S_A,S_I,S_H,P\n",
                Encoding.UTF8);
            Debug.Log($"[CsvStageLogger] Guardando en: {path}");
        }

        var c = CultureInfo.InvariantCulture;   // evita comas decimales
        string line = string.Join(",",
            pid, r.stageName, "V" + (r.version + 1),
            r.time.ToString("F2", c), r.hintPauseTime.ToString("F2", c),
            r.attempts, r.interactions, r.idealInteractions, r.hints, r.cappedBySolution ? 1 : 0,
            r.sT.ToString("F3", c), r.sA.ToString("F3", c), r.sI.ToString("F3", c),
            r.sH.ToString("F3", c), r.P.ToString("F3", c));

        File.AppendAllText(path, line + "\n", Encoding.UTF8);
    }

    [ContextMenu("Abrir carpeta de datos")]
    private void OpenFolder() => Application.OpenURL(Application.persistentDataPath);
}