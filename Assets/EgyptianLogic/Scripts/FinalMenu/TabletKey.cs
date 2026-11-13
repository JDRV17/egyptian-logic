using UnityEngine;
using UnityEngine.UI;

/// <summary>Una tecla de la tablet. Al tocarla le avisa al teclado.</summary>
public class TabletKey : MonoBehaviour
{
    public enum Action { Character, Backspace, Clear, Space, Text }

    public TabletKeyboard keyboard;
    public Action action = Action.Character;
    [Tooltip("Carácter (o texto, para Action = Text) que escribe la tecla.")]
    public string value = "A";

    private void Awake()
    {
        var b = GetComponent<Button>();
        if (b != null) b.onClick.AddListener(Press);
    }

    public void Press()
    {
        if (keyboard == null) return;
        switch (action)
        {
            case Action.Backspace: keyboard.Backspace(); break;
            case Action.Clear: keyboard.Clear(); break;
            case Action.Space: keyboard.Type(" "); break;
            default: keyboard.Type(value); break;
        }
    }
}
