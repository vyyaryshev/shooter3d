using System.Collections.Generic;
using UnityEngine;

public class FpsObjectPool : MonoBehaviour
{
    private static FpsObjectPool instance;

    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime = 3f)
    {
        if (prefab == null)
            return null;

        if (instance == null)
        {
            GameObject root = new GameObject("FpsObjectPool");
            instance = root.AddComponent<FpsObjectPool>();
        }

        return instance.SpawnInternal(prefab, position, rotation, lifetime);
    }

    public static void Return(GameObject prefab, GameObject instanceObject)
    {
        if (instance == null || prefab == null || instanceObject == null)
            return;

        instance.ReturnInternal(prefab, instanceObject);
    }

    private GameObject SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (!pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            pools.Add(prefab, pool);
        }

        GameObject item = pool.Count > 0 ? pool.Dequeue() : Instantiate(prefab);
        item.transform.SetPositionAndRotation(position, rotation);
        item.SetActive(true);

        PooledEffect pooledEffect = item.GetComponent<PooledEffect>();
        if (pooledEffect == null)
            pooledEffect = item.AddComponent<PooledEffect>();

        pooledEffect.Play(prefab, lifetime);
        return item;
    }

    private void ReturnInternal(GameObject prefab, GameObject instanceObject)
    {
        instanceObject.SetActive(false);
        instanceObject.transform.SetParent(transform);

        if (!pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            pools.Add(prefab, pool);
        }

        pool.Enqueue(instanceObject);
    }
}
