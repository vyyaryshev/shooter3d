using UnityEngine;

public class save : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
   /*  







using UnityEngine;

public class BulletController : MonoBehaviour
{
    void Start()
    {
        Destroy(gameObject, 3);
    }
    private void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }
}




using UnityEngine;

public class Shoot : MonoBehaviour
{
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform shootPoint;
    [SerializeField] float power;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject newBullet = Instantiate(bulletPrefab, shootPoint.position,
                bulletPrefab.transform.rotation);
            newBullet.GetComponent<Rigidbody>().AddForce(shootPoint.forward
                                                         * power * Time.deltaTime, ForceMode.Impulse);
        }
    }
}



using UnityEngine;

public class Shoot : MonoBehaviour
{
    [SerializeField] GameObject bullet;
    [SerializeField] Transform shootPoint;
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Instantiate(bullet, shootPoint.position,
                bullet.transform.rotation);
        }
    }
                          */                              