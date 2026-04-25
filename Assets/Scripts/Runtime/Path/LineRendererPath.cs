using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LineRendererPath : MonoBehaviour
{
    public enum PathType
    {
        Linear,   // Uses waypoints
        Curved    // Uses parametric generation (ellipse)
    }

    [Header("Path Mode")]
    [SerializeField] private PathType pathType = PathType.Curved;

    [Header("Shared")]
    [SerializeField] private LineRenderer line;
    [SerializeField] private int samples = 64;
    [SerializeField] private Transform space; // optional local space

    [Header("Linear Path")]
    [SerializeField] private List<Transform> waypoints;

    [Header("Curved Path (Ellipse)")]
    [SerializeField] private float radiusA = 1f;
    [SerializeField] private float radiusB = 1f;

    public PathType Mode => pathType;
    public bool UsesWaypoints => pathType == PathType.Linear;

    // -------------------- Lifecycle --------------------

    private void OnValidate()
    {
        if (line == null)
            line = GetComponent<LineRenderer>();

        if (pathType == PathType.Curved)
            RedrawCurved();
        else
            RedrawLinear();
    }

    // -------------------- Public API --------------------

    /// <summary>
    /// Evaluate position along the path. t wraps in [0,1).
    /// </summary>
    public Vector3 Evaluate(float t)
    {
        t = Mathf.Repeat(t, 1f);

        return pathType == PathType.Curved
            ? EvaluateEllipse(t)
            : EvaluateWaypoints(t);
    }

    public void InitCurved(float a, float b, int sampleCount = 64)
    {
        pathType = PathType.Curved;
        radiusA = Mathf.Max(0.0001f, a);
        radiusB = Mathf.Max(0.0001f, b);
        samples = Mathf.Max(8, sampleCount);
        RedrawCurved();
    }

    public void InitLinear(List<Transform> pts)
    {
        pathType = PathType.Linear;
        waypoints = pts;
        RedrawLinear();
    }

    // -------------------- Curved --------------------

    private Vector3 EvaluateEllipse(float t)
    {
        float angle = t * Mathf.PI * 2f;
        Vector3 local = new Vector3(
            Mathf.Cos(angle) * radiusA,
            0f,
            Mathf.Sin(angle) * radiusB
        );

        return space ? space.TransformPoint(local) : local;
    }

    private void RedrawCurved()
    {
        if (line == null) return;

        int count = Mathf.Max(8, samples);
        line.positionCount = count + 1;

        for (int i = 0; i <= count; i++)
        {
            float t = (float)i / count;
            line.SetPosition(i, EvaluateEllipse(t));
        }
    }

    // -------------------- Linear --------------------

    private Vector3 EvaluateWaypoints(float t)
    {
        if (waypoints == null || waypoints.Count < 2)
            return transform.position;

        int segmentCount = waypoints.Count - 1;
        float scaled = t * segmentCount;

        int i = Mathf.FloorToInt(scaled);
        int j = Mathf.Clamp(i + 1, 0, waypoints.Count - 1);

        float localT = scaled - i;

        return Vector3.Lerp(
            waypoints[i].position,
            waypoints[j].position,
            localT
        );
    }

    private void RedrawLinear()
    {
        if (line == null || waypoints == null || waypoints.Count < 2)
            return;

        line.positionCount = waypoints.Count;
        for (int i = 0; i < waypoints.Count; i++)
            line.SetPosition(i, waypoints[i].position);
    }
}
