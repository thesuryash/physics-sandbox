using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ImportExportWindow : EditorWindow
{
    // Variables for the Export section
    [SerializeField] private List<GameObject> objectsToExport = new List<GameObject>();
    private string exportFilePath = "";

    // Variables for the Import section
    private string importFilePath = "";

    // Using SerializedObject makes drawing lists in Editor Windows significantly easier
    private SerializedObject serializedWindow;
    private SerializedProperty objectsToExportProp;

    [MenuItem("Physics Sandbox/Import & Export Data")]
    public static void ShowWindow()
    {
        // Creates the window and sets its title
        GetWindow<ImportExportWindow>("Import / Export");
    }

    private void OnEnable()
    {
        // Bind the serialized object to this window to easily draw the list UI
        serializedWindow = new SerializedObject(this);
        objectsToExportProp = serializedWindow.FindProperty("objectsToExport");
    }

    private void OnGUI()
    {
        // Add some padding at the top
        GUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

        DrawExportSection();

        GUILayout.Space(15);

        // Draw a visual separator line
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.Space(10);

        DrawImportSection();

        EditorGUILayout.EndVertical();
    }

    private void DrawExportSection()
    {
        GUILayout.Label("Export Metadata to JSON", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select the root GameObjects you want to serialize and export.", MessageType.Info);

        // 1. Draw the draggable list of GameObjects
        serializedWindow.Update();
        EditorGUILayout.PropertyField(objectsToExportProp, true);
        serializedWindow.ApplyModifiedProperties();

        GUILayout.Space(5);

        // 2. Draw the File Path and Browse Button on the same line
        GUILayout.BeginHorizontal();
        exportFilePath = EditorGUILayout.TextField("Save Path", exportFilePath);

        if (GUILayout.Button("Browse", GUILayout.Width(75)))
        {
            // Opens the native OS save file dialog
            string path = EditorUtility.SaveFilePanel("Save Export JSON", "", "SceneMetadataExport", "json");
            if (!string.IsNullOrEmpty(path))
            {
                exportFilePath = path;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 3. The main Export Button
        if (GUILayout.Button("Export to JSON", GUILayout.Height(30)))
        {
            if (objectsToExport.Count == 0)
            {
                Debug.LogWarning("Export Cancelled: No GameObjects selected for export.");
                return;
            }

            if (string.IsNullOrEmpty(exportFilePath))
            {
                Debug.LogWarning("Export Cancelled: No save path specified.");
                return;
            }

            Debug.Log($"Export button clicked! Ready to serialize {objectsToExport.Count} objects to: {exportFilePath}");
            // TODO: Hook up your internal serialization manager here
        }
    }

    private void DrawImportSection()
    {
        GUILayout.Label("Import Metadata from JSON", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Load a previously exported JSON file to recreate scene objects.", MessageType.Info);

        // 1. Draw the File Path and Browse Button on the same line
        GUILayout.BeginHorizontal();
        importFilePath = EditorGUILayout.TextField("Load Path", importFilePath);

        if (GUILayout.Button("Browse", GUILayout.Width(75)))
        {
            // Opens the native OS open file dialog
            string path = EditorUtility.OpenFilePanel("Select Import JSON", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                importFilePath = path;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 2. The main Import Button
        if (GUILayout.Button("Import from JSON", GUILayout.Height(30)))
        {
            if (string.IsNullOrEmpty(importFilePath))
            {
                Debug.LogWarning("Import Cancelled: No load path specified.");
                return;
            }

            Debug.Log($"Import button clicked! Ready to read JSON from: {importFilePath}");
            // TODO: Hook up your internal deserialization manager here
        }
    }
}