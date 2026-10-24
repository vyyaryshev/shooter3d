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
    [Header("Impact")]
    [SerializeField, Min(1)] private int damage = 35;
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

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.constraints = RigidbodyConstraints.FreezeRotation;
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
        body.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
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
            Vector3 up = Mathf.Abs(Vector3.Dot(lockedDirection, Vector3.up)) > 0.99f
                ? Vector3.forward : Vector3.up;
            body.MoveRotation(Quaternion.LookRotation(lockedDirection, up));
        }
        body.linearVelocity = lockedDirection * flightSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        Impact(collision.collider, point);
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
        Health victim = other.GetComponentInParent<Health>();
        if (victim != null)
            victim.Change(-damage);
        if (impactEffect != null)
        {
            GameObject effect = Instantiate(impactEffect, point, Quaternion.identity);
            Destroy(effect, 5f);
        }
        Destroy(gameObject);
    }
}
