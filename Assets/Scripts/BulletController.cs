

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
        Health health = collision.gameObject.GetComponent<Health>();
        if (health)
        {
            health.Change(-damage);
        }
        Destroy(gameObject);
    }
}