using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Floor : MonoBehaviour
{
    [Header("Surface Properties")]
    [Tooltip("How slippery is the floor? (0 = Ice, 1 = Sandpaper)")]
    [Range(0f, 1f)] public float friction = 0.6f;

    [Tooltip("How bouncy is the floor? (0 = No bounce, 1 = Superball)")]
    [Range(0f, 1f)] public float bounciness = 0.0f;

    private PhysicMaterial _floorMaterial;
    private Collider _collider;

    void Awake()
    {
        _collider = GetComponent<Collider>();
        InitializeMaterial();
    }

    void OnValidate()
    {
        // Allows you to tweak values in the Inspector and see results immediately
        if (_floorMaterial != null) UpdateMaterialProperties();
    }

    private void InitializeMaterial()
    {
        // Create a new Physics Material in memory (not saved to disk)
        _floorMaterial = new PhysicMaterial("RuntimeFloorMat");
        _collider.material = _floorMaterial;
        UpdateMaterialProperties();
    }

    private void UpdateMaterialProperties()
    {
        _floorMaterial.dynamicFriction = friction;
        _floorMaterial.staticFriction = friction;
        _floorMaterial.bounciness = bounciness;

        // Ensure the physics engine mixes these values correctly
        _floorMaterial.frictionCombine = PhysicMaterialCombine.Average;
        _floorMaterial.bounceCombine = PhysicMaterialCombine.Average;
    }
}