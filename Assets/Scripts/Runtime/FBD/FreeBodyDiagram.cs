using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Mass))]
public class FreeBodyDiagram : MonoBehaviour
{
    public enum ArrowMode { Mode2D, Mode3D }

    [Header("Visualization Settings")]
    public ArrowMode visualizationMode = ArrowMode.Mode2D;
    public float globalArrowScale = 0.2f;
    public float maxArrowLength = 3f;

    [Range(0.001f, 0.2f)]
    public float screenProportionMultiplier = 0.05f;

    [Header("Overlay Settings")]
    [Tooltip("If true, the FBD arrows and box will render on top of all 3D objects.")]
    public bool drawOverObjects = true;

    [Space(10)]
    [Range(0.001f, 0.1f)] public float thresholdPercent = 0.02f;
    public bool showGravity = true;
    public bool showNormal = true;
    public bool showFriction = true;

    [Header("FBD Box Representation")]
    public bool showFBDBox = true;
    public float fbdBoxSize = 1.5f;
    public float fbdBoxThickness = 0.05f;
    public Color fbdBoxColor = Color.white;

    [Header("Stabilization")]
    [Range(0.01f, 0.5f)] public float frictionSmoothing = 0.1f;

    // --- NEW: Added Label Fields alongside the Colors ---
    [Header("Labels & Colors")]
    public string gravityLabel = "Fg";
    public Color gravityColor = Color.red;
    [Space(5)]
    public string normalLabel = "Fn";
    public Color normalColor = Color.yellow;
    [Space(5)]
    public string frictionLabel = "Ff";
    public Color frictionColor = Color.blue;

    [Space(10)]
    [SerializeField] private Material _arrowMaterial;

    private Rigidbody _rb;
    private Mass _massData;
    private Camera _mainCamera;

    private Arrow2D _gravityArrow, _normalArrow, _frictionArrow;
    private LineRenderer _fbdBoxRenderer;

    private Vector3 _smoothedVelocityDir = Vector3.zero;

    private bool _isColliding = false;
    private Vector3 _lastContactNormal = Vector3.up;
    private float _timeSinceLastContact = 999f;
    private const float CONTACT_BUFFER_TIME = 0.1f;



    private void Awake()
    {
        // --- THE FIX: Force an initial calculation for when the game starts paused ---
        if (_massData != null && _rb != null)
        {
            float weight = _massData.TotalMass * Physics.gravity.magnitude;
            float deadzone = weight * thresholdPercent;

            UpdateGravity(weight, deadzone);
            UpdateNormalAndFriction(weight, deadzone);
        }
    }

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _massData = GetComponent<Mass>();
        _mainCamera = Camera.main;

        InitializeArrows();
        InitializeFBDBox();

