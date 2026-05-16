using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshActivationOnLanding : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Rigidbody enemyRigidbody;
    [SerializeField] private float sampleRadius = 1.5f;
    [SerializeField] private float maxSnapDistance = 0.4f;
    [SerializeField] private float maxLandingVerticalSpeed = 0.2f;
    [SerializeField] private bool makeRigidbodyKinematicAfterLanding = true;

    private bool isActivated;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (enemyRigidbody == null)
            enemyRigidbody = GetComponent<Rigidbody>();

        if (agent != null)
            agent.enabled = false;

        if (enemyRigidbody != null)
        {
            enemyRigidbody.useGravity = true;
            enemyRigidbody.isKinematic = false;
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

        Debug.Log(gameObject.name + " активировал NavMeshAgent");
    }
}
