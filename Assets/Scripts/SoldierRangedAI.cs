using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SoldierRangedAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform player;
    [SerializeField] private float targetSearchInterval = 0.5f;

    [Header("Movement")]
    [SerializeField] private float detectionRange = 30f;
    [SerializeField] private float keepDistance = 12f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float navMeshSampleRadius = 3f;
    [SerializeField] private bool warpToNavMeshAfterSpawn = true;

    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private int damage = 10;
    [SerializeField] private float fireRange = 24f;
    [SerializeField] private float fireInterval = 1.25f;
    [SerializeField] private float bulletSpeed = 28f;
    [SerializeField] private float aimHeightOffset = 1.2f;
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask lineOfSightMask = ~0;

    [Header("Animation")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string aimingParameter = "Aiming";
    [SerializeField] private string shootTrigger = "Shoot";
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string runStateName = "Run";
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField] private string deathStateName = "Death";
    [SerializeField] private float attackAnimationLock = 0.35f;

    private NavMeshAgent agent;
    private Animator animator;
    private EnemyController enemyController;

    private float nextPlayerSearchTime;
    private float nextShotTime;
    private float attackAnimationLockUntil;
    private bool isDead;

    private bool hasSpeedParameter;
    private bool hasAimingParameter;
    private bool hasShootTrigger;
    private string currentMovementState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        enemyController = GetComponent<EnemyController>();

        if (agent != null)
            agent.stoppingDistance = keepDistance;

        if (firePoint == null)
            firePoint = FindFirePoint();

        CacheAnimatorParameters();
    }

    private void OnEnable()
    {
        if (warpToNavMeshAfterSpawn)
            StartCoroutine(WarpToNavMeshAfterSpawn());
    }

    private void Start()
    {
        ResolvePlayer(true);
    }

    private void Update()
    {
        if (isDead || (enemyController != null && enemyController.IsDead()))
            return;

        if (!EnsureAgentOnNavMesh())
        {
            UpdateMovementAnimation(0f);
            return;
        }

        ResolvePlayer(false);

        if (player == null)
        {
            StopMoving();
            SetAiming(false);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange)
        {
            StopMoving();
            SetAiming(false);
            return;
        }

        RotateTowardsPlayer();
        SetAiming(true);

        if (distanceToPlayer > keepDistance)
            MoveToPlayer();
        else
            StopMoving();

        if (distanceToPlayer <= fireRange && HasLineOfSight())
            TryShoot();

        UpdateMovementAnimation(agent.velocity.magnitude);
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
        StopMoving();

        if (agent != null)
            agent.enabled = false;

        if (animator != null && !string.IsNullOrWhiteSpace(deathStateName))
            animator.Play(deathStateName);
    }

    private IEnumerator WarpToNavMeshAfterSpawn()
    {
        yield return new WaitForFixedUpdate();
        ForceWarpToNearestNavMesh();
    }

    private void MoveToPlayer()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        agent.stoppingDistance = keepDistance;
        agent.SetDestination(player.position);
    }

    private void StopMoving()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        UpdateMovementAnimation(0f);
    }

    private void RotateTowardsPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private bool HasLineOfSight()
    {
        if (!requireLineOfSight)
            return true;

        Vector3 origin = GetFireOrigin();
        Vector3 target = GetAimTarget();
        Vector3 direction = target - origin;

        if (!Physics.Raycast(origin, direction.normalized, out RaycastHit hit, fireRange, lineOfSightMask, QueryTriggerInteraction.Ignore))
            return false;

        return hit.transform == player || hit.transform.root == player.root;
    }

    private void TryShoot()
    {
        if (bulletPrefab == null || Time.time < nextShotTime)
            return;

        nextShotTime = Time.time + fireInterval;

        Vector3 origin = GetFireOrigin();
        Vector3 direction = (GetAimTarget() - origin).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);

        GameObject bulletObject = Instantiate(bulletPrefab, origin, rotation);
        if (bulletObject.TryGetComponent(out BulletController bullet))
            bullet.Initialize(damage, gameObject);

        if (bulletObject.TryGetComponent(out Rigidbody bulletRigidbody))
        {
            bulletRigidbody.useGravity = false;
            bulletRigidbody.linearVelocity = direction * bulletSpeed;
        }

        if (animator != null && hasShootTrigger)
            animator.SetTrigger(shootTrigger);

        if (PlayStateIfPossible(attackStateName))
            attackAnimationLockUntil = Time.time + attackAnimationLock;
    }

    private Vector3 GetFireOrigin()
    {
        if (firePoint != null)
            return firePoint.position;

        return transform.position + Vector3.up * aimHeightOffset + transform.forward * 0.75f;
    }

    private Vector3 GetAimTarget()
    {
        return player.position + Vector3.up * aimHeightOffset;
    }

    private bool EnsureAgentOnNavMesh()
    {
        if (agent == null || !agent.enabled)
            return false;

        if (agent.isOnNavMesh)
            return true;

        return ForceWarpToNearestNavMesh();
    }

    public bool ForceWarpToNearestNavMesh()
    {
        if (agent == null || !agent.enabled)
            return false;

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSampleRadius, agent.areaMask))
            return false;

        agent.Warp(hit.position);
        return agent.isOnNavMesh;
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

    private Transform FindFirePoint()
    {
        string[] names = { "ShootFX", "FirePoint", "Muzzle", "MuzzlePoint" };
        Transform[] children = GetComponentsInChildren<Transform>(true);

        foreach (string childName in names)
        {
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == childName)
                    return children[i];
            }
        }

        return null;
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null)
            return;

        hasSpeedParameter = HasAnimatorParameter(speedParameter, AnimatorControllerParameterType.Float);
        hasAimingParameter = HasAnimatorParameter(aimingParameter, AnimatorControllerParameterType.Bool);
        hasShootTrigger = HasAnimatorParameter(shootTrigger, AnimatorControllerParameterType.Trigger);
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == parameterType)
                return true;
        }

        return false;
    }

    private void UpdateMovementAnimation(float speed)
    {
        if (Time.time < attackAnimationLockUntil)
            return;

        if (animator != null && hasSpeedParameter)
            animator.SetFloat(speedParameter, speed);

        string movementState = speed > 0.1f ? runStateName : idleStateName;
        if (currentMovementState == movementState)
            return;

        if (PlayStateIfPossible(movementState))
            currentMovementState = movementState;
    }

    private void SetAiming(bool aiming)
    {
        if (animator != null && hasAimingParameter)
            animator.SetBool(aimingParameter, aiming);
    }

    private bool PlayStateIfPossible(string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return false;

        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, stateHash))
            return false;

        animator.Play(stateHash);
        return true;
    }
}
