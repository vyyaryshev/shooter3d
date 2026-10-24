using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent), typeof(Health))]
public class ISOMechAI : MonoBehaviour
{
    [Header("Target and movement")]
    [SerializeField] private Transform player;
    [SerializeField, Min(1f)] private float detectionRange = 30f;
    [SerializeField, Min(0.1f)] private float shootingDistance = 10f;
    [SerializeField, Min(0f)] private float rotationSpeed = 180f;
    [SerializeField, Min(0f)] private float turnAnimationThreshold = 5f;
    [SerializeField] private LayerMask sightMask = ~0;

    [Header("Guns")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform[] firePoints;
    [SerializeField] private Transform[] cannonBones;
    [SerializeField, Min(1)] private int shotDamage = 10;
    [SerializeField, Min(0.1f)] private float shotInterval = 0.6f;
    [SerializeField, Min(1f)] private float bulletSpeed = 25f;
    [SerializeField, Min(0f)] private float recoilDistance = 0.08f;
    [SerializeField, Min(0f)] private float cannonAimSpeed = 180f;
    [SerializeField, Range(0f, 90f)] private float maxCannonAimAngle = 70f;

    [Header("Periodic close attack")]
    [SerializeField] private Vector2 meleeInterval = new Vector2(6f, 10f);
    [SerializeField, Min(0.1f)] private float meleeRange = 3f;
    [SerializeField, Min(1)] private int meleeDamage = 35;
    [SerializeField, Min(0.1f)] private float approachTimeout = 6f;
    [SerializeField, Min(0.05f)] private float windup = 0.4f;
    [SerializeField, Min(0.05f)] private float recovery = 0.5f;
    [SerializeField] private float cannonSwingAngle = 40f;

    private NavMeshAgent agent;
    private Health health;
    private Animator animator;
    private NavMeshActivationOnLanding landing;
    private Collider targetCollider;
    private Health targetHealth;
    private readonly RaycastHit[] sightHits = new RaycastHit[32];
    private Vector3[] cannonPositions;
    private Quaternion[] cannonRotations;
    private Quaternion[] cannonAimOffsets;
    private Vector3[] cannonAimAxes;
    private bool wantsShot;
    private bool poseApplied;
    private bool dead;
    private bool spotted;
    private bool approaching;
    private bool swinging;
    private bool hitApplied;
    private bool gunsReady;
    private float swingStarted;
    private float approachEnds;
    private float nextMelee;
    private float nextShot;
    private float nextSearch;
    private float nextPathUpdate;
    private float stunnedUntil;
    private float recoil;
    private int gunIndex;
    private int animationState;

    private static readonly int IdleState = Animator.StringToHash("Idle");
    private static readonly int WalkState = Animator.StringToHash("Walk");
    private static readonly int DeathState = Animator.StringToHash("Death");

    private bool AgentReady => agent != null && agent.enabled && agent.isOnNavMesh;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        animator = GetComponentInChildren<Animator>();
        landing = GetComponent<NavMeshActivationOnLanding>();
        agent.updateRotation = false;
        if (animator != null)
            animator.applyRootMotion = false;

        cannonBones = cannonBones ?? new Transform[0];
        cannonPositions = new Vector3[cannonBones.Length];
        cannonRotations = new Quaternion[cannonBones.Length];
        cannonAimOffsets = new Quaternion[cannonBones.Length];
        cannonAimAxes = new Vector3[cannonBones.Length];
        for (int i = 0; i < cannonBones.Length; i++)
        {
            cannonAimOffsets[i] = Quaternion.identity;
            Transform bone = cannonBones[i];
            if (bone == null)
                continue;
            // The model's left and right bones have different local axes.
            // FirePoint's blue Z axis defines the actual barrel direction.
            Vector3 forward = transform.forward;
            if (firePoints != null)
            {
                foreach (Transform muzzle in firePoints)
                {
                    if (muzzle != null && muzzle.IsChildOf(bone))
                    {
                        forward = muzzle.forward;
                        break;
                    }
                }
            }
            cannonAimAxes[i] = bone.InverseTransformDirection(forward);
        }
        gunsReady = bulletPrefab != null && bulletPrefab.TryGetComponent<BulletController>(out _)
            && bulletPrefab.TryGetComponent<Rigidbody>(out _) && firePoints != null && firePoints.Length > 0;
        if (!gunsReady)
            Debug.LogWarning("ISOMechAI: assign fire points and a projectile with BulletController and Rigidbody.", this);
        if (animator == null || animator.runtimeAnimatorController == null)
            Debug.LogWarning("ISOMechAI: assign the generated Idle/Walk/Death Animator Controller.", this);
    }

