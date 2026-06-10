using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool _loadOnStart = true;

    // The Lookup Table: Key = "Wood_Ice", Value = InteractionData
    private Dictionary<string, InteractionData> _interactionLookup;
    private PhysicsConfig _config;

    // -- Global Properties for UI Binding --
    public Vector3 Gravity
    {
        get => Physics.gravity;
        set { Physics.gravity = value; }
    }

    public float TimeScale
    {
        get => Time.timeScale;
        set { Time.timeScale = Mathf.Clamp(value, 0f, 10f); }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (_loadOnStart) LoadConfig();
    }

    public void LoadConfig()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "physics_config.json");

        if (!File.Exists(path))
        {
            Debug.LogError($"[EnvironmentManager] Config file not found at: {path}");
            return;
        }

        try
        {
            // 1. Read & Parse
            string json = File.ReadAllText(path);
            _config = JsonUtility.FromJson<PhysicsConfig>(json);

            // 2. Apply Global Settings
            Physics.gravity = _config.globalSettings.gravity;
            Time.timeScale = _config.globalSettings.timeScale;

            // --- FIX: Apply Bounce Threshold ---
            // If JSON has a value > 0, use it. Otherwise, use 0.2 (better than Unity's default 2.0).
            float threshold = _config.globalSettings.bounceThreshold;
            Physics.bounceThreshold = (threshold > 0f) ? threshold : 0.2f;

            // 3. Build Dictionary for Fast Lookup
            _interactionLookup = new Dictionary<string, InteractionData>();

            foreach (var rule in _config.interactions)
            {
                // Store both "A_B" and "B_A" keys so lookup order doesn't matter
                string key1 = $"{rule.materialA}_{rule.materialB}";
                string key2 = $"{rule.materialB}_{rule.materialA}";

                if (!_interactionLookup.ContainsKey(key1)) _interactionLookup[key1] = rule;
                if (!_interactionLookup.ContainsKey(key2)) _interactionLookup[key2] = rule;
            }

            Debug.Log($"[EnvironmentManager] System Ready. Loaded {_interactionLookup.Count / 2} unique interactions.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnvironmentManager] JSON Parse Error: {e.Message}");
        }
    }
    /// <summary>
    /// Returns the specific physics rules for two material IDs.
    /// If no rule exists, returns a default generic interaction.
    /// </summary>
    public InteractionData GetInteraction(string id1, string id2)
    {
        if (_interactionLookup == null) return GetDefaultInteraction();

        string key = $"{id1}_{id2}";
        if (_interactionLookup.TryGetValue(key, out InteractionData data))
        {
            return data;
        }

        // Optional: Checking for "Default" material fallback could go here
        return GetDefaultInteraction();
    }

    // --- ADD THIS FUNCTION ---
    public MaterialData GetMaterialData(string id)
    {
        // Safety check: if config isn't loaded, return nothing
        if (_config == null || _config.materials == null) return null;

        // Find the material definition in the list
        return _config.materials.FirstOrDefault(m => m.id == id);
    }

    private InteractionData GetDefaultInteraction()
    {
        return new InteractionData
        {
            staticFriction = 0.5f,
            dynamicFriction = 0.5f,
            restitution = 0.5f
        };
    }

    // Helper to get Color for a material ID (for visualizing)
    public Color GetMaterialColor(string id)
    {
        if (_config == null) return Color.white;
        var mat = _config.materials.FirstOrDefault(m => m.id == id);
        if (mat != null && ColorUtility.TryParseHtmlString(mat.color, out Color c)) return c;
        return Color.gray;
    }
}