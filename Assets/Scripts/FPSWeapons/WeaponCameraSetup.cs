using UnityEngine;

public class WeaponCameraSetup : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Camera weaponCamera;
    [SerializeField] private Transform weaponRoot;
    [SerializeField] private string weaponLayerName = "Weapon";
    [SerializeField] private float weaponNearClipPlane = 0.01f;

    [ContextMenu("Apply Weapon Camera Setup")]
    public void Apply()
    {
        int weaponLayer = LayerMask.NameToLayer(weaponLayerName);
        if (weaponLayer < 0)
        {
            Debug.LogWarning("Layer not found: " + weaponLayerName);
            return;
        }

        if (weaponRoot != null)
            SetLayerRecursively(weaponRoot.gameObject, weaponLayer);

        int weaponMask = 1 << weaponLayer;

        if (worldCamera != null)
            worldCamera.cullingMask &= ~weaponMask;

        if (weaponCamera != null)
        {
            weaponCamera.cullingMask = weaponMask;
            weaponCamera.nearClipPlane = weaponNearClipPlane;
            weaponCamera.clearFlags = CameraClearFlags.Depth;

            if (worldCamera != null)
                weaponCamera.depth = worldCamera.depth + 1;
        }
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;

        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
