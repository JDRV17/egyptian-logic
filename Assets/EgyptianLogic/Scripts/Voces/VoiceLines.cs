using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tabla de guiones: código (N2-03), clip y texto del subtítulo. Un solo asset para todo el juego.
/// Se crea y se llena con Egyptian Logic → Voces → Configurar voz y subtítulos en esta escena.
/// </summary>
[CreateAssetMenu(menuName = "Egyptian Logic/Guiones de voz", fileName = "VoiceLines")]
public class VoiceLines : ScriptableObject
{
    [Serializable]
    public class Line
    {
        public string code;
        public AudioClip clip;
        [TextArea(3, 10)] public string text;
    }

    public List<Line> lines = new List<Line>();

    /// <summary>Texto del clip: primero por referencia al clip, luego por el nombre del archivo (que empiece con el código).</summary>
    public string TextFor(AudioClip clip)
    {
        if (clip == null) return null;
        foreach (var l in lines)
            if (l != null && l.clip == clip) return l.text;
        foreach (var l in lines)
            if (l != null && !string.IsNullOrEmpty(l.code) && NameMatches(clip.name, l.code)) return l.text;
        return null;
    }

    /// <summary>"N2-03", "N2-03 baldosa" o "N2-03_v2" son del código N2-03; "N2-031" no.</summary>
    public static bool NameMatches(string clipName, string code)
    {
        if (string.IsNullOrEmpty(clipName) || string.IsNullOrEmpty(code)) return false;
        if (!clipName.StartsWith(code, StringComparison.OrdinalIgnoreCase)) return false;
        return clipName.Length == code.Length || !char.IsDigit(clipName[code.Length]);
    }
}
