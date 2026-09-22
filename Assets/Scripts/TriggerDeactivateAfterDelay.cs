using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerDeactivateAfterDelay : MonoBehaviour
{
    [SerializeField] private Collider triggerCollider;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float delay = 3f;
    [SerializeField] private bool triggerOnce = true;

    private Coroutine timerRoutine;
    private bool triggered;

    private void Awake()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            TriggerDeactivateAfterDelayRelay relay = triggerCollider.GetComponent<TriggerDeactivateAfterDelayRelay>();
            if (relay == null)
                relay = triggerCollider.gameObject.AddComponent<TriggerDeactivateAfterDelayRelay>();

            relay.Initialize(this);
        }
    }

    public void TriggerEntered(Collider other)
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

        gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;

        delay = Mathf.Max(0f, delay);
    }
}

[DisallowMultipleComponent]
public class TriggerDeactivateAfterDelayRelay : MonoBehaviour
{
    private TriggerDeactivateAfterDelay owner;

    public void Initialize(TriggerDeactivateAfterDelay target)
    {
        owner = target;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null)
            owner.TriggerEntered(other);
    }
}
