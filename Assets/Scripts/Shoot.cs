using UnityEngine;
using UnityEngine.InputSystem;

public class Shoot : MonoBehaviour
{
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform shootPoint;
    [SerializeField] float power = 5000.0f;
    [SerializeField] int shootDelay = 10;
    private int delayCounter = 0;

    void Update()
    {
        if(delayCounter > 0) { delayCounter--; }
        if (Mouse.current.leftButton.isPressed && delayCounter <= 0)
        {
            delayCounter = this.shootDelay;
            //Instantiate(bullet, shootPoint.position, bullet.transform.rotation);
            GameObject newBullet = Instantiate(bulletPrefab, shootPoint.position,
              bulletPrefab.transform.rotation);
            newBullet.GetComponent<Rigidbody>().AddForce(shootPoint.forward * power * Time.deltaTime, ForceMode.Impulse);
            print("Shoot : MonoBehaviour - void Update()");
        }
    }
}