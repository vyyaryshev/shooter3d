using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class MechRocket : MonoBehaviour
{
    [Header("Launch and flight")]
    [SerializeField, Min(0.05f)] private float ascentDuration = 0.7f;
    [SerializeField, Min(0.1f)] private float ascentSpeed = 12f;
    [SerializeField, Min(0.1f)] private float flightSpeed = 25f;
    [SerializeField, Min(0.1f)] private float lifetime = 8f;
    [Tooltip("Nose direction in root local space. Zero detects the longest mesh axis. Reverse the axis if the model flies tail-first.")]
    [SerializeField] private Vector3 modelForwardAxis = Vector3.zero;
    [Header("Impact")]
    [SerializeField, Min(1)] private int damage = 35;
    [SerializeField, Min(0.1f)] private float explosionRadius = 3f;
    [SerializeField, Min(0f)] private float explosionForce = 250f;
    [SerializeField] private LayerMask explosionMask = ~0;
    [SerializeField] private LayerMask explosionOcclusionMask = ~0;
    [SerializeField] private GameObject impactEffect;

    private Rigidbody body;
    private Transform target;
    private Collider targetCollider;
    private Transform owner;
    private Vector3 lockedDirection;
    private float elapsed;
    private bool initialized;
    private bool directionLocked;
    private bool hit;
    private Quaternion modelRotationOffset;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        Vector3 axis = modelForwardAxis.sqrMagnitude > 0.001f ? modelForwardAxis.normalized : DetectModelAxis();
        modelRotationOffset = Quaternion.FromToRotation(axis, Vector3.forward);
    }

    private Vector3 DetectModelAxis()
    {
        Vector3 axis = Vector3.forward;
        float longest = 0f;
        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null)
                continue;
            Vector3 size = filter.sharedMesh.bounds.size;
            for (int i = 0; i < 3; i++)
            {
                Vector3 extent = Vector3.zero;
                extent[i] = size[i];
                extent = transform.InverseTransformVector(filter.transform.TransformVector(extent));
                if (extent.sqrMagnitude <= longest)
                    continue;
                longest = extent.sqrMagnitude;
                axis = extent.normalized;
            }
        }
        return axis;
    }

    private Quaternion FlightRotation(Vector3 direction)
    {
        Vector3 up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
        return Quaternion.LookRotation(direction, up) * modelRotationOffset;
    }

    public void Initialize(Transform newTarget, GameObject launcher)
    {
        target = newTarget;
        targetCollider = target != null ? target.GetComponentInChildren<Collider>() : null;
        owner = launcher != null ? launcher.transform : null;
        if (launcher != null)
        {
            Collider[] rocketColliders = GetComponentsInChildren<Collider>();
            foreach (Collider own in launcher.GetComponentsInChildren<Collider>())
                foreach (Collider rocket in rocketColliders)
                    Physics.IgnoreCollision(rocket, own);
        }
        elapsed = 0f;
        directionLocked = false;
        initialized = true;
        body.rotation = FlightRotation(Vector3.up);
        body.linearVelocity = Vector3.up * ascentSpeed;
        Destroy(gameObject, lifetime);
    }

    private void Start()
    {
        if (initialized)
            return;
        Debug.LogWarning("Call MechRocket.Initialize when spawning a rocket.", this);
        Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (!initialized || hit)
            return;
        elapsed += Time.fixedDeltaTime;
        if (elapsed < ascentDuration)
        {
            body.rotation = FlightRotation(Vector3.up);
            body.linearVelocity = Vector3.up * ascentSpeed;
            return;
        }

        if (!directionLocked)
        {
            // Sample the player's position exactly once at the end of the ascent.
            // After this point the rocket never reads or follows the target again.
            Vector3 aimPoint = targetCollider != null ? targetCollider.bounds.center
                : target != null ? target.position + Vector3.up : body.position + Vector3.up;
            lockedDirection = (aimPoint - body.position).normalized;
            if (lockedDirection.sqrMagnitude < 0.001f)
                lockedDirection = Vector3.up;
            directionLocked = true;
            target = null;
            targetCollider = null;
        }
        body.rotation = FlightRotation(lockedDirection);
        body.linearVelocity = lockedDirection * flightSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        Vector3 normal = collision.contactCount > 0 ? collision.GetContact(0).normal : Vector3.up;
        Impact(collision.collider, point + normal * 0.05f);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Environment trigger volumes (arenas, spawners) are not solid obstacles.
        if (!other.isTrigger)
            Impact(other, transform.position);
    }

    private void Impact(Collider other, Vector3 point)
    {
        if (hit || !initialized || (owner != null && other.transform.IsChildOf(owner)))
            return;
        hit = true;
        HashSet<Health> damaged = new HashSet<Health>();
        HashSet<Rigidbody> pushed = new HashSet<Rigidbody>();
        foreach (Collider nearby in Physics.OverlapSphere(point, explosionRadius, explosionMask, QueryTriggerInteraction.Ignore))
        {
            if (nearby.transform.IsChildOf(transform) || (owner != null && nearby.transform.IsChildOf(owner)))
                continue;
            Vector3 closest = nearby.ClosestPoint(point);
            Health victim = nearby.GetComponentInParent<Health>();
            if (BlastBlocked(point, closest, nearby, victim))
                continue;
            if (victim != null && damaged.Add(victim))
                victim.Change(-damage);
            Rigidbody affected = nearby.attachedRigidbody;
            if (affected != null && !affected.isKinematic && pushed.Add(affected))
                affected.AddExplosionForce(explosionForce, point, explosionRadius, 0.2f, ForceMode.Impulse);
        }
        if (impactEffect != null)
        {
            GameObject effect = Instantiate(impactEffect, point, Quaternion.identity);
            Destroy(effect, 5f);
        }
        else
            RocketExplosionEffect.Spawn(point, explosionRadius);
        Destroy(gameObject);
    }

    private bool BlastBlocked(Vector3 origin, Vector3 closest, Collider targetCollider, Health victim)
    {
        Vector3 direction = closest - origin;
        if (direction.sqrMagnitude < 0.0001f)
            return false;
        foreach (RaycastHit obstruction in Physics.RaycastAll(origin, direction.normalized,
            direction.magnitude, explosionOcclusionMask, QueryTriggerInteraction.Ignore))
        {
            if (obstruction.collider == targetCollider || obstruction.transform.IsChildOf(transform))
                continue;
            if (victim != null && obstruction.collider.GetComponentInParent<Health>() == victim)
                continue;
            return true;
        }
        return false;
    }
}
