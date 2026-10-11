using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class BurnableTapestry : MonoBehaviour
{
    [SerializeField] private Renderer tapestryRenderer;
    [SerializeField] private string burnProperty = "_Burn";
    [SerializeField] private ParticleSystem[] flames;
    [SerializeField] private Light burnLight;
    [SerializeField] private float burnDuration = 4f;
    [SerializeField] private float maxLightIntensity = 3f;
    [SerializeField] private GameObject[] disableWhenDone; // colliders, etc.

    [SerializeField] private Transform flameFront;   // hijo con las partículas, en el borde inferior
    [SerializeField] private Color charColor = new Color(0.08f, 0.05f, 0.04f);

    [SerializeField] private float fadeOutDuration = 1.5f;

    public UnityEvent onBurned;

    private Material mat;
    private bool burning;

    private void Awake()
    {
        mat = tapestryRenderer.material;
        if (mat.HasProperty(burnProperty))
            mat.SetFloat(burnProperty, 0f);
        if (burnLight != null) burnLight.intensity = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (burning) return;
        var torch = other.GetComponentInParent<Torch>();
        if (torch != null && torch.IsLit) StartBurn();
    }

    public void StartBurn()
    {
        if (burning) return;
        burning = true;
        StartCoroutine(BurnRoutine());
    }

    private IEnumerator BurnRoutine()
    {
        foreach (var f in flames) if (f != null) f.Play();

        // Estos valores se leen aquí, justo al empezar
        Transform tapestry = tapestryRenderer.transform;
        Vector3 startScale = tapestry.localScale;
        Vector3 frontStart = flameFront != null ? flameFront.position : Vector3.zero;
        bool hasBaseColor = mat.HasProperty("_BaseColor");
        Color startColor = hasBaseColor ? mat.GetColor("_BaseColor") : mat.color;

        // Altura del tapiz en el mundo, para que el frente de llamas suba lo correcto
        float worldHeight = tapestryRenderer.bounds.size.y;

        float t = 0f;
        while (t < burnDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / burnDuration);

            // El tapiz se encoge hacia arriba y se ennegrece
            var s = startScale;
            s.y = Mathf.Lerp(startScale.y, 0f, k);
            tapestry.localScale = s;

            Color c = Color.Lerp(startColor, charColor, Mathf.Clamp01(k * 2f));
            if (hasBaseColor) mat.SetColor("_BaseColor", c); else mat.color = c;

            // El frente de llamas sube junto con el borde
            if (flameFront != null)
                flameFront.position = frontStart + Vector3.up * (worldHeight * k);

            if (burnLight != null)
                burnLight.intensity = Mathf.Sin(k * Mathf.PI) * maxLightIntensity;

            yield return null;
        }

        // Desvanecimiento final: el tapiz quemado se vuelve transparente
        Color burnt = hasBaseColor ? mat.GetColor("_BaseColor") : mat.color;
        float f2 = 0f;
        while (f2 < fadeOutDuration)
        {
            f2 += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(f2 / fadeOutDuration);

            Color faded = new Color(burnt.r, burnt.g, burnt.b, a);
            if (hasBaseColor) mat.SetColor("_BaseColor", faded); else mat.color = faded;

            // La luz y las llamas se apagan suavemente con él
            if (burnLight != null) burnLight.intensity = Mathf.Lerp(burnLight.intensity, 0f, Time.deltaTime * 4f);

            yield return null;
        }

        foreach (var f in flames) if (f != null) f.Stop();   // deja de emitir; las partículas existentes se disipan solas
        foreach (var go in disableWhenDone) if (go != null) go.SetActive(false);
        tapestryRenderer.enabled = false;
        onBurned?.Invoke();
    }
}