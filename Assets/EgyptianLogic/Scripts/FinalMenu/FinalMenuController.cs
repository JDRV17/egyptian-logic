using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controla la tablet del menú final: pantalla de resultados, pantalla de envío del informe
/// y los botones de enviar, volver al menú y salir.
/// </summary>
public class FinalMenuController : MonoBehaviour
{
    [Header("Pantallas")]
    public CanvasGroup resultsScreen;
    public CanvasGroup sendScreen;
    [Tooltip("Duración del fundido entre pantallas (0 = cambio instantáneo).")]
    public float fadeDuration = 0.25f;

    [Header("Resultados")]
    public FinalResultsView results;

    [Header("Enviar informe")]
    public TabletKeyboard keyboard;
    public TabletField nameField;
    public TabletField emailField;
    public TMP_Text statusText;
    public Button sendButton;
    public ReportMailer mailer;

    [Header("Escenas")]
    [Tooltip("Escena del menú de inicio (debe estar en Build Settings).")]
    public string mainMenuScene = "EgyptMainMenu";

    [Header("Jugador")]
    [Tooltip("First Person de PC. Mientras se escribe se pone su velocidad en 0 para que WASD no lo mueva; la cámara sigue libre. Si está vacío se busca solo.")]
    public PC_playerController pcPlayer;

    [Header("Informe")]
    [Tooltip("Guarda una copia del PDF en persistentDataPath cada vez que se intenta enviar (aunque el correo falle).")]
    public bool saveLocalCopy = true;
    [Tooltip("Letra del PDF: Times (documento formal) o Helvetica.")]
    public PdfFont pdfFont = PdfFont.Times;

    [Header("Sonidos (opcionales)")]
    public AudioSource successSound;
    public AudioSource failSound;

    public Color statusNormal = new Color(0.66f, 0.6f, 0.49f);
    public Color statusOk = new Color(0.5f, 0.69f, 0.41f);
    public Color statusError = new Color(0.85f, 0.4f, 0.3f);

    private Coroutine fade;
    private bool sentOnce;
    private float savedMoveSpeed = -1f;

    private void Start()
    {
        if (pcPlayer == null) pcPlayer = FindObjectOfType<PC_playerController>();
        Time.timeScale = 1f;   // por si se llegó desde el menú de pausa
        SetScreen(resultsScreen, true, instant: true);
        SetScreen(sendScreen, false, instant: true);
        if (keyboard != null) keyboard.onSubmit.AddListener(Send);
    }

    // ---------------------------------------------------------------- Navegación

    public void ShowResults()
    {
        if (keyboard != null) keyboard.ClearFocus();
        SetTyping(false);
        Switch(sendScreen, resultsScreen);
    }

    public void ShowSend()
    {
        if (nameField != null && string.IsNullOrEmpty(nameField.Value))
            nameField.SetValue(SessionData.Instance.PlayerName);

        if (!sentOnce) Status("", statusNormal);   // tras un envío se deja el mensaje de confirmación
        Switch(resultsScreen, sendScreen);
        SetTyping(true);

        if (keyboard != null)
            keyboard.SetActiveField(nameField != null && string.IsNullOrEmpty(nameField.Value) ? nameField : emailField);
    }

