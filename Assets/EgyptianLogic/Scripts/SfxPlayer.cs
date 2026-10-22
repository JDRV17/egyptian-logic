using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SfxPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip[] clips;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;

    private AudioSource src;

    private void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
    }

    public void Play()
    {
        if (clips == null || clips.Length == 0) return;
        src.pitch = Random.Range(pitchRange.x, pitchRange.y);
        src.PlayOneShot(clips[Random.Range(0, clips.Length)], volume);
    }
}