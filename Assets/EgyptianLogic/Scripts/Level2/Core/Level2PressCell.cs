using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Celda/botón presionable creada en tiempo de ejecución (tablas de verdad, mapas de Karnaugh, teclados).
/// Se hunde hacia +Z local al presionarse (el jugador mira el panel desde -Z).
/// Funciona con rayo o con la mano directa (evento Select del XRSimpleInteractable).
/// </summary>
public class Level2PressCell : MonoBehaviour
{
    public event Action<Level2PressCell> Pressed;

    public Renderer Body { get; private set; }
    public TextMeshPro Label { get; private set; }
    public int Index { get; set; }
    public bool Interactable { get; set; } = true;

    private XRSimpleInteractable interactable;
    private Transform visual;
    private Vector3 visualRest;
    private float depth;
    private Coroutine anim;

    public void Init(Renderer body, TextMeshPro label, Transform visualRoot, float pressDepth)
    {
        Body = body;
        Label = label;
        visual = visualRoot;
        visualRest = visual.localPosition;
        depth = pressDepth;

        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null) interactable = gameObject.AddComponent<XRSimpleInteractable>();
        interactable.selectEntered.AddListener(OnSelect);
    }

    private void OnDestroy()
    {
        if (interactable != null) interactable.selectEntered.RemoveListener(OnSelect);
    }

    private void OnSelect(SelectEnterEventArgs args) => Press();

    public void Press()
    {
        if (!Interactable) return;
        if (anim != null) StopCoroutine(anim);
        if (isActiveAndEnabled) anim = StartCoroutine(Animate());
        Pressed?.Invoke(this);
    }

    public void SetText(string text)
    {
        if (Label != null) Label.text = text;
    }

    public void SetColor(Color c, bool glow = false) => Level2Visuals.SetColor(Body, c, glow, 0.6f);

    private IEnumerator Animate()
    {
        Vector3 down = visualRest + new Vector3(0f, 0f, depth);
        float t = 0f;
        while (t < 1f) { t += Time.deltaTime * 20f; visual.localPosition = Vector3.Lerp(visualRest, down, t); yield return null; }
        yield return new WaitForSeconds(0.08f);
        t = 0f;
        while (t < 1f) { t += Time.deltaTime * 12f; visual.localPosition = Vector3.Lerp(down, visualRest, t); yield return null; }
        visual.localPosition = visualRest;
        anim = null;
    }

    /// <summary>
    /// Crea una celda: raíz (con el interactable) → "Visual" (se mueve) → "Body" (cubo escalado) + "Label".
    /// Si se pasa un prefab, se instancia y se buscan su Renderer y su TextMeshPro.
    /// </summary>
    public static Level2PressCell Create(Transform parent, string name, Vector3 localPos, Vector2 size, string text,
                                         Color color, GameObject prefab = null, float thickness = 0.03f)
    {
        GameObject root;
        Renderer body;
        TextMeshPro label;
        Transform visual;

        if (prefab != null)
        {
            root = Instantiate(prefab, parent);
            root.name = name;
            root.transform.localPosition = localPos;
            root.transform.localRotation = Quaternion.identity;
            body = root.GetComponentInChildren<Renderer>();
            label = root.GetComponentInChildren<TextMeshPro>();
            visual = root.transform.childCount > 0 ? root.transform.GetChild(0) : root.transform;
        }
        else
        {
            root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;

            var v = new GameObject("Visual");
            v.transform.SetParent(root.transform, false);
            visual = v.transform;

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Body";
            cube.transform.SetParent(visual, false);
            cube.transform.localScale = new Vector3(size.x, size.y, thickness);
            body = cube.GetComponent<Renderer>();

            label = Level2Visuals.CreateLabel(visual, "Label", text,
                new Vector3(0f, 0f, -thickness * 0.5f - 0.002f), size * 0.85f, 0.6f);
        }

        var cell = root.GetComponent<Level2PressCell>();
        if (cell == null) cell = root.AddComponent<Level2PressCell>();
        cell.Init(body, label, visual, thickness * 0.5f);
        cell.SetText(text);
        cell.SetColor(color);
        return cell;
    }
}