    private void Start()
    {
        SetPlayer(player);
        ScheduleMelee();
    }

    public void SetPlayer(Transform target)
    {
        player = target;
        targetCollider = target != null ? target.GetComponentInChildren<Collider>() : null;
        targetHealth = target != null ? target.GetComponentInParent<Health>() : null;
    }

    private void Update()
    {
        // Remove last frame's additive pose before Animator evaluates the next one.
        RestoreCannonPose();
        wantsShot = false;
        if (dead || health.GetHealth() <= 0)
            return;

        if (Time.time < stunnedUntil || (landing != null && !landing.IsActivated) || !AgentReady)
        {
            CancelAttack();
            StopMoving();
            PlayMovement(false);
            return;
        }

        if (player == null && Time.time >= nextSearch)
        {
            nextSearch = Time.time + 0.5f;
            GameObject target = GameObject.FindGameObjectWithTag("Player");
            if (target != null)
                SetPlayer(target.transform);
        }

        if (player == null || (targetHealth != null && targetHealth.GetHealth() <= 0)
            || Vector3.Distance(transform.position, player.position) > detectionRange)
        {
            CancelAttack();
            StopMoving();
            PlayMovement(false);
            return;
        }

        if (!spotted)
        {
            spotted = true;
            SendMessage("EnemySpottedPlayer", SendMessageOptions.DontRequireReceiver);
        }

        Quaternion previousRotation = transform.rotation;
        Vector3 lookPosition = player.position;
        if (!swinging && !agent.isStopped && agent.hasPath && !agent.pathPending)
            lookPosition = agent.steeringTarget;
        Vector3 direction = Vector3.ProjectOnPlane(lookPosition - transform.position, Vector3.up);
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
        bool turning = Quaternion.Angle(previousRotation, transform.rotation)
            > turnAnimationThreshold * Time.deltaTime;

        if (swinging)
        {
            StopMoving();
            PlayMovement(false, turning);
            float elapsed = Time.time - swingStarted;
            if (!hitApplied && elapsed >= windup)
            {
                hitApplied = true;
                if (TargetInMeleeRange() && FacingTarget() && HasSight(AimOrigin()))
                    targetHealth?.Change(-meleeDamage);
            }
            if (elapsed >= windup + recovery)
            {
                swinging = false;
                ScheduleMelee();
            }
            return;
        }

        if (!approaching && Time.time >= nextMelee)
        {
            approaching = true;
            approachEnds = Time.time + approachTimeout;
        }
        if (approaching && Time.time >= approachEnds)
        {
            approaching = false;
            ScheduleMelee();
        }

        if (approaching && TargetInMeleeRange() && FacingTarget() && HasSight(AimOrigin()))
        {
            approaching = false;
            swinging = true;
            hitApplied = false;
            swingStarted = Time.time;
            StopMoving();
            PlayMovement(false, turning);
            SendMessage("EnemyAttackStarted", SendMessageOptions.DontRequireReceiver);
            return;
        }

        float stopDistance = approaching ? meleeRange * 0.65f : shootingDistance;
        float distance = Vector3.Distance(transform.position, player.position);
        bool hasSight = HasSight(AimOrigin());
        if (distance > stopDistance || !hasSight)
        {
            agent.isStopped = false;
            // Combat distance only applies with a clear shot. Behind a wall it can
            // otherwise stop the agent before it reaches the corner of its route.
            agent.stoppingDistance = hasSight ? stopDistance : 0.1f;
            if (Time.time >= nextPathUpdate && !agent.pathPending)
            {
                nextPathUpdate = Time.time + 0.2f;
                agent.SetDestination(player.position);
            }
        }
        else
            StopMoving();

        wantsShot = !approaching && distance <= shootingDistance + 2f && FacingTarget();
        PlayMovement(agent.velocity.sqrMagnitude > 0.01f, turning);
    }

