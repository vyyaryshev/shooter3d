using UnityEngine;

public class TrapShooter : MonoBehaviour
{
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float shootInterval = 2f;

    void Start()
    {
        InvokeRepeating(nameof(Shoot), 1f, shootInterval);
    }

    void Shoot()
    {
        Instantiate(fireballPrefab, shootPoint.position, shootPoint.rotation);
    }
}