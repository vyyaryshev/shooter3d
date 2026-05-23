using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform weaponRoot;
    [SerializeField] private Transform cameraRoot;

    [Header("Position")]
    [SerializeField] private Vector3 positionKick = new Vector3(0f, 0f, -0.08f);
    [SerializeField] private float positionReturnSpeed = 18f;

    [Header("Rotation")]
    [SerializeField] private Vector2 verticalRecoil = new Vector2(1.2f, 2.4f);
    [SerializeField] private Vector2 horizontalRecoil = new Vector2(-0.8f, 0.8f);
    [SerializeField] private Vector2 rollRecoil = new Vector2(-0.25f, 0.25f);
    [SerializeField] private float rotationReturnSpeed = 16f;

    private Vector3 initialWeaponPosition;
    private Quaternion initialWeaponRotation;
    private Quaternion initialCameraRotation;
    private Vector3 currentPositionOffset;
    private Vector3 targetPositionOffset;
    private Vector3 currentRotationOffset;
    private Vector3 targetRotationOffset;

    private void Awake()
    {
        if (weaponRoot == null)
            weaponRoot = transform;

        initialWeaponPosition = weaponRoot.localPosition;
        initialWeaponRotation = weaponRoot.localRotation;

        if (cameraRoot != null)
            initialCameraRotation = cameraRoot.localRotation;
    }

    private void LateUpdate()
    {
        targetPositionOffset = Vector3.Lerp(targetPositionOffset, Vector3.zero, positionReturnSpeed * Time.deltaTime);
        currentPositionOffset = Vector3.Lerp(currentPositionOffset, targetPositionOffset, positionReturnSpeed * Time.deltaTime);

        targetRotationOffset = Vector3.Lerp(targetRotationOffset, Vector3.zero, rotationReturnSpeed * Time.deltaTime);
        currentRotationOffset = Vector3.Lerp(currentRotationOffset, targetRotationOffset, rotationReturnSpeed * Time.deltaTime);

        weaponRoot.localPosition = initialWeaponPosition + currentPositionOffset;
        weaponRoot.localRotation = initialWeaponRotation * Quaternion.Euler(currentRotationOffset);

        if (cameraRoot != null)
            cameraRoot.localRotation = initialCameraRotation * Quaternion.Euler(currentRotationOffset.x, currentRotationOffset.y, 0f);
    }

    public void AddRecoil(float multiplier = 1f)
    {
        targetPositionOffset += positionKick * multiplier;
        targetRotationOffset += new Vector3(
            -Random.Range(verticalRecoil.x, verticalRecoil.y),
            Random.Range(horizontalRecoil.x, horizontalRecoil.y),
            Random.Range(rollRecoil.x, rollRecoil.y)) * multiplier;
    }
}
