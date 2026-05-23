using UnityEngine;

public class SurfaceIdentifier : MonoBehaviour
{
    [SerializeField] private WeaponSurfaceType surfaceType = WeaponSurfaceType.Default;

    public WeaponSurfaceType SurfaceType => surfaceType;
}
