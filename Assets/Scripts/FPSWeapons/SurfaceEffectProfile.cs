using System;
using UnityEngine;

[CreateAssetMenu(menuName = "FPS Weapons/Surface Effect Profile")]
public class SurfaceEffectProfile : ScriptableObject
{
    [SerializeField] private SurfaceEffectDefinition defaultEffect;
    [SerializeField] private SurfaceEffectDefinition[] effects;

    public SurfaceEffectDefinition GetEffect(RaycastHit hit)
    {
        SurfaceIdentifier identifier = hit.collider.GetComponentInParent<SurfaceIdentifier>();
        if (identifier != null)
            return FindByType(identifier.SurfaceType);

        PhysicMaterial material = hit.collider.sharedMaterial;
        if (material != null)
        {
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i].MatchesMaterial(material))
                    return effects[i];
            }
        }

        string hitTag = hit.collider.tag;
        for (int i = 0; i < effects.Length; i++)
        {
            if (effects[i].MatchesTag(hitTag))
                return effects[i];
        }

        return defaultEffect;
    }

    private SurfaceEffectDefinition FindByType(WeaponSurfaceType type)
    {
        for (int i = 0; i < effects.Length; i++)
        {
            if (effects[i].surfaceType == type)
                return effects[i];
        }

        return defaultEffect;
    }
}

[Serializable]
public class SurfaceEffectDefinition
{
    public WeaponSurfaceType surfaceType = WeaponSurfaceType.Default;
    public PhysicMaterial[] physicMaterials;
    public string[] tags;
    public GameObject impactParticles;
    public GameObject decalPrefab;
    public float decalOffset = 0.01f;
    public float lifetime = 3f;

    public bool MatchesMaterial(PhysicMaterial material)
    {
        if (physicMaterials == null)
            return false;

        for (int i = 0; i < physicMaterials.Length; i++)
        {
            if (physicMaterials[i] == material)
                return true;
        }

        return false;
    }

    public bool MatchesTag(string hitTag)
    {
        if (tags == null)
            return false;

        for (int i = 0; i < tags.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(tags[i]) && tags[i] == hitTag)
                return true;
        }

        return false;
    }
}
