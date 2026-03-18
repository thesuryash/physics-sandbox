using UnityEngine;

public class InclinedPlane : MonoBehaviour
{
    [Header("Dimensions")]
    [Range(0f, 89f)] public float angle = 30f;
    [Min(1f)] public float length = 10f;
    [Min(1f)] public float width = 4f;
    public float thickness = 0.5f;

    [Header("Physics")]
    public string materialID = "Wood"; // Default material

    private Transform _pivot;
    private Transform _ramp;
    private EnvironmentSurface _surface;

    private void OnValidate()
    {
        // Updates the shape in real-time while editing in the Inspector
        BuildStructure();
    }

    private void Start()
    {
        BuildStructure();
    }

    public void BuildStructure()
    {
        // 1. Create the Ramp Object if it doesn't exist
        if (_ramp == null)
        {
            // Try to find an existing child named "RampMesh"
            _ramp = transform.Find("RampMesh");

            if (_ramp == null)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "RampMesh";
                cube.transform.SetParent(this.transform);

                // Remove the default PhysicsBody if one exists (we want a static collider)
                DestroyImmediate(cube.GetComponent<PhysicsBody>());

                _ramp = cube.transform;
            }
        }

        // 2. Add Environment Surface logic automatically
        _surface = _ramp.GetComponent<EnvironmentSurface>();
        if (_surface == null) _surface = _ramp.gameObject.AddComponent<EnvironmentSurface>();

        // Sync the material setting
        if (_surface.materialID != materialID)
        {
            _surface.materialID = materialID;
            // Force a visual update if the game is running
            if (Application.isPlaying) _surface.UpdateVisuals();
        }

        // 3. Apply Dimensions (The Pivot Logic)
        // We scale the cube to the desired length
        _ramp.localScale = new Vector3(length, thickness, width);

        // We shift the cube by half its length. 
        // This puts the "left" edge exactly at the parent's origin (0,0,0).
        _ramp.localPosition = new Vector3(length / 2f, -thickness / 2f, 0f);

        // 4. Apply Rotation
        // We rotate the Parent (this object), and the offset child swings up like a drawbridge.
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}