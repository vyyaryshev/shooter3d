using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FpsWeaponViewSetup : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Camera weaponCamera;
    [SerializeField] private string weaponLayerName = "Weapon";
    [SerializeField] private float nearClipPlane = 0.01f;
    [SerializeField] private float farClipPlane = 30f;
    [SerializeField] private bool disableWeaponColliders = true;
    [SerializeField] private bool disableLegacyShoot = true;

    private int weaponLayer;
    private int weaponMask;

    private void Awake()
    {
        ResolveCamera();
        ResolveWeaponLayer();

        if (worldCamera == null || weaponLayer < 0)
        {
            Debug.LogWarning("FpsWeaponViewSetup: не найдена камера игрока или layer Weapon.");
            return;
        }

        SetupWeaponCamera();
        ApplyWeaponLayer(transform);

        if (disableWeaponColliders)
            DisableColliders();

        if (disableLegacyShoot)
            DisableOldShootScripts();
    }

    private void LateUpdate()
    {
        if (worldCamera == null || weaponCamera == null)
            return;

        weaponCamera.fieldOfView = worldCamera.fieldOfView;
    }

    private void ResolveCamera()
    {
        if (worldCamera == null)
            worldCamera = GetComponentInParent<Camera>();

        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private void ResolveWeaponLayer()
    {
        weaponLayer = LayerMask.NameToLayer(weaponLayerName);
        if (weaponLayer >= 0)
            weaponMask = 1 << weaponLayer;
    }

    private void SetupWeaponCamera()
    {
        if (weaponCamera == null)
            weaponCamera = FindExistingWeaponCamera();

        if (weaponCamera == null)
            weaponCamera = CreateWeaponCamera();

        worldCamera.cullingMask &= ~weaponMask;

        weaponCamera.clearFlags = CameraClearFlags.Depth;
        weaponCamera.cullingMask = weaponMask;
        weaponCamera.nearClipPlane = nearClipPlane;
        weaponCamera.farClipPlane = farClipPlane;
        weaponCamera.depth = worldCamera.depth + 1f;
        weaponCamera.fieldOfView = worldCamera.fieldOfView;

        SetupUrpCameraStack();
    }

    private void SetupUrpCameraStack()
    {
        UniversalAdditionalCameraData worldCameraData = worldCamera.GetUniversalAdditionalCameraData();
        UniversalAdditionalCameraData weaponCameraData = weaponCamera.GetUniversalAdditionalCameraData();

        worldCameraData.renderType = CameraRenderType.Base;
        weaponCameraData.renderType = CameraRenderType.Overlay;

        var cameraStack = worldCameraData.cameraStack;
        if (cameraStack != null && !cameraStack.Contains(weaponCamera))
            cameraStack.Add(weaponCamera);
    }

    private Camera FindExistingWeaponCamera()
    {
        Transform cameraTransform = worldCamera.transform.Find("Weapon Camera");
        if (cameraTransform == null)
            return null;

        return cameraTransform.GetComponent<Camera>();
    }

    private Camera CreateWeaponCamera()
    {
        GameObject cameraObject = new GameObject("Weapon Camera");
        cameraObject.transform.SetParent(worldCamera.transform, false);

        Camera createdCamera = cameraObject.AddComponent<Camera>();
        AudioListener listener = cameraObject.GetComponent<AudioListener>();
        if (listener != null)
            Destroy(listener);

        return createdCamera;
    }

    private void ApplyWeaponLayer(Transform root)
    {
        root.gameObject.layer = weaponLayer;

        for (int i = 0; i < root.childCount; i++)
            ApplyWeaponLayer(root.GetChild(i));
    }

    private void DisableColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    private void DisableOldShootScripts()
    {
        Shoot[] oldShootScripts = GetComponentsInChildren<Shoot>(true);
        for (int i = 0; i < oldShootScripts.Length; i++)
            oldShootScripts[i].enabled = false;
    }
}
