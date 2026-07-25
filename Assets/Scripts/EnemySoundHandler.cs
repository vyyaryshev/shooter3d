using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class EnemySoundHandler : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool createAudioSourceIfMissing = true;

    [Header("Clips")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip spottedClip;
    [SerializeField] private AudioClip damageClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip attackClip;

    [Header("Footsteps")]
    [SerializeField] private float footstepInterval = 0.45f;
    [SerializeField] private float movingSpeedThreshold = 0.15f;
    [SerializeField] private bool useFootsteps = true;

    [Header("Playback")]
    [SerializeField] private float volume = 1f;
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    private NavMeshAgent agent;
    private Vector3 previousPosition;
    private float nextFootstepTime;
    private bool isDead;
    private bool spottedWasPlayed;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null && createAudioSourceIfMissing)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }

        previousPosition = transform.position;
    }

    private void Update()
    {
        if (!useFootsteps || isDead)
        {
            previousPosition = transform.position;
            return;
        }

        float speed = GetMovementSpeed();
        if (speed >= movingSpeedThreshold && Time.time >= nextFootstepTime)
        {
            PlayRandomClip(footstepClips);
            nextFootstepTime = Time.time + footstepInterval;
        }

        previousPosition = transform.position;
    }

    public void HealthChanged(HealthChangedMessage message)
    {
        if (message.health <= 0)
        {
            PlayDeathSound();
            return;
        }

        if (message.healthChange < 0)
            PlayDamageSound();
    }

    public void EnemySpottedPlayer()
    {
        PlaySpottedSound();
    }

    public void OnPlayerSpotted()
    {
        PlaySpottedSound();
    }

    public void EnemyAttackStarted()
    {
        PlayAttackSound();
    }

    public void OnAttackStarted()
    {
        PlayAttackSound();
    }

    public void PlaySpottedSound()
    {
        if (spottedWasPlayed || isDead)
            return;

        spottedWasPlayed = true;
        PlayClip(spottedClip);
    }

    public void PlayDamageSound()
    {
        if (isDead)
            return;

        PlayClip(damageClip);
    }

    public void PlayDeathSound()
    {
        if (isDead)
            return;

        isDead = true;
        PlayClip(deathClip);
    }

    public void PlayAttackSound()
    {
        if (isDead)
            return;

        PlayClip(attackClip);
    }

    private float GetMovementSpeed()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            return agent.velocity.magnitude;

        Vector3 movement = transform.position - previousPosition;
        return movement.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
    }

    private void PlayRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        PlayClip(clip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip, volume);
    }
}
