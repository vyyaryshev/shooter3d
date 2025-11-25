

using UnityEngine;

public class BulletController : MonoBehaviour
{
    [SerializeField] private int damage;

    void Start()
    {
        Destroy(gameObject, 3);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HealthScript health = collision.gameObject.GetComponent<HealthScript>();
        if (health)
        {
            health.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}