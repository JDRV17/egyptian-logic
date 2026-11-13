using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>Qué tema trabaja cada reto. Se edita en el Inspector de FinalResultsView.</summary>
[Serializable]
public class StageTopic
{
    public int level = 1;
    [UnityEngine.Tooltip("Índice del reto dentro de su nivel (0 = primer reto).")]
    public int stageIndex;
    [UnityEngine.Tooltip("Código que ve el jugador, por ejemplo 1.3.")]
    public string code;
    [UnityEngine.Tooltip("Nombre del reto (puede quedar vacío).")]
    public string title;
    [UnityEngine.Tooltip("Tema de lógica digital que se recomienda repasar si el reto costó.")]
    public string topic;

    public StageTopic() { }

    public StageTopic(int level, int stageIndex, string code, string title, string topic)
    {
        this.level = level;
        this.stageIndex = stageIndex;
        this.code = code;
        this.title = title;
        this.topic = topic;
    }

    /// <summary>"3.4 El escarabajo mecánico" o, sin título, "Reto 1.3".</summary>
    public string DisplayName =>
        string.IsNullOrWhiteSpace(title) ? $"Reto {code}" : $"{code} {title}";
}

/// <summary>Valores por defecto de la tabla reto → tema.</summary>
public static class FinalTopics
{
    public static readonly string[] DefaultLevelNames =
    {
        "Compuertas y sistemas numéricos",
        "Lógica combinacional",
        "Lógica secuencial"
    };

    public static List<StageTopic> Defaults()
    {
        return new List<StageTopic>
        {
            new StageTopic(1, 0, "1.1", "", "Compuertas lógicas (AND, OR, XOR, NOR, NOT)"),
            new StageTopic(1, 1, "1.2", "", "Cómo viaja una señal por un circuito de compuertas"),
            new StageTopic(1, 2, "1.3", "", "Sistemas numéricos: binario, decimal y hexadecimal"),
            new StageTopic(1, 3, "1.4", "", "Bits, combinaciones y logaritmos en base 2"),
            new StageTopic(1, 4, "1.5", "", "Números negativos en complemento a 2"),

            new StageTopic(2, 0, "2.1", "El detective de verdades", "Tablas de verdad"),
            new StageTopic(2, 1, "2.2", "Solo tengo esto", "Compuertas universales (NAND)"),
            new StageTopic(2, 2, "2.3", "El ahorro de compuertas", "Simplificación con mapas de Karnaugh"),
            new StageTopic(2, 3, "2.4", "El selector", "Multiplexores"),
            new StageTopic(2, 4, "2.5", "El repartidor", "Decodificadores"),

            new StageTopic(3, 0, "3.1", "El cofre con memoria", "Latches y la señal Enable (latch D)"),
            new StageTopic(3, 1, "3.2", "El registro de desplazamiento", "Registros de desplazamiento"),
            new StageTopic(3, 2, "3.3", "El guardián de estados", "Máquinas de estados: leer un diagrama de transiciones"),
            new StageTopic(3, 3, "3.4", "El escarabajo mecánico", "Diseño de máquinas de estados"),
            new StageTopic(3, 4, "3.5", "Contrarreloj binaria", "Contadores binarios")
        };
    }
}

public enum Mastery { PorRepasar, EnProgreso, Dominado }

[Serializable]
public class ReportSettings
{
    [UnityEngine.Tooltip("P promedio desde el cual un tema se considera dominado.")]
    [UnityEngine.Range(0f, 1f)] public float masteredAt = 0.75f;
    [UnityEngine.Tooltip("P promedio desde el cual un tema va en progreso (debajo: por repasar).")]
    [UnityEngine.Range(0f, 1f)] public float progressAt = 0.5f;
    [UnityEngine.Tooltip("Un reto entra en \"lo que más te costó\" si su P es menor que esto.")]
    [UnityEngine.Range(0f, 1f)] public float hardBelow = 0.75f;
    [UnityEngine.Tooltip("Máximo de retos en \"lo que más te costó\".")]
    [UnityEngine.Range(1, 6)] public int maxHard = 3;
    [UnityEngine.Tooltip("θ desde el cual se sugiere el nivel intermedio y el avanzado (como en el motor).")]
    public float intermediateFrom = -0.5f, advancedFrom = 0.5f;
}

public class LevelSummary
{
    public int level;
    public string name;
    public float meanP;
    public int played;
    public Mastery mastery;
}

