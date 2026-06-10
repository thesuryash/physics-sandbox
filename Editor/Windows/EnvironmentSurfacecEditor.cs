using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(EnvironmentSurface))]
[CanEditMultipleObjects]
public class EnvironmentSurfaceEditor : Editor
{
    private SerializedProperty _materialIDProp;
    private SerializedProperty _updateColorProp;

    // The list of options loaded from JSON
    private string[] _options;
    private bool _jsonLoaded = false;

    private void OnEnable()
    {
        // Link to the variables in the runtime script
        _materialIDProp = serializedObject.FindProperty("materialID");
        _updateColorProp = serializedObject.FindProperty("updateColorOnStart");

        LoadOptions();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Header
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Surface Configuration", EditorStyles.boldLabel);

        // --- THE DROPDOWN LOGIC ---
        if (_jsonLoaded && _options != null && _options.Length > 0)
        {
            // 1. Find current index
            int index = -1;
            string currentVal = _materialIDProp.stringValue;

            for (int i = 0; i < _options.Length; i++)
            {
                if (_options[i] == currentVal) { index = i; break; }
            }

            // 2. Handle "Not Found" case (default to first item)
            if (index == -1)
            {
                index = 0;
                if (string.IsNullOrEmpty(currentVal)) _materialIDProp.stringValue = _options[0];
            }

            // 3. Draw the Dropdown
            int newIndex = EditorGUILayout.Popup("Material Type", index, _options);

            // 4. Save Selection
            if (newIndex >= 0 && newIndex < _options.Length)
            {
                _materialIDProp.stringValue = _options[newIndex];
            }
        }
        else
        {
            // Fallback: If JSON is missing, show a text field so you can still work
            EditorGUILayout.HelpBox("JSON config not found. Using manual text entry.", MessageType.Warning);
            EditorGUILayout.PropertyField(_materialIDProp, new GUIContent("Material ID"));

            if (GUILayout.Button("Reload JSON")) LoadOptions();
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_updateColorProp);

        serializedObject.ApplyModifiedProperties();
    }

    private void LoadOptions()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "physics_config.json");

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                PhysicsConfig config = JsonUtility.FromJson<PhysicsConfig>(json);

                if (config != null && config.materials != null)
                {
                    _options = config.materials.Select(x => x.id).ToArray();
                    _jsonLoaded = true;
                    return;
                }
            }
            catch
            {
                Debug.LogWarning("Failed to parse physics_config.json");
            }
        }
        _jsonLoaded = false;
    }
}