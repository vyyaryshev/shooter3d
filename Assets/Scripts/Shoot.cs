using UnityEngine;
using UnityEngine.InputSystem;

public class Shoot : MonoBehaviour
{
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float distance = 100f;
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

            Ray ray = new Ray(shootPoint.position, shootPoint.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, distance))
            {
                Debug.Log("Попал в: " + hit.collider.name);

                // ВАЖНО: ищем не только на объекте, но и у родителя
                EnemyHealth hp = hit.collider.GetComponentInParent<EnemyHealth>();

                if (hp != null)
                {
                    hp.TakeDamage(damage); // <-- будет работать, если метод есть
                }
                else
                {
                    Debug.Log("У объекта нет EnemyHealth");
                }
            }
            else
            {
                Debug.Log("Мимо");
            }
        }
    }
}