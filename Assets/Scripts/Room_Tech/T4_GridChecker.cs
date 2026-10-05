using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class T4_GridChecker : MonoBehaviour
{
    private const string PuzzleId = "llave";

    public Transform PrintParent;

    [Header("Botones correctos (deben estar todos ON)")]
    public GameObject CubeButton8, CubeButton11, CubeButton12,
                      CubeButton14, CubeButton20, CubeButton23;

    [Header("Todos los botones del tablero (correctos + incorrectos)")]
    public List<GameObject> allButtons;

    public Material myMat;
    public bool FigurePrinted = false;
    public Animator printerAnimator;

    private List<GameObject> correctButtons;

    void Awake()
    {
        correctButtons = new List<GameObject>
        {
            CubeButton8, CubeButton11, CubeButton12,
            CubeButton14, CubeButton20, CubeButton23
        };
    }

    // Llamar este método desde el botón "Imprimir" (onClick / evento XR).
    // Representa UN INTENTO -> se cuenta siempre, sin importar si es correcto.
    public void CubesChecker()
    {
        if (FigurePrinted) return;

        // --- TRACKING: cada pulsación de "Imprimir" es un intento ---
        PuzzleTracker.Instance?.RegisterInteraction(PuzzleId);

        // 1) todos los correctos deben estar activados
        foreach (var btn in correctButtons)
        {
            if (btn == null || !btn.GetComponent<T4_ButtonPresser>().LightActivated)
                return;
        }

        // 2) ningún botón incorrecto puede estar activado
        foreach (var btn in allButtons)
        {
            if (btn == null)
                continue;

            if (correctButtons.Contains(btn))
                continue;

            if (btn.GetComponent<T4_ButtonPresser>().LightActivated)
            {
                return; // combinación inválida
            }
        }

        // combinación exacta correcta
        Color newColor = new Color(Random.value, Random.value, Random.value, 1.0f);
        CreateCube(new Vector3(0.1f, 0f, 0.05f), newColor);
        CreateCube(new Vector3(0f, 0f, 0f), newColor);
        CreateCube(new Vector3(0.05f, 0f, 0f), newColor);
        CreateCube(new Vector3(0.15f, 0f, 0f), newColor);
        CreateCube(new Vector3(0.2f, 0f, -0.05f), newColor);
        CreateCube(new Vector3(0.1f, 0f, -0.1f), newColor);

        if (printerAnimator != null)
        {
            printerAnimator.Play("MatrixDrawer", 0, 0f);
        }

        FigurePrinted = true;

        // --- TRACKING: puzzle completado ---
        PuzzleTracker.Instance?.CompletePuzzle(PuzzleId);
    }

    void CreateCube(Vector3 localPos, Color color)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(PrintParent);
        cube.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        cube.transform.localPosition = localPos;
        cube.GetComponent<BoxCollider>().enabled = false;

        Renderer rend = cube.GetComponent<Renderer>();
        rend.material = myMat;
        rend.material.SetColor("_BaseColor", color);
    }
}