    public void BackToMainMenu()
    {
        SetTyping(false);
        SessionData.Instance.ClearAll();   // la próxima persona empieza de cero
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void Quit()
    {
        Debug.Log("[FinalMenuController] Saliendo del juego.");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ---------------------------------------------------------------- Envío

    public void Send()
    {
        if (mailer != null && mailer.IsSending) return;

        string name = nameField != null ? nameField.Value.Trim() : "";
        string email = emailField != null ? emailField.Value.Trim() : "";

        if (!TabletKeyboard.IsValidEmail(email))
        {
            Status("Escribe un correo válido, por ejemplo nombre@correo.com", statusError);
            Play(failSound);
            if (keyboard != null && emailField != null) keyboard.SetActiveField(emailField);
            return;
        }
        if (results == null || results.Model == null)
        {
            Status("No hay resultados para enviar.", statusError);
            return;
        }
        if (mailer == null)
        {
            Status("Falta el componente ReportMailer.", statusError);
            return;
        }

        if (!string.IsNullOrEmpty(name)) results.Model.playerName = name;
        byte[] pdf;
        try
        {
            pdf = FinalReportPdf.Build(results.Model, email, pdfFont);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[FinalMenuController] Error al generar el PDF: " + ex);
            Status("No se pudo generar el informe.", statusError);
            return;
        }

        if (saveLocalCopy) SavePdf(pdf);

        Status("Enviando informe...", statusNormal);
        if (sendButton != null) sendButton.interactable = false;

        mailer.Send(email, name, pdf, FinalReportPdf.FileName(results.Model),
            onSuccess: () =>
            {
                sentOnce = true;
                if (sendButton != null) sendButton.interactable = true;
                Status($"¡Listo! Enviamos el informe a {email}", statusOk);
                Play(successSound);
            },
            onError: msg =>
            {
                if (sendButton != null) sendButton.interactable = true;
                Status(msg, statusError);
                Play(failSound);
            });
    }

    /// <summary>Guarda el PDF en persistentDataPath, para probar el informe sin enviar correos.</summary>
    [ContextMenu("Guardar PDF de prueba")]
    public void SavePdfForTesting()
    {
        if (results == null) return;
        if (results.Model == null) results.Refresh();
        if (nameField != null && !string.IsNullOrWhiteSpace(nameField.Value)) results.Model.playerName = nameField.Value.Trim();
        SavePdf(FinalReportPdf.Build(results.Model, emailField != null ? emailField.Value : "", pdfFont));
    }

    private void SavePdf(byte[] pdf)
    {
        try
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, FinalReportPdf.FileName(results.Model));
            System.IO.File.WriteAllBytes(path, pdf);
            Debug.Log("[FinalMenuController] PDF guardado en " + path);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[FinalMenuController] No se pudo guardar la copia del PDF: " + ex.Message);
        }
    }

    // ---------------------------------------------------------------- Utilidades

    private void Status(string text, Color color)
    {
        if (statusText == null) return;
        statusText.text = text;
        statusText.color = color;
    }

    /// <summary>
    /// Congela solo el movimiento (no la cámara): el rayo del First Person sale del centro de la pantalla,
    /// así que el jugador tiene que poder mirar las teclas para tocarlas.
    /// </summary>
    private void SetTyping(bool typing)
    {
        if (pcPlayer == null) return;
        if (typing)
        {
            if (savedMoveSpeed < 0f) savedMoveSpeed = pcPlayer.moveSpeed;
            pcPlayer.moveSpeed = 0f;
        }
        else if (savedMoveSpeed >= 0f)
        {
            pcPlayer.moveSpeed = savedMoveSpeed;
            savedMoveSpeed = -1f;
        }
    }

    private static void Play(AudioSource s)
    {
        if (s != null) s.Play();
    }

    private void Switch(CanvasGroup from, CanvasGroup to)
    {
        if (fade != null) StopCoroutine(fade);
        if (fadeDuration <= 0f || !isActiveAndEnabled)
        {
            SetScreen(from, false, true);
            SetScreen(to, true, true);
            return;
        }
        fade = StartCoroutine(FadeRoutine(from, to));
    }

    private IEnumerator FadeRoutine(CanvasGroup from, CanvasGroup to)
    {
        float half = fadeDuration * 0.5f;
        if (from != null && from.gameObject.activeSelf)
        {
            from.interactable = false;
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                from.alpha = 1f - t / half;
                yield return null;
            }
        }
        SetScreen(from, false, true);

        if (to != null)
        {
            to.gameObject.SetActive(true);
            to.alpha = 0f;
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                to.alpha = t / half;
                yield return null;
            }
        }
        SetScreen(to, true, true);
        fade = null;
    }

    private static void SetScreen(CanvasGroup g, bool on, bool instant)
    {
        if (g == null) return;
        g.gameObject.SetActive(on);
        g.alpha = on ? 1f : 0f;
        g.interactable = on;
        g.blocksRaycasts = on;
    }
}
