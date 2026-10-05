using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using UnityEngine;

public class EmailSender : MonoBehaviour
{
    public static EmailSender Instance { get; private set; }

    [Header("Cuenta remitente (predefinida)")]
    [Tooltip("Servidor SMTP, ej: smtp.gmail.com")]
    public string smtpHost = "smtp.gmail.com";
    public int smtpPort = 587;
    public bool enableSsl = true;

    [Tooltip("Correo desde el que se envían los reportes.")]
    public string fromEmail = "tu.correo@gmail.com";

    [Tooltip("Contraseña de aplicación (NO la contraseña normal de la cuenta). En Gmail: Cuenta > Seguridad > Contraseñas de aplicaciones.")]
    public string fromPassword = "";

    public string fromDisplayName = "SpaceCode O2 Program";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SendReportEmail(string toEmail, string playerName, byte[] pdfBytes, string fileName,
                                 Action onSuccess = null, Action<string> onError = null)
    {
        try
        {
            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                client.EnableSsl = enableSsl;
                client.Credentials = new NetworkCredential(fromEmail, fromPassword);

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(fromEmail, fromDisplayName);
                    message.To.Add(new MailAddress(toEmail));
                    message.Subject = "Tu informe de retroalimentación - SpaceCode O2 Program";
                    message.Body = $"Hola {playerName},\n\nAdjunto encontrarás tu informe de retroalimentación " +
                                   $"generado tras completar la experiencia SpaceCode: O2 Program.\n\nSaludos.";

                    using (var stream = new MemoryStream(pdfBytes))
                    {
                        var attachment = new Attachment(stream, fileName, "application/pdf");
                        message.Attachments.Add(attachment);

                        client.Send(message);
                    }
                }
            }

            Debug.Log($"Correo enviado correctamente a {toEmail}");
            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error al enviar correo: {ex.Message}");
            onError?.Invoke(ex.Message);
        }
    }
}