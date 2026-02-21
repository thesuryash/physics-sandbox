using System.IO;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshFilter))]
public class RigidBodyDragApplier : MonoBehaviour
{
    private Rigidbody rb;
    private MeshFilter meshFilter;

    [Header("Drag Configuration")]
    public DragSettings settings;
    private DirectionalDragLookup lookup;

    [Header("Storage")]
    [Tooltip("The folder where the binary files will be saved.")]
    public string saveDirectory = "Assets/BakedDragData";

    [SerializeField, HideInInspector]
    private string uniqueObjectId;

    [Header("Debug")]
    [Tooltip("Check this to print the applied drag force to the console.")]
    public bool debugLogForce = false;

    public string BinaryFilePath
    {
        get
        {
            if (string.IsNullOrEmpty(uniqueObjectId)) GenerateNewId();

            string safeName = gameObject.name.Replace(" ", "_");
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(c.ToString(), "");
            }
            return $"{saveDirectory}/{safeName}_{uniqueObjectId}.dat";
        }
    }

    void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueObjectId)) GenerateNewId();
    }

    public void GenerateNewId()
    {
        uniqueObjectId = System.Guid.NewGuid().ToString();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.drag = 0f;

        if (File.Exists(BinaryFilePath))
        {
            lookup = DirectionalDragLookup.LoadFromBinary(BinaryFilePath);
        }
    }

    void FixedUpdate()
    {
        if (lookup == null || rb.velocity.sqrMagnitude < 0.0001f) return;

        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
        Vector3 force = DragForceCalculator.ComputeDrag(localVelocity, rb.velocity, settings, lookup);

        rb.AddForce(force, ForceMode.Force);

        // --- NEW: Print to console if the checkbox is ticked ---
        if (debugLogForce)
        {
            // Prints the object name, the 3D force vector, and the total force magnitude in Newtons
            //Debug.Log($"[{gameObject.name}] Drag Force: {force} | Magnitude: {force.magnitude:F2} N");
            Debug.Log($"[{gameObject.name}] Drag Force: {force} | Magnitude: {force.magnitude:F6} N");
        }
    }

    // --- BAKING METHODS ---
    public void BakeData(bool isComplex)
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) return;

        Directory.CreateDirectory(saveDirectory);

        LookupBuilder builder = new LookupBuilder();
        IAreaEstimator areaEst;
        ICdEstimator cdEst = new HeuristicCdEstimator { baseCd = 0.5f, cavityBoost = 0.8f };

        if (isComplex) areaEst = new RasterSilhouetteAreaEstimator { resolution = 100 };
        else areaEst = new ConvexHullProjectedAreaEstimator();

        DirectionalDragLookup newLookup = builder.Build(meshFilter.sharedMesh, settings, areaEst, cdEst);
        newLookup.SaveToBinary(BinaryFilePath);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    public void ClearBake()
    {
        if (File.Exists(BinaryFilePath))
        {
            File.Delete(BinaryFilePath);
            if (File.Exists(BinaryFilePath + ".meta")) File.Delete(BinaryFilePath + ".meta");

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}