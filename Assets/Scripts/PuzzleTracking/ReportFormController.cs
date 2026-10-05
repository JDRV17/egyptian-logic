using UnityEngine;
using UnityEngine.UI;

public class ReportFormController : MonoBehaviour
{
    [Header("Campos del formulario")]
    public InputField nombreInput;
    public InputField correoInput;

    [Header("Valores por defecto si el campo queda vacío")]
    public string nombrePorDefecto = "Usuario";
    public string correoPorDefecto = "usuario@correo.com";

    [Header("Feedback (opcional)")]
    public Text mensajeFeedback;

    public GenerarReporteButton generarReporteButton;
    public EmailSender emailSender;
    public MenuManager menuManager;
    public ReportInputController reportInputController;

    public void OnEnviarReporteClicked()
    {
        string nombre = nombreInput != null ? nombreInput.text.Trim() : "";
        string correo = correoInput != null ? correoInput.text.Trim() : "";

        if (string.IsNullOrEmpty(nombre)) nombre = nombrePorDefecto;
        if (string.IsNullOrEmpty(correo)) correo = correoPorDefecto;

        if (PuzzleTracker.Instance == null || SkillCalculator.Instance == null)
        {
            MostrarMensaje("ERROR: no se encontró el GameManager.");
            return;
        }

        byte[] pdfBytes = generarReporteButton.GenerarReportePdf(nombre, correo, out string fileName);
        if (pdfBytes == null)
        {
            MostrarMensaje("ERROR al generar el PDF. Revisa la consola.");
            return;
        }

        MostrarMensaje("Enviando reporte por correo...");

        emailSender.SendReportEmail(correo, nombre, pdfBytes, fileName,
            onSuccess: () =>
            {
                MostrarMensaje($"¡Reporte enviado a {correo}!");
                CerrarTodo();
            },
            onError: (err) =>
            {
                MostrarMensaje($"ERROR al enviar el correo: {err}");
            });
    }

    private void CerrarTodo()
    {
        if (VirtualKeyboardController.Instance != null)
            VirtualKeyboardController.Instance.Close();

        if (reportInputController != null)
            reportInputController.RestorePlayerMovement();

        if (menuManager != null)
            menuManager.CloseGenerarReporte();
    }

    private void MostrarMensaje(string texto)
    {
        if (mensajeFeedback != null) mensajeFeedback.text = texto;
        else Debug.Log(texto);
    }
}