using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

[CustomEditor(typeof(InclinedPlane))]
[CanEditMultipleObjects]
public class InclinedPlaneEditor : Editor
{
    private SerializedProperty _angleProp;
    private SerializedProperty _lengthProp;
    private SerializedProperty _widthProp;
    private SerializedProperty _thicknessProp;
    private SerializedProperty _materialIDProp;

    private string[] _materialOptions;
    private bool _jsonLoaded = false;

    private void OnEnable()
    {
        _angleProp = serializedObject.FindProperty("angle");
        _lengthProp = serializedObject.FindProperty("length");
        _widthProp = serializedObject.FindProperty("width");
        _thicknessProp = serializedObject.FindProperty("thickness");
        _materialIDProp = serializedObject.FindProperty("materialID");

        LoadMaterialList();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dimensions", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_angleProp);
        EditorGUILayout.PropertyField(_lengthProp);
        EditorGUILayout.PropertyField(_widthProp);
        EditorGUILayout.PropertyField(_thicknessProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Physics Material", EditorStyles.boldLabel);

        // --- THE DROPDOWN ---
        if (_jsonLoaded && _materialOptions != null && _materialOptions.Length > 0)
        {
            int currentIndex = System.Array.IndexOf(_materialOptions, _materialIDProp.stringValue);
            if (currentIndex == -1) currentIndex = 0;

            int newIndex = EditorGUILayout.Popup("Material Type", currentIndex, _materialOptions);
            _materialIDProp.stringValue = _materialOptions[newIndex];
        }
        else
        {
            EditorGUILayout.HelpBox("JSON not found. Using manual entry.", MessageType.Warning);
            EditorGUILayout.PropertyField(_materialIDProp);
            if (GUILayout.Button("Reload JSON")) LoadMaterialList();
        }

        if (serializedObject.ApplyModifiedProperties())
        {
            // If any value changed, force the ramp to rebuild itself immediately
            (target as InclinedPlane).BuildStructure();
        }
    }

    private void LoadMaterialList()
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
                    _materialOptions = config.materials.Select(m => m.id).ToArray();
                    _jsonLoaded = true;
                }
            }
            catch { _jsonLoaded = false; }
        }
    }
}