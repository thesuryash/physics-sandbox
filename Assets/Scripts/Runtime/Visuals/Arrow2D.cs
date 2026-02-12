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
    public Color color = Color.red;
    [SerializeField] private Material _baseMaterial;

    [Header("External Reference")]
    public float scaleFactor = 0.5f;
    public bool autoUpdateFromReference = true;
    public enum ForceSource { Manual, Velocity, Gravity, NormalForce, Friction }
    public ForceSource source = ForceSource.Manual;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;
    private Rigidbody _rb;

    // --- Life Cycle ---

    private void Reset()
    {
        // Safe setup for new components
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

    private void Start() => BuildArrow();

    private void Update()
    {
        if (!Application.isPlaying || !autoUpdateFromReference) return;

        if (_rb == null) _rb = GetComponentInParent<Rigidbody>();
        if (_rb == null) return;

        float magnitude = 0f;

        switch (source)
        {
            case ForceSource.Velocity:
                magnitude = _rb.velocity.magnitude;
                if (magnitude > 0.01f) transform.up = _rb.velocity.normalized;
                break;

            case ForceSource.Gravity:
                magnitude = _rb.mass * Physics.gravity.magnitude;
                transform.up = Physics.gravity.normalized;
                break;

                // Note: Normal/Friction are calculated by the FBD script and pushed via SetData
        }

        length = magnitude * scaleFactor;
        BuildArrow();
    }

    // --- Core Logic ---

    public void SetData(float magnitude, Color newColor)
    {
        length = magnitude;
        color = newColor;
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

        // Apply base material if assigned
        if (_baseMaterial != null && _meshRenderer.sharedMaterial != _baseMaterial)
        {
            _meshRenderer.sharedMaterial = _baseMaterial;
        }

        // Color update via PropertyBlock (No Material Leaking)
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