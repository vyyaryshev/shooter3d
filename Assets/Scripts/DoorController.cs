
using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] float speed;
    private float timeToOpen = 1;
    private float openingTime;

    public IEnumerator OpenDoorCorutine()
    {
        while (openingTime < timeToOpen)
        {
            openingTime += Time.deltaTime;
            transform.Translate(Vector3.down * speed * Time.deltaTime);
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }
}