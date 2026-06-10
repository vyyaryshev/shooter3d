using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class RoboDroneAI : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private bool rotateVisualOnly = true;
    [Tooltip("Yaw correction for models that face sideways relative to Unity's +Z forward axis.")]
    [SerializeField] private float visualYawOffset = -90f;
    [SerializeField] private Vector3 visualRotationOffset;
    [SerializeField] private string[] firePointNames = { "FirePoint", "ShootPoint", "MuzzlePoint", "Muzzle" };

    [Header("Target")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float viewDistance = 18f;
    [SerializeField] private float loseTargetDistance = 26f;
    [SerializeField] private bool requireLineOfSight = false;
    [SerializeField] private LayerMask lineOfSightMask = ~0;
    [SerializeField] private float aimHeightOffset = 1.2f;
    [SerializeField] private float playerSearchInterval = 0.5f;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolPointTolerance = 0.6f;

    [Header("Orbit")]
    [SerializeField] private bool orbitAroundDroneLayer = true;
    [SerializeField] private string orbitCenterLayerName = "Drone";
    [SerializeField] private float orbitCenterSearchRadius = 50f;
    [SerializeField] private float orbitCenterRefreshInterval = 1f;
    [SerializeField] private float minOrbitRadius = 5f;
    [SerializeField] private float maxOrbitRadius = 9f;
    [SerializeField] private float orbitHeight = 2.5f;
    [SerializeField] private float verticalWaveAmplitude = 0.7f;
    [SerializeField] private float minOrbitAngularSpeed = 45f;
    [SerializeField] private float maxOrbitAngularSpeed = 95f;
    [SerializeField] private float orbitPositionSharpness = 6f;
    [SerializeField] private float orbitRetargetInterval = 1.5f;
    [SerializeField] private float rotationSharpness = 12f;

    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private int projectileDamage = 15;
    [SerializeField] private float projectileSpeed = 22f;
    [SerializeField] private float projectileLifeTime = 5f;
    [SerializeField] private float minShootInterval = 0.7f;
    [SerializeField] private float maxShootInterval = 2.2f;
    [SerializeField] private bool addRigidbodyIfMissing = true;
    [SerializeField] private ParticleSystem muzzleFlash;

    private NavMeshAgent agent;
    private Animator animator;
    private EnemyController enemyController;
    private int currentPatrolPoint;
    private float orbitAngle;
    private float orbitRadius;
    private float orbitDirection = 1f;
    private float orbitAngularSpeed;
    private float nextOrbitRetargetTime;
    private float nextShootTime;
    private float nextPlayerSearchTime;
    private float nextOrbitCenterSearchTime;
    private Transform orbitCenter;
    private bool isOrbiting;
    private Transform player;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        enemyController = GetComponent<EnemyController>();

        if (visualRoot == null)
            visualRoot = FindDefaultVisualRoot();

        if (firePoint == null)
            firePoint = FindChildByName(firePointNames);

        if (firePoint == null)
            firePoint = visualRoot != null ? visualRoot : transform;
    }

    private void Start()
    {
        ResolvePlayer(true);
        ResolveOrbitCenter(true);
        PickNewOrbitSettings();
        ScheduleNextShot();

        if (IsAgentReady())
            GoToNextPatrolPoint();
    }

    private void Update()
    {
        if (enemyController != null && enemyController.IsDead())
            return;

        ResolvePlayer(false);
        ResolveOrbitCenter(false);

        if (player == null)
        {
            Patrol();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (CanSeePlayer(distanceToPlayer))
        {
            OrbitPlayer();
            TryShoot();
            return;
        }

        if (isOrbiting && distanceToPlayer <= loseTargetDistance)
        {
            OrbitPlayer();
            TryShoot();
            return;
        }

        Patrol();
    }

    private void ResolvePlayer(bool force)
    {
        if (!force && player != null && player.gameObject.activeInHierarchy)
            return;

        if (!force && Time.time < nextPlayerSearchTime)
            return;

        nextPlayerSearchTime = Time.time + playerSearchInterval;

        if (string.IsNullOrWhiteSpace(playerTag))
        {
            player = null;
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
            player = playerObject.transform;
        else
            player = null;
    }

    private bool CanSeePlayer(float distanceToPlayer)
    {
        if (distanceToPlayer > viewDistance)
            return false;

        if (!requireLineOfSight)
            return true;

        Vector3 start = transform.position;
        Vector3 target = GetAimPoint();
        Vector3 direction = target - start;

        if (Physics.Raycast(start, direction.normalized, out RaycastHit hit, direction.magnitude, lineOfSightMask, QueryTriggerInteraction.Ignore))
            return hit.transform.root == player.root;

        return true;
    }

    private void Patrol()
    {
        isOrbiting = false;

        if (!IsAgentReady() || patrolPoints == null || patrolPoints.Length == 0)
            return;

        agent.isStopped = false;
        agent.updateRotation = !rotateVisualOnly;

        if (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance)
            GoToNextPatrolPoint();

        RotateToMovement();

        if (animator != null)
            animator.Play("Run");
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0 || !IsAgentReady())
            return;

        Transform point = patrolPoints[currentPatrolPoint];
        currentPatrolPoint = (currentPatrolPoint + 1) % patrolPoints.Length;

        if (point != null)
            agent.SetDestination(point.position);
    }

    private void OrbitPlayer()
    {
        isOrbiting = true;

        if (IsAgentReady())
        {
            agent.isStopped = true;
            agent.updateRotation = false;
        }

        if (Time.time >= nextOrbitRetargetTime)
            PickNewOrbitSettings();

        orbitAngle += orbitDirection * orbitAngularSpeed * Time.deltaTime;
        Vector3 offset = Quaternion.Euler(0f, orbitAngle, 0f) * Vector3.forward * orbitRadius;
        float wave = Mathf.Sin(Time.time * 2f) * verticalWaveAmplitude;
        Vector3 targetPosition = GetOrbitCenterPosition() + offset + Vector3.up * (orbitHeight + wave);

        // The empty root owns movement; the child model owns visible aiming.
        transform.position = Vector3.Lerp(transform.position, targetPosition, orbitPositionSharpness * Time.deltaTime);
        RotateToPlayer();

        if (animator != null)
            animator.Play("Run");
    }

    private void PickNewOrbitSettings()
    {
        orbitRadius = Random.Range(minOrbitRadius, maxOrbitRadius);
        orbitDirection = Random.value < 0.5f ? -1f : 1f;
        orbitAngularSpeed = Random.Range(minOrbitAngularSpeed, maxOrbitAngularSpeed);

        if (player != null)
        {
            Vector3 toDrone = transform.position - GetOrbitCenterPosition();
            toDrone.y = 0f;

            if (toDrone.sqrMagnitude > 0.01f)
                orbitAngle = Mathf.Atan2(toDrone.x, toDrone.z) * Mathf.Rad2Deg;
            else
                orbitAngle = Random.Range(0f, 360f);
        }

        nextOrbitRetargetTime = Time.time + orbitRetargetInterval;
    }

    private void RotateToPlayer()
    {
        Vector3 direction = GetAimPoint() - transform.position;
        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        ApplyVisibleRotation(targetRotation);
    }

    private void RotateToMovement()
    {
        if (!rotateVisualOnly || agent == null || agent.desiredVelocity.sqrMagnitude < 0.01f)
            return;

        Vector3 direction = agent.desiredVelocity;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        ApplyVisibleRotation(targetRotation);
    }

    private void ApplyVisibleRotation(Quaternion targetRotation)
    {
        targetRotation *= Quaternion.Euler(0f, visualYawOffset, 0f);
        targetRotation *= Quaternion.Euler(visualRotationOffset);

        Transform rotationTarget = rotateVisualOnly && visualRoot != null ? visualRoot : transform;
        rotationTarget.rotation = Quaternion.Slerp(rotationTarget.rotation, targetRotation, rotationSharpness * Time.deltaTime);
    }

    private void TryShoot()
    {
        if (Time.time < nextShootTime || projectilePrefab == null || firePoint == null)
            return;

        FireProjectile();
        ScheduleNextShot();
    }

    private void FireProjectile()
    {
        Vector3 aimDirection = GetAimPoint() - firePoint.position;
        if (aimDirection.sqrMagnitude < 0.01f)
            aimDirection = transform.forward;

        Quaternion rotation = Quaternion.LookRotation(aimDirection.normalized, Vector3.up);
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, rotation);

        bool hasBulletController = projectile.TryGetComponent(out BulletController bullet);
        bool hasFireball = projectile.TryGetComponent(out Fireball _);
        if (hasBulletController)
            bullet.Initialize(projectileDamage, gameObject);

        if (hasBulletController)
        {
            Rigidbody bulletRigidbody = GetOrCreateProjectileRigidbody(projectile);
            if (bulletRigidbody != null)
                bulletRigidbody.linearVelocity = rotation * Vector3.forward * projectileSpeed;
        }
        else if (!hasFireball)
        {
            if (!projectile.TryGetComponent(out RoboDroneProjectile droneProjectile))
                droneProjectile = projectile.AddComponent<RoboDroneProjectile>();

            GetOrCreateProjectileRigidbody(projectile);
            droneProjectile.Initialize(projectileDamage, gameObject, projectileSpeed, projectileLifeTime);
        }

        if (muzzleFlash != null)
            muzzleFlash.Play(true);
    }

    private Rigidbody GetOrCreateProjectileRigidbody(GameObject projectile)
    {
        if (projectile.TryGetComponent(out Rigidbody projectileRigidbody))
            return projectileRigidbody;

        if (!addRigidbodyIfMissing)
            return null;

        projectileRigidbody = projectile.AddComponent<Rigidbody>();
        projectileRigidbody.useGravity = false;
        projectileRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        return projectileRigidbody;
    }

    private Vector3 GetAimPoint()
    {
        return player != null ? player.position + Vector3.up * aimHeightOffset : transform.position + transform.forward;
    }

    private Vector3 GetOrbitCenterPosition()
    {
        if (orbitAroundDroneLayer && orbitCenter != null)
            return orbitCenter.position;

        return player != null ? player.position : transform.position;
    }

    private void ScheduleNextShot()
    {
        nextShootTime = Time.time + Random.Range(minShootInterval, maxShootInterval);
    }

    private bool IsAgentReady()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private Transform FindDefaultVisualRoot()
    {
        return transform.childCount > 0 ? transform.GetChild(0) : null;
    }

    private Transform FindChildByName(string[] names)
    {
        if (names == null)
            return null;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < names.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(names[i]))
                continue;

            for (int childIndex = 0; childIndex < children.Length; childIndex++)
            {
                if (children[childIndex].name == names[i])
                    return children[childIndex];
            }
        }

        return null;
    }

    private void ResolveOrbitCenter(bool force)
    {
        if (!orbitAroundDroneLayer)
        {
            orbitCenter = null;
            return;
        }

        if (!force && orbitCenter != null && orbitCenter.gameObject.activeInHierarchy)
            return;

        if (!force && Time.time < nextOrbitCenterSearchTime)
            return;

        nextOrbitCenterSearchTime = Time.time + orbitCenterRefreshInterval;

        int droneLayer = LayerMask.NameToLayer(orbitCenterLayerName);
        if (droneLayer < 0)
        {
            orbitCenter = null;
            return;
        }

        Collider[] candidates = Physics.OverlapSphere(transform.position, orbitCenterSearchRadius, 1 << droneLayer, QueryTriggerInteraction.Collide);
        orbitCenter = FindClosestOrbitCenter(candidates);

        if (orbitCenter == null)
            orbitCenter = FindClosestOrbitCenterTransform(droneLayer);
    }

    private Transform FindClosestOrbitCenter(Collider[] candidates)
    {
        Transform closest = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i] == null || candidates[i].transform.root == transform.root)
                continue;

            float distance = Vector3.SqrMagnitude(candidates[i].transform.position - transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = candidates[i].transform;
            }
        }

        return closest;
    }

    private Transform FindClosestOrbitCenterTransform(int droneLayer)
    {
        Transform[] candidates = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        Transform closest = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < candidates.Length; i++)
        {
            Transform candidate = candidates[i];
            if (candidate == null || candidate.root == transform.root || candidate.gameObject.layer != droneLayer)
                continue;

            float distance = Vector3.SqrMagnitude(candidate.position - transform.position);
            if (distance > orbitCenterSearchRadius * orbitCenterSearchRadius)
                continue;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = candidate;
            }
        }

        return closest;
    }
}
