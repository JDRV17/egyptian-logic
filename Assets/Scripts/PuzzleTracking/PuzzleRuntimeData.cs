[System.Serializable]
public class PuzzleRuntimeData
{
    public string puzzleId;
    public float elapsedSeconds;
    public int interactions;
    public bool hintUsed; // TODO PENDIENTE: aún no hay sistema de pistas conectado en ningún puzzle. Queda en false por defecto.
    public bool started;
    public bool completed;

    public int GetErrorsCount(PuzzleDefinition def)
    {
        if (def == null || !def.hasIdealInteractions) return 0;
        return UnityEngine.Mathf.Max(0, interactions - def.idealInteractions);
    }
}