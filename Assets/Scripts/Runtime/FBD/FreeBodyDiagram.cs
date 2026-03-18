using System;
using UnityEngine;

[RequireComponent(typeof(PhysicsBody), typeof(Mass))]
public class FreeBodyDiagram : MonoBehaviour
{
    public enum ArrowMode { Mode2D, Mode3D }

    [Header("Visualization Settings")]
    public ArrowMode visualizationMode = ArrowMode.Mode2D;
    public float globalArrowScale = 0.2f;
    public float maxArrowLength = 3f;

    [Range(0.001f, 1f)]
    public float screenProportionMultiplier = 0.05f;

    [Header("Overlay Settings")]
    [Tooltip("If true, the FBD arrows and box will render on top of all 3D objects.")]
    public bool drawOverObjects = true;

    [Space(10)]
    [Range(0.001f, 0.1f)] public float thresholdPercent = 0.02f;
    public bool showGravity = true;
    public bool showNormal = true;
    public bool showFriction = true;
    public bool showDrag = true;
    public bool showNetForce = true;

    [Header("FBD Box Representation")]
    public bool showFBDBox = true;
    public float fbdBoxSize = 1.5f;
    public float fbdBoxThickness = 0.05f;
    public Color fbdBoxColor = Color.white;

    [Header("Physics Scale Factors")]
    [Range(0.01f, 0.5f)] public float frictionSmoothing = 0.1f;
    [Range(0.1f, 5.0f)] public float dragSensitivity = 1.0f;

    [Header("Labels & Colors")]
    public string gravityLabel = "Fg";
    public Color gravityColor = Color.red;
    [Space(5)]
    public string netForceLabel = "F_net";
    public Color netForceColor = Color.cyan;

    [Space(5)]
    public string normalLabel = "Fn";
    public Color normalColor = Color.yellow;
    [Space(5)]
    public string frictionLabel = "Ff";
    public Color frictionColor = Color.blue;
    [Space(5)]
    public string dragLabel = "Fd";
    public Color dragColor = Color.cyan;

    [Space(10)]
    [SerializeField] private Material _arrowMaterial;

    private PhysicsBody _rb;
    private Mass _massData;
    private Camera _mainCamera;

    // ADDED _netForceArrow
    private Arrow2D _gravityArrow, _normalArrow, _frictionArrow, _dragArrow, _netForceArrow;
    private LineRenderer _fbdBoxRenderer;

    // Track the actual active vector of each force so we can sum them for Net Force
    private Vector3 _forceGravity, _forceNormal, _forceFriction, _forceDrag;

    private Vector3 _smoothedVelocityDir = Vector3.zero;

    private bool _isColliding = false;
    private Vector3 _lastContactNormal = Vector3.up;
    private float _timeSinceLastContact = 999f;
    private const float CONTACT_BUFFER_TIME = 0.1f;

    private void Awake()
    {
        _rb = GetComponent<PhysicsBody>();
        _massData = GetComponent<Mass>();

        PhysicsBody pb = GetComponent<PhysicsBody>();
        if (pb == null) pb = gameObject.AddComponent<PhysicsBody>();
    }

    private void Start()
    {
        _mainCamera = Camera.main;

        InitializeArrows();
        InitializeFBDBox();

        if (_massData != null && _rb != null)
        {
            float weight = _massData.TotalMass * Physics.gravity.magnitude;
            float deadzone = weight * thresholdPercent;

            UpdateGravity(weight, deadzone, true);
            UpdateNormalAndFriction(weight, deadzone, true);
            UpdateDrag(true);
            UpdateNetForce(true); // Added true to force snap on start
        }
    }

    private void Update()
    {
        SyncArrowSettings(_gravityArrow);
        SyncArrowSettings(_normalArrow);
        SyncArrowSettings(_frictionArrow);
        SyncArrowSettings(_dragArrow);
        SyncArrowSettings(_netForceArrow); // Sync Net Force Arrow

        if (_fbdBoxRenderer != null && _fbdBoxRenderer.sharedMaterial != null)
        {
            if (drawOverObjects)
            {
                _fbdBoxRenderer.sharedMaterial.renderQueue = 4000;
                _fbdBoxRenderer.sharedMaterial.SetInt("_ZTest", 8);
            }
            else
            {
                _fbdBoxRenderer.sharedMaterial.renderQueue = _arrowMaterial != null ? _arrowMaterial.renderQueue : 2000;
                _fbdBoxRenderer.sharedMaterial.SetInt("_ZTest", 4);
            }
        }
    }

    private void SyncArrowSettings(Arrow2D arrow)
    {
        if (arrow == null) return;
        arrow.screenSizeMultiplier = screenProportionMultiplier;
        arrow.drawOverObjects = drawOverObjects;
    }


    private Vector3 _forceNet; // Add this to your trackable vectors at the top!

