using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Mass : MonoBehaviour
{
    [Header("Flexible DNA")]
    public MassDefinition definition = new MassDefinition();

    [Header("Live State")]
    [SerializeField] private float _totalMass;
    [SerializeField] private float _temperature = 293f;
    [SerializeField] private float _netCharge = 0f;

    private Rigidbody _rb;

    // Getters for external systems
    public float TotalMass => _totalMass;
    public float Temperature => _temperature;
    public float Charge => _netCharge;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        UpdatePhysicalProperties();
    }

    /// <summary>
    /// This is the main "sync" method. Call this after changing 
    /// any values in the definition (like density or bounciness).
    /// </summary>
    public void UpdatePhysicalProperties()
    {
        // 1. Sync Mass
        float volume = transform.lossyScale.x * transform.lossyScale.y * transform.lossyScale.z;
        _totalMass = volume * definition.density;
        _rb.mass = _totalMass;

        // 2. Sync Surface (Friction/Bounce)
        // We update the PhysicMaterial assigned to the collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            // We create a unique instance so we don't change other obects
            PhysicMaterial mat = new PhysicMaterial();
            mat.bounciness = definition.restitution;
            mat.staticFriction = definition.staticFriction;
            mat.dynamicFriction = definition.dynamicFriction;
            col.material = mat;
        }
    }

    // --- Sandbox Interaction Methods ---

    public void ChangeDensity(float newDensity)
    {
        definition.SetDensity(newDensity);
        UpdatePhysicalProperties(); // Refresh the simulation
    }

    public void AddHeat(float joules)
    {
        float deltaT = joules / (_totalMass * definition.specificHeat);
        _temperature += deltaT;
    }

    public void ModifyCharge(float amount)
    {
        _netCharge += amount;
    }
}