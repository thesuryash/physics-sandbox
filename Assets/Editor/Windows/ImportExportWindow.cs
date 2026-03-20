using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using ImportExport.Export;
// Add the Import namespace
using ImportExport.Import;

public class ImportExportWindow : EditorWindow
{
    [SerializeField] private List<GameObject> objectsToExport = new List<GameObject>();
    private string exportFilePath = "";
    private string importFilePath = "";

    private SerializedObject serializedWindow;
    private SerializedProperty objectsToExportProp;

    private ExportManager exportManager;
    // 1. Add the Import Manager
    private ImportManager importManager;

    [MenuItem("Physics Sandbox/Import & Export Data")]
    public static void ShowWindow()
    {
        GetWindow<ImportExportWindow>("Import / Export");
    }

    private void OnEnable()
    {
        serializedWindow = new SerializedObject(this);
        objectsToExportProp = serializedWindow.FindProperty("objectsToExport");

        exportManager = new ExportManager();
        // 2. Initialize the Import Manager
        importManager = new ImportManager();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

        DrawExportSection();

        GUILayout.Space(15);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(10);

        DrawImportSection();

        EditorGUILayout.EndVertical();
    }

    private void DrawExportSection()
    {
        GUILayout.Label("Export Metadata to JSON", EditorStyles.boldLabel);

        if (GUILayout.Button("Find All Root Objects in Scene", GUILayout.Height(25)))
        {
            PopulateWithSceneRoots();
        }

        GUILayout.Space(5);

        serializedWindow.Update();
        EditorGUILayout.PropertyField(objectsToExportProp, true);
        serializedWindow.ApplyModifiedProperties();

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        exportFilePath = EditorGUILayout.TextField("Save Path", exportFilePath);
        if (GUILayout.Button("Browse", GUILayout.Width(75)))
        {
            string path = EditorUtility.SaveFilePanel("Save Export JSON", "", "FullSceneExport", "json");
            if (!string.IsNullOrEmpty(path)) exportFilePath = path;
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Export to JSON", GUILayout.Height(30)))
        {
            if (objectsToExport.Count == 0 || string.IsNullOrEmpty(exportFilePath))
            {
                EditorUtility.DisplayDialog("Error", "List is empty or path is invalid!", "OK");
                return;
            }
            exportManager.ExportScene(objectsToExport, exportFilePath);
            EditorUtility.DisplayDialog("Success", "Export Complete!", "OK");
        }
    }

    private void DrawImportSection()
    {
        GUILayout.Label("Import Metadata from JSON", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        importFilePath = EditorGUILayout.TextField("Load Path", importFilePath);
        if (GUILayout.Button("Browse", GUILayout.Width(75)))
        {
            string path = EditorUtility.OpenFilePanel("Select Import JSON", "", "json");
            if (!string.IsNullOrEmpty(path)) importFilePath = path;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 3. THE IMPORT HOOK: Call the ImportManager
        if (GUILayout.Button("Import from JSON", GUILayout.Height(30)))
        {
            if (string.IsNullOrEmpty(importFilePath))
            {
                EditorUtility.DisplayDialog("Error", "Please select a file to import.", "OK");
                return;
            }

            importManager.ImportScene(importFilePath);
            EditorUtility.DisplayDialog("Success", "Reconstruction complete! Check your Hierarchy.", "OK");
        }
    }

    private void PopulateWithSceneRoots()
    {
        objectsToExport.Clear();
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        objectsToExport.AddRange(roots);
        serializedWindow.Update();
    }
}