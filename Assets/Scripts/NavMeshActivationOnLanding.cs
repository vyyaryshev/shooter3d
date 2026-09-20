using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class NavMeshActivationOnLanding : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Rigidbody enemyRigidbody;

    [Header("Landing")]
    [SerializeField] private float sampleRadius = 1.5f;
    [SerializeField] private float maxSnapDistance = 0.4f;
    [SerializeField] private float maxLandingVerticalSpeed = 0.2f;
    [SerializeField] private bool startFallingOnEnable;
    [SerializeField] private bool makeRigidbodyKinematicAfterLanding = true;

    [Header("Support Check")]
    [SerializeField] private bool fallWhenSupportIsLost = true;
    [SerializeField] private float groundCheckHeight = 0.25f;
    [SerializeField] private float groundCheckDistance = 1.5f;
    [SerializeField] private float maxStableSupportAngle = 45f;
    [SerializeField] private LayerMask supportMask = ~0;

    [Header("AI During Fall")]
    [SerializeField] private bool disableAiUntilLanding = true;
    [SerializeField] private MonoBehaviour[] behavioursToEnableAfterLanding;
    [SerializeField] private string[] autoDisabledBehaviourNames =
    {
        "MutantAI",
        "SoldierRangedAI",
        "EnemyShoot"
    };

    private bool isActivated;
    private bool isFalling;
    private bool cachedAgentSettings;
    private bool originalUpdatePosition;
    private bool originalUpdateRotation;
    private MonoBehaviour[] autoDisabledBehaviours;

    public bool IsActivated => isActivated;
    public bool IsFalling => isFalling;

    private void Awake()
    {
        ResolveReferences();
        CacheAgentSettings();
        PrepareSuspended();

        if (startFallingOnEnable)
            BeginLanding();
    }

    private void OnEnable()
    {
        if (isActivated)
            return;

        PrepareSuspended();

        if (startFallingOnEnable)
            BeginLanding();
    }

    private void Start()
    {
        WarnIfNoSolidCollider();
    }

    public void BeginLanding()
    {
        if (isFalling)
            return;

        isActivated = false;
        isFalling = true;
        ResolveReferences();

        if (disableAiUntilLanding)
            DisableAiBehaviours();

        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.enabled = false;
        }

        if (enemyRigidbody != null)
        {
            enemyRigidbody.useGravity = true;
            enemyRigidbody.isKinematic = false;
            enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            enemyRigidbody.WakeUp();
        }
    }

    public void BeginFall()
    {
        BeginLanding();
    }

    public void StartFalling()
    {
        BeginLanding();
    }

    private void PrepareSuspended()
    {
        ResolveReferences();

        if (disableAiUntilLanding)
            DisableAiBehaviours();

        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.enabled = false;
        }

        if (enemyRigidbody != null)
        {
            enemyRigidbody.linearVelocity = Vector3.zero;
            enemyRigidbody.angularVelocity = Vector3.zero;
            enemyRigidbody.useGravity = false;
            enemyRigidbody.isKinematic = true;
            enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    private void Update()
    {
        if (!isFalling)
            CheckSupportBeforeFall();

        if (isActivated || !isFalling || agent == null)
            return;

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            return;

        if (hit.distance > maxSnapDistance)
            return;

        if (enemyRigidbody != null && Mathf.Abs(enemyRigidbody.linearVelocity.y) > maxLandingVerticalSpeed)
            return;

        ActivateAgent(hit.position);
    }

    private void CheckSupportBeforeFall()
    {
        if (!fallWhenSupportIsLost)
            return;

        Vector3 origin = transform.position + Vector3.up * groundCheckHeight;
        float rayDistance = groundCheckHeight + groundCheckDistance;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance, supportMask, QueryTriggerInteraction.Ignore))
        {
            BeginLanding();
            return;
        }

        float supportAngle = Vector3.Angle(hit.normal, Vector3.up);
        if (supportAngle > maxStableSupportAngle)
            BeginLanding();
    }

    private void ActivateAgent(Vector3 navMeshPosition)
    {
        isActivated = true;
        isFalling = false;

        if (enemyRigidbody != null)
        {
            enemyRigidbody.linearVelocity = Vector3.zero;
            enemyRigidbody.angularVelocity = Vector3.zero;

            if (makeRigidbodyKinematicAfterLanding)
            {
                enemyRigidbody.useGravity = false;
                enemyRigidbody.isKinematic = true;
            }
        }

        transform.position = navMeshPosition;
        agent.enabled = true;
        agent.updatePosition = originalUpdatePosition;
        agent.updateRotation = originalUpdateRotation;
        agent.Warp(navMeshPosition);
        agent.isStopped = false;

        EnableAiBehaviours();
        Debug.Log(gameObject.name + " activated NavMeshAgent after landing");
    }

    private void DisableAiBehaviours()
    {
        if (behavioursToEnableAfterLanding != null && behavioursToEnableAfterLanding.Length > 0)
        {
            for (int i = 0; i < behavioursToEnableAfterLanding.Length; i++)
            {
                if (behavioursToEnableAfterLanding[i] != null)
                    behavioursToEnableAfterLanding[i].enabled = false;
            }

            return;
        }

        if (autoDisabledBehaviours != null)
        {
            for (int i = 0; i < autoDisabledBehaviours.Length; i++)
            {
                if (autoDisabledBehaviours[i] != null)
                    autoDisabledBehaviours[i].enabled = false;
            }

            return;
        }

        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        autoDisabledBehaviours = new MonoBehaviour[behaviours.Length];
        int count = 0;

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null || behaviours[i] == this || !ShouldAutoDisable(behaviours[i]))
                continue;

            behaviours[i].enabled = false;
            autoDisabledBehaviours[count] = behaviours[i];
            count++;
        }
    }

    private void EnableAiBehaviours()
    {
        if (!disableAiUntilLanding)
            return;

        if (behavioursToEnableAfterLanding != null && behavioursToEnableAfterLanding.Length > 0)
        {
            for (int i = 0; i < behavioursToEnableAfterLanding.Length; i++)
            {
                if (behavioursToEnableAfterLanding[i] != null)
                    behavioursToEnableAfterLanding[i].enabled = true;
            }

            return;
        }

        if (autoDisabledBehaviours == null)
            return;

        for (int i = 0; i < autoDisabledBehaviours.Length; i++)
        {
            if (autoDisabledBehaviours[i] != null)
                autoDisabledBehaviours[i].enabled = true;
        }
    }

    private bool ShouldAutoDisable(MonoBehaviour behaviour)
    {
        if (autoDisabledBehaviourNames == null)
            return false;

        string typeName = behaviour.GetType().Name;
        for (int i = 0; i < autoDisabledBehaviourNames.Length; i++)
        {
            if (autoDisabledBehaviourNames[i] == typeName)
                return true;
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (enemyRigidbody == null)
            enemyRigidbody = GetComponent<Rigidbody>();
    }

    private void CacheAgentSettings()
    {
        if (cachedAgentSettings || agent == null)
            return;

        originalUpdatePosition = agent.updatePosition;
        originalUpdateRotation = agent.updateRotation;
        cachedAgentSettings = true;
    }

    private void WarnIfNoSolidCollider()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].enabled && !colliders[i].isTrigger)
                return;
        }

        Debug.LogWarning(gameObject.name + " has NavMeshActivationOnLanding, but no enabled non-trigger Collider. It cannot physically land.");
    }

    private void OnValidate()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (enemyRigidbody == null)
            enemyRigidbody = GetComponent<Rigidbody>();

        sampleRadius = Mathf.Max(0.1f, sampleRadius);
        maxSnapDistance = Mathf.Max(0.01f, maxSnapDistance);
        maxLandingVerticalSpeed = Mathf.Max(0f, maxLandingVerticalSpeed);
    }
}

