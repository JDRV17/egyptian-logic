using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepSound : MonoBehaviour
{
    [SerializeField] private Transform rig;              // XR Origin
    [SerializeField] private AudioClip[] clips;
    [SerializeField] private float stepDistance = 0.7f;  // metros entre pasos
    [Range(0f, 1f)] [SerializeField] private float volume = 0.6f;

    private AudioSource src;
    private Vector3 lastPos;
    private float accumulated;

    private void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        if (rig == null) rig = transform;
        lastPos = rig.position;
    }

    private void Update()
    {
        Vector3 p = rig.position;
        Vector3 delta = p - lastPos;
        delta.y = 0f;
        lastPos = p;

        // Ignora teletransportes grandes
        if (delta.magnitude > 1f) return;

        accumulated += delta.magnitude;
        if (accumulated >= stepDistance && clips.Length > 0)
        {
            accumulated = 0f;
            src.pitch = Random.Range(0.95f, 1.05f);
            src.PlayOneShot(clips[Random.Range(0, clips.Length)], volume);
        }
    }
}