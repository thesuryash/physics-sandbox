using UnityEngine;

//[RequireComponent(typeof(PhysicsBody))]
public class Mass : MonoBehaviour
{
    [Header("Flexible DNA")]
    public MassDefinition definition = new MassDefinition();

    [Header("Geometry & State")]
    [Tooltip("Drag the MeshFilter here to calculate accurate volume. If left empty, it assumes a 1x1x1 cube.")]
    public MeshFilter targetMesh;

    [SerializeField] private float _totalMass;
    [SerializeField] private float _temperature = 293f;
    [SerializeField] private float _netCharge = 0f;

    [Header("Visualizations")]
    [Tooltip("The Free Body Diagram controller dedicated to this mass.")]
    public FreeBodyDiagram fbd; // Note: Change 'FreeBodyDiagram' to whatever your actual FBD script is named!

    private PhysicsBody _rb;

    // Getters for external systems
    public float TotalMass => _totalMass;
    public float Temperature => _temperature;
    public float Charge => _netCharge;

    [Header("Visualizations")]
    [Tooltip("Drag a 3D model (Prefab or GLB) here to automatically attach it as the visual mesh.")]
    public GameObject visualModel;

    //void Awake()
    //{
    //    _rb = GetComponent<Rigidbody>();

    //    // --- NEW: AUTO-GENERATE FBD ---
    //    // Look for an FBD. If it doesn't exist, create one instantly!
    //    fbd = GetComponent<FreeBodyDiagram>();
    //    if (fbd == null)
    //    {
    //        fbd = gameObject.AddComponent<FreeBodyDiagram>();
    //    }
    //    // ------------------------------

    //    UpdatePhysicalProperties();
    //}

    void Awake()
    {
        _rb = GetComponent<PhysicsBody>();

        // --- NEW: AUTO-HEAL MANUAL MESH COLLIDERS ---
        // If you manually attach this to a GLB, it fixes the "None (Mesh)" bug instantly!
        MeshCollider mc = GetComponent<MeshCollider>();
        if (mc != null && mc.sharedMesh == null)
        {
            // GLBs usually hide their MeshFilter on a child object
            MeshFilter childMesh = GetComponentInChildren<MeshFilter>();
            if (childMesh != null) mc.sharedMesh = childMesh.sharedMesh;
        }
        // --------------------------------------------

        // Look for an FBD. If it doesn't exist, create one instantly!
        fbd = GetComponent<FreeBodyDiagram>();
        if (fbd == null)
        {
            fbd = gameObject.AddComponent<FreeBodyDiagram>();
        }

        UpdatePhysicalProperties();

        PhysicsBody pb = GetComponent<PhysicsBody>();
        if (pb == null) pb = gameObject.AddComponent<PhysicsBody>();
    }

    public void UpdatePhysicalProperties()
    {
        // 1. Calculate Accurate Volume (The Bulletproof Method)
        float volume = 1f;

        // Grab all renderers on this object AND any child objects (fixes the Empty Root problem)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length > 0)
        {
            // Create a bounding box based on the first renderer
            Bounds totalBounds = renderers[0].bounds;

            // Expand the bounding box to encapsulate every other child mesh
            for (int i = 1; i < renderers.Length; i++)
            {
                totalBounds.Encapsulate(renderers[i].bounds);
            }

            // totalBounds.size is already scaled to world space, so no need to multiply by lossyScale!
            volume = totalBounds.size.x * totalBounds.size.y * totalBounds.size.z;

            // Safety catch for perfectly flat objects (like a 2D plane) so volume isn't 0
            if (volume == 0) volume = 0.01f;
        }
        else
        {
            // Fallback to basic scale if no meshes exist at all
            volume = transform.lossyScale.x * transform.lossyScale.y * transform.lossyScale.z;
        }

        // 2. Sync Mass (Density * Volume)
        _totalMass = volume * definition.density;

        if (_rb == null) _rb = GetComponent<PhysicsBody>();
        if (_rb != null) _rb.mass = _totalMass;

        // 3. Fetch baseline material data from your EnvironmentManager
        if (EnvironmentManager.Instance != null)
        {
            MaterialData matData = EnvironmentManager.Instance.GetMaterialData(definition.materialID);

            if (matData != null)
            {
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    PhysicMaterial pMat = new PhysicMaterial(matData.id);
                    pMat.staticFriction = matData.staticFriction;
                    pMat.dynamicFriction = matData.dynamicFriction;
                    pMat.bounciness = matData.bounciness;

                    pMat.frictionCombine = PhysicMaterialCombine.Multiply;
                    pMat.bounceCombine = PhysicMaterialCombine.Multiply;

                    col.material = pMat;
                }

                // Apply color to ALL nested meshes so the whole object tints correctly
                Color newColor = EnvironmentManager.Instance.GetMaterialColor(definition.materialID);
                foreach (var rend in renderers)
                {
                    rend.material.color = newColor;
                }
            }
        }
    }

    // --- Sandbox Interaction Methods ---

    public void ChangeDensity(float newDensity)
    {
        definition.SetDensity(newDensity);
        UpdatePhysicalProperties();
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