    private Vector3 AimPoint() => targetCollider != null
        ? targetCollider.bounds.center : player.position + Vector3.up;

    private Vector3 AimOrigin() => transform.position + Vector3.up * Mathf.Max(0.5f, agent.height * 0.6f);

    private bool FacingTarget()
    {
        Vector3 direction = Vector3.ProjectOnPlane(AimPoint() - transform.position, Vector3.up);
        return direction.sqrMagnitude < 0.001f || Vector3.Angle(transform.forward, direction) < 35f;
    }

    private bool TargetInMeleeRange()
    {
        // Collider distance accounts for a CharacterController whose pivot is at the feet.
        Vector3 point = targetCollider != null ? targetCollider.ClosestPoint(AimOrigin()) : AimPoint();
        return Vector3.Distance(AimOrigin(), point) <= meleeRange;
    }

    private bool HasSight(Vector3 origin)
    {
        Vector3 ray = AimPoint() - origin;
        int count = Physics.RaycastNonAlloc(origin, ray.normalized, sightHits, ray.magnitude,
            sightMask, QueryTriggerInteraction.Ignore);
        if (count == sightHits.Length)
            return false;
        float nearest = float.PositiveInfinity;
        Collider blocker = null;
        for (int i = 0; i < count; i++)
        {
            Collider candidate = sightHits[i].collider;
            if (candidate.transform.IsChildOf(transform) || sightHits[i].distance >= nearest)
                continue;
            nearest = sightHits[i].distance;
            blocker = candidate;
        }
        return blocker == null || blocker.transform.IsChildOf(player)
            || (targetHealth != null && blocker.GetComponentInParent<Health>() == targetHealth);
    }

    private void TryShoot()
    {
        if (!gunsReady || player == null || Time.time < nextShot)
            return;
        Transform muzzle = firePoints[gunIndex % firePoints.Length];
        if (muzzle == null)
        {
            gunIndex = (gunIndex + 1) % firePoints.Length;
            return;
        }
        if (!HasSight(muzzle.position))
            return;
        Vector3 direction = (AimPoint() - muzzle.position).normalized;
        if (direction.sqrMagnitude < 0.001f || Vector3.Angle(muzzle.forward, direction) > 5f)
            return;
        nextShot = Time.time + shotInterval;
        gunIndex = (gunIndex + 1) % firePoints.Length;
        GameObject projectile = Instantiate(bulletPrefab, muzzle.position, Quaternion.LookRotation(direction));
        projectile.GetComponent<BulletController>().Initialize(shotDamage, gameObject);
        Rigidbody body = projectile.GetComponent<Rigidbody>();
        body.useGravity = false;
        body.linearVelocity = direction * bulletSpeed;
        recoil = recoilDistance;
        SendMessage("EnemyAttackStarted", SendMessageOptions.DontRequireReceiver);
    }