    private void FixedUpdate()
    {
        if (_rb == null || _massData == null) return;

        if (globalArrowScale <= 0.0001f)
        {
            HideAllArrows();
            return;
        }

        _timeSinceLastContact += Time.fixedDeltaTime;

        // STEP 1: Calculate the pure math for all forces (No drawing yet)
        CalculateAllForces(false);

        // STEP 2: Find the absolute largest force magnitude currently active
        float maxMag = 0f;
        if (showGravity) maxMag = Mathf.Max(maxMag, _forceGravity.magnitude);
        if (showNormal) maxMag = Mathf.Max(maxMag, _forceNormal.magnitude);
        if (showFriction) maxMag = Mathf.Max(maxMag, _forceFriction.magnitude);
        if (showDrag) maxMag = Mathf.Max(maxMag, _forceDrag.magnitude);
        if (showNetForce) maxMag = Mathf.Max(maxMag, _forceNet.magnitude);

        // STEP 3: Calculate Proportional Scale
        float currentScale = globalArrowScale;

        // If the biggest force scaled up exceeds our max arrow length...
        if (maxMag * globalArrowScale > maxArrowLength)
        {
            // We scale EVERY arrow down proportionally so the biggest one perfectly fits the max length.
            // This preserves the exact visual ratios between all vectors!
            currentScale = maxArrowLength / maxMag;
        }

        // STEP 4: Draw all arrows using the exact same scale factor
        DrawArrow(_gravityArrow, showGravity, _forceGravity, gravityColor, currentScale);
        DrawArrow(_normalArrow, showNormal, _forceNormal, normalColor, currentScale);
        DrawArrow(_frictionArrow, showFriction, _forceFriction, frictionColor, currentScale);
        DrawArrow(_dragArrow, showDrag, _forceDrag, dragColor, currentScale);
        DrawArrow(_netForceArrow, showNetForce, _forceNet, netForceColor, currentScale);
    }

    private void CalculateAllForces(bool forceSnap)
    {
        float weight = _massData.TotalMass * Physics.gravity.magnitude;
        float deadzone = weight * thresholdPercent;

        // 1. GRAVITY
        _forceGravity = (weight > deadzone) ? Physics.gravity.normalized * weight : Vector3.zero;

        // 2. DRAG
        float speed = _rb.velocity.magnitude;
        _forceDrag = (speed > 0.1f) ? -_rb.velocity.normalized * (speed * dragSensitivity) : Vector3.zero;

        // 3. NORMAL & FRICTION
        _forceNormal = Vector3.zero;
        _forceFriction = Vector3.zero;

        RaycastHit hit;
        float rayDist = transform.lossyScale.y * 0.6f;
        bool rayHit = Physics.Raycast(transform.position, Physics.gravity.normalized, out hit, rayDist);
        bool isTouching = rayHit || _isColliding || (_timeSinceLastContact < CONTACT_BUFFER_TIME);

        if (isTouching)
        {
            float m = _massData.TotalMass;
            float g = Physics.gravity.magnitude;
            Vector3 surfaceNormal = rayHit ? hit.normal : _lastContactNormal;

            float angle = Vector3.Angle(-Physics.gravity.normalized, surfaceNormal);
            float normalMag = m * g * Mathf.Cos(angle * Mathf.Deg2Rad);

            if (normalMag > deadzone)
            {
                _forceNormal = surfaceNormal * normalMag;
            }

            Vector3 planarVelocity = Vector3.ProjectOnPlane(_rb.velocity, surfaceNormal);
            float velocityMag = planarVelocity.magnitude;

            if (velocityMag > 0.05f)
            {
                if (forceSnap) _smoothedVelocityDir = planarVelocity.normalized;
                else _smoothedVelocityDir = Vector3.Slerp(_smoothedVelocityDir, planarVelocity.normalized, frictionSmoothing);

                _forceFriction = -_smoothedVelocityDir * (velocityMag * 2f);
            }
            else
            {
                _smoothedVelocityDir = Vector3.zero;
            }
        }

        // 4. NET FORCE
        _forceNet = _forceGravity + _forceNormal + _forceFriction + _forceDrag;
    }

    private void DrawArrow(Arrow2D arrow, bool isEnabled, Vector3 forceVector, Color color, float scaleFactor, bool forceSnap = false)
    {
        // Only draw if the toggle is checked AND the vector actually has magnitude
        if (isEnabled && forceVector.magnitude > 0.001f)
        {
            arrow.gameObject.SetActive(true);
            float length = forceVector.magnitude * scaleFactor;
            arrow.UpdateData(length, forceVector.normalized, color, forceSnap);
        }
        else
        {
            arrow.gameObject.SetActive(false);
        }
    }

