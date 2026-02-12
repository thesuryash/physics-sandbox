using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Mass))]
public class FreeBodyDiagram : MonoBehaviour
{
    [Header("Visualization Settings")]
    public float globalArrowScale = 0.2f;
    public bool showGravity = true;
    public bool showNormal = true;
    public bool showFriction = true;

    [Header("Colors")]
    public Color gravityColor = Color.red;
    public Color normalColor = Color.yellow;
    public Color frictionColor = Color.blue;

    [SerializeField] private Material _arrowMaterial;

    private Rigidbody _rb;
    private Mass _massData; // Direct link to your Mass system

    private Arrow2D _gravityArrow;
    private Arrow2D _normalArrow;
    private Arrow2D _frictionArrow;

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

        UpdateGravity();
        UpdateNormalAndFriction();
    }

    private void UpdateGravity()
    {
        if (!showGravity) { _gravityArrow.gameObject.SetActive(false); return; }

        _gravityArrow.gameObject.SetActive(true);

        // Use the dynamic TotalMass from your Mass class
        float weight = _massData.TotalMass * Physics.gravity.magnitude;

        _gravityArrow.transform.up = Physics.gravity.normalized;
        _gravityArrow.SetData(weight * globalArrowScale, gravityColor);
    }

    private void UpdateNormalAndFriction()
    {
        RaycastHit hit;
        // Search distance slightly larger than the object's radius
        float rayDist = transform.lossyScale.y * 0.6f;
        bool isGrounded = Physics.Raycast(transform.position, Physics.gravity.normalized, out hit, rayDist);

        if (isGrounded)
        {
            float g = Physics.gravity.magnitude;
            float m = _massData.TotalMass;

            // 1. Normal Force (N = m * g * cos(theta))
            _normalArrow.gameObject.SetActive(showNormal);
            float angle = Vector3.Angle(-Physics.gravity.normalized, hit.normal);
            float normalMag = m * g * Mathf.Cos(angle * Mathf.Deg2Rad);

            _normalArrow.transform.up = hit.normal;
            _normalArrow.SetData(normalMag * globalArrowScale, normalColor);

            // 2. Friction Force (Points opposite to velocity)
            if (showFriction && _rb.velocity.magnitude > 0.05f)
            {
                _frictionArrow.gameObject.SetActive(true);

                // Visualizing static/kinetic friction can be complex; 
                // for now, we scale it to show energy loss relative to speed.
                _frictionArrow.transform.up = -_rb.velocity.normalized;
                _frictionArrow.SetData(_rb.velocity.magnitude * globalArrowScale * 2f, frictionColor);
            }
            else { _frictionArrow.gameObject.SetActive(false); }
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
        arrow.color = arrowColor;

        // Reflection to set the private material field in Arrow2D
        var field = typeof(Arrow2D).GetField("_baseMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(arrow, _arrowMaterial);

        return arrow;
    }
}