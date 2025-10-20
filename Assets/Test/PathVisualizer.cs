using Pathfinding;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PathVisualizer : MonoBehaviour
{
    public Seeker seeker; // Use Seeker if available
    public Transform startTransform;
    public Transform endTransform;
    public float requestInterval = 0.25f; // seconds between path requests when endpoints move
    public bool drawGizmos = true;
    public Color gizmoColor = Color.green;

    // Line width (exposed so you can tweak it in the inspector)
    public float lineWidth = 0.15f;

    LineRenderer lineRenderer;
    List<Vector3> currentVectorPath = new List<Vector3>();
    float lastRequestTime = -999f;
    Vector3 lastStartPos;
    Vector3 lastEndPos;
    const float movementThreshold = 0.01f;

    void Awake()
    {
        // Try to get a Seeker component if one isn't assigned
        if (seeker == null) seeker = GetComponent<Seeker>();

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // Basic LineRenderer setup (tweak in inspector)
        lineRenderer.positionCount = 0;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
#if UNITY_5_6_OR_NEWER
        // widthMultiplier on newer Unity versions can scale the whole line
        lineRenderer.widthMultiplier = 1f;
#endif
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.loop = false;
        // Ensure the line is drawn in world space
        lineRenderer.useWorldSpace = true;
    }

    void Start()
    {
        if (startTransform != null) lastStartPos = startTransform.position;
        if (endTransform != null) lastEndPos = endTransform.position;
        RequestPath(); // initial request
    }

    void Update()
    {
        if (startTransform == null || endTransform == null) return;

        bool moved = (startTransform.position - lastStartPos).sqrMagnitude > movementThreshold * movementThreshold
                  || (endTransform.position - lastEndPos).sqrMagnitude > movementThreshold * movementThreshold;

        if (moved && Time.time - lastRequestTime >= requestInterval)
        {
            RequestPath();
            lastStartPos = startTransform.position;
            lastEndPos = endTransform.position;
            lastRequestTime = Time.time;
        }

        // Apply width in case it was changed in the inspector at runtime
        if (lineRenderer != null)
        {
            if (!Mathf.Approximately(lineRenderer.startWidth, lineWidth) || !Mathf.Approximately(lineRenderer.endWidth, lineWidth))
            {
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;
            }
        }

        // Optional: smoothly update LineRenderer if path exists
        if (currentVectorPath != null && currentVectorPath.Count > 1)
        {
            lineRenderer.positionCount = currentVectorPath.Count;
            for (int i = 0; i < currentVectorPath.Count; i++)
            {
                lineRenderer.SetPosition(i, currentVectorPath[i]);
            }
        }
        else
        {
            lineRenderer.positionCount = 0;
        }
    }

    void RequestPath()
    {
        if (AstarPath.active == null)
        {
            Debug.LogWarning("AstarPath.active is null. Make sure AstarPath exists and graphs are scanned.");
            return;
        }

        Vector3 start = startTransform.position;
        Vector3 end = endTransform.position;

        if (seeker != null)
        {
            // Use Seeker to start the path (recommended)
            seeker.StartPath(start, end, OnPathComplete);
        }
        else
        {
            // Fallback to directly constructing an ABPath if no Seeker is present
            var path = ABPath.Construct(start, end, OnPathComplete);
            AstarPath.StartPath(path);
        }
    }

    void OnPathComplete(Path p)
    {
        if (p == null) return;

        if (p.error)
        {
            currentVectorPath = new List<Vector3>();
            Debug.LogWarning("Path failed: " + p.errorLog);
            return;
        }

        // 'vectorPath' is a public List<Vector3> on Path (final world-space path)
        currentVectorPath = new List<Vector3>(((IEnumerable<Vector3>)p.vectorPath));
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || currentVectorPath == null || currentVectorPath.Count < 2) return;

        Gizmos.color = gizmoColor;
        for (int i = 0; i < currentVectorPath.Count - 1; i++)
        {
            Gizmos.DrawLine(currentVectorPath[i], currentVectorPath[i + 1]);
        }
    }
}