    private void HideAllArrows()
    {
        _gravityArrow.gameObject.SetActive(false);
        _normalArrow.gameObject.SetActive(false);
        _frictionArrow.gameObject.SetActive(false);
        _dragArrow.gameObject.SetActive(false);
        _netForceArrow.gameObject.SetActive(false);
    }


    // ==============================================
    // NEW IMPLEMENTATION: UPDATE NET FORCE
    // ==============================================
    private void UpdateNetForce(bool forceSnap = false)
    {
        if (!showNetForce)
        {
            if (_netForceArrow != null) _netForceArrow.gameObject.SetActive(false);
            return;
        }

        // Calculate the vector sum of all currently active visual forces
        Vector3 totalForce = _forceGravity + _forceNormal + _forceFriction + _forceDrag;

        if (totalForce.magnitude > 0.001f) // Small deadzone to prevent jitter
        {
            _netForceArrow.gameObject.SetActive(true);

            // Scale and clamp the net force arrow exactly like the others
            float length = Mathf.Min(totalForce.magnitude * globalArrowScale, maxArrowLength);
            _netForceArrow.UpdateData(length, totalForce.normalized, netForceColor, forceSnap);
        }
        else
        {
            _netForceArrow.gameObject.SetActive(false);
        }
    }

    private Color GetContrastBackgroundColor(Color textColor)
    {
        // Standard relative luminance formula (weights based on human eye sensitivity)
        float luminance = (0.299f * textColor.r) + (0.587f * textColor.g) + (0.114f * textColor.b);

        // If the color is visually bright, use a black background. Otherwise, use white.
        return luminance > 0.5f ? Color.black : Color.white;
    }

    private void UpdateGravity(float weight, float deadzone, bool forceSnap = false)
    {
        bool active = showGravity && weight > deadzone;
        _gravityArrow.gameObject.SetActive(active);

        if (active)
        {
            _forceGravity = Physics.gravity.normalized * weight;
            float length = Mathf.Min(_forceGravity.magnitude * globalArrowScale, maxArrowLength);
            _gravityArrow.UpdateData(length, Physics.gravity.normalized, gravityColor, forceSnap);
        }
        else
        {
            _forceGravity = Vector3.zero;
        }
    }

    private void UpdateDrag(bool forceSnap = false)
    {
        float speed = _rb.velocity.magnitude;
        bool active = showDrag && speed > 0.1f;
        _dragArrow.gameObject.SetActive(active);

        if (active)
        {
            float dragMag = speed * dragSensitivity;
            _forceDrag = -_rb.velocity.normalized * dragMag;

            float length = Mathf.Min(dragMag * globalArrowScale, maxArrowLength);
            _dragArrow.UpdateData(length, -_rb.velocity.normalized, dragColor, forceSnap);
        }
        else
        {
            _forceDrag = Vector3.zero;
        }
    }

    private void UpdateNormalAndFriction(float weight, float deadzone, bool forceSnap = false)
    {
        RaycastHit hit;
        float rayDist = transform.lossyScale.y * 0.6f;

        bool rayHit = Physics.Raycast(transform.position, Physics.gravity.normalized, out hit, rayDist);
        bool isTouching = rayHit || _isColliding || (_timeSinceLastContact < CONTACT_BUFFER_TIME);

        if (isTouching)
        {
            float m = _massData.TotalMass;
            float g = Physics.gravity.magnitude;
            Vector3 surfaceNormal = rayHit ? hit.normal : _lastContactNormal;

            // 1. NORMAL FORCE
            float angle = Vector3.Angle(-Physics.gravity.normalized, surfaceNormal);
            float normalMag = m * g * Mathf.Cos(angle * Mathf.Deg2Rad);

            if (showNormal && normalMag > deadzone)
            {
                _forceNormal = surfaceNormal * normalMag;
                _normalArrow.gameObject.SetActive(true);
                float length = Mathf.Min(normalMag * globalArrowScale, maxArrowLength);
                _normalArrow.UpdateData(length, surfaceNormal, normalColor, forceSnap);
            }
            else
            {
                _forceNormal = Vector3.zero;
                _normalArrow.gameObject.SetActive(false);
            }

            // 2. FRICTION FORCE
            Vector3 planarVelocity = Vector3.ProjectOnPlane(_rb.velocity, surfaceNormal);
            float velocityMag = planarVelocity.magnitude;

            if (showFriction && velocityMag > 0.05f)
            {
                if (forceSnap) _smoothedVelocityDir = planarVelocity.normalized;
                else _smoothedVelocityDir = Vector3.Slerp(_smoothedVelocityDir, planarVelocity.normalized, frictionSmoothing);

                float frictionMag = velocityMag * 2f;
                _forceFriction = -_smoothedVelocityDir * frictionMag;

                _frictionArrow.gameObject.SetActive(true);
                float length = Mathf.Min(frictionMag * globalArrowScale, maxArrowLength);
                _frictionArrow.UpdateData(length, -_smoothedVelocityDir, frictionColor, forceSnap);
            }
            else
            {
                _forceFriction = Vector3.zero;
                _frictionArrow.gameObject.SetActive(false);
                _smoothedVelocityDir = Vector3.zero;
            }
        }
        else
        {
            _forceNormal = Vector3.zero;
            _forceFriction = Vector3.zero;

            _normalArrow.gameObject.SetActive(false);
            _frictionArrow.gameObject.SetActive(false);
            _smoothedVelocityDir = Vector3.zero;
        }
    }

