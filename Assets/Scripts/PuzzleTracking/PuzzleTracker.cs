using System;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleTracker : MonoBehaviour
{
    public static PuzzleTracker Instance { get; private set; }

    public List<PuzzleDefinition> puzzleDefinitions;

    public event Action<string> OnPuzzleStarted;
    public event Action<string> OnPuzzleCompleted;
    public event Action<string> OnAnyChange;

    private Dictionary<string, PuzzleDefinition> defs = new Dictionary<string, PuzzleDefinition>();
    private Dictionary<string, PuzzleRuntimeData> runtime = new Dictionary<string, PuzzleRuntimeData>();
    private string activePuzzleId;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var d in puzzleDefinitions)
        {
            defs[d.puzzleId] = d;
            runtime[d.puzzleId] = new PuzzleRuntimeData { puzzleId = d.puzzleId };
        }
    }

    void Update()
    {
        if (activePuzzleId != null && runtime.TryGetValue(activePuzzleId, out var data) && !data.completed)
            data.elapsedSeconds += Time.deltaTime;
    }

    public void RegisterInteraction(string puzzleId)
    {
        if (!runtime.TryGetValue(puzzleId, out var data) || data.completed) return;

        bool wasStarted = data.started;

        if (activePuzzleId != puzzleId)
            activePuzzleId = puzzleId;

        data.started = true;
        data.interactions++;

        if (!wasStarted) OnPuzzleStarted?.Invoke(puzzleId);
        OnAnyChange?.Invoke(puzzleId);
    }

    public void RegisterHintUsed(string puzzleId)
    {
        if (runtime.TryGetValue(puzzleId, out var data))
        {
            data.hintUsed = true;
            OnAnyChange?.Invoke(puzzleId);
        }
    }

    public void CompletePuzzle(string puzzleId)
    {
        if (!runtime.TryGetValue(puzzleId, out var data) || data.completed) return;

        data.completed = true;
        if (activePuzzleId == puzzleId) activePuzzleId = null;

        OnPuzzleCompleted?.Invoke(puzzleId);
        OnAnyChange?.Invoke(puzzleId);
    }

    public PuzzleDefinition GetDefinition(string id) => defs.TryGetValue(id, out var d) ? d : null;
    public PuzzleRuntimeData GetRuntime(string id) => runtime.TryGetValue(id, out var r) ? r : null;
    public IReadOnlyList<PuzzleDefinition> AllDefinitions => puzzleDefinitions;
    public bool IsActive(string id) => activePuzzleId == id;

    public void ResetAll()
    {
        activePuzzleId = null;
        foreach (var d in puzzleDefinitions)
            runtime[d.puzzleId] = new PuzzleRuntimeData { puzzleId = d.puzzleId };
        OnAnyChange?.Invoke(null);
    }
}