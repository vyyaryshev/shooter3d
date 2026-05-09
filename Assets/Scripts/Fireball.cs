using UnityEngine;

public class Fireball : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private int damage = 25;
    [SerializeField] private float lifeTime = 5f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // »грок
        if (other.CompareTag("Player"))
        {
            Health playerHealth = other.GetComponent<Health>();

            if (playerHealth != null)
            {
                playerHealth.Change(-damage);
            }

            Destroy(gameObject);
            return;
        }

        // ¬раг
        if (other.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }

            Destroy(gameObject);
            return;
        }

        // ”ничтожение при попадании в стены и прочее
        Destroy(gameObject);
    }
}