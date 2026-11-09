using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Teclado físico para escribir expresiones booleanas (A, B, ', +, paréntesis…).
/// Las teclas se generan al iniciar; "&lt;-" borra un símbolo y "C" borra todo.
/// </summary>
public class ExpressionKeypad : MonoBehaviour
{
    [Tooltip("Teclas que escriben texto. Se agregan solas las de borrar.")]
    public string[] keys = { "A", "B", "C", "D", "'", "+", "(", ")" };
    public int columns = 4;
    public Vector2 keySize = new Vector2(0.08f, 0.08f);
    public float gap = 0.015f;
    [Tooltip("Prefab opcional para las teclas (Renderer + TextMeshPro).")]
    public GameObject keyPrefab;
    public int maxLength = 32;

    [Header("Pantalla")]
    public TextMeshPro display;
    public string placeholder = "_";

    [Header("Colores")]
    public Color keyColor = new Color(0.75f, 0.62f, 0.38f);
    public Color editColor = new Color(0.55f, 0.3f, 0.2f);

    /// <summary>Se dispara con cada tecla (para contar interacciones).</summary>
    public event Action KeyPressed;
    public event Action<string> TextChanged;

    public string Text { get; private set; } = "";

    private readonly List<Level2PressCell> cells = new List<Level2PressCell>();
    private bool built;
    public bool Locked { get; set; }

    private void Start() => Build();

        public void Build()
    {
        if (built) return;
        built = true;

        // Las teclas de edición van al final y se reconocen por su posición, no por su texto,
        // para que no choquen con variables (por ejemplo, la variable C).
        var all = new List<string>(keys);
        int backIndex = all.Count;
        all.Add("<-");
        all.Add("CLR");

        int rows = Mathf.CeilToInt(all.Count / (float)columns);
        float stepX = keySize.x + gap, stepY = keySize.y + gap;
        float x0 = -(columns - 1) * stepX * 0.5f;
        float y0 = (rows - 1) * stepY * 0.5f;

        for (int i = 0; i < all.Count; i++)
        {
            int r = i / columns, c = i % columns;
            string k = all[i];
            bool isBack = i == backIndex;
            bool isClear = i == backIndex + 1;
            bool edit = isBack || isClear;
            string goName = isBack ? "Key_Borrar" : isClear ? "Key_BorrarTodo" : "Key_" + k;

            var cell = Level2PressCell.Create(transform, goName, new Vector3(x0 + c * stepX, y0 - r * stepY, 0f), keySize, k,
                edit ? editColor : keyColor, keyPrefab);
            cell.Pressed += _ => OnKey(k, isBack, isClear);
            cells.Add(cell);
        }

        Refresh();
    }

    private void OnKey(string k, bool isBack, bool isClear)
    {
        if (Locked) return;

        if (isBack) { if (Text.Length > 0) Text = Text.Substring(0, Text.Length - 1); }
        else if (isClear) Text = "";
        else if (Text.Length + k.Length <= maxLength) Text += k;

        Refresh();
        KeyPressed?.Invoke();
        TextChanged?.Invoke(Text);
    }

    public void Clear()
    {
        Text = "";
        Refresh();
    }

    private void Refresh()
    {
        if (display != null) display.text = Text.Length == 0 ? placeholder : Text;
    }
}