    private void LateUpdate()
    {
        if (dead || Time.time < stunnedUntil)
            return;
        bool aiming = player != null && !swinging && AgentReady
            && (landing == null || landing.IsActivated)
            && (targetHealth == null || targetHealth.GetHealth() > 0)
            && Vector3.Distance(transform.position, player.position) <= detectionRange;
        recoil = Mathf.MoveTowards(recoil, 0f, recoilDistance * 8f * Time.deltaTime);
        float swing = 0f;
        if (swinging)
        {
            float elapsed = Time.time - swingStarted;
            float phase = elapsed / windup;
            if (phase < 0.65f)
                swing = -Mathf.Sin(phase / 0.65f * Mathf.PI * 0.5f);
            else if (phase < 1f)
                swing = Mathf.Lerp(-1f, 1f, (phase - 0.65f) / 0.35f);
            else
                swing = Mathf.Cos(Mathf.Clamp01((elapsed - windup) / recovery) * Mathf.PI * 0.5f);
        }
        for (int i = 0; i < cannonBones.Length; i++)
        {
            Transform bone = cannonBones[i];
            if (bone == null || bone.parent == null)
                continue;
            cannonPositions[i] = bone.localPosition;
            cannonRotations[i] = bone.localRotation;
            Quaternion aimOffset = Quaternion.identity;
            if (aiming)
            {
                Vector3 direction = AimPoint() - bone.position;
                if (direction.sqrMagnitude > 0.001f)
                {
                    direction = Vector3.RotateTowards(transform.forward, direction.normalized,
                        maxCannonAimAngle * Mathf.Deg2Rad, 0f);
                    Vector3 localDirection = bone.parent.InverseTransformDirection(direction);
                    aimOffset = Quaternion.FromToRotation(bone.localRotation * cannonAimAxes[i], localDirection);
                }
            }
            cannonAimOffsets[i] = Quaternion.RotateTowards(cannonAimOffsets[i], aimOffset,
                cannonAimSpeed * Time.deltaTime);
            bone.localRotation = cannonAimOffsets[i] * bone.localRotation;
            bone.localPosition += bone.parent.InverseTransformVector(-transform.forward * recoil);
            Vector3 axis = bone.parent.InverseTransformDirection(transform.right);
            bone.localRotation = Quaternion.AngleAxis(swing * cannonSwingAngle, axis) * bone.localRotation;
        }
        poseApplied = true;
        // Fire after Animator and procedural aiming have placed the barrel for this frame.
        if (wantsShot)
            TryShoot();
    }

    private void RestoreCannonPose()
    {
        if (!poseApplied)
            return;
        for (int i = 0; i < cannonBones.Length; i++)
        {
            if (cannonBones[i] == null)
                continue;
            cannonBones[i].localPosition = cannonPositions[i];
            cannonBones[i].localRotation = cannonRotations[i];
        }
        poseApplied = false;
    }

    public void PlayerMeleeHit(MeleeHitMessage message)
    {
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + message.stunDuration);
        CancelAttack();
        StopMoving();
    }

    public void HealthChanged(HealthChangedMessage message)
    {
        if (message.health > 0 || dead)
            return;
        dead = true;
        CancelAttack();
        RestoreCannonPose();
        StopMoving();
        if (animator != null && animator.HasState(0, DeathState))
            animator.CrossFadeInFixedTime(DeathState, 0.1f);
        agent.enabled = false;
    }

    private void ScheduleMelee() => nextMelee = Time.time + Random.Range(meleeInterval.x, meleeInterval.y);

    private void CancelAttack()
    {
        if (approaching || swinging)
            ScheduleMelee();
        approaching = false;
        swinging = false;
        recoil = 0f;
    }

    private void StopMoving()
    {
        if (AgentReady)
            agent.isStopped = true;
    }

    private void PlayMovement(bool moving, bool turning = false)
    {
        // Reuse the leg cycle so existing controllers also animate turns in place.
        int state = moving || turning ? WalkState : IdleState;
        if (animationState == state || animator == null || !animator.HasState(0, state))
            return;
        animator.CrossFadeInFixedTime(state, 0.15f);
        animationState = state;
    }

    private void OnDisable()
    {
        RestoreCannonPose();
        CancelAttack();
        StopMoving();
    }

    private void OnValidate()
    {
        meleeInterval.x = Mathf.Max(1f, meleeInterval.x);
        meleeInterval.y = Mathf.Max(meleeInterval.x, meleeInterval.y);
        shootingDistance = Mathf.Max(meleeRange + 0.5f, shootingDistance);
    }
}