        // Force an initial calculation for when the game starts paused
        if (_massData != null && _rb != null)
        {
            float weight = _massData.TotalMass * Physics.gravity.magnitude;
            float deadzone = weight * thresholdPercent;

            // --- THE FIX: Pass 'true' to forceSnap on frame one ---
            UpdateGravity(weight, deadzone, true);
            UpdateNormalAndFriction(weight, deadzone, true);
        }
    }

    // Added 'forceSnap' parameter
    private void UpdateGravity(float weight, float deadzone, bool forceSnap = false)
    {
        bool active = showGravity && weight > deadzone;
        _gravityArrow.gameObject.SetActive(active);

        if (active)
        {
            float length = Mathf.Min(weight * globalArrowScale, maxArrowLength);
            _gravityArrow.UpdateData(length, Physics.gravity.normalized, gravityColor, forceSnap);
        }
    }

    // Added 'forceSnap' parameter
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
                _normalArrow.gameObject.SetActive(true);
                float length = Mathf.Min(normalMag * globalArrowScale, maxArrowLength);
                _normalArrow.UpdateData(length, surfaceNormal, normalColor, forceSnap);
            }
            else { _normalArrow.gameObject.SetActive(false); }

            // 2. FRICTION FORCE (Planar)
            Vector3 planarVelocity = Vector3.ProjectOnPlane(_rb.velocity, surfaceNormal);
            float velocityMag = planarVelocity.magnitude;

            bool wasActive = _frictionArrow.gameObject.activeSelf;
            float threshold = wasActive ? 0.05f : 0.15f;

            if (showFriction && velocityMag > threshold)
            {
                _frictionArrow.gameObject.SetActive(true);

                // If snapping, bypass Slerp entirely
                if (forceSnap) _smoothedVelocityDir = planarVelocity.normalized;
                else _smoothedVelocityDir = Vector3.Slerp(_smoothedVelocityDir, planarVelocity.normalized, frictionSmoothing);

                float length = Mathf.Min(velocityMag * globalArrowScale * 2f, maxArrowLength);
                _frictionArrow.UpdateData(length, -_smoothedVelocityDir, frictionColor, forceSnap);
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
            _smoothedVelocityDir = Vector3.zero;
        }
    }

    private void InitializeArrows()
    {
        // --- NEW: Pass the labels down to the creation method ---
        _gravityArrow = CreateArrowResource("FBD_Gravity", gravityColor, gravityLabel);
        _normalArrow = CreateArrowResource("FBD_Normal", normalColor, normalLabel);
        _frictionArrow = CreateArrowResource("FBD_Friction", frictionColor, frictionLabel);
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

    private void Update()
    {
        // Sync the master sliders and checkboxes to the arrows every frame
        if (_gravityArrow != null)
        {
            _gravityArrow.screenSizeMultiplier = screenProportionMultiplier;
            _gravityArrow.drawOverObjects = drawOverObjects;
        }
        if (_normalArrow != null)
        {
            _normalArrow.screenSizeMultiplier = screenProportionMultiplier;
            _normalArrow.drawOverObjects = drawOverObjects;
        }
        if (_frictionArrow != null)
        {
            _frictionArrow.screenSizeMultiplier = screenProportionMultiplier;
            _frictionArrow.drawOverObjects = drawOverObjects;
        }

        // Sync the Box LineRenderer material
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

    private void FixedUpdate()
    {
        if (_rb == null || _massData == null) return;

        if (globalArrowScale <= 0.0001f)
        {
            if (_gravityArrow.gameObject.activeSelf) _gravityArrow.gameObject.SetActive(false);
            if (_normalArrow.gameObject.activeSelf) _normalArrow.gameObject.SetActive(false);
            if (_frictionArrow.gameObject.activeSelf) _frictionArrow.gameObject.SetActive(false);
            return;
        }

        _timeSinceLastContact += Time.fixedDeltaTime;

        float weight = _massData.TotalMass * Physics.gravity.magnitude;
        float deadzone = weight * thresholdPercent;

        UpdateGravity(weight, deadzone);
        UpdateNormalAndFriction(weight, deadzone);
    }

    private void LateUpdate()
    {
        if (_fbdBoxRenderer == null) return;

        if (!showFBDBox || globalArrowScale <= 0.0001f || _mainCamera == null)
        {
            _fbdBoxRenderer.enabled = false;
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

    private void UpdateGravity(float weight, float deadzone)
    {
        bool active = showGravity && weight > deadzone;
        _gravityArrow.gameObject.SetActive(active);

        if (active)
        {
            float length = Mathf.Min(weight * globalArrowScale, maxArrowLength);
            _gravityArrow.UpdateData(length, Physics.gravity.normalized, gravityColor);
        }
    }

    private void UpdateNormalAndFriction(float weight, float deadzone)
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
                _normalArrow.gameObject.SetActive(true);
                float length = Mathf.Min(normalMag * globalArrowScale, maxArrowLength);
                _normalArrow.UpdateData(length, surfaceNormal, normalColor);
            }
            else { _normalArrow.gameObject.SetActive(false); }

            // 2. FRICTION FORCE (Planar)
            Vector3 planarVelocity = Vector3.ProjectOnPlane(_rb.velocity, surfaceNormal);
            float velocityMag = planarVelocity.magnitude;

            bool wasActive = _frictionArrow.gameObject.activeSelf;
            float threshold = wasActive ? 0.05f : 0.15f;

            if (showFriction && velocityMag > threshold)
            {
                _frictionArrow.gameObject.SetActive(true);
                _smoothedVelocityDir = Vector3.Slerp(_smoothedVelocityDir, planarVelocity.normalized, frictionSmoothing);
                float length = Mathf.Min(velocityMag * globalArrowScale * 2f, maxArrowLength);
                _frictionArrow.UpdateData(length, -_smoothedVelocityDir, frictionColor);
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
            _smoothedVelocityDir = Vector3.zero;
        }
    }

    // --- NEW: Added 'labelStr' parameter to inject the text ---
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

        // Push the label settings down to the Arrow2D script
        arrow.labelText = labelStr;
        arrow.labelColor = arrowColor;

        var field = typeof(Arrow2D).GetField("_baseMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(arrow, _arrowMaterial);

        return arrow;
    }
}