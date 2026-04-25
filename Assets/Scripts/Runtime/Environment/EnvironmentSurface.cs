using UnityEngine;
using System.Linq; 


[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Renderer))]
public class EnvironmentSurface : MonoBehaviour
{
    // This string is what gets saved. The Editor script will fill it via Dropdown.
    [HideInInspector] // Hide the default text box so we can draw our custom dropdown
    public string materialID = "Wood";

    [Tooltip("If true, updates the object color to match the material settings on Start.")]
    public bool updateColorOnStart = true;

    private Collider _myCollider;
    private Renderer _myRenderer;
    [SerializeField] private PhysicsConfig _config;

    void Start()
    {
        _myCollider = GetComponent<Collider>();
        _myRenderer = GetComponent<Renderer>();

        // Create the unique material instance
        _myCollider.material = new PhysicMaterial($"{materialID}_Runtime");

        // --- NEW: Apply Base Stats Immediately ---
        if (EnvironmentManager.Instance != null)
        {
            var data = EnvironmentManager.Instance.GetMaterialData(materialID);
            if (data != null)
            {
                _myCollider.material.staticFriction = data.staticFriction;
                _myCollider.material.dynamicFriction = data.dynamicFriction;
                _myCollider.material.bounciness = data.bounciness;

                // Set Combine Modes to ensure Bounciness works
                _myCollider.material.frictionCombine = PhysicMaterialCombine.Average;
                _myCollider.material.bounceCombine = PhysicMaterialCombine.Maximum;
            }

            UpdateVisuals();
        }
    }

    // Add this new function to EnvironmentManager
    public MaterialData GetMaterialData(string id)
    {
        if (_config == null || _config.materials == null) return null;
        return _config.materials.FirstOrDefault(m => m.id == id);
    }

    public void UpdateVisuals()
    {
        if (EnvironmentManager.Instance == null) return;

        Color c = EnvironmentManager.Instance.GetMaterialColor(materialID);
        if (_myRenderer != null) _myRenderer.material.color = c;
    }

    void OnCollisionEnter(Collision collision)
    {
        var other = collision.gameObject.GetComponent<EnvironmentSurface>();
        string otherID = (other != null) ? other.materialID : "Default";

        if (EnvironmentManager.Instance != null)
        {
            var rule = EnvironmentManager.Instance.GetInteraction(this.materialID, otherID);
            ApplyPhysicsRule(rule);
        }
    }

    private void ApplyPhysicsRule(InteractionData rule)
    {
        if (_myCollider.material == null) return;

        _myCollider.material.staticFriction = rule.staticFriction;
        _myCollider.material.dynamicFriction = rule.dynamicFriction;
        _myCollider.material.bounciness = rule.restitution;
        _myCollider.material.frictionCombine = PhysicMaterialCombine.Average;
        _myCollider.material.bounceCombine = PhysicMaterialCombine.Average;
    }
}