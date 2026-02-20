using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Arrow2D : MonoBehaviour
{
    [Header("Dimensions")]
    public float length = 2f;
    public float shaftWidth = 0.1f;
    public float headSize = 0.3f;
    public float headWidth = 0.4f;

    [Header("Visuals")]
    public Color color = UnityEngine.Color.red; // Explicitly named to avoid conflicts
    [SerializeField] private Material _baseMaterial;

    [Header("Stabilization")]
    [Range(0f, 1f)] public float stabilization = 0.5f;
    [Tooltip("Smoothing factor for the arrow length and direction.")]

    [Header("External Reference")]
    public float scaleFactor = 0.5f;
    public bool autoUpdateFromReference = true;
    public enum ForceSource { Manual, Velocity, Gravity, NormalForce, Friction }
    public ForceSource source = ForceSource.Manual;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;
    private Rigidbody _rb;

    // Internal smoothing states
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
        if (gameObject.activeInHierarchy) BuildArrow();
    }

    private void Start()
    {
        _smoothedLength = length;
        _smoothedUp = transform.up;
        BuildArrow();
    }

    private void Update()
    {
        if (!Application.isPlaying || !autoUpdateFromReference) return;

        if (_rb == null) _rb = GetComponentInParent<Rigidbody>();
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

    public void SetData(float magnitude, Color newColor)
    {
        color = newColor;

        if (Application.isPlaying)
        {
            ApplyStabilizedData(magnitude, transform.up);
        }
        else
        {
            length = magnitude;
            BuildArrow();
        }
    }

    private void ApplyStabilizedData(float targetLength, Vector3 targetUp)
    {
        // Low-pass filter for stabilization
        float lerpFactor = 1f - stabilization;

        _smoothedLength = Mathf.Lerp(_smoothedLength, targetLength, lerpFactor);
        length = _smoothedLength;

        if (targetUp.sqrMagnitude > 0.001f)
        {
            _smoothedUp = Vector3.Slerp(_smoothedUp, targetUp, lerpFactor);
            transform.up = _smoothedUp;
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

        if (_baseMaterial != null && _meshRenderer.sharedMaterial != _baseMaterial)
        {
            _meshRenderer.sharedMaterial = _baseMaterial;
        }

        MaterialPropertyBlock props = new MaterialPropertyBlock();
        props.SetColor("_Color", color);
        props.SetColor("_BaseColor", color);
        _meshRenderer.SetPropertyBlock(props);

        GenerateMesh();
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
            new int[] { 0, 2, 1, 1, 2, 3, 4, 6, 5 } :
            new int[] { 0, 1, 2, 1, 3, 2, 4, 5, 6 };

        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }
}