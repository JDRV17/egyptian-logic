using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class SkillBarEntry
{
    [Tooltip("Debe coincidir EXACTO con dimensionName en el DimensionWeightMatrix")]
    public string dimensionName;

    public Image fillImage;
    public Text percentText;
}

public class HabilidadesMenuController : MonoBehaviour
{
    [Header("Una entrada por cada una de las 5 habilidades")]
    public List<SkillBarEntry> entries = new List<SkillBarEntry>();

    private SkillCalculator skillCalculator;

    void Start()
    {
        skillCalculator = SkillCalculator.Instance;
        if (skillCalculator == null)
            Debug.LogError("SkillCalculator.Instance es null. Verifica que el GameManager exista y haya cargado antes que esta escena.");
    }

    void Update()
    {
        if (skillCalculator == null) return;

        foreach (var entry in entries)
        {
            float percent = skillCalculator.GetDimensionPercent(entry.dimensionName);

            if (entry.fillImage != null)
                entry.fillImage.fillAmount = percent / 100f;

            if (entry.percentText != null)
                entry.percentText.text = $"{Mathf.RoundToInt(percent)}%";
        }
    }
}