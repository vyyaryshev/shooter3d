using UnityEngine;
using UnityEngine.InputSystem;

public class Shoot : MonoBehaviour
{
    [SerializeField] private Transform shootPoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 30f;
    [SerializeField] private int damage = 100;
    [SerializeField] private int shootDelay = 10;

    private int delayCounter = 0;

    void Update()
    {
        if (delayCounter > 0)
            delayCounter--;

        if (Mouse.current.leftButton.isPressed && delayCounter <= 0)
        {
            delayCounter = shootDelay;

            if (shootPoint == null)
            {
                Debug.LogWarning("Shoot: не назначен shootPoint");
                return;
            }

            if (bulletPrefab == null)
            {
                Debug.LogWarning("Shoot: не назначен BulletPrefab");
                return;
            }

            GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);

            if (bullet.TryGetComponent(out BulletController bulletController))
            {
                bulletController.SetDamage(damage);
            }

            if (bullet.TryGetComponent(out Rigidbody bulletRigidbody))
            {
                bulletRigidbody.linearVelocity = shootPoint.forward * bulletSpeed;
            }
        }
    }
}
