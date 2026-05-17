using UnityEngine;

public class BulletController : MonoBehaviour
{
    [SerializeField] private int damage;
    [SerializeField] private float lifeTime = 3f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void SetDamage(int newDamage)
    {
        damage = newDamage;
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
        Debug.Log("Попал по " + target.name);

        OldHealth health = target.GetComponentInParent<OldHealth>();
        if (health)
        {
            health.Change(-damage);
        }

        EnemyHealth enemyHealth = target.GetComponentInParent<EnemyHealth>();
        if (enemyHealth)
        {
            enemyHealth.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
