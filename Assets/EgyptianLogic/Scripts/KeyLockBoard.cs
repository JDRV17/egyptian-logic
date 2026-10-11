using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class KeyLockBoard : MonoBehaviour
{
    [System.Serializable]
    public class VersionAnswers
    {
        public string name = "V1";
        [Tooltip("Respuesta de cada pergamino, en el orden de las cerraduras. Ej: 1,2,3,4")]
        public int[] answers;
    }

    [SerializeField] private TextSelector selector;

    [Tooltip("Cerraduras en orden: posición 0 = pergamino 1, etc.")]
    [SerializeField] private XRSocketInteractor[] locks;

    [Tooltip("Elemento 0 = V1, 1 = V2, 2 = V3.")]
    [SerializeField] private VersionAnswers[] versions = new VersionAnswers[3];

    public UnityEvent onSolved;
    public bool IsSolved { get; private set; }

    private void OnEnable()
    {
        foreach (var l in locks)
        {
            if (l == null) continue;
            l.selectEntered.AddListener(OnLockChanged);
            l.selectExited.AddListener(OnLockChangedExit);
        }
    }

    private void OnDisable()
    {
        foreach (var l in locks)
        {
            if (l == null) continue;
            l.selectEntered.RemoveListener(OnLockChanged);
            l.selectExited.RemoveListener(OnLockChangedExit);
        }
    }

    private void OnLockChanged(SelectEnterEventArgs a) => StartCoroutine(EvaluateNextFrame());
    private void OnLockChangedExit(SelectExitEventArgs a) => StartCoroutine(EvaluateNextFrame());

    private IEnumerator EvaluateNextFrame()
    {
        yield return null; // deja que el socket actualice su estado
        Evaluate();
    }

    private void Evaluate()
    {
        if (IsSolved || selector == null) return;

        var answers = versions[selector.CurrentVersion].answers;
        if (answers == null || answers.Length != locks.Length) return;

        for (int i = 0; i < locks.Length; i++)
        {
            var held = locks[i] != null ? locks[i].firstInteractableSelected : null;
            if (held == null) return;

            var key = held.transform.GetComponent<HangingKey>();
            if (key == null || key.KeyValue != answers[i]) return;
        }

        IsSolved = true;
        Debug.Log("[KeyLockBoard] ¡Código correcto!");
        onSolved?.Invoke();
    }
}