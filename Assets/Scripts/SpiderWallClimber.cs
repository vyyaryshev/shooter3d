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
    [SerializeField] private bool useEnemySeparation = true;

    [Header("Surface")]
    [SerializeField] private LayerMask climbableMask = ~0;
    [SerializeField] private float surfaceOffset = 0.18f;
    [SerializeField] private float surfaceProbeDistance = 0.8f;
    [SerializeField] private float forwardProbeDistance = 0.7f;
    [SerializeField] private float edgeProbeDistance = 0.35f;
    [SerializeField] private float surfaceSnapSpeed = 16f;
    [SerializeField] private float normalSmoothing = 14f;
    [SerializeField] private bool ignoreCharactersAsSurfaces = true;
    [SerializeField] private bool climbOnlyWhenShortensPath = true;
    [SerializeField] private float minClimbDistanceGain = 0.5f;
    [SerializeField] private float climbTransitionCost = 0.75f;
    [SerializeField] private float elevatedTargetHeight = 1.0f;
    [SerializeField] private float underTargetHorizontalDistance = 1.2f;
    [SerializeField] private float elevatedTargetWallSearchRadius = 1.6f;
    [SerializeField] private int elevatedTargetWallSearchRays = 12;
    [SerializeField] private float edgeTopProbeHeight = 0.7f;
    [SerializeField] private float edgeTopProbeForward = 0.7f;
    [SerializeField] private float edgeTopProbeDown = 1.4f;

    [Header("Attack")]
    [SerializeField] private int damage = 15;
    [SerializeField] private float attackDistance = 1.6f;
    [SerializeField] private float attackCooldown = 1.25f;

    private NavMeshAgent navMeshAgent;
    private Rigidbody spiderRigidbody;
    private EnemyController enemyController;
    private EnemySeparation enemySeparation;

    private Vector3 surfaceNormal = Vector3.up;
    private Vector3 desiredMoveDirection;
    private float nextPlayerSearchTime;
    private float nextAttackTime;
    private bool isDead;
    private bool hasSpottedPlayer;
    private Collider[] ownColliders;
    private bool targetIsElevated;
    private bool targetIsLower;

    public Vector3 SurfaceNormal => surfaceNormal;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        spiderRigidbody = GetComponent<Rigidbody>();
        enemyController = GetComponent<EnemyController>();
        enemySeparation = GetComponent<EnemySeparation>();
        if (useEnemySeparation)
        {
            if (enemySeparation == null)
                enemySeparation = gameObject.AddComponent<EnemySeparation>();

            enemySeparation.SetAutoApply(false);
        }

        ownColliders = GetComponentsInChildren<Collider>();

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
        UpdateTargetHeightState();
        UpdateDesiredDirection();

        if (!TryFindSurface(out RaycastHit surfaceHit))
            return;

        UpdateSurfaceAlignment(surfaceHit);
        ApplyEnemySeparation();

        if (player == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackDistance)
        {
            TryAttack();
            return;
        }

        if (distanceToPlayer > stopDistance && !ShouldWaitForClimbSurface())
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

    private void UpdateTargetHeightState()
    {
        targetIsElevated = false;
        targetIsLower = false;

        if (player == null)
            return;

        float heightDelta = player.position.y - transform.position.y;
        targetIsElevated = heightDelta >= elevatedTargetHeight;
        targetIsLower = heightDelta <= -elevatedTargetHeight;
    }

    private bool ShouldWaitForClimbSurface()
    {
        if (!targetIsElevated || player == null || !IsMostlyHorizontalSurface())
            return false;

        Vector3 horizontalOffset = player.position - transform.position;
        horizontalOffset.y = 0f;
        return horizontalOffset.magnitude <= underTargetHorizontalDistance;
    }

    private bool TryFindSurface(out RaycastHit surfaceHit)
    {
        if (targetIsElevated && IsMostlyHorizontalSurface() && TryFindElevatedTargetClimbSurface(out surfaceHit))
            return true;

        if (targetIsLower && IsMostlyHorizontalSurface() && TryFindLowerTargetDescendSurface(out surfaceHit))
            return true;

        if (!IsMostlyHorizontalSurface() && TryFindEdgeLandingSurface(out surfaceHit))
            return true;

        Vector3 origin = transform.position + surfaceNormal * surfaceOffset;

        Vector3 forward = desiredMoveDirection.sqrMagnitude > 0.0001f ? desiredMoveDirection : transform.forward;
        Vector3 forwardOrigin = transform.position + surfaceNormal * surfaceOffset;
        if (TryRaycastSurface(forwardOrigin, forward, forwardProbeDistance, true, out surfaceHit))
            return true;

        if (TryRaycastSurface(origin, -surfaceNormal, surfaceOffset + surfaceProbeDistance, false, out surfaceHit))
            return true;

        Vector3 edgeOrigin = transform.position + forward * edgeProbeDistance + surfaceNormal * surfaceOffset;
        return TryRaycastSurface(edgeOrigin, -surfaceNormal, surfaceOffset + surfaceProbeDistance, false, out surfaceHit);
    }

    private bool TryFindElevatedTargetClimbSurface(out RaycastHit bestHit)
    {
        bestHit = default;

        if (player == null)
            return false;

        Vector3 toPlayer = player.position - transform.position;
        Vector3 preferredDirection = Vector3.ProjectOnPlane(toPlayer, Vector3.up).normalized;
        if (preferredDirection.sqrMagnitude < 0.0001f)
            preferredDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        int rayCount = Mathf.Max(1, elevatedTargetWallSearchRays);
        float bestScore = float.NegativeInfinity;
        bool found = false;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = rayCount == 1 ? 0f : Mathf.Lerp(-90f, 90f, i / (float)(rayCount - 1));
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * preferredDirection;
            Vector3 origin = transform.position + Vector3.up * surfaceOffset;

            if (!TryRaycastSurface(origin, direction, elevatedTargetWallSearchRadius, true, out RaycastHit hit))
                continue;

            if (Vector3.Dot(hit.normal, Vector3.up) > 0.65f)
                continue;

            float directionScore = Vector3.Dot(direction.normalized, preferredDirection);
            float heightScore = Mathf.Clamp01((player.position.y - hit.point.y) / Mathf.Max(0.01f, elevatedTargetHeight));
            float score = directionScore + heightScore - hit.distance * 0.1f;

            if (score <= bestScore)
                continue;

            bestScore = score;
            bestHit = hit;
            found = true;
        }

        return found;
    }

    private bool TryFindLowerTargetDescendSurface(out RaycastHit bestHit)
    {
        bestHit = default;

        if (player == null)
            return false;

        Vector3 toPlayer = player.position - transform.position;
        Vector3 preferredDirection = Vector3.ProjectOnPlane(toPlayer, Vector3.up).normalized;
        if (preferredDirection.sqrMagnitude < 0.0001f)
            preferredDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        int rayCount = Mathf.Max(1, elevatedTargetWallSearchRays);
        float bestScore = float.NegativeInfinity;
        bool found = false;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = rayCount == 1 ? 0f : Mathf.Lerp(-90f, 90f, i / (float)(rayCount - 1));
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * preferredDirection;
            Vector3 origin = transform.position + Vector3.up * surfaceOffset;

            if (!TryRaycastSurface(origin, direction, elevatedTargetWallSearchRadius, true, out RaycastHit hit))
                continue;

            if (Vector3.Dot(hit.normal, Vector3.up) > 0.65f)
                continue;

            float directionScore = Vector3.Dot(direction.normalized, preferredDirection);
            float descentScore = Mathf.Clamp01((hit.point.y - player.position.y) / Mathf.Max(0.01f, elevatedTargetHeight));
            float score = directionScore + descentScore - hit.distance * 0.1f;

            if (score <= bestScore)
                continue;

            bestScore = score;
            bestHit = hit;
            found = true;
        }

        return found;
    }

    private bool TryFindEdgeLandingSurface(out RaycastHit surfaceHit)
    {
        surfaceHit = default;

        Vector3 forward = desiredMoveDirection.sqrMagnitude > 0.0001f ? desiredMoveDirection : transform.forward;
        Vector3 tangentForward = Vector3.ProjectOnPlane(forward, surfaceNormal).normalized;
        if (tangentForward.sqrMagnitude < 0.0001f)
            tangentForward = transform.forward;

        Vector3 probeOrigin = transform.position
            + tangentForward * edgeTopProbeForward
            + Vector3.up * edgeTopProbeHeight;

        if (!TryRaycastSurface(probeOrigin, Vector3.down, edgeTopProbeDown, true, out RaycastHit hit))
            return false;

        bool isHorizontalSurface = Vector3.Dot(hit.normal, Vector3.up) > 0.65f;
        if (!isHorizontalSurface)
            return false;

        if (targetIsElevated && hit.point.y + surfaceOffset < transform.position.y)
            return false;

        if (targetIsLower && hit.point.y - surfaceOffset > transform.position.y)
            return false;

        surfaceHit = hit;
        return true;
    }

    private bool TryRaycastSurface(Vector3 origin, Vector3 direction, float distance, bool candidateClimbTransition, out RaycastHit bestHit)
    {
        bestHit = default;
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, climbableMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return false;

        float bestDistance = float.PositiveInfinity;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || !IsValidSurface(hit, candidateClimbTransition))
                continue;

            if (hit.distance >= bestDistance)
                continue;

            bestDistance = hit.distance;
            bestHit = hit;
            found = true;
        }

        return found;
    }

    private bool IsValidSurface(RaycastHit hit, bool candidateClimbTransition)
    {
        if (IsOwnCollider(hit.collider))
            return false;

        if (ignoreCharactersAsSurfaces && IsCharacterSurface(hit.collider))
            return false;

        if (candidateClimbTransition && climbOnlyWhenShortensPath && !DoesSurfaceShortenPath(hit))
            return false;

        return true;
    }

    private bool DoesSurfaceShortenPath(RaycastHit hit)
    {
        if (player == null)
            return true;

        if ((targetIsElevated || targetIsLower) && IsMostlyHorizontalSurface())
        {
            bool isWallLikeSurface = Vector3.Dot(hit.normal, Vector3.up) < 0.65f;
            if (isWallLikeSurface)
                return true;
        }

        float currentDistance = Vector3.Distance(transform.position, player.position);
        float candidateDistance = Vector3.Distance(hit.point + hit.normal * surfaceOffset, player.position) + climbTransitionCost;
        return candidateDistance <= currentDistance - minClimbDistanceGain;
    }

    private bool IsMostlyHorizontalSurface()
    {
        return Vector3.Dot(surfaceNormal, Vector3.up) > 0.65f;
    }

    private bool IsOwnCollider(Collider candidate)
    {
        if (candidate == null || ownColliders == null)
            return false;

        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] == candidate)
                return true;
        }

        return false;
    }

    private bool IsCharacterSurface(Collider candidate)
    {
        Transform candidateRoot = candidate.transform.root;
        if (candidateRoot == transform.root)
            return true;

        return candidate.GetComponentInParent<Health>() != null
            || candidate.GetComponentInParent<EnemyController>() != null
            || candidate.GetComponentInParent<SpiderWallClimber>() != null;
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

    private void ApplyEnemySeparation()
    {
        if (!useEnemySeparation || enemySeparation == null)
            return;

        transform.position += enemySeparation.GetSeparationOffset(surfaceNormal, Time.deltaTime);
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
        minClimbDistanceGain = Mathf.Max(0f, minClimbDistanceGain);
        climbTransitionCost = Mathf.Max(0f, climbTransitionCost);
        elevatedTargetHeight = Mathf.Max(0f, elevatedTargetHeight);
        underTargetHorizontalDistance = Mathf.Max(0f, underTargetHorizontalDistance);
        elevatedTargetWallSearchRadius = Mathf.Max(0.01f, elevatedTargetWallSearchRadius);
        elevatedTargetWallSearchRays = Mathf.Max(1, elevatedTargetWallSearchRays);
        edgeTopProbeHeight = Mathf.Max(0.01f, edgeTopProbeHeight);
        edgeTopProbeForward = Mathf.Max(0.01f, edgeTopProbeForward);
        edgeTopProbeDown = Mathf.Max(0.01f, edgeTopProbeDown);
        damage = Mathf.Max(0, damage);
        attackDistance = Mathf.Max(0.01f, attackDistance);
        attackCooldown = Mathf.Max(0.01f, attackCooldown);
    }
}
