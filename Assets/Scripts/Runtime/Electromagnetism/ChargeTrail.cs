using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChargeTrail : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The moving object (Charge) whose position will be tracked by the trail.")]
    [SerializeField] private Transform charge;

    [Header("Visual Configuration")]
    [Tooltip("Optional: The LineRenderer component. If left empty, one will be fetched or added automatically.")]
    [SerializeField] private LineRenderer line;

    [Tooltip("The material to apply to the LineRenderer.")]
    [SerializeField] private Material lineMaterial;

    [Tooltip("The width of the trail line.")]
    [SerializeField] private float lineWidth = 0.1f;

    [Header("Trail Performance")]
    [Tooltip("Time in seconds between adding new points to the trail. Lower values make the line smoother but use more points.")]
    [SerializeField] private float pointInterval = 0.1f;   // seconds between points

    [Tooltip("The maximum number of points allowed in the trail before the oldest ones are removed.")]
    [SerializeField] private int maxPoints = 300;

    [Header("UI Controls")]
    [Tooltip("Optional: A UI Button that triggers a scene reset when clicked.")]
    [SerializeField] private Button resetButton;

    private float t;
    private Vector3 originalPos;
    private readonly List<Vector3> points = new();

    private void Awake()
    {
        if (charge != null) originalPos = charge.position;

        if (line == null) line = GetComponent<LineRenderer>();
        if (line == null) line = gameObject.AddComponent<LineRenderer>();

        if (lineMaterial != null) line.material = lineMaterial; // Added null check for safety
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetGame);
    }

    private void Update()
    {
        if (charge == null) return;

        // Auto reset if charge goes too far
        var p = charge.position;
        if (Mathf.Abs(originalPos.x - p.x) >= 80f ||
            Mathf.Abs(originalPos.y - p.y) >= 80f ||
            Mathf.Abs(originalPos.z - p.z) >= 80f)
        {
            ResetGame();
            return;
        }

        t += Time.deltaTime;
        if (t < pointInterval) return;
        t = 0f;

        // Add a point
        points.Add(p);
        if (points.Count > maxPoints)
            points.RemoveAt(0);

        // Push to line renderer
        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
            line.SetPosition(i, points[i]);
    }

    public void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}