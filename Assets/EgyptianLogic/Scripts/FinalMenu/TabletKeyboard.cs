using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Teclado de la tablet. Escribe en el campo activo: en el nombre pone mayúscula al inicio de cada palabra
/// y en el correo todo va en minúscula y sin espacios. En el editor también acepta el teclado físico.
/// </summary>
public class TabletKeyboard : MonoBehaviour
{
    public List<TabletField> fields = new List<TabletField>();
    [Tooltip("Permite escribir con el teclado del computador (útil en el editor y en PC).")]
    public bool acceptPhysicalKeyboard = true;
    public AudioSource keySound;
    [Tooltip("Se llama al presionar Enter en el último campo.")]
    public UnityEvent onSubmit = new UnityEvent();

    public TabletField Active { get; private set; }

    private static readonly Regex EmailRegex =
        new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant);

    public static bool IsValidEmail(string s) => !string.IsNullOrEmpty(s) && EmailRegex.IsMatch(s);

    public void SetActiveField(TabletField f)
    {
        Active = f;
        foreach (var x in fields) if (x != null) x.SetActive(x == f);
    }

    public void ClearFocus() => SetActiveField(null);

    public void Type(string s)
    {
        if (Active == null || string.IsNullOrEmpty(s)) return;
        string v = Active.Value;
        foreach (char c in s) v = Apply(Active.kind, v, c);
        Active.SetValue(v);
        Click();
    }

    public void Backspace()
    {
        if (Active == null || Active.Value.Length == 0) return;
        Active.SetValue(Active.Value.Substring(0, Active.Value.Length - 1));
        Click();
    }

    public void Clear()
    {
        if (Active == null) return;
        Active.SetValue("");
        Click();
    }

    /// <summary>Reglas de escritura según el tipo de campo. Público para las pruebas.</summary>
    public static string Apply(TabletField.Kind kind, string current, char c)
    {
        current = current ?? "";
        if (kind == TabletField.Kind.Email)
        {
            if (char.IsWhiteSpace(c)) return current;
            return current + char.ToLowerInvariant(c);
        }

        // Nombre: sin dobles espacios ni espacio inicial; mayúscula al empezar cada palabra.
        if (c == ' ')
            return current.Length == 0 || current.EndsWith(" ") ? current : current + " ";
        bool wordStart = current.Length == 0 || current.EndsWith(" ");
        return current + (wordStart ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
    }

    private void Click()
    {
        if (keySound != null) keySound.Play();
    }

    private void Update()
    {
        if (!acceptPhysicalKeyboard || Active == null) return;
        string input = Input.inputString;
        if (string.IsNullOrEmpty(input)) return;

        foreach (char c in input)
        {
            if (c == '\b') Backspace();
            else if (c == '\n' || c == '\r') NextOrSubmit();
            else if (c == '\t') NextOrSubmit();
            else if (!char.IsControl(c)) Type(c.ToString());
        }
    }

    private void NextOrSubmit()
    {
        int i = fields.IndexOf(Active);
        if (i >= 0 && i < fields.Count - 1) SetActiveField(fields[i + 1]);
        else onSubmit?.Invoke();
    }
}
