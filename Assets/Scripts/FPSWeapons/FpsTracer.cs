using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class FpsTracer : MonoBehaviour
{
    [SerializeField] private float width = 0.025f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
    }

    public void Play(Vector3 start, Vector3 end)
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }
}
