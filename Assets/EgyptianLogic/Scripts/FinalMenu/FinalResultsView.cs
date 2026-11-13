using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pantalla de resultados de la tablet. Lee SessionData, arma el ReportModel y llena los textos.
/// Las filas de niveles y de retos difíciles se clonan desde una plantilla (hija desactivada).
/// </summary>
public class FinalResultsView : MonoBehaviour
{
    [Header("Contenido")]
    [Tooltip("Qué tema trabaja cada reto. Si un reto no está aquí, se muestra con su nombre del tracker.")]
    public List<StageTopic> topics = FinalTopics.Defaults();
    public string[] levelNames = (string[])FinalTopics.DefaultLevelNames.Clone();
    public ReportSettings settings = new ReportSettings();
    [Tooltip("Solo en el editor: si no hay resultados (abriste la escena directo), muestra datos de ejemplo.")]
    public bool sampleDataInEditor = true;

    [Header("Textos")]
    public TMP_Text greeting;
    public TMP_Text overallScore;
    public TMP_Text overallLabel;
    public TMP_Text suggestedLevel;
    public TMP_Text summaryLine;
    public TMP_Text topicsText;
    public TMP_Text emptyHardText;

    [Header("Listas")]
    [Tooltip("Fila de nivel: hijos \"Nombre\" (TMP), \"Valor\" (TMP) y \"Barra/Relleno\" (Image).")]
    public RectTransform levelRowTemplate;
    [Tooltip("Fila de reto difícil: hijos \"Titulo\" (TMP) y \"Motivo\" (TMP).")]
    public RectTransform hardRowTemplate;

    [Header("Colores por dominio")]
    public Color masteredColor = new Color(0.50f, 0.69f, 0.41f);
    public Color progressColor = new Color(0.85f, 0.70f, 0.37f);
    public Color reviewColor = new Color(0.78f, 0.33f, 0.24f);

    public ReportModel Model { get; private set; }

    private readonly List<GameObject> spawned = new List<GameObject>();

    private void Start() => Refresh();

    [ContextMenu("Actualizar")]
    public void Refresh()
    {
        var session = SessionData.Instance;
        IReadOnlyList<StageResult> results = session.Results;
        float theta = session.FinalTheta;
        bool sample = false;

        if (results.Count == 0 && sampleDataInEditor && Application.isEditor)
        {
            results = FinalReport.SampleResults();
            theta = 0.1f;
            sample = true;
        }

        Model = FinalReport.Build(results, topics, levelNames, theta, session.PlayerName, settings, DateTime.Now);
        Model.isSample = sample;
        Fill();
    }

    private void Fill()
    {
        var m = Model;
        foreach (var go in spawned) if (go != null) Destroy(go);
        spawned.Clear();

        string who = string.IsNullOrWhiteSpace(m.playerName) ? "" : " " + m.playerName;
        Set(greeting, $"Informe del Dr.{who}" + (m.isSample ? "  <size=70%><color=#A8987C>(datos de ejemplo)</color></size>" : ""));

        if (!m.hasData)
        {
            Set(overallScore, "-");
            Set(overallLabel, "Sin datos");
            Set(suggestedLevel, "");
            Set(summaryLine, "No se registraron retos en esta partida.");
            Set(topicsText, "");
            Show(emptyHardText, true, "");
            return;
        }

        Set(overallScore, FinalReport.Score100(m.overallP).ToString());
        if (overallScore != null) overallScore.color = ColorOf(m.overallMastery);
        Set(overallLabel, FinalReport.MasteryLabel(m.overallMastery));
        Set(suggestedLevel, $"Nivel sugerido: {m.suggestedLevel}");
        Set(summaryLine, $"{m.stagesPlayed} retos  ·  {FinalReport.FormatTime(m.totalTime)} de juego");

        if (levelRowTemplate != null)
        {
            levelRowTemplate.gameObject.SetActive(false);
            foreach (var l in m.levels)
            {
                var row = Clone(levelRowTemplate);
                Set(Find<TMP_Text>(row, "Nombre"), l.name);
                Set(Find<TMP_Text>(row, "Valor"), FinalReport.Score100(l.meanP).ToString());
                var fill = Find<Image>(row, "Barra/Relleno");
                if (fill != null)
                {
                    var rt = fill.rectTransform;
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = new Vector2(Mathf.Clamp01(l.meanP), 1f);
                    rt.offsetMin = rt.offsetMax = Vector2.zero;
                    fill.color = ColorOf(l.mastery);
                }
            }
        }

        if (hardRowTemplate != null)
        {
            hardRowTemplate.gameObject.SetActive(false);
            foreach (var h in m.hard)
            {
                var row = Clone(hardRowTemplate);
                Set(Find<TMP_Text>(row, "Titulo"), $"{h.name}  <color=#A8987C>{FinalReport.Score100(h.result.P)}/100</color>");
                Set(Find<TMP_Text>(row, "Motivo"), h.reasons);
            }
        }
        Show(emptyHardText, m.hard.Count == 0, "Ningún reto se te resistió. ¡Buen trabajo!");

        if (m.topicsToReview.Count == 0)
            Set(topicsText, "No quedan temas pendientes. Puedes volver a jugar para intentar las versiones más difíciles.");
        else
            Set(topicsText, "• " + string.Join("\n• ", m.topicsToReview));
    }

    public Color ColorOf(Mastery m) =>
        m == Mastery.Dominado ? masteredColor : m == Mastery.EnProgreso ? progressColor : reviewColor;

    private RectTransform Clone(RectTransform template)
    {
        var go = Instantiate(template.gameObject, template.parent);
        go.name = template.name.Replace("Plantilla", "").Trim('_', ' ');
        go.SetActive(true);
        spawned.Add(go);
        return (RectTransform)go.transform;
    }

    private static T Find<T>(Transform root, string path) where T : Component
    {
        var t = root.Find(path);
        return t != null ? t.GetComponent<T>() : null;
    }

    private static void Set(TMP_Text t, string value)
    {
        if (t != null) t.text = value;
    }

    private static void Show(TMP_Text t, bool on, string value)
    {
        if (t == null) return;
        t.gameObject.SetActive(on);
        t.text = value;
    }

    [ContextMenu("Restaurar temas por defecto")]
    private void ResetTopics()
    {
        topics = FinalTopics.Defaults();
        levelNames = (string[])FinalTopics.DefaultLevelNames.Clone();
    }
}
