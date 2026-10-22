using System;

[Serializable]
public class VersionIdeals
{
    public float tIdeal = 30f;   // segundos del jugador ideal
    public int iIdeal = 4;       // interacciones del jugador ideal
}

[Serializable]
public class StageConfig
{
    public string name = "1.1";
    [UnityEngine.Tooltip("El reloj arranca al entrar a la HintZone (1.1 y 1.3).")]
    public bool startOnZoneEnter;
    [UnityEngine.Tooltip("Tiene botón de submit: cada pulsación cuenta como interacción (1.1, 1.3, 1.5).")]
    public bool hasSubmit;
    public VersionIdeals[] byVersion = new VersionIdeals[3];
}

[Serializable]
public class StageResult
{
    public string stageName;
    public int stageIndex, version, attempts, interactions, idealInteractions, hints;
    public float time, hintPauseTime, idealTime;
    public float sT, sA, sI, sH, P;
    public bool cappedBySolution;
}