public class HardStage
{
    public StageResult result;
    public string name;
    public string topic;
    public string reasons;
}

/// <summary>Todo lo que muestra la tablet y lo que va en el PDF. Se calcula una vez con FinalReport.Build.</summary>
public class ReportModel
{
    public string playerName;
    public DateTime date;
    public bool hasData;
    public bool isSample;
    public float overallP;
    public Mastery overallMastery;
    public string suggestedLevel;
    public float totalTime;
    public int stagesPlayed;
    public readonly List<LevelSummary> levels = new List<LevelSummary>();
    public readonly List<HardStage> hard = new List<HardStage>();
    public readonly List<string> topicsToReview = new List<string>();
    public readonly List<(StageResult result, StageTopic topic)> stages = new List<(StageResult, StageTopic)>();
}

/// <summary>Lógica pura (sin escena): resume los StageResult de SessionData para el informe final.</summary>
public static class FinalReport
{
    public static ReportModel Build(IReadOnlyList<StageResult> results, IList<StageTopic> topics,
                                    IList<string> levelNames, float finalTheta, string playerName,
                                    ReportSettings settings, DateTime date)
    {
        settings = settings ?? new ReportSettings();
        var model = new ReportModel
        {
            playerName = string.IsNullOrWhiteSpace(playerName) ? "" : playerName.Trim(),
            date = date
        };

        if (results == null || results.Count == 0)
        {
            model.suggestedLevel = "-";
            return model;
        }

        model.hasData = true;
        float sum = 0f;
        var byLevel = new SortedDictionary<int, List<StageResult>>();
        foreach (var r in results)
        {
            sum += r.P;
            model.totalTime += r.time;
            if (!byLevel.TryGetValue(r.level, out var list)) byLevel[r.level] = list = new List<StageResult>();
            list.Add(r);
            model.stages.Add((r, Find(topics, r)));
        }

        model.stages.Sort((a, b) => a.result.level != b.result.level
            ? a.result.level.CompareTo(b.result.level)
            : a.result.stageIndex.CompareTo(b.result.stageIndex));
        model.stagesPlayed = results.Count;
        model.overallP = sum / results.Count;
        model.overallMastery = MasteryOf(model.overallP, settings);
        model.suggestedLevel = SuggestedLevel(finalTheta, settings);

        foreach (var kv in byLevel)
        {
            float s = 0f;
            foreach (var r in kv.Value) s += r.P;
            float mean = s / kv.Value.Count;
            model.levels.Add(new LevelSummary
            {
                level = kv.Key,
                name = LevelName(levelNames, kv.Key),
                meanP = mean,
                played = kv.Value.Count,
                mastery = MasteryOf(mean, settings)
            });
        }

        // Retos más difíciles: P más bajo primero; a igual P, más intentos primero.
        var sorted = new List<StageResult>(results);
        sorted.Sort((a, b) =>
        {
            int c = a.P.CompareTo(b.P);
            return c != 0 ? c : b.attempts.CompareTo(a.attempts);
        });

        foreach (var r in sorted)
        {
            if (model.hard.Count >= settings.maxHard) break;
            if (r.P >= settings.hardBelow) break;
            var t = Find(topics, r);
            model.hard.Add(new HardStage
            {
                result = r,
                name = t != null ? t.DisplayName : (string.IsNullOrWhiteSpace(r.stageName) ? $"Reto {r.level}.{r.stageIndex + 1}" : r.stageName),
                topic = t != null ? t.topic : "",
                reasons = Reasons(r)
            });
        }

        foreach (var h in model.hard)
            if (!string.IsNullOrWhiteSpace(h.topic) && !model.topicsToReview.Contains(h.topic))
                model.topicsToReview.Add(h.topic);

        return model;
    }

    public static StageTopic Find(IList<StageTopic> topics, StageResult r)
    {
        if (topics == null) return null;
        foreach (var t in topics)
            if (t != null && t.level == r.level && t.stageIndex == r.stageIndex) return t;
        return null;
    }

    public static string LevelName(IList<string> names, int level)
    {
        string baseName = $"Nivel {level}";
        if (names != null && level >= 1 && level <= names.Count && !string.IsNullOrWhiteSpace(names[level - 1]))
            return $"{baseName} · {names[level - 1]}";
        return baseName;
    }

    public static Mastery MasteryOf(float p, ReportSettings s)
    {
        if (p >= s.masteredAt) return Mastery.Dominado;
        if (p >= s.progressAt) return Mastery.EnProgreso;
        return Mastery.PorRepasar;
    }

