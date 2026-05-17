using UnityEngine;

public class BulletController : MonoBehaviour
{
    [SerializeField] private int damage;
    [SerializeField] private float lifeTime = 3f;

    private Transform ownerRoot;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    public void Initialize(int newDamage, GameObject owner)
    {
        damage = newDamage;
        ownerRoot = owner != null ? owner.transform.root : null;

        if (owner == null)
            return;

        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
        Collider[] bulletColliders = GetComponentsInChildren<Collider>();

        foreach (Collider bulletCollider in bulletColliders)
        {
            foreach (Collider ownerCollider in ownerColliders)
            {
                Physics.IgnoreCollision(bulletCollider, ownerCollider);
            }
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

        Debug.Log("Попал по " + target.name);

        Health health = target.GetComponentInParent<Health>();
        if (health)
        {
            health.Change(-damage);
        }

        Destroy(gameObject);
    }
}
