using UnityEngine;

[DisallowMultipleComponent]
public class RoboDroneProjectile : MonoBehaviour
{
    [SerializeField] private int damage = 15;
    [SerializeField] private float speed = 18f;
    [SerializeField] private float lifeTime = 5f;

    private Transform ownerRoot;
    private Rigidbody projectileRigidbody;
    private bool usesRigidbody;
    private bool applyDamage = true;

    private void Awake()
    {
        projectileRigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (!usesRigidbody)
            transform.position += transform.forward * speed * Time.deltaTime;
    }

    public void Initialize(int newDamage, GameObject owner, float newSpeed, float newLifeTime, bool shouldApplyDamage = true)
    {
        damage = newDamage;
        speed = newSpeed;
        lifeTime = newLifeTime;
        applyDamage = shouldApplyDamage;
        ownerRoot = owner != null ? owner.transform.root : null;

        IgnoreOwnerColliders(owner);

        projectileRigidbody = GetComponent<Rigidbody>();
        usesRigidbody = projectileRigidbody != null && !projectileRigidbody.isKinematic;

        if (usesRigidbody)
            projectileRigidbody.linearVelocity = transform.forward * speed;
    }

    private void IgnoreOwnerColliders(GameObject owner)
    {
        if (owner == null)
            return;

        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
        Collider[] projectileColliders = GetComponentsInChildren<Collider>();

        for (int i = 0; i < projectileColliders.Length; i++)
        {
            for (int ownerIndex = 0; ownerIndex < ownerColliders.Length; ownerIndex++)
                Physics.IgnoreCollision(projectileColliders[i], ownerColliders[ownerIndex]);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject target)
    {
        if (ownerRoot != null && target.transform.root == ownerRoot)
            return;

        Health health = applyDamage ? target.GetComponentInParent<Health>() : null;
        if (health != null)
            health.Change(-damage);

        Destroy(gameObject);
    }
}
