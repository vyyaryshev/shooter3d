using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class SpiderWallClimber : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform player;
    [SerializeField] private float targetSearchInterval = 0.5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float stopDistance = 1.4f;
    [SerializeField] private bool disableNavMeshAgent = true;
    [SerializeField] private bool makeRigidbodyKinematic = true;

    [Header("Surface")]
    [SerializeField] private LayerMask climbableMask = ~0;
    [SerializeField] private float surfaceOffset = 0.18f;
    [SerializeField] private float surfaceProbeDistance = 0.8f;
    [SerializeField] private float forwardProbeDistance = 0.7f;
    [SerializeField] private float edgeProbeDistance = 0.35f;
    [SerializeField] private float surfaceSnapSpeed = 16f;
    [SerializeField] private float normalSmoothing = 14f;

    [Header("Attack")]
    [SerializeField] private int damage = 15;
    [SerializeField] private float attackDistance = 1.6f;
    [SerializeField] private float attackCooldown = 1.25f;

    private NavMeshAgent navMeshAgent;
    private Rigidbody spiderRigidbody;
    private EnemyController enemyController;

    private Vector3 surfaceNormal = Vector3.up;
    private Vector3 desiredMoveDirection;
    private float nextPlayerSearchTime;
    private float nextAttackTime;
    private bool isDead;
    private bool hasSpottedPlayer;

    public Vector3 SurfaceNormal => surfaceNormal;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        spiderRigidbody = GetComponent<Rigidbody>();
        enemyController = GetComponent<EnemyController>();

        if (disableNavMeshAgent && navMeshAgent != null)
            navMeshAgent.enabled = false;

        if (makeRigidbodyKinematic && spiderRigidbody != null)
        {
            spiderRigidbody.useGravity = false;
            spiderRigidbody.isKinematic = true;
        }

        surfaceNormal = transform.up;
    }

    private void Update()
    {
        if (isDead || (enemyController != null && enemyController.IsDead()))
            return;

        ResolvePlayer(false);
        UpdateDesiredDirection();

        if (!TryFindSurface(out RaycastHit surfaceHit))
            return;

        UpdateSurfaceAlignment(surfaceHit);

        if (player == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackDistance)
        {
            TryAttack();
            return;
        }

        if (distanceToPlayer > stopDistance)
            MoveOnSurface();
    }

    public void SetPlayer(Transform target)
    {
        player = target;
    }

    public void HealthChanged(HealthChangedMessage message)
    {
        if (message.health > 0 || isDead)
            return;

        isDead = true;

        if (navMeshAgent != null)
            navMeshAgent.enabled = false;

        if (spiderRigidbody != null)
        {
            spiderRigidbody.isKinematic = false;
            spiderRigidbody.useGravity = true;
        }

        enabled = false;
    }

    private void ResolvePlayer(bool force)
    {
        if (player != null || string.IsNullOrWhiteSpace(playerTag))
            return;

        if (!force && Time.time < nextPlayerSearchTime)
            return;

        nextPlayerSearchTime = Time.time + targetSearchInterval;
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
            player = playerObject.transform;
    }

    private void UpdateDesiredDirection()
    {
        if (player == null)
        {
            desiredMoveDirection = Vector3.ProjectOnPlane(transform.forward, surfaceNormal).normalized;
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        desiredMoveDirection = Vector3.ProjectOnPlane(toPlayer, surfaceNormal).normalized;

        if (desiredMoveDirection.sqrMagnitude < 0.0001f)
            desiredMoveDirection = Vector3.ProjectOnPlane(transform.forward, surfaceNormal).normalized;
    }

    private bool TryFindSurface(out RaycastHit surfaceHit)
    {
        Vector3 origin = transform.position + surfaceNormal * surfaceOffset;

        Vector3 forward = desiredMoveDirection.sqrMagnitude > 0.0001f ? desiredMoveDirection : transform.forward;
        Vector3 forwardOrigin = transform.position + surfaceNormal * surfaceOffset;
        if (Physics.Raycast(forwardOrigin, forward, out surfaceHit, forwardProbeDistance, climbableMask, QueryTriggerInteraction.Ignore))
            return true;

        if (Physics.Raycast(origin, -surfaceNormal, out surfaceHit, surfaceOffset + surfaceProbeDistance, climbableMask, QueryTriggerInteraction.Ignore))
            return true;

        Vector3 edgeOrigin = transform.position + forward * edgeProbeDistance + surfaceNormal * surfaceOffset;
        return Physics.Raycast(edgeOrigin, -surfaceNormal, out surfaceHit, surfaceOffset + surfaceProbeDistance, climbableMask, QueryTriggerInteraction.Ignore);
    }

    private void UpdateSurfaceAlignment(RaycastHit surfaceHit)
    {
        float normalT = 1f - Mathf.Exp(-normalSmoothing * Time.deltaTime);
        surfaceNormal = Vector3.Slerp(surfaceNormal, surfaceHit.normal, normalT).normalized;

        Vector3 targetPosition = surfaceHit.point + surfaceNormal * surfaceOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, surfaceSnapSpeed * Time.deltaTime);

        Vector3 forward = desiredMoveDirection.sqrMagnitude > 0.0001f ? desiredMoveDirection : Vector3.ProjectOnPlane(transform.forward, surfaceNormal);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.Cross(transform.right, surfaceNormal);

        Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, surfaceNormal);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void MoveOnSurface()
    {
        Vector3 move = Vector3.ProjectOnPlane(transform.forward, surfaceNormal).normalized;
        transform.position += move * moveSpeed * Time.deltaTime;

        if (!hasSpottedPlayer && player != null)
        {
            hasSpottedPlayer = true;
            gameObject.SendMessage("EnemySpottedPlayer", SendMessageOptions.DontRequireReceiver);
        }
    }

    private void TryAttack()
    {
        if (player == null || Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;
        gameObject.SendMessage("EnemyAttackStarted", SendMessageOptions.DontRequireReceiver);

        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth != null)
            playerHealth.Change(-damage);
    }

    private void OnValidate()
    {
        targetSearchInterval = Mathf.Max(0.05f, targetSearchInterval);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        stopDistance = Mathf.Max(0f, stopDistance);
        surfaceOffset = Mathf.Max(0.01f, surfaceOffset);
        surfaceProbeDistance = Mathf.Max(0.01f, surfaceProbeDistance);
        forwardProbeDistance = Mathf.Max(0.01f, forwardProbeDistance);
        edgeProbeDistance = Mathf.Max(0.01f, edgeProbeDistance);
        surfaceSnapSpeed = Mathf.Max(0f, surfaceSnapSpeed);
        normalSmoothing = Mathf.Max(0f, normalSmoothing);
        damage = Mathf.Max(0, damage);
        attackDistance = Mathf.Max(0.01f, attackDistance);
        attackCooldown = Mathf.Max(0.01f, attackCooldown);
    }
}
