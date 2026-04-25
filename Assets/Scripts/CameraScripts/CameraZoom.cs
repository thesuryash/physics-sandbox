using UnityEngine;
using System.Collections.Generic;

public class CameraOrbitSafe : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Add multiple objects here to look at their center, or leave empty to look forward.")]
    [SerializeField] private List<Transform> targets = new List<Transform>();
    [SerializeField] private Vector3 targetOffset = Vector3.zero;

    [Header("Distance")]
    [SerializeField] private float distance = 12f;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private float zoomSpeedMouse = 6f;
    [SerializeField] private float zoomSpeedTouch = 0.02f;

    [Header("Rotation")]
    [SerializeField] private float mouseRotationSpeed = 10f;
    [SerializeField] private float touchRotationSpeed = 0.2f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Mouse")]
    [SerializeField] private bool requireMouseHold = true;
    [SerializeField] private int mouseButton = 0; // 0 = Left click, 1 = Right click, 2 = Middle click

    private float yaw;
    private float pitch;
    private float targetDistance;

    private void Start()
    {
        Vector3 focus = GetFocusPoint();
        Vector3 toCamera = transform.position - focus;

        if (!IsFinite(toCamera) || toCamera.sqrMagnitude < 0.0001f)
        {
            toCamera = new Vector3(0f, 0f, -distance);
        }

        distance = toCamera.magnitude;
        targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);

        Vector3 dir = toCamera.normalized;
        pitch = Mathf.Asin(dir.y) * Mathf.Rad2Deg;
        yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
    }

    private void LateUpdate()
    {
        // If there are no targets, the camera will just stay where it is
        if (targets.Count == 0) return;

        Vector3 focus = GetFocusPoint();
        if (!IsFinite(focus)) return;

        HandleInput();

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -targetDistance);
        Vector3 desiredPosition = focus + offset;

        if (IsFinite(desiredPosition) && IsFinite(rotation))
        {
            transform.SetPositionAndRotation(desiredPosition, rotation);
        }
    }

    private void HandleInput()
    {
        bool allowOrbit = true;

        // Uncomment and adjust this section if you want to block the camera when hovering over UI elements
        /*
        if (!PanelInteraction.isMouseinPlayArea || SliderInteraction.isMouseOverSlider)
        {
            allowOrbit = false;
        }
        */

        if (Input.touchCount == 1 && allowOrbit)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;
                yaw += delta.x * touchRotationSpeed;
                pitch -= delta.y * touchRotationSpeed;
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 t0Prev = t0.position - t0.deltaPosition;
            Vector2 t1Prev = t1.position - t1.deltaPosition;

            float prevMagnitude = (t0Prev - t1Prev).magnitude;
            float currentMagnitude = (t0.position - t1.position).magnitude;
            float deltaMagnitude = prevMagnitude - currentMagnitude;

            targetDistance += deltaMagnitude * zoomSpeedTouch;
        }
        else
        {
            if (allowOrbit && (!requireMouseHold || Input.GetMouseButton(mouseButton)))
            {
                float mouseX = Input.GetAxisRaw("Mouse X");
                float mouseY = Input.GetAxisRaw("Mouse Y");

                yaw += mouseX * mouseRotationSpeed;
                pitch -= mouseY * mouseRotationSpeed;
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                targetDistance -= scroll * zoomSpeedMouse * Mathf.Max(1f, targetDistance * 0.15f);
            }
        }
    }

    // ==========================================
    // PUBLIC METHODS (Used by your Dropdown UI)
    // ==========================================

    public void AddTarget(Transform newTarget)
    {
        if (newTarget != null && !targets.Contains(newTarget))
        {
            targets.Add(newTarget);
        }
    }

    public void RemoveTarget(Transform targetToRemove)
    {
        if (targets.Contains(targetToRemove))
        {
            targets.Remove(targetToRemove);
        }
    }

    public void ClearAllTargets()
    {
        targets.Clear();
    }

    // ==========================================
    // INTERNAL MULTI-TARGET LOGIC
    // ==========================================

    private Vector3 GetFocusPoint()
    {
        if (targets.Count == 0) return transform.position + transform.forward * distance;

        // If there's only one target, look directly at it
        if (targets.Count == 1 && targets[0] != null)
            return targets[0].position + targetOffset;

        // If multiple targets, calculate the center point between all of them
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        bool hasValidTarget = false;

        foreach (Transform t in targets)
        {
            if (t != null)
            {
                min = Vector3.Min(min, t.position);
                max = Vector3.Max(max, t.position);
                hasValidTarget = true;
            }
        }

        if (!hasValidTarget) return Vector3.zero;

        Vector3 center = (min + max) / 2f;
        return center + targetOffset;
    }

    // ==========================================
    // SAFETY CHECKS
    // ==========================================

    private static bool IsFinite(float v)
    {
        return !(float.IsNaN(v) || float.IsInfinity(v));
    }

    private static bool IsFinite(Vector3 v)
    {
        return IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
    }

    private static bool IsFinite(Quaternion q)
    {
        return IsFinite(q.x) && IsFinite(q.y) && IsFinite(q.z) && IsFinite(q.w);
    }
}