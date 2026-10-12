using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ImpactSound : MonoBehaviour
{
    [SerializeField] private AudioClip[] clips;
    [SerializeField] private float minImpactSpeed = 1f;
    [SerializeField] private float maxImpactSpeed = 6f;
    [SerializeField] private float cooldown = 0.15f;

    private AudioSource src;
    private float lastTime = -10f;

    private void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
    }

    private void OnCollisionEnter(Collision c)
    {
        float speed = c.relativeVelocity.magnitude;
        if (speed < minImpactSpeed || Time.time - lastTime < cooldown) return;
        if (clips == null || clips.Length == 0) return;

        lastTime = Time.time;
        float vol = Mathf.InverseLerp(minImpactSpeed, maxImpactSpeed, speed);
        src.pitch = Random.Range(0.93f, 1.07f);
        src.PlayOneShot(clips[Random.Range(0, clips.Length)], Mathf.Clamp(vol, 0.15f, 1f));
    }
}