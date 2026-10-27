using UnityEngine;
using UnityEngine.Rendering;

public class RocketExplosionEffect : MonoBehaviour
{
    private Material material;
    private Texture2D texture;
    private Light flash;
    private float age;

    public static void Spawn(Vector3 position, float radius)
    {
        GameObject root = new GameObject("Rocket Explosion");
        root.transform.position = position;
        root.AddComponent<RocketExplosionEffect>().Initialize(radius);
    }

    private void Initialize(float radius)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader != null)
        {
            texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 15.5f;
                    pixels[y * 32 + x] = new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - distance), 2f));
                }
            texture.SetPixels(pixels);
            texture.Apply();
            material = new Material(shader);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_MainTex", texture);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        CreateBurst("Fire", new Color(1f, 0.35f, 0.03f), 45, 0.55f, radius * 2f, radius * 0.3f);
        CreateBurst("Smoke", new Color(0.18f, 0.18f, 0.18f, 0.65f), 24, 1.8f, radius * 0.6f, radius * 0.5f);
        flash = gameObject.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = new Color(1f, 0.45f, 0.1f);
        flash.range = radius * 3f;
        flash.intensity = 5f;
        Destroy(gameObject, 2.5f);
    }

    private void CreateBurst(string label, Color color, short count, float duration, float speed, float size)
    {
        GameObject child = new GameObject(label);
        child.transform.SetParent(transform, false);
        ParticleSystem particles = child.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = false;
        main.duration = duration;
        main.startLifetime = duration;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count;
        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;
        var overLifetime = particles.colorOverLifetime;
        overLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        overLifetime.color = fade;
        if (material != null)
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        particles.Play();
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (flash != null)
            flash.intensity = 5f * Mathf.Clamp01(1f - age / 0.2f);
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
        if (texture != null)
            Destroy(texture);
    }
}
