using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>Contenido de Resources/MailCredentials.json (archivo local, fuera de git).</summary>
[Serializable]
public class MailCredentials
{
    public string email;
    public string password;
}

/// <summary>
/// Envía el informe en PDF por SMTP (por defecto Gmail con contraseña de aplicación), igual que el EmailSender
/// del juego base, pero en un hilo aparte para que el visor no se congele mientras se envía.
/// </summary>
public class ReportMailer : MonoBehaviour
{
    [Header("Cuenta remitente")]
    [Tooltip("Nombre del archivo en una carpeta Resources (sin extensión) con {\"email\": ..., \"password\": ...}. " +
             "Se usa cuando From Email o From Password están vacíos. Ese archivo está en .gitignore: así la contraseña no llega a GitHub.")]
    public string credentialsResource = "MailCredentials";
    public string smtpHost = "smtp.gmail.com";
    public int smtpPort = 587;
    public bool enableSsl = true;
    [Tooltip("Correo desde el que se envían los informes. Déjalo vacío para leerlo del archivo de credenciales.")]
    public string fromEmail = "";
    [Tooltip("Contraseña de aplicación de Gmail. Déjala vacía y usa el archivo de credenciales para no subirla a GitHub.")]
    public string fromPassword = "";
    public string fromDisplayName = "Egyptian Logic";

    [Header("Mensaje")]
    public string subject = "Tu informe de Egyptian Logic";
    [TextArea(4, 10)]
    [Tooltip("{nombre} se reemplaza por el nombre del jugador.")]
    public string body =
        "Hola {nombre},\n\n" +
        "Adjunto encontrarás tu informe de desempeño en Egyptian Logic, con los retos que más te costaron " +
        "y los temas que te recomendamos repasar.\n\n" +
        "¡Gracias por jugar!";
    [Tooltip("Opcional: copia oculta del informe (por ejemplo, el correo del investigador).")]
    public string bcc = "";
    public int timeoutMs = 20000;

    public bool IsConfigured
    {
        get
        {
            ResolveCredentials(out var email, out var password);
            return !string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password);
        }
    }

    /// <summary>Usa los campos del Inspector si están llenos; si no, el archivo de Resources.</summary>
    public void ResolveCredentials(out string email, out string password)
    {
        email = fromEmail;
        password = fromPassword;
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password)) return;
        if (string.IsNullOrWhiteSpace(credentialsResource)) return;

        var file = Resources.Load<TextAsset>(credentialsResource);
        if (file == null) return;
        try
        {
            var c = JsonUtility.FromJson<MailCredentials>(file.text);
            if (c == null) return;
            if (string.IsNullOrWhiteSpace(email)) email = c.email?.Trim();
            if (string.IsNullOrWhiteSpace(password)) password = c.password?.Replace(" ", "").Trim();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ReportMailer] No se pudo leer Resources/{credentialsResource}: {ex.Message}");
        }
    }
    public bool IsSending { get; private set; }

    private Action pendingSuccess;
    private Action<string> pendingError;
    private Task<string> task;

    /// <summary>onError recibe un mensaje corto para mostrar en la tablet.</summary>
    public void Send(string toEmail, string playerName, byte[] pdf, string fileName,
                     Action onSuccess, Action<string> onError)
    {
        if (IsSending) { onError?.Invoke("Ya se está enviando un informe."); return; }
        if (!IsConfigured)
        {
            Debug.LogWarning($"[ReportMailer] Falta la cuenta remitente: llena Resources/{credentialsResource}.json o los campos del Inspector.");
            onError?.Invoke("El envío de correos no está configurado.");
            return;
        }

        IsSending = true;
        pendingSuccess = onSuccess;
        pendingError = onError;

        ResolveCredentials(out var from, out var pass);
        string host = smtpHost, display = fromDisplayName,
               subj = subject, text = (body ?? "").Replace("{nombre}", string.IsNullOrWhiteSpace(playerName) ? "" : playerName),
               copy = bcc;
        int port = smtpPort, timeout = timeoutMs;
        bool ssl = enableSsl;

        // SmtpClient.Send bloquea: se hace en otro hilo y el resultado se recoge en Update.
        task = Task.Run(() =>
        {
            try
            {
                using (var client = new SmtpClient(host, port))
                using (var message = new MailMessage())
                using (var stream = new MemoryStream(pdf))
                {
                    client.EnableSsl = ssl;
                    client.Timeout = timeout;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(from, pass);

                    message.From = new MailAddress(from, display);
                    message.To.Add(new MailAddress(toEmail));
                    if (!string.IsNullOrWhiteSpace(copy)) message.Bcc.Add(new MailAddress(copy.Trim()));
                    message.Subject = subj;
                    message.Body = text;
                    message.Attachments.Add(new Attachment(stream, fileName, "application/pdf"));
                    client.Send(message);
                }
                return null;
            }
            catch (Exception ex)
            {
                return ex.GetType().Name + ": " + ex.Message;
            }
        });
    }

    private void Update()
    {
        if (task == null || !task.IsCompleted) return;

        string error = task.IsFaulted ? task.Exception?.GetBaseException().Message : task.Result;
        task = null;
        IsSending = false;

        var ok = pendingSuccess;
        var fail = pendingError;
        pendingSuccess = null;
        pendingError = null;

        if (error == null)
        {
            Debug.Log("[ReportMailer] Informe enviado.");
            ok?.Invoke();
        }
        else
        {
            Debug.LogError("[ReportMailer] No se pudo enviar el informe: " + error);
            fail?.Invoke("No se pudo enviar. Revisa el correo y la conexión a internet.");
        }
    }
}
