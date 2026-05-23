using UnityEngine;

public class FpsCameraShake : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float recoverySpeed = 12f;

    private Vector3 initialPosition;
    private float trauma;
    private float amplitude;

    private void Awake()
    {
        if (target == null)
            target = transform;

        initialPosition = target.localPosition;
    }

    private void LateUpdate()
    {
        trauma = Mathf.MoveTowards(trauma, 0f, recoverySpeed * Time.deltaTime);
        float shake = trauma * trauma * amplitude;
        Vector3 offset = Random.insideUnitSphere * shake;
        target.localPosition = initialPosition + offset;
    }

    public void Shake(float amount, float strength)
    {
        trauma = Mathf.Clamp01(trauma + amount);
        amplitude = Mathf.Max(amplitude, strength);
    }
}
