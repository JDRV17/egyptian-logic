using UnityEngine;

[System.Serializable]
public class HintVersionTexts
{
    [TextArea(2, 6)] public string[] hints = new string[4];
}

[System.Serializable]
public class HintStage
{
    public string name = "1.2";

    [Tooltip("Quien conoce la versión activa de este reto: LightCombinationLock (1.1), PuzzleVersionManager (1.2), SpinningCubes (1.3) o TextSelector (1.4 y 1.5).")]
    public MonoBehaviour versionSource;

    [Tooltip("Elemento 0 = V1, 1 = V2, 2 = V3.")]
    public HintVersionTexts[] byVersion = new HintVersionTexts[3];

    public bool skipFirstHint;   // true en 1.1

    [System.NonSerialized] public bool solved;
    [System.NonSerialized] public bool arrived;
    [System.NonSerialized] public int used;     // última pista entregada (0..4)
    [System.NonSerialized] public int scored;   // pistas 2..4 entregadas: la H de la fórmula
}