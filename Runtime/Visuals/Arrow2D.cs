using UnityEngine;
using TMPro;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Arrow2D : MonoBehaviour
{
    [Header("Zoom Overlay (Constant Screen Size)")]
    public bool maintainScreenSize = true;
    [Range(0.001f, 0.1f)] public float screenSizeMultiplier = 0.05f;

    [Header("Overlay Settings")]
    public bool drawOverObjects = true;
    private bool _prevDrawOverObjects = false; // Tracks changes for real-time toggling

    [Header("Label")]
    public string labelText = "";
    public float labelSize = 2f;
    [Tooltip("How far past the arrowhead the text should float.")]
    public float labelOffset = 0.5f;
    public Color labelColor = Color.white;

    [Header("Dimensions")]
    public float length = 2f;
    public float shaftWidth = 0.1f;
    public float headSize = 0.3f;
    public float headWidth = 0.4f;

    [Header("Visuals")]
    [Tooltip("The base color for the arrow.")]
    public Color arrowColor = Color.cyan;
    [SerializeField] private Material _baseMaterial;

    [Header("Stabilization")]
    [Range(0f, 1f)] public float stabilization = 0.5f;

    [Header("External Reference")]
    public float scaleFactor = 0.5f;
    public bool autoUpdateFromReference = true;
    public enum ForceSource { Manual, Velocity, Gravity, NormalForce, Friction }
    public ForceSource source = ForceSource.Manual;
    public Color labelBgColor = Color.black;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;
    private PhysicsBody _rb;
    private Camera _mainCamera;

    private TextMeshPro _textMesh;
    private bool _labelNeedsUpdate = true;

    private float _smoothedLength;
    private Vector3 _smoothedUp;

    private void Reset()
    {
        length = 2f;
        shaftWidth = 0.1f;
        headSize = 0.3f;
        headWidth = 0.4f;
        var renderer = GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void OnEnable() => BuildArrow();

    private void OnValidate()
    {
        if (gameObject.activeInHierarchy)
        {
            BuildArrow();
            _labelNeedsUpdate = true;
        }
    }

    //private void Start()
    //{
    //    _smoothedLength = length;
    //    _smoothedUp = transform.up;
    //    _mainCamera = Camera.main;
    //    BuildArrow();
    //    _labelNeedsUpdate = true;

    //    // Force an overlay update on frame 1
    //    _prevDrawOverObjects = !drawOverObjects;
    //}

    private void Start()
    {
        // --- THE FIX: Only grab transform.up if the FBD script hasn't already set our direction! ---
        if (_smoothedUp == Vector3.zero)
        {
            _smoothedUp = transform.up;
        }

        _smoothedLength = length;
        _mainCamera = Camera.main;
        BuildArrow();
        _labelNeedsUpdate = true;

        // Force an overlay update on frame 1
        _prevDrawOverObjects = !drawOverObjects;
    }

    private void Update()
    {
        if (!Application.isPlaying || !autoUpdateFromReference) return;

        if (_rb == null) _rb = GetComponentInParent<PhysicsBody>();
        if (_rb == null) return;

        float targetMag = 0f;
        Vector3 targetDir = transform.up;

        switch (source)
        {
            case ForceSource.Velocity:
                targetMag = _rb.velocity.magnitude;
                if (targetMag > 0.01f) targetDir = _rb.velocity.normalized;
                break;

            case ForceSource.Gravity:
                targetMag = _rb.mass * Physics.gravity.magnitude;
                targetDir = Physics.gravity.normalized;
                break;
        }

        ApplyStabilizedData(targetMag * scaleFactor, targetDir);
    }

    private void LateUpdate()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;

        // --- Toggle Material State (X-Ray Mode) ---
        if (drawOverObjects != _prevDrawOverObjects)
        {
            _prevDrawOverObjects = drawOverObjects;
            UpdateOverlayState();
        }

        if (_labelNeedsUpdate)
        {
            SetupLabelProps();
            _labelNeedsUpdate = false;
        }

        // --- Zoom Math ---
        if (maintainScreenSize && _mainCamera != null)
        {
            float zoomFactor = 1f;

            if (_mainCamera.orthographic)
            {
                zoomFactor = _mainCamera.orthographicSize * screenSizeMultiplier;
            }
            else
            {
                Plane cameraPlane = new Plane(_mainCamera.transform.forward, _mainCamera.transform.position);
                float distance = cameraPlane.GetDistanceToPoint(transform.position);

                float fovMultiplier = Mathf.Tan(_mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f;
                zoomFactor = distance * fovMultiplier * screenSizeMultiplier;
            }

            transform.localScale = new Vector3(zoomFactor, zoomFactor, zoomFactor);
        }
        else
        {
            transform.localScale = Vector3.one;
        }

        // --- Dynamic Billboarding for the Arrow Mesh ---
        if (_mainCamera != null && _smoothedUp.sqrMagnitude > 0.001f)
        {
            Vector3 camForward = _mainCamera.transform.forward;

            // We use Vector3.Cross to prevent Unity math errors if you look perfectly straight down the arrow
            if (Vector3.Cross(camForward, _smoothedUp).sqrMagnitude > 0.001f)
            {
                // Lock the Y axis to the force direction (_smoothedUp), and pivot the Z axis to face the camera
                transform.rotation = Quaternion.LookRotation(-camForward, _smoothedUp);
            }
            else
            {
                transform.up = _smoothedUp; // Fallback
            }
        }

        // --- Dynamic Label Positioning & Billboarding ---
        if (_textMesh != null && _textMesh.gameObject.activeSelf)
        {
            if (_mainCamera != null)
            {
                _textMesh.transform.rotation = _mainCamera.transform.rotation;
            }

            float sign = length < 0 ? -1f : 1f;
            _textMesh.transform.localPosition = new Vector3(0, length + (labelOffset * sign), 0);
        }
    }



    public void UpdateData(float magnitude, Vector3 direction, Color newColor, bool forceSnap = false)
    {
        arrowColor = newColor; // Updated to use the single serialized color reference

        // If playing AND we aren't forcing a snap, smooth it out
        if (Application.isPlaying && !forceSnap)
        {
            ApplyStabilizedData(magnitude, direction);
        }
        else
        {
            // Instantly snap the visuals AND the internal smoothing states!
            length = magnitude;
            _smoothedLength = magnitude;
            _smoothedUp = direction.normalized;
            BuildArrow();
        }
    }




    //private void OnGUI()
    //{
    //    if (string.IsNullOrEmpty(labelText)) return;
        
    //    Camera cam = Camera.main;
    //    if (cam == null) return;

    //    // Calculate where the arrow tip is on the 2D screen
    //    // (Adjust 'transform.position' if you want the label at the tip vs the base)
    //    Vector3 screenPos = cam.WorldToScreenPoint(transform.position);

    //    // If the arrow is behind the camera, don't draw the label
    //    if (screenPos.z < 0) return; 

    //    // 1. Setup the text style
    //    GUIStyle style = new GUIStyle(GUI.skin.label);
    //    style.fontStyle = FontStyle.Bold;
    //    style.normal.textColor = labelColor;

    //    // 2. Calculate exactly how big the text is
    //    Vector2 textSize = style.CalcSize(new GUIContent(labelText));

    //    // Center the text box over the position
    //    Rect textRect = new Rect(screenPos.x - (textSize.x / 2), (Screen.height - screenPos.y) - (textSize.y / 2), textSize.x, textSize.y);
        
    //    // Add 4 pixels of padding to the background box so it isn't completely flush with the text
    //    Rect bgRect = new Rect(textRect.x - 4, textRect.y - 2, textRect.width + 8, textRect.height + 4);

    //    // 3. DRAW THE BACKGROUND
    //    GUI.color = labelBgColor; 
    //    GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

    //    // 4. DRAW THE TEXT
    //    GUI.color = Color.white; // Reset master GUI color so text draws normally
    //    GUI.Label(textRect, labelText, style);
    //}





    private void ApplyStabilizedData(float targetLength, Vector3 targetUp)
    {
        float lerpFactor = 1f - stabilization;

        _smoothedLength = Mathf.Lerp(_smoothedLength, targetLength, lerpFactor);
        length = _smoothedLength;

        if (targetUp.sqrMagnitude > 0.001f)
        {
            _smoothedUp = Vector3.Slerp(_smoothedUp, targetUp, lerpFactor).normalized;
        }

        BuildArrow();
    }

    public void BuildArrow()
    {
        if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
        if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshFilter == null || _meshRenderer == null) return;

        if (_mesh == null)
        {
            _mesh = new Mesh { name = "ArrowMesh" };
            _meshFilter.sharedMesh = _mesh;
        }

        if (_baseMaterial != null)
        {
            // Instead of forcing the overlay, we create a safe instance and let UpdateOverlayState manage it
            if (_meshRenderer.sharedMaterial == null || _meshRenderer.sharedMaterial == _baseMaterial)
            {
                Material matInstance = new Material(_baseMaterial);
                matInstance.name = _baseMaterial.name + " (Instance)";
                _meshRenderer.sharedMaterial = matInstance;
            }
            UpdateOverlayState();
        }

        // Apply the serialized 'arrowColor'
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        props.SetColor("_Color", arrowColor);
        props.SetColor("_BaseColor", arrowColor);
        _meshRenderer.SetPropertyBlock(props);

        GenerateMesh();
    }

    // --- NEW: Safely handles swapping back and forth between Normal and Overlay (X-Ray) modes ---
    private void UpdateOverlayState()
    {
        if (_meshRenderer != null && _meshRenderer.sharedMaterial != null && _baseMaterial != null)
        {
            Material mat = _meshRenderer.sharedMaterial;
            if (drawOverObjects)
            {
                mat.renderQueue = 4000;
                mat.SetInt("_ZTest", 8); // Always draw
            }
            else
            {
                mat.renderQueue = _baseMaterial.renderQueue;
                mat.SetInt("_ZTest", 4); // LEqual (Standard depth test)
            }
        }

        // Also update the label's depth settings so the text doesn't clip when the arrow is X-Ray!
        if (_textMesh != null && _textMesh.fontMaterial != null)
        {
            if (drawOverObjects) _textMesh.fontMaterial.SetInt("unity_GUIZTestMode", 8);
            else _textMesh.fontMaterial.SetInt("unity_GUIZTestMode", 4);
        }
    }

    private void GenerateMesh()
    {
        if (_mesh == null) return;
        if (Mathf.Abs(length) < 0.001f) { _mesh.Clear(); return; }

        _mesh.Clear();

        bool isNegative = length < 0;
        float absoluteLength = Mathf.Abs(length);
        float effectiveLength = Mathf.Max(absoluteLength, headSize);
        float shaftLength = Mathf.Max(0, effectiveLength - headSize);
        float sign = isNegative ? -1f : 1f;

        float halfWidth = shaftWidth / 2f;
        float halfHeadWidth = headWidth / 2f;

        Vector3[] vertices = new Vector3[7];
        vertices[0] = new Vector3(-halfWidth, 0, 0);
        vertices[1] = new Vector3(halfWidth, 0, 0);
        vertices[2] = new Vector3(-halfWidth, shaftLength * sign, 0);
        vertices[3] = new Vector3(halfWidth, shaftLength * sign, 0);
        vertices[4] = new Vector3(-halfHeadWidth, shaftLength * sign, 0);
        vertices[5] = new Vector3(halfHeadWidth, shaftLength * sign, 0);
        vertices[6] = new Vector3(0, effectiveLength * sign, 0);

        int[] triangles = !isNegative ?
            new int[] {
                0, 2, 1,   1, 2, 3,   4, 6, 5,
                1, 2, 0,   3, 2, 1,   5, 6, 4
            } :
            new int[] {
                0, 1, 2,   1, 3, 2,   4, 5, 6,
                2, 1, 0,   2, 3, 1,   6, 5, 4
            };

        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }

    private void SetupLabelProps()
    {
        if (string.IsNullOrEmpty(labelText))
        {
            if (_textMesh != null) _textMesh.gameObject.SetActive(false);
            return;
        }

        if (_textMesh == null)
        {
            Transform child = transform.Find("ArrowLabel");
            if (child != null)
            {
                _textMesh = child.GetComponent<TextMeshPro>();
            }
            else
            {
                GameObject labelObj = new GameObject("ArrowLabel");
                labelObj.transform.SetParent(this.transform);

                _textMesh = labelObj.AddComponent<TextMeshPro>();
                _textMesh.alignment = TextAlignmentOptions.Center;
                _textMesh.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        _textMesh.gameObject.SetActive(true);
        _textMesh.text = labelText;
        _textMesh.color = labelColor;
        _textMesh.fontSize = labelSize;
        _textMesh.transform.localScale = Vector3.one;

        // Ensure the label immediately adopts the correct overlay state
        UpdateOverlayState();
    }
}