    public static string MasteryLabel(Mastery m)
    {
        switch (m)
        {
            case Mastery.Dominado: return "Dominado";
            case Mastery.EnProgreso: return "En progreso";
            default: return "Por repasar";
        }
    }

    public static string SuggestedLevel(float theta, ReportSettings s)
    {
        if (theta >= s.advancedFrom) return "Avanzado";
        if (theta >= s.intermediateFrom) return "Intermedio";
        return "Básico";
    }

    /// <summary>Hasta dos motivos, en orden de importancia, en segunda persona.</summary>
    public static string Reasons(StageResult r)
    {
        var list = new List<string>();
        if (r.cappedBySolution) list.Add("usaste la pista con la solución");

        int failed = r.attempts - 1;
        if (failed >= 1) list.Add(failed == 1 ? "1 intento fallido" : $"{failed} intentos fallidos");

        if (!r.cappedBySolution && r.hints > 0) list.Add(r.hints == 1 ? "pediste 1 pista" : $"pediste {r.hints} pistas");

        if (r.idealInteractions > 0 && r.interactions >= 2 * r.idealInteractions)
            list.Add("hiciste muchos movimientos de más");

        if (r.idealTime > 0f && r.time > 2f * r.idealTime)
            list.Add("tardaste más del doble del tiempo esperado");

        if (list.Count == 0) return "Tu puntaje en este reto quedó por debajo de lo esperado.";
        if (list.Count > 2) list.RemoveRange(2, list.Count - 2);
        string s = string.Join(" y ", list);
        return char.ToUpper(s[0], CultureInfo.InvariantCulture) + s.Substring(1) + ".";
    }

    public static int Score100(float p) => (int)Math.Round(Math.Max(0f, Math.Min(1f, p)) * 100f, MidpointRounding.AwayFromZero);

    public static string FormatTime(float seconds)
    {
        int total = (int)Math.Round(Math.Max(0f, seconds));
        int h = total / 3600, m = (total % 3600) / 60, s = total % 60;
        return h > 0 ? $"{h}:{m:00}:{s:00}" : $"{m}:{s:00}";
    }

    /// <summary>Datos de ejemplo para probar la escena sin jugar los niveles (solo se usan en el editor).</summary>
    public static List<StageResult> SampleResults()
    {
        StageResult R(int level, int idx, int version, float p, int attempts, int hints, bool cap,
                      float time, float ideal, int inter, int idealInter) => new StageResult
        {
            stageName = $"{level}.{idx + 1}", level = level, stageIndex = idx, version = version, P = p,
            attempts = attempts, hints = hints, cappedBySolution = cap, time = time, idealTime = ideal,
            interactions = inter, idealInteractions = idealInter
        };

        return new List<StageResult>
        {
            R(1, 0, 1, 0.92f, 1, 0, false, 55f, 60f, 9, 9),
            R(1, 1, 1, 0.81f, 1, 1, false, 31f, 25f, 3, 2),
            R(1, 2, 2, 0.64f, 2, 1, false, 140f, 90f, 14, 9),
            R(1, 3, 1, 0.88f, 1, 0, false, 40f, 35f, 9, 8),
            R(1, 4, 1, 0.30f, 3, 3, true, 120f, 25f, 15, 6),
            R(3, 0, 1, 0.85f, 1, 0, false, 22f, 20f, 2, 2),
            R(3, 2, 1, 0.41f, 4, 2, false, 70f, 24f, 20, 6),
            R(3, 3, 1, 0.58f, 2, 1, false, 260f, 120f, 30, 13),
            R(3, 4, 1, 0.77f, 1, 1, false, 80f, 75f, 6, 5),
            R(3, 1, 1, 0.95f, 1, 0, false, 35f, 40f, 4, 4)
        };
    }

    /// <summary>Texto plano del informe (útil para el cuerpo del correo y para depurar).</summary>
    public static string ToPlainText(ReportModel m)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Desempeño general: {Score100(m.overallP)}/100 ({MasteryLabel(m.overallMastery)})");
        sb.AppendLine($"Nivel sugerido: {m.suggestedLevel}");
        foreach (var l in m.levels) sb.AppendLine($"{l.name}: {Score100(l.meanP)}/100");
        if (m.topicsToReview.Count > 0)
        {
            sb.AppendLine("Temas para repasar:");
            foreach (var t in m.topicsToReview) sb.AppendLine("- " + t);
        }
        return sb.ToString();
    }
}
