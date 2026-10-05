using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class InputFieldKeyboardTrigger : MonoBehaviour, ISelectHandler
{
    private InputField field;

    void Awake()
    {
        field = GetComponent<InputField>();
        field.readOnly = true; // ignora el teclado físico; solo el teclado virtual puede escribir aquí
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (VirtualKeyboardController.Instance != null)
            VirtualKeyboardController.Instance.OpenFor(field);
    }
}