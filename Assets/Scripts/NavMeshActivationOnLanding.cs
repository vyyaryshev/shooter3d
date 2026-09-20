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
    [SerializeField] private bool makeRigidbodyKinematicAfterLanding = true;

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
    private MonoBehaviour[] autoDisabledBehaviours;

    public bool IsActivated => isActivated;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (enemyRigidbody == null)
            enemyRigidbody = GetComponent<Rigidbody>();

        if (disableAiUntilLanding)
            DisableAiBehaviours();

        if (agent != null)
            agent.enabled = false;

        if (enemyRigidbody != null)
        {
            enemyRigidbody.useGravity = true;
            enemyRigidbody.isKinematic = false;
            enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    private void Update()
    {
        if (isActivated || agent == null)
            return;

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            return;

        if (hit.distance > maxSnapDistance)
            return;

        if (enemyRigidbody != null && Mathf.Abs(enemyRigidbody.linearVelocity.y) > maxLandingVerticalSpeed)
            return;

        ActivateAgent(hit.position);
    }

    private void ActivateAgent(Vector3 navMeshPosition)
    {
        isActivated = true;

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
        agent.Warp(navMeshPosition);
        agent.isStopped = false;

        EnableAiBehaviours();
        Debug.Log(gameObject.name + " активировал NavMeshAgent");
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
