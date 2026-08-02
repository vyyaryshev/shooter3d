using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class UrpMaterialFixer
{
    private const string UrpLitShaderName = "Universal Render Pipeline/Lit";
    private const string UrpParticlesUnlitShaderName = "Universal Render Pipeline/Particles/Unlit";

    [MenuItem("Tools/Shooter3D/Materials/Fix Selected Materials To URP")]
    private static void FixSelectedMaterials()
    {
        string[] materialPaths = GetSelectedMaterialPaths();
        if (materialPaths.Length == 0)
        {
            Debug.LogWarning("Select one or more materials or folders before running material fix.");
            return;
        }

        FixMaterials(materialPaths, false);
    }

    [MenuItem("Tools/Shooter3D/Materials/Fix Problem Materials In Project")]
    private static void FixProblemMaterialsInProject()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        List<string> materialPaths = new List<string>(guids.Length);

        foreach (string guid in guids)
            materialPaths.Add(AssetDatabase.GUIDToAssetPath(guid));

        FixMaterials(materialPaths.ToArray(), true);
    }

    private static string[] GetSelectedMaterialPaths()
    {
        Object[] selectedAssets = Selection.GetFiltered<Object>(SelectionMode.Assets);
        HashSet<string> materialPaths = new HashSet<string>();

        foreach (Object selectedAsset in selectedAssets)
        {
            string path = AssetDatabase.GetAssetPath(selectedAsset);
            if (string.IsNullOrEmpty(path))
                continue;

            if (AssetDatabase.IsValidFolder(path))
            {
                string[] guids = AssetDatabase.FindAssets("t:Material", new[] { path });
                foreach (string guid in guids)
                    materialPaths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }
            else if (path.EndsWith(".mat"))
            {
                materialPaths.Add(path);
            }
        }

        string[] paths = new string[materialPaths.Count];
        materialPaths.CopyTo(paths);
        return paths;
    }

    private static void FixMaterials(string[] materialPaths, bool onlyProblemMaterials)
    {
        Shader urpLitShader = Shader.Find(UrpLitShaderName);
        Shader urpParticlesShader = Shader.Find(UrpParticlesUnlitShaderName);

        if (urpLitShader == null)
        {
            Debug.LogError($"Cannot find shader: {UrpLitShaderName}. Check that URP is installed and active.");
            return;
        }

        int changedCount = 0;
        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (string path in materialPaths)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || IsEditorOnlyMaterial(path))
                    continue;

                if (onlyProblemMaterials && !NeedsUrpFix(material))
                    continue;

                Shader targetShader = ShouldUseParticleShader(path, material) && urpParticlesShader != null
                    ? urpParticlesShader
                    : urpLitShader;

                ConvertMaterial(material, targetShader);
                EditorUtility.SetDirty(material);
                changedCount++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"URP material fix finished. Changed materials: {changedCount}");
    }

    private static bool NeedsUrpFix(Material material)
    {
        if (material.shader == null)
            return true;

        string shaderName = material.shader.name;
        if (shaderName.StartsWith("Universal Render Pipeline/"))
            return false;

        return shaderName == "Standard"
            || shaderName == "Autodesk Interactive"
            || shaderName.StartsWith("HDRP/")
            || shaderName.Contains("HDRenderPipeline")
            || shaderName.Contains("InternalErrorShader")
            || shaderName.StartsWith("Legacy Shaders/");
    }

    private static bool IsEditorOnlyMaterial(string path)
    {
        return path.Contains("/TextMesh Pro/")
            || path.Contains("\\TextMesh Pro\\")
            || path.Contains("/Editor/")
            || path.Contains("\\Editor\\");
    }

    private static bool ShouldUseParticleShader(string path, Material material)
    {
        string shaderName = material.shader != null ? material.shader.name : string.Empty;

        return path.Contains("Particle")
            || path.Contains("Particles")
            || path.Contains("VFX")
            || shaderName.Contains("Particle");
    }

    private static void ConvertMaterial(Material material, Shader targetShader)
    {
        Texture baseTexture = GetTexture(material, "_BaseMap", "_MainTex", "_BaseColorMap", "_BaseColor");
        Texture normalTexture = GetTexture(material, "_BumpMap", "_NormalMap");
        Texture metallicTexture = GetTexture(material, "_MetallicGlossMap", "_MaskMap");
        Texture emissionTexture = GetTexture(material, "_EmissionMap", "_EmissiveColorMap");

        Color baseColor = GetColor(material, Color.white, "_BaseColor", "_Color");
        Color emissionColor = GetColor(material, Color.black, "_EmissionColor", "_EmissiveColor");
        float metallic = GetFloat(material, 0f, "_Metallic");
        float smoothness = GetFloat(material, 0.5f, "_Smoothness", "_Glossiness");
        float alphaClip = GetFloat(material, 0f, "_AlphaClip");
        float cutoff = GetFloat(material, 0.5f, "_Cutoff");

        material.shader = targetShader;

        SetTexture(material, "_BaseMap", baseTexture);
        SetTexture(material, "_MainTex", baseTexture);
        SetTexture(material, "_BumpMap", normalTexture);
        SetTexture(material, "_MetallicGlossMap", metallicTexture);
        SetTexture(material, "_EmissionMap", emissionTexture);

        SetColor(material, "_BaseColor", baseColor);
        SetColor(material, "_Color", baseColor);
        SetColor(material, "_EmissionColor", emissionColor);
        SetFloat(material, "_Metallic", metallic);
        SetFloat(material, "_Smoothness", smoothness);
        SetFloat(material, "_AlphaClip", alphaClip);
        SetFloat(material, "_Cutoff", cutoff);

        if (normalTexture != null)
            material.EnableKeyword("_NORMALMAP");

        if (emissionTexture != null || emissionColor.maxColorComponent > 0f)
            material.EnableKeyword("_EMISSION");
    }

    private static Texture GetTexture(Material material, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
                return material.GetTexture(propertyName);
        }

        return null;
    }

    private static Color GetColor(Material material, Color fallback, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
                return material.GetColor(propertyName);
        }

        return fallback;
    }

    private static float GetFloat(Material material, float fallback, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
                return material.GetFloat(propertyName);
        }

        return fallback;
    }

    private static void SetTexture(Material material, string propertyName, Texture texture)
    {
        if (texture != null && material.HasProperty(propertyName))
            material.SetTexture(propertyName, texture);
    }

    private static void SetColor(Material material, string propertyName, Color color)
    {
        if (material.HasProperty(propertyName))
            material.SetColor(propertyName, color);
    }

    private static void SetFloat(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
            material.SetFloat(propertyName, value);
    }
}
