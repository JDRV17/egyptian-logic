using UnityEngine;
using UnityEngine.Events;

public class BinaryTorchBoard : MonoBehaviour
{
    [Tooltip("De MSB a LSB.")]
    [SerializeField] private BitTorch[] bits;
    [Tooltip("Número detrás del tapiz por versión: V1, V2, V3.")]
    [SerializeField] private int[] numberByVersion = { 5, 9, 12 };
    [SerializeField] private TextSelector selector;   // reutiliza la versión de 1.4 o crea otro
    public UnityEvent onSolved;

    public int Attempts { get; private set; }

    public UnityEvent onIncorrect;
    [SerializeField] private TMPro.TMP_Text feedbackText;

    public void Submit()
    {
        Attempts++;

        int n = bits.Length;               // 9
        int value = 0;
        for (int i = 0; i < n; i++)
        {
            int bit = bits[i].IsOn ? 1 : 0;
            int weight = 1 << (n - 1 - i);          // 256,128,...,1
            value += (i == 0) ? -bit * weight : bit * weight;
        }

        int target = numberByVersion[selector.CurrentVersion];
        Debug.Log($"[BinaryTorchBoard] valor={value}, objetivo={target}");

        if (target < -(1 << (n - 1)) || target >= (1 << (n - 1)))
            Debug.LogError($"[BinaryTorchBoard] {target} no cabe en {n} bits (rango {-(1 << (n - 1))} a {(1 << (n - 1)) - 1}).");

        if (value == target)
        {
            if (feedbackText != null) feedbackText.text = "¡Felicitaciones!";
            onSolved?.Invoke();
        }
        else
        {
            if (feedbackText != null) feedbackText.text = "Vuelve a intentarlo";
            onIncorrect?.Invoke();
        }
    }
}