using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HintManager : MonoBehaviour
{
    [SerializeField] private InputActionReference hintAction;
    [SerializeField] private HintStage[] stages;
    [SerializeField] private HintScrollSpawner spawner;
    [SerializeField] private float cooldown = 10f;

    public event Action<int, int> onHintGiven;   // (índice de reto, número de pista 1..4)

    private float lastTime = -999f;

    private void OnEnable()
    {
        if (hintAction == null) return;
        hintAction.action.Enable();
        hintAction.action.performed += OnPressed;
    }

    private void OnDisable()
    {
        if (hintAction == null) return;
        hintAction.action.performed -= OnPressed;
    }

    private void OnPressed(InputAction.CallbackContext ctx) => RequestHint();

    // --- Llamadas desde los retos (conéctalas en Inspector) ---
    public void MarkArrived(int stage) { if (Valid(stage)) stages[stage].arrived = true; }
    public void MarkSolved(int stage) { if (Valid(stage)) stages[stage].solved = true; }

    private bool Valid(int i) => stages != null && i >= 0 && i < stages.Length;

    public int CurrentStage()
    {
        for (int i = 0; i < stages.Length; i++)
            if (!stages[i].solved) return i;
        return -1;
    }

    public int HintsScored(int stage) => Valid(stage) ? stages[stage].scored : 0;

    public void RequestHint()
    {
        if (Time.time - lastTime < cooldown) return;

        int s = CurrentStage();
        if (s < 0) return;
        var st = stages[s];

        int next = st.used + 1;
        int minHint = (st.arrived || st.skipFirstHint) ? 2 : 1;
        next = Mathf.Clamp(Mathf.Max(next, minHint), 1, 4);

        var texts = st.byVersion[GetVersion(st)];
        string text = texts != null && texts.hints.Length >= next ? texts.hints[next - 1] : null;
        if (string.IsNullOrEmpty(text)) return;

        lastTime = Time.time;
        st.used = Mathf.Max(st.used, next);
        if (next >= 2) st.scored = Mathf.Min(st.scored + 1, 3);

        spawner.Spawn(text, next == 4);
        onHintGiven?.Invoke(s, next);
    }

    private int GetVersion(HintStage st)
    {
        int v = 0;
        switch (st.versionSource)
        {
            case LightCombinationLock l: v = (int)l.Version; break;
            case PuzzleVersionManager p: v = (int)p.CurrentVersion; break;
            case SpinningCubes s: v = (int)s.CurrentVersion; break;
            case TextSelector t: v = t.CurrentVersion; break;
        }
        return Mathf.Clamp(v, 0, st.byVersion.Length - 1);
    }
}