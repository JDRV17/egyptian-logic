using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DimensionWeight
{
    public string dimensionName;
    [Tooltip("Puntos que aporta este puzzle a la dimensión si score=1.0. La columna debe sumar 100.")]
    public float weightPoints;
}

[Serializable]
public class PuzzleDimensionRow
{
    public string puzzleId;
    public List<DimensionWeight> weights;
}

[CreateAssetMenu(fileName = "DimensionWeightMatrix", menuName = "PuzzleTracking/Dimension Weight Matrix")]
public class DimensionWeightMatrix : ScriptableObject
{
    public List<string> dimensionNames = new List<string>
    {
        "Descomposición de problemas",
        "Identificación de patrones",
        "Abstracción y análisis de información",
        "Pensamiento lógico y toma de decisiones",
        "Diseño secuencial"
    };

    public List<string> dimensionDescriptions = new List<string> { "", "", "", "", "" };

    public List<PuzzleDimensionRow> rows = new List<PuzzleDimensionRow>
    {
        new PuzzleDimensionRow { puzzleId = "tubos", weights = new List<DimensionWeight> {
            new DimensionWeight { dimensionName = "Descomposición de problemas", weightPoints = 20 },
            new DimensionWeight { dimensionName = "Identificación de patrones", weightPoints = 45 },
            new DimensionWeight { dimensionName = "Abstracción y análisis de información", weightPoints = 10 },
            new DimensionWeight { dimensionName = "Pensamiento lógico y toma de decisiones", weightPoints = 20 },
            new DimensionWeight { dimensionName = "Diseño secuencial", weightPoints = 20 },
        }},
        new PuzzleDimensionRow { puzzleId = "botones", weights = new List<DimensionWeight> {
            new DimensionWeight { dimensionName = "Descomposición de problemas", weightPoints = 15 },
            new DimensionWeight { dimensionName = "Identificación de patrones", weightPoints = 10 },
            new DimensionWeight { dimensionName = "Abstracción y análisis de información", weightPoints = 40 },
            new DimensionWeight { dimensionName = "Pensamiento lógico y toma de decisiones", weightPoints = 15 },
            new DimensionWeight { dimensionName = "Diseño secuencial", weightPoints = 5 },
        }},
        new PuzzleDimensionRow { puzzleId = "llave", weights = new List<DimensionWeight> {
            new DimensionWeight { dimensionName = "Descomposición de problemas", weightPoints = 20 },
            new DimensionWeight { dimensionName = "Identificación de patrones", weightPoints = 15 },
            new DimensionWeight { dimensionName = "Abstracción y análisis de información", weightPoints = 45 },
            new DimensionWeight { dimensionName = "Pensamiento lógico y toma de decisiones", weightPoints = 10 },
            new DimensionWeight { dimensionName = "Diseño secuencial", weightPoints = 25 },
        }},
        new PuzzleDimensionRow { puzzleId = "hanoi", weights = new List<DimensionWeight> {
            new DimensionWeight { dimensionName = "Descomposición de problemas", weightPoints = 40 },
            new DimensionWeight { dimensionName = "Identificación de patrones", weightPoints = 20 },
            new DimensionWeight { dimensionName = "Abstracción y análisis de información", weightPoints = 5 },
            new DimensionWeight { dimensionName = "Pensamiento lógico y toma de decisiones", weightPoints = 25 },
            new DimensionWeight { dimensionName = "Diseño secuencial", weightPoints = 40 },
        }},
        new PuzzleDimensionRow { puzzleId = "pintar", weights = new List<DimensionWeight> {
            new DimensionWeight { dimensionName = "Descomposición de problemas", weightPoints = 5 },
            new DimensionWeight { dimensionName = "Identificación de patrones", weightPoints = 10 },
            new DimensionWeight { dimensionName = "Abstracción y análisis de información", weightPoints = 0 },
            new DimensionWeight { dimensionName = "Pensamiento lógico y toma de decisiones", weightPoints = 30 },
            new DimensionWeight { dimensionName = "Diseño secuencial", weightPoints = 10 },
        }},
    };

    public float GetWeight(string puzzleId, string dimensionName)
    {
        foreach (var row in rows)
        {
            if (row.puzzleId != puzzleId) continue;
            foreach (var w in row.weights)
                if (w.dimensionName == dimensionName) return w.weightPoints;
        }
        return 0f;
    }
}