    private void InitializeArrows()
    {
        _gravityArrow = CreateArrowResource("FBD_Gravity", gravityColor, gravityLabel);
        _normalArrow = CreateArrowResource("FBD_Normal", normalColor, normalLabel);
        _frictionArrow = CreateArrowResource("FBD_Friction", frictionColor, frictionLabel);
        _dragArrow = CreateArrowResource("FBD_Drag", dragColor, dragLabel);

        // Added Net Force Instantiation
        _netForceArrow = CreateArrowResource("FBD_NetForce", netForceColor, netForceLabel);
    }

    private Arrow2D CreateArrowResource(string arrowName, Color arrowColor, string labelStr)
    {
        Transform existing = transform.Find(arrowName);
        if (existing != null) return existing.GetComponent<Arrow2D>();

        GameObject go = new GameObject(arrowName);
        go.transform.SetParent(this.transform);
        go.transform.localPosition = Vector3.zero;

        Arrow2D arrow = go.AddComponent<Arrow2D>();
        arrow.autoUpdateFromReference = false;
        arrow.maintainScreenSize = true;
        arrow.screenSizeMultiplier = screenProportionMultiplier;
        arrow.drawOverObjects = drawOverObjects;
        arrow.labelText = labelStr;
        arrow.labelColor = arrowColor;

        arrow.labelBgColor = GetContrastBackgroundColor(arrowColor);

        var field = typeof(Arrow2D).GetField("_baseMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(arrow, _arrowMaterial);

        return arrow;
    }

    private void InitializeFBDBox()
    {
        GameObject boxObj = new GameObject("FBD_CenterBox");
        boxObj.transform.SetParent(this.transform);
        boxObj.transform.localPosition = Vector3.zero;

        _fbdBoxRenderer = boxObj.AddComponent<LineRenderer>();
        _fbdBoxRenderer.useWorldSpace = true;
        _fbdBoxRenderer.loop = true;
        _fbdBoxRenderer.positionCount = 4;
        _fbdBoxRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _fbdBoxRenderer.receiveShadows = false;

        if (_arrowMaterial != null)
        {
            Material overlayMat = new Material(_arrowMaterial);
            overlayMat.name = _arrowMaterial.name + " (Box Instance)";
            _fbdBoxRenderer.sharedMaterial = overlayMat;
        }
    }

    private void LateUpdate()
    {
        if (_fbdBoxRenderer == null || !showFBDBox || globalArrowScale <= 0.0001f || _mainCamera == null)
        {
            if (_fbdBoxRenderer != null) _fbdBoxRenderer.enabled = false;
            return;
        }

        _fbdBoxRenderer.enabled = true;

        float zoomFactor = 1f;
        if (_mainCamera.orthographic)
        {
            zoomFactor = _mainCamera.orthographicSize * screenProportionMultiplier;
        }
        else
        {
            Plane cameraPlane = new Plane(_mainCamera.transform.forward, _mainCamera.transform.position);
            float distance = cameraPlane.GetDistanceToPoint(transform.position);
            float fovMultiplier = Mathf.Tan(_mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f;
            zoomFactor = distance * fovMultiplier * screenProportionMultiplier;
        }

        float scaledSize = fbdBoxSize * zoomFactor;
        float scaledThickness = fbdBoxThickness * zoomFactor;

        _fbdBoxRenderer.startWidth = scaledThickness;
        _fbdBoxRenderer.endWidth = scaledThickness;
        _fbdBoxRenderer.startColor = fbdBoxColor;
        _fbdBoxRenderer.endColor = fbdBoxColor;

        Vector3 center = transform.position;
        Vector3 right = _mainCamera.transform.right * (scaledSize * 0.5f);
        Vector3 up = _mainCamera.transform.up * (scaledSize * 0.5f);

        _fbdBoxRenderer.SetPosition(0, center + up - right);
        _fbdBoxRenderer.SetPosition(1, center + up + right);
        _fbdBoxRenderer.SetPosition(2, center - up + right);
        _fbdBoxRenderer.SetPosition(3, center - up - right);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.contactCount > 0)
        {
            _isColliding = true;
            _lastContactNormal = collision.GetContact(0).normal;
            _timeSinceLastContact = 0f;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        _isColliding = false;
    }
}

