using UnityEngine;

[DisallowMultipleComponent]
public class RocketLauncher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MechRocket rocketPrefab;
    [SerializeField] private Transform launchPoint;
    [SerializeField] private Transform target;
    [Tooltip("Object whose colliders rockets ignore. Defaults to this GameObject.")]
    [SerializeField] private GameObject owner;

    [Header("Direct fire")]
    [SerializeField] private bool automaticFire = true;
    [SerializeField, Min(0.1f)] private float range = 30f;
    [SerializeField] private Vector2Int rocketsPerSalvo = new Vector2Int(3, 5);
    [SerializeField, Min(0.05f)] private float launchInterval = 0.3f;
    [SerializeField, Min(0.1f)] private float salvoCooldown = 8f;
    [SerializeField, Range(0f, 15f)] private float launchSpreadAngle = 3f;
    [SerializeField, Min(0f)] private float launchPositionSpread = 0.1f;

    private Collider targetCollider;
    private Health targetHealth;
    private float nextSearch;
    private float nextRocket;
    private float nextSalvo;
    private int remaining;

    private void Awake()
    {
        if (launchPoint == null)
            launchPoint = transform;
        if (owner == null)
            owner = gameObject;
        if (rocketPrefab == null)
        {
            Debug.LogError("RocketLauncher: assign a prefab with MechRocket.", this);
            enabled = false;
        }
        SetTarget(target);
    }

    public void SetTarget(Transform value)
    {
        target = value;
        targetCollider = target != null ? target.GetComponentInChildren<Collider>() : null;
        targetHealth = target != null ? target.GetComponentInParent<Health>() : null;
    }

    private void Update()
    {
        if (target == null && Time.time >= nextSearch)
        {
            nextSearch = Time.time + 0.5f;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                SetTarget(player.transform);
        }
        if (!TargetReady())
        {
            CancelSalvo();
            return;
        }
        if (automaticFire && remaining == 0)
            FireSalvo();
        if (remaining == 0 || Time.time < nextRocket)
            return;

        Vector2 offset = Random.insideUnitCircle * launchPositionSpread;
        Vector3 origin = launchPoint.position + launchPoint.right * offset.x + launchPoint.up * offset.y;
        Vector3 point = targetCollider != null ? targetCollider.bounds.center : target.position + Vector3.up;
        Vector3 direction = point - origin;
        if (direction.sqrMagnitude < 0.001f)
            direction = launchPoint.forward;
        direction.Normalize();
        Vector3 up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
        Vector2 spread = Random.insideUnitCircle * Mathf.Tan(launchSpreadAngle * Mathf.Deg2Rad);
        Quaternion aim = Quaternion.LookRotation(direction, up);
        direction = aim * new Vector3(spread.x, spread.y, 1f).normalized;
        MechRocket rocket = Instantiate(rocketPrefab, origin, aim);
        // Direction is chosen at launch. There is no ascent or subsequent target tracking.
        rocket.InitializeDirect(owner, direction);
        remaining--;
        nextRocket = Time.time + launchInterval;
        if (remaining == 0)
            nextSalvo = Time.time + salvoCooldown;
    }

    [ContextMenu("Fire Salvo (Play Mode)")]
    public void FireSalvo()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || rocketPrefab == null
            || remaining > 0 || Time.time < nextSalvo || !TargetReady())
            return;
        int minimum = Mathf.Clamp(rocketsPerSalvo.x, 1, 20);
        int maximum = Mathf.Clamp(rocketsPerSalvo.y, minimum, 20);
        remaining = Random.Range(minimum, maximum + 1);
        nextRocket = Time.time;
    }

    private bool TargetReady() => target != null && (targetHealth == null || targetHealth.GetHealth() > 0)
        && Vector3.Distance(launchPoint.position, target.position) <= range;

    private void CancelSalvo()
    {
        if (remaining > 0)
            nextSalvo = Time.time + salvoCooldown;
        remaining = 0;
    }

    private void OnDisable() => CancelSalvo();

    public void HealthChanged(HealthChangedMessage message)
    {
        if (message.health <= 0)
            enabled = false;
    }

    public void PlayerMeleeHit(MeleeHitMessage message)
    {
        CancelSalvo();
        nextSalvo = Mathf.Max(nextSalvo, Time.time + message.stunDuration);
    }
}
