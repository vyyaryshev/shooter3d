using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class EnemySeparation : MonoBehaviour
{
    [SerializeField] private LayerMask enemyMask = ~0;
    [SerializeField] private float separationRadius = 0.8f;
    [SerializeField] private float separationStrength = 2.5f;
    [SerializeField] private float maxCorrectionSpeed = 2f;
    [SerializeField] private bool autoApply = true;

    private readonly Collider[] nearbyColliders = new Collider[24];

    private Collider[] ownColliders;
    private NavMeshAgent navMeshAgent;

    private void Awake()
    {
        ownColliders = GetComponentsInChildren<Collider>();
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void LateUpdate()
    {
        if (!autoApply)
            return;

        Vector3 offset = GetSeparationOffset(transform.up, Time.deltaTime);
        ApplyOffset(offset);
    }

    public void SetAutoApply(bool value)
    {
        autoApply = value;
    }

    public Vector3 GetSeparationOffset(Vector3 surfaceNormal, float deltaTime)
    {
        Vector3 direction = GetSeparationDirection(surfaceNormal);
        if (direction.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        float maxStep = maxCorrectionSpeed * Mathf.Max(0f, deltaTime);
        return direction * Mathf.Min(separationStrength * Mathf.Max(0f, deltaTime), maxStep);
    }

    public Vector3 GetSeparationDirection(Vector3 surfaceNormal)
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, separationRadius, nearbyColliders, enemyMask, QueryTriggerInteraction.Ignore);
        Vector3 separation = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            Collider other = nearbyColliders[i];
            if (other == null || IsOwnCollider(other) || !IsEnemyCollider(other))
                continue;

            Vector3 closestPoint = other.ClosestPoint(transform.position);
            Vector3 away = transform.position - closestPoint;

            if (away.sqrMagnitude < 0.0001f)
                away = transform.position - other.transform.position;

            away = Vector3.ProjectOnPlane(away, surfaceNormal);
            float distance = Mathf.Max(0.01f, away.magnitude);
            float weight = Mathf.Clamp01((separationRadius - distance) / separationRadius);
            separation += away.normalized * weight;
        }

        return separation.sqrMagnitude > 0.0001f ? separation.normalized : Vector3.zero;
    }

    private void ApplyOffset(Vector3 offset)
    {
        if (offset.sqrMagnitude < 0.000001f)
            return;

        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.Move(offset);
            return;
        }

        transform.position += offset;
    }

    private bool IsOwnCollider(Collider other)
    {
        if (ownColliders == null)
            return false;

        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] == other)
                return true;
        }

        return other.transform.root == transform.root;
    }

    private bool IsEnemyCollider(Collider other)
    {
        return other.GetComponentInParent<Health>() != null
            || other.GetComponentInParent<EnemyController>() != null
            || other.GetComponentInParent<MutantAI>() != null
            || other.GetComponentInParent<SoldierRangedAI>() != null
            || other.GetComponentInParent<SpiderWallClimber>() != null;
    }

    private void OnValidate()
    {
        separationRadius = Mathf.Max(0.01f, separationRadius);
        separationStrength = Mathf.Max(0f, separationStrength);
        maxCorrectionSpeed = Mathf.Max(0f, maxCorrectionSpeed);
    }
}
