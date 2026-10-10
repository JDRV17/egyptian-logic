using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class SpinningCubes : MonoBehaviour
{
    public enum Version { V1 = 0, V2 = 1, V3 = 2 }
    public bool IsReady { get; private set; }

    [System.Serializable]
    public class VersionLayout
    {
        public string name = "V1";
        [Tooltip("Por fila (0-2): columna (0=base 2, 1=base 10, 2=base 16) del cubo que se queda en '?'.")]
        public int[] hiddenColumnPerRow = { 0, 1, 2 };
    }

    [Header("Cubos (orden: fila 0 col 0, col 1, col 2, fila 1 col 0... = 9 cubos)")]
    [SerializeField] private Transform[] cubes = new Transform[9];

    [Tooltip("Mismo orden que Cubes. Solo llena los 4 cubos que pueden quedar en '?'; el resto déjalos en None.")]
    [SerializeField] private CubeRotator[] rotators = new CubeRotator[9];

    public UnityEvent onSolved;
    public bool IsSolved { get; private set; }

    [Header("Versión")]
    [SerializeField] private bool randomizeOnStart = true;
    [SerializeField] private Version forcedVersion = Version.V1;

    [Tooltip("Posición 0 = V1, 1 = V2, 2 = V3.")]
    [SerializeField]
    private VersionLayout[] layouts =
{
    new VersionLayout { name = "V1", hiddenColumnPerRow = new[] { 1, 1, 2 } },
    new VersionLayout { name = "V2", hiddenColumnPerRow = new[] { 0, 1, 2 } },
    new VersionLayout { name = "V3", hiddenColumnPerRow = new[] { 0, 1, 2 } },
};

    [Header("Giro")]
    [Tooltip("Grados desde la cara '?' hasta la cara de cada versión: V1 = 270, V2 = 180, V3 = 90.")]
    [SerializeField] private float[] angleByVersion = { 270f, 180f, 90f };
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float rotateDuration = 1.5f;
    [Tooltip("Pausa entre un cubo y el siguiente.")]
    [SerializeField] private float staggerDelay = 0.2f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Eventos")]
    public UnityEvent onRevealStarted;
    public UnityEvent onRevealFinished;

    [Header("Submit")]
    [SerializeField] private TMP_Text feedbackText;   // opcional
    public UnityEvent onIncorrect;
    public int Attempts { get; private set; }

    public Version CurrentVersion { get; private set; }
    public bool HasRevealed { get; private set; }

    private Quaternion[] initialRotations;

    private void Awake()
    {
        initialRotations = new Quaternion[cubes.Length];
        for (int i = 0; i < cubes.Length; i++)
            if (cubes[i] != null) initialRotations[i] = cubes[i].localRotation;

        CurrentVersion = randomizeOnStart
            ? (Version)Random.Range(0, 3)
            : forcedVersion;

        for (int i = 0; i < rotators.Length; i++)
        {
            if (rotators[i] == null) continue;
            rotators[i].SetInteractable(false);
        }

        Debug.Log($"[Puzzle13Manager] Versión activa: {CurrentVersion}");
    }

    /// <summary>Para cuando el desempeño defina la versión. Solo antes de revelar.</summary>
    public void SelectVersion(Version v)
    {
        if (HasRevealed) return;
        CurrentVersion = v;
    }

    /// <summary>Conéctalo al evento On Solved del FixedInputCircuit del reto 1.2.</summary>
    public void Reveal()
    {
        if (HasRevealed) return;
        HasRevealed = true;
        StartCoroutine(RevealRoutine());
    }

    /// <summary>Grados que debe tener cada cubo respecto a su cara "?" para mostrar el valor correcto.</summary>
    public float CorrectAngle => angleByVersion[(int)CurrentVersion];

    private bool IsHidden(int cubeIndex)
    {
        int row = cubeIndex / 3;
        int col = cubeIndex % 3;
        var layout = layouts[(int)CurrentVersion];
        return layout != null && row < layout.hiddenColumnPerRow.Length && layout.hiddenColumnPerRow[row] == col;
    }

    private IEnumerator RevealRoutine()
    {
        onRevealStarted?.Invoke();

        float angle = CorrectAngle;
        int finished = 0;
        int toRotate = 0;

        for (int i = 0; i < cubes.Length; i++)
            if (cubes[i] != null && !IsHidden(i)) toRotate++;

        int correctStep = Mathf.RoundToInt(angle / 90f);

        // Mezclar las caras de los cubos ocultos (están mirando al "?", no se ve el cambio)
        for (int i = 0; i < rotators.Length && i < cubes.Length; i++)
            if (rotators[i] != null && IsHidden(i))
                rotators[i].ShuffleFaces(correctStep);

        for (int i = 0; i < cubes.Length; i++)
        {
            if (cubes[i] == null || IsHidden(i)) continue;

            StartCoroutine(RotateCube(i, angle, () => finished++));
            yield return new WaitForSeconds(staggerDelay);
        }

        while (finished < toRotate) yield return null;

        for (int i = 0; i < rotators.Length; i++)
            if (rotators[i] != null && IsHidden(i))
                rotators[i].SetInteractable(true);

        IsReady = true;

        onRevealFinished?.Invoke();
    }

    private IEnumerator RotateCube(int index, float angle, System.Action done)
    {
        Transform cube = cubes[index];
        Quaternion start = initialRotations[index];
        Quaternion end = start * Quaternion.AngleAxis(angle, rotationAxis);

        float t = 0f;
        while (t < rotateDuration)
        {
            t += Time.deltaTime;
            cube.localRotation = Quaternion.SlerpUnclamped(start, end, curve.Evaluate(Mathf.Clamp01(t / rotateDuration)));
            yield return null;
        }

        cube.localRotation = end;
        done?.Invoke();
    }

    /// <summary>Conéctalo al botón de submit (On Pressed).</summary>
    public void Submit()
    {
        if (IsSolved || !IsReady) return;

        Attempts++;

        for (int i = 0; i < rotators.Length; i++)
        {
            if (!IsHidden(i)) continue;
            if (rotators[i] == null || !rotators[i].IsCorrect)
            {
                Debug.Log($"[Puzzle13Manager] Incorrecto (intento {Attempts}).");
                if (feedbackText != null) feedbackText.text = "Vuelve a intentarlo";
                onIncorrect?.Invoke();
                return;
            }
        }

        IsSolved = true;
        for (int i = 0; i < rotators.Length; i++)
            if (rotators[i] != null) rotators[i].SetInteractable(false);

        Debug.Log($"[Puzzle13Manager] ¡Reto 1.3 resuelto en {Attempts} intento(s)!");
        if (feedbackText != null) feedbackText.text = "¡Felicitaciones!";
        onSolved?.Invoke();
    }

    [ContextMenu("Probar revelado")] private void TestReveal() => Reveal();
}