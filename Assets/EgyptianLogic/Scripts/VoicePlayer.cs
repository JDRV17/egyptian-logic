using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Reproduce las voces del faraón y del narrador con un solo AudioSource 2D (se oye igual en cualquier punto).
/// Si llega una voz nueva mientras otra suena, corta la anterior y empieza la nueva, con su subtítulo.
/// Se llama desde los eventos del Inspector (onSolved, onLevelComplete, etc.) con VoicePlayer → Play (AudioClip).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class VoicePlayer : MonoBehaviour
{
    [Tooltip("Voz que suena al empezar la escena (por ejemplo N1-01). Vacío = ninguna.")]
    public AudioClip playOnStart;
    [Tooltip("Segundos de espera antes de la voz inicial, para que termine el fundido de entrada.")]
    public float startDelay = 1.5f;
    [Tooltip("Grupo Voices del mixer.")]
    public AudioMixerGroup output;

    [Header("Subtítulos (opcional)")]
    public VoiceLines lines;
    public SubtitleDisplay subtitles;

    private AudioSource source;

    public bool IsSpeaking => source != null && source.isPlaying;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;   // 2D
        if (output != null) source.outputAudioMixerGroup = output;
    }

    private void Start()
    {
        if (playOnStart != null) Invoke(nameof(PlayStartClip), startDelay);
    }

    private void PlayStartClip() => Play(playOnStart);

    /// <summary>Corta la voz que esté sonando (si hay) y reproduce esta.</summary>
    public void Play(AudioClip clip)
    {
        if (clip == null) return;
        CancelInvoke(nameof(PlayStartClip));   // si algo pasa antes de la bienvenida, la bienvenida ya no suena
        source.Stop();
        source.clip = clip;
        source.Play();
        Debug.Log("[VoicePlayer] " + clip.name);

        if (subtitles != null)
        {
            string text = lines != null ? lines.TextFor(clip) : null;
            if (string.IsNullOrWhiteSpace(text)) subtitles.Hide();
            else subtitles.Show(text, clip.length);
        }
    }

    public void Stop()
    {
        source.Stop();
        if (subtitles != null) subtitles.Hide();
    }
}
