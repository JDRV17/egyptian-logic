using UnityEngine;

public class PaintModeManager : MonoBehaviour
{
    public static PaintModeManager Instance { get; private set; }

    public string CurrentColor { get; private set; } = "R"; // "R" o "B" por defecto

    [Header("Feedback visual opcional (resalta el botón activo)")]
    public GameObject redButtonHighlight;
    public GameObject blueButtonHighlight;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        UpdateHighlights();
    }

    public void SetRed()
    {
        CurrentColor = "R";
        UpdateHighlights();
    }

    public void SetBlue()
    {
        CurrentColor = "B";
        UpdateHighlights();
    }

    private void UpdateHighlights()
    {
        if (redButtonHighlight != null) redButtonHighlight.SetActive(CurrentColor == "R");
        if (blueButtonHighlight != null) blueButtonHighlight.SetActive(CurrentColor == "B");
    }
}