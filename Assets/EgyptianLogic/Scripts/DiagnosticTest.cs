using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class DiagnosticTest : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        [Tooltip("Objeto con el TMP de la pregunta. Se activa solo en su turno.")]
        public GameObject root;
        [Tooltip("Textos de las 4 opciones, en orden A, B, C, D.")]
        public string[] optionTexts = new string[4];
        [Range(0, 3)] public int correctIndex;
    }

    [Tooltip("Los 4 botones compartidos, en orden A, B, C, D.")]
    [SerializeField] private XRPhysicalButton[] buttons = new XRPhysicalButton[4];
    [Tooltip("El TMP hijo de cada botón, en el mismo orden.")]
    [SerializeField] private TMP_Text[] buttonLabels = new TMP_Text[4];
    [SerializeField] private GameObject buttonsRoot;   // padre de los 4 botones (opcional)

    [SerializeField] private Question[] questions = new Question[2];

    public UnityEvent onFinished;
    public UnityEvent onCorrect;
    public UnityEvent onWrong;

    private int current = -1;
    private int correctCount;
    private bool finished = true;

    private void Awake()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int idx = i;
            if (buttons[i] != null) buttons[i].onPressed.AddListener(() => Answer(idx));
        }
        foreach (var q in questions) if (q.root != null) q.root.SetActive(false);
        if (buttonsRoot != null) buttonsRoot.SetActive(false);
    }

    /// <summary>Conéctalo al Continuar de la narrativa.</summary>
    public void StartTest()
    {
        current = -1;
        correctCount = 0;
        finished = false;
        if (buttonsRoot != null) buttonsRoot.SetActive(true);
        Next();
    }

    private void Next()
    {
        if (current >= 0 && questions[current].root != null)
            questions[current].root.SetActive(false);

        current++;

        if (current >= questions.Length)
        {
            finished = true;
            if (buttonsRoot != null) buttonsRoot.SetActive(false);
            SessionData.Instance.SetDiagnostic(correctCount);
            Debug.Log($"[DiagnosticTest] Aciertos: {correctCount} → V{SessionData.Instance.InitialVersion + 1}");
            onFinished?.Invoke();
            return;
        }

        var q = questions[current];
        if (q.root != null) q.root.SetActive(true);
        for (int i = 0; i < buttonLabels.Length && i < q.optionTexts.Length; i++)
            if (buttonLabels[i] != null) buttonLabels[i].text = q.optionTexts[i];
    }

    private void Answer(int option)
    {
        if (finished) return;
        if (option == questions[current].correctIndex) { correctCount++; onCorrect?.Invoke(); }
        else onWrong?.Invoke();
        Next();
    }
}