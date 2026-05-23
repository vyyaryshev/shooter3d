using UnityEngine;

public class ShellEjector : MonoBehaviour
{
    [SerializeField] private Transform ejectionPoint;
    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private float shellLifetime = 6f;
    [SerializeField] private Vector3 localEjectionForce = new Vector3(1.5f, 1.2f, -0.2f);
    [SerializeField] private Vector3 localTorque = new Vector3(25f, 8f, 15f);

    public void Eject()
    {
        if (shellPrefab == null || ejectionPoint == null)
            return;

        GameObject shell = FpsObjectPool.Spawn(shellPrefab, ejectionPoint.position, ejectionPoint.rotation, shellLifetime);
        if (shell == null)
            return;

        if (shell.TryGetComponent(out Rigidbody shellRigidbody))
        {
            shellRigidbody.linearVelocity = Vector3.zero;
            shellRigidbody.angularVelocity = Vector3.zero;
            shellRigidbody.AddForce(ejectionPoint.TransformDirection(localEjectionForce), ForceMode.Impulse);
            shellRigidbody.AddTorque(ejectionPoint.TransformDirection(localTorque), ForceMode.Impulse);
        }
    }
}
