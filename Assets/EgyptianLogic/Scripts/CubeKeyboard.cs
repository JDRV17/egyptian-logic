using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class CubeKeyboard : MonoBehaviour
{
    [SerializeField] private TMP_Text display;            // TMP que muestra el nombre
    [SerializeField] private XRPhysicalButton deleteKey;      // "borrar"
    [SerializeField] private XRPhysicalButton clearAllKey;    // "borrar todo"
    [SerializeField] private XRPhysicalButton confirmKey;     // "confirmar"
    [SerializeField] private int maxLength = 12;

    [Tooltip("Se dispara solo cuando el nombre es válido. Aquí van Hide/Show de los grupos.")]
    public UnityEvent onConfirmed;

    private string text = "";

    private void Awake()
    {
        foreach (var key in GetComponentsInChildren<XRPhysicalButton>(true))
        {
            if (key == deleteKey || key == clearAllKey || key == confirmKey) continue;
            var label = key.GetComponentInChildren<TMP_Text>(true);
            if (label == null || label.text.Length != 1) continue;
            string c = label.text == "_" ? " " : label.text;   // "_" = espacio
            key.onPressed.AddListener(() => Add(c));
        }

        if (deleteKey != null) deleteKey.onPressed.AddListener(Delete);
        if (clearAllKey != null) clearAllKey.onPressed.AddListener(ClearAll);
        if (confirmKey != null) confirmKey.onPressed.AddListener(Confirm);
        Refresh();
    }

    private void Add(string c)
    {
        if (text.Length >= maxLength) return;
        if (c == " " && (text.Length == 0 || text.EndsWith(" "))) return;   // sin espacios iniciales ni dobles
        text += c;
        Refresh();
    }

    private void Delete()
    {
        if (text.Length == 0) return;
        text = text.Substring(0, text.Length - 1);
        Refresh();
    }

    private void ClearAll()
    {
        text = "";
        Refresh();
    }

    private void Refresh()
    {
        if (display != null) display.text = text.Length == 0 ? "_" : text;
    }

    public void Confirm()
    {
        if (text.Trim().Length == 0) return;       // nombre vacío: no avanza
        SessionData.Instance.SetPlayerName(text.Trim());
        onConfirmed?.Invoke();
    }
}