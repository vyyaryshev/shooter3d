using UnityEngine;

public class MaterialChanger : MonoBehaviour
{
    [SerializeField] private Material material;

    private void Start()
    {
        material.color = Color.green;
    }
}