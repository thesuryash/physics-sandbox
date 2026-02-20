using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Mass))]
public class FreeBodyDiagram : MonoBehaviour
{
    [Header("Visualization Settings")]
    public float globalArrowScale = 0.2f;
    [Range(0.001f, 0.1f)] public float thresholdPercent = 0.02f; // 2% threshold
    public bool showGravity = true;
    public bool showNormal = true;
    public bool showFriction = true;

    [Header("Stabilization")]
    [Range(0.01f, 0.5f)] public float frictionSmoothing = 0.1f;
    [Tooltip("Higher value = more stable but slower to react.")]

    [Header("Colors")]
    public Color gravityColor = Color.red;
    public Color normalColor = Color.yellow;
    public Color frictionColor = Color.blue;

    [SerializeField] private Material _arrowMaterial;

    private Rigidbody _rb;
    private Mass _massData;
    private Arrow2D _gravityArrow, _normalArrow, _frictionArrow;

    // Smoothing state
    private Vector3 _smoothedVelocityDir = Vector3.zero;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _massData = GetComponent<Mass>();
        InitializeArrows();
    }

    private void InitializeArrows()
    {
        _gravityArrow = CreateArrowResource("FBD_Gravity", gravityColor);
        _normalArrow = CreateArrowResource("FBD_Normal", normalColor);
        _frictionArrow = CreateArrowResource("FBD_Friction", frictionColor);
    }

    private void FixedUpdate()
    {
        if (_rb == null || _massData == null) return;

        // Base reference for our threshold (Weight of the object)
        float weight = _massData.TotalMass * Physics.gravity.magnitude;
        float deadzone = weight * thresholdPercent;

        UpdateGravity(weight, deadzone);
        UpdateNormalAndFriction(weight, deadzone);
    }

    private void UpdateGravity(float weight, float deadzone)
    {
        bool active = showGravity && weight > deadzone;
        _gravityArrow.gameObject.SetActive(active);

        if (active)
        {
            _gravityArrow.transform.up = Physics.gravity.normalized;
            _gravityArrow.SetData(weight * globalArrowScale, gravityColor);
        }
    }

    private void UpdateNormalAndFriction(float weight, float deadzone)
    {
        RaycastHit hit;
        float rayDist = transform.lossyScale.y * 0.6f;
        bool isGrounded = Physics.Raycast(transform.position, Physics.gravity.normalized, out hit, rayDist);

        if (isGrounded)
        {
            float m = _massData.TotalMass;
            float g = Physics.gravity.magnitude;

            // 1. Normal Force with thresholding
            float angle = Vector3.Angle(-Physics.gravity.normalized, hit.normal);
            float normalMag = m * g * Mathf.Cos(angle * Mathf.Deg2Rad);

            if (showNormal && normalMag > deadzone)
            {
                _normalArrow.gameObject.SetActive(true);
                _normalArrow.transform.up = hit.normal;
                _normalArrow.SetData(normalMag * globalArrowScale, normalColor);
            }
            else { _normalArrow.gameObject.SetActive(false); }

            // 2. Friction Force (Stabilized)
            float velocityMag = _rb.velocity.magnitude;

            // Hysteresis: Stay on easier than turning on to prevent flicker
            bool wasActive = _frictionArrow.gameObject.activeSelf;
            float threshold = wasActive ? 0.05f : 0.15f;

            if (showFriction && velocityMag > threshold)
            {
                _frictionArrow.gameObject.SetActive(true);

                // Smooth the direction to prevent jittery rotation
                _smoothedVelocityDir = Vector3.Slerp(_smoothedVelocityDir, _rb.velocity.normalized, frictionSmoothing);
                _frictionArrow.transform.up = -_smoothedVelocityDir;

                // Use the smoothed length from Arrow2D's internal stabilization if available, 
                // otherwise the raw velocity magnitude
                _frictionArrow.SetData(velocityMag * globalArrowScale * 2f, frictionColor);
            }
            else
            {
                _frictionArrow.gameObject.SetActive(false);
                _smoothedVelocityDir = Vector3.zero;
            }
        }
        else
        {
            _normalArrow.gameObject.SetActive(false);
            _frictionArrow.gameObject.SetActive(false);
        }
    }

    private Arrow2D CreateArrowResource(string arrowName, Color arrowColor)
    {
        Transform existing = transform.Find(arrowName);
        if (existing != null) return existing.GetComponent<Arrow2D>();

        GameObject go = new GameObject(arrowName);
        go.transform.SetParent(this.transform);
        go.transform.localPosition = Vector3.zero;

        Arrow2D arrow = go.AddComponent<Arrow2D>();
        arrow.autoUpdateFromReference = false;

        // Use reflection to set private field
        var field = typeof(Arrow2D).GetField("_baseMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(arrow, _arrowMaterial);

        return arrow;
    }
}