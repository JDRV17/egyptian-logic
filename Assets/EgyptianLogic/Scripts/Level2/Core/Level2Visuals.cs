using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>Cambios de color que funcionan con URP/Lit, Standard y Unlit.</summary>
public static class Level2Visuals
{
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int Emission = Shader.PropertyToID("_EmissionColor");

    public static void SetColor(Renderer r, Color c, bool glow, float glowIntensity = 1.5f)
    {
        if (r == null) return;
        var m = r.material; // instancia propia del renderer
        if (m.HasProperty(BaseColor)) m.SetColor(BaseColor, c);
        if (m.HasProperty(ColorId)) m.SetColor(ColorId, c);
        if (m.HasProperty(Emission))
        {
            if (glow)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor(Emission, c * glowIntensity);
            }
            else
            {
                m.SetColor(Emission, Color.black);
                m.DisableKeyword("_EMISSION");
            }
        }
    }

    public static TextMeshPro CreateLabel(Transform parent, string name, string text, Vector3 localPos, Vector2 size, float maxFontSize = 1f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.rectTransform.sizeDelta = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.01f;
        tmp.fontSizeMax = maxFontSize;
        tmp.enableWordWrapping = true;
        tmp.color = new Color(0.98f, 0.93f, 0.8f);
        tmp.text = text;
        return tmp;
    }
}
