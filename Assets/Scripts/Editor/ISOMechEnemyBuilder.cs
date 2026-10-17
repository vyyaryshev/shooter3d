using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class ISOMechEnemyBuilder
{
    private const string Source = "Assets/RSG_UnderWater_Pack/ISO/HDRP/";

    [MenuItem("Tools/Shooter3D/Create ISO Mech Enemy")]
    public static void CreateEnemy()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Create the prefab outside Play Mode.");
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(Source + "Prefabs/Mech/P_ISO_Mech.prefab");
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (source == null || shader == null)
            throw new InvalidOperationException("ISO Mech source prefab and URP/Lit shader are required.");

        // A new directory on every invocation protects previous prefab edits.
        string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/Prefabs/ISOMechEnemy");
        AssetDatabase.CreateFolder("Assets/Prefabs", System.IO.Path.GetFileName(folder));
        Scene preview = EditorSceneManager.NewPreviewScene();
        GameObject root = new GameObject("ISO Mech Enemy");
        SceneManager.MoveGameObjectToScene(root, preview);
        try
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(source, preview);
            visual.transform.SetParent(root.transform, false);
            visual.name = "Visual";
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = new Bounds(root.transform.position, Vector3.zero);
            foreach (Renderer renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            // Fit the imported model to a useful enemy size without changing its FBX scale.
            visual.transform.localScale *= 2.5f / Mathf.Max(0.01f, bounds.size.y);
            bounds = new Bounds(root.transform.position, Vector3.zero);
            foreach (Renderer renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            visual.transform.position -= Vector3.up * bounds.min.y;

            Material material = CreateMaterial(folder, shader);
            foreach (Renderer renderer in renderers)
                renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();

            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                // Clips bind paths relative to the FBX root, not the outer prefab wrapper.
                Transform modelRoot = visual.transform.Find("SK_ISO_Mech");
                if (modelRoot == null)
                    throw new InvalidOperationException("Missing SK_ISO_Mech model root in the source prefab.");
                animator = modelRoot.gameObject.AddComponent<Animator>();
                animator.avatar = AssetDatabase.LoadAllAssetsAtPath(Source + "Models/Mech/SK_ISO_Mech.fbx")
                    .OfType<Avatar>().FirstOrDefault();
            }
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(folder + "/MechEnemy.controller");
            AddState(controller, "Idle", "Idle", true);
            AddState(controller, "Walk", "WalkForward", true);
            AddState(controller, "Death", "Death", false);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.height = 2.5f;
            agent.radius = Mathf.Max(0.5f, bounds.size.x * 0.3f);
            agent.speed = 3f;
            agent.acceleration = 8f;
            agent.angularSpeed = 180f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.height = agent.height;
            collider.radius = agent.radius;
            collider.center = Vector3.up * (agent.height * 0.5f);
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            root.AddComponent<Health>();
            root.AddComponent<EnemyController>();
            ISOMechAI ai = root.AddComponent<ISOMechAI>();

            string[] boneNames = { "ShotgunTop_L", "ShotgunTop_R", "ShotgunBot_L", "ShotgunBot_R" };
            Transform[] bones = new Transform[boneNames.Length];
            Transform[] muzzles = new Transform[boneNames.Length];
            Transform[] hierarchy = visual.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < boneNames.Length; i++)
            {
                bones[i] = hierarchy.FirstOrDefault(t => t.name == boneNames[i]);
                if (bones[i] == null)
                    throw new InvalidOperationException("Missing cannon bone: " + boneNames[i]);
                Transform muzzle = new GameObject("FirePoint_" + boneNames[i]).transform;
                muzzle.SetParent(bones[i], false);
                // Starting position; the barrel tips can be adjusted in Prefab Mode.
                muzzle.position = bones[i].position + root.transform.forward * 0.8f;
                muzzle.rotation = root.transform.rotation;
                muzzles[i] = muzzle;
            }

            SerializedObject settings = new SerializedObject(ai);
            settings.FindProperty("bulletPrefab").objectReferenceValue = CreateProjectile(folder, preview);
            AssignArray(settings.FindProperty("cannonBones"), bones);
            AssignArray(settings.FindProperty("firePoints"), muzzles);
            settings.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, folder + "/ISOMechEnemy.prefab");
            AssetDatabase.SaveAssets();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log("ISO Mech enemy created: " + folder + "/ISOMechEnemy.prefab. Check barrel FirePoints before use.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    private static void AddState(AnimatorController controller, string stateName, string clipName, bool loop)
    {
        string path = Source + "Animations/Mech/A_ISO_Mech_" + clipName + "_01.fbx";
        AnimationClip imported = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal));
        if (imported == null)
            throw new InvalidOperationException("Missing animation: " + path);
        AnimationClip copy = UnityEngine.Object.Instantiate(imported);
        copy.name = stateName;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(copy);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(copy, settings);
        AssetDatabase.CreateAsset(copy, System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(controller))
            .Replace('\\', '/') + "/" + stateName + ".anim");
        AnimatorState state = controller.layers[0].stateMachine.AddState(stateName);
        state.motion = copy;
        if (stateName == "Idle")
            controller.layers[0].stateMachine.defaultState = state;
    }

    private static Material CreateMaterial(string folder, Shader shader)
    {
        Material material = new Material(shader) { name = "Mech URP" };
        string textures = Source + "Textures/Mech/T_Mech_LOD2_";
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textures + "B.png"));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textures + "N.png"));
        material.EnableKeyword("_NORMALMAP");
        material.SetFloat("_Metallic", 0.7f);
        material.SetFloat("_Smoothness", 0.4f);
        material.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textures + "E.png"));
        material.SetColor("_EmissionColor", Color.white);
        material.EnableKeyword("_EMISSION");
        // HDRP mask channels differ from URP metallic maps, so don't bind them directly.
        AssetDatabase.CreateAsset(material, folder + "/MechURP.mat");
        return material;
    }

    private static GameObject CreateProjectile(string folder, Scene preview)
    {
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        SceneManager.MoveGameObjectToScene(bullet, preview);
        try
        {
            bullet.name = "Mech Projectile";
            bullet.transform.localScale = Vector3.one * 0.12f;
            Rigidbody body = bullet.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            bullet.AddComponent<BulletController>();
            return PrefabUtility.SaveAsPrefabAsset(bullet, folder + "/MechProjectile.prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bullet);
        }
    }

    private static void AssignArray(SerializedProperty property, Transform[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
