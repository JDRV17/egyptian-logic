using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Campo de texto de la tablet. No es un InputField: así no abre el teclado del sistema en el Quest
/// y funciona igual con el rayo de VR y con el mouse. Al tocarlo pasa a ser el campo activo del teclado.
/// </summary>
public class TabletField : MonoBehaviour
{
    public enum Kind { Name, Email }

    public TabletKeyboard keyboard;
    public Kind kind = Kind.Email;
    public TMP_Text label;
    public Image background;
    [Tooltip("Texto gris cuando el campo está vacío.")]
    public string placeholder = "Toca aquí para escribir";
    public int maxLength = 60;

    public Color normalColor = new Color(0.06f, 0.045f, 0.03f, 1f);
    public Color activeColor = new Color(0.16f, 0.12f, 0.07f, 1f);
    public Color placeholderColor = new Color(0.55f, 0.5f, 0.42f, 1f);
    public Color textColor = new Color(0.95f, 0.91f, 0.84f, 1f);

    public string Value { get; private set; } = "";
    public bool IsActive { get; private set; }

    private void Awake()
    {
        var b = GetComponent<Button>();
        if (b != null) b.onClick.AddListener(Focus);
    }

    private void OnEnable() => Refresh();

    public void Focus()
    {
        if (keyboard != null) keyboard.SetActiveField(this);
    }

    public void SetActive(bool on)
    {
        IsActive = on;
        Refresh();
    }

    public void SetValue(string v)
    {
        v = v ?? "";
        if (v.Length > maxLength) v = v.Substring(0, maxLength);
        Value = v;
        Refresh();
    }

    public void Refresh()
    {
        if (background != null) background.color = IsActive ? activeColor : normalColor;
        if (label == null) return;

        bool caret = IsActive && Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f;
        if (Value.Length == 0)
        {
            label.color = placeholderColor;
            label.text = IsActive ? (caret ? "|" : "") : placeholder;
        }
        else
        {
            label.color = textColor;
            label.text = Value + (caret ? "|" : "");
        }
    }

    private void Update()
    {
        if (IsActive) Refresh();   // parpadeo del cursor
    }
}
