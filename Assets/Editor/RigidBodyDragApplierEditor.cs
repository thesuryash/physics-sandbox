using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(RigidBodyDragApplier))]
public class RigidBodyDragApplierEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RigidBodyDragApplier script = (RigidBodyDragApplier)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Pre-Processing Tools", EditorStyles.boldLabel);

        // Display the unique GUID file path so the user can see it, but can't break it
        EditorGUILayout.LabelField("Unique Target File:");
        EditorGUILayout.SelectableLabel(script.BinaryFilePath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.Space(5);

        bool fileExists = File.Exists(script.BinaryFilePath);

        if (!fileExists)
        {
            EditorGUILayout.HelpBox("No binary file found! Drag forces will NOT be applied to this object at runtime. Please generate a bake.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("Binary file found and ready for runtime.", MessageType.Info);
        }

        EditorGUILayout.Space(5);

        EditorGUI.BeginDisabledGroup(fileExists);

        if (GUILayout.Button("Temp Pre-processing (Recommended for Simple Meshes)"))
        {
            if (EditorUtility.DisplayDialog("Confirm Temp Bake",
                "This will use Convex Hull estimation.\n\nIt is incredibly fast but less accurate for objects with holes. Proceed?",
                "Yes, Generate", "Cancel"))
            {
                script.BakeData(isComplex: false);
            }
        }

        if (GUILayout.Button("Hard Pre-processing (Recommended for Complex Meshes)"))
        {
            if (EditorUtility.DisplayDialog("Confirm Hard Bake",
                "This will use Rasterization estimation.\n\nIt accurately maps holes and complex silhouettes, but takes longer to process. Proceed?",
                "Yes, Generate", "Cancel"))
            {
                script.BakeData(isComplex: true);
            }
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(5);

        EditorGUI.BeginDisabledGroup(!fileExists);

        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("Clear Baked File"))
        {
            if (EditorUtility.DisplayDialog("Confirm Delete",
                "Are you sure you want to delete the baked binary file? You will need to re-bake to get drag forces back.",
                "Delete", "Cancel"))
            {
                script.ClearBake();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUI.EndDisabledGroup();
    }
}