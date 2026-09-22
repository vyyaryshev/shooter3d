using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerDeactivateAfterDelay : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private GameObject objectToDeactivate;
    [SerializeField] private float delay = 3f;
    [SerializeField] private bool triggerOnce = true;

    private Coroutine timerRoutine;
    private bool triggered;

    private void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;

        if (objectToDeactivate == null)
            objectToDeactivate = gameObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && triggered)
            return;

        if (!other.CompareTag(playerTag))
            return;

        triggered = true;

        if (timerRoutine != null)
            StopCoroutine(timerRoutine);

        timerRoutine = StartCoroutine(DeactivateAfterDelay());
    }

    private IEnumerator DeactivateAfterDelay()
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (objectToDeactivate != null)
            objectToDeactivate.SetActive(false);
    }

    private void OnValidate()
    {
        delay = Mathf.Max(0f, delay);
    }
}
