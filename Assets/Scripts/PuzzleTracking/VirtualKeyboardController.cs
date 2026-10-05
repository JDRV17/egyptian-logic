using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VirtualKeyboardController : MonoBehaviour
{
    public static VirtualKeyboardController Instance { get; private set; }

    [Header("Panel del teclado (se activa/desactiva)")]
    public GameObject keyboardPanel;

    [Header("Generación automática de teclas")]
    public Transform keysContainer;
    public Button keyButtonPrefab;

    public int keyFontSize = 24;
    public float keyPreferredWidth = 60f;
    public float keyPreferredHeight = 60f;
    public float rowSpacing = 8f;

    private InputField activeField;

    private readonly string[] letterRows = new string[]
    {
        "QWERTYUIOP",
        "ASDFGHJKL",
        "ZXCVBNM"
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (keyboardPanel != null)
            keyboardPanel.SetActive(false);

        BuildKeyboard();
    }

    private void BuildKeyboard()
    {
        if (keysContainer == null || keyButtonPrefab == null) return;

        LayoutGroup existingLayout = keysContainer.GetComponent<LayoutGroup>();
        if (existingLayout != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(existingLayout);
#else
            Destroy(existingLayout);
#endif
        }

        VerticalLayoutGroup vlg = keysContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = rowSpacing;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        for (int i = keysContainer.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            DestroyImmediate(keysContainer.GetChild(i).gameObject);
#else
            Destroy(keysContainer.GetChild(i).gameObject);
#endif
        }

        foreach (string row in letterRows)
            CreateKeyRow(row);

        GameObject actionRow = CreateRowContainer();
        CreateKey(actionRow.transform, "@", () => AppendChar("@"), keyPreferredWidth);
        CreateKey(actionRow.transform, ".", () => AppendChar("."), keyPreferredWidth);
        CreateKey(actionRow.transform, "ESPACIO", AppendSpace, keyPreferredWidth * 3f);
        CreateKey(actionRow.transform, "BORRAR", Backspace, keyPreferredWidth * 1.5f);
        CreateKey(actionRow.transform, "LISTO", Confirm, keyPreferredWidth * 1.5f);
    }

    private void CreateKeyRow(string letters)
    {
        GameObject rowObj = CreateRowContainer();
        foreach (char c in letters)
        {
            string letter = c.ToString();
            CreateKey(rowObj.transform, letter, () => AppendChar(letter), keyPreferredWidth);
        }
    }

    private GameObject CreateRowContainer()
    {
        GameObject rowObj = new GameObject("Row", typeof(RectTransform));
        rowObj.transform.SetParent(keysContainer, false);

        HorizontalLayoutGroup hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        return rowObj;
    }

    private void CreateKey(Transform parent, string label, UnityEngine.Events.UnityAction onClick, float preferredWidth)
    {
        Button newKey = Instantiate(keyButtonPrefab, parent);
        newKey.gameObject.SetActive(true);
        newKey.gameObject.name = $"Key_{label}";

        LayoutElement le = newKey.gameObject.GetComponent<LayoutElement>();
        if (le == null) le = newKey.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = preferredWidth;
        le.preferredHeight = keyPreferredHeight;

        Text keyText = newKey.GetComponentInChildren<Text>();
        if (keyText != null)
        {
            keyText.text = label;
            keyText.fontSize = keyFontSize;
        }

        newKey.onClick.RemoveAllListeners();
        newKey.onClick.AddListener(onClick);
    }

    public void OpenFor(InputField field)
    {
        activeField = field;

        if (activeField != null)
        {
            activeField.DeactivateInputField();
            EventSystem.current.SetSelectedGameObject(activeField.gameObject);
        }

        if (keyboardPanel != null)
            keyboardPanel.SetActive(true);
    }

    public void Close()
    {
        if (keyboardPanel != null)
            keyboardPanel.SetActive(false);
        activeField = null;
    }

    public void AppendChar(string character)
    {
        if (activeField == null) return;

        activeField.text += character;
        RefreshField();
    }

    public void AppendSpace() => AppendChar(" ");

    public void Backspace()
    {
        if (activeField == null || activeField.text.Length == 0) return;

        activeField.text = activeField.text.Substring(0, activeField.text.Length - 1);
        RefreshField();
    }

    private void RefreshField()
    {
        activeField.caretPosition = activeField.text.Length;

        Text textComponent = activeField.textComponent;
        if (textComponent != null)
        {
            textComponent.text = activeField.text;
            LayoutRebuilder.ForceRebuildLayoutImmediate(textComponent.rectTransform);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(activeField.GetComponent<RectTransform>());
        activeField.ForceLabelUpdate();
    }

    public void Confirm() => Close();
}