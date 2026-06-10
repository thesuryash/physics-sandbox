using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class LessonImporterWindow : EditorWindow
{
    private string lessonTitle = "New Physics Lesson";

    [MenuItem("Physics Sandbox/Lesson Importer")]
    public static void ShowWindow()
    {
        GetWindow<LessonImporterWindow>("Lesson Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Drag & Drop Lesson Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        lessonTitle = EditorGUILayout.TextField("Lesson Title", lessonTitle);

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("1. Save your PowerPoint as PNGs.\n2. Drag that folder straight from your computer's File Explorer into the box below.", MessageType.Info);
        GUILayout.Space(10);

        // Create the Drag and Drop Area
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 100.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "\n\nDROP SLIDE FOLDER HERE", new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });

        HandleDragAndDrop(dropArea);
    }

    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                // Show a visual indicator that copying is allowed
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (string draggedPath in DragAndDrop.paths)
                    {
                        ProcessDroppedItem(draggedPath);
                    }
                }
                Event.current.Use();
                break;
        }
    }

    private void ProcessDroppedItem(string sourcePath)
    {
        // Gently catch if they tried to drop a PPTX directly
        if (Path.GetExtension(sourcePath).ToLower() == ".pptx")
        {
            EditorUtility.DisplayDialog("Format Not Supported", "Unity cannot read .pptx files directly.\n\nPlease open the presentation, go to File -> Save As -> PNG, and drag the resulting folder here.", "Got it");
            return;
        }

        if (!Directory.Exists(sourcePath))
        {
            EditorUtility.DisplayDialog("Error", "Please drop a folder containing your slide images.", "OK");
            return;
        }

        string folderName = Path.GetFileName(sourcePath);
        string unityFolderPath = $"Assets/PhysicsSlides/{folderName}";

        // Create the base directory in Unity if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/PhysicsSlides"))
        {
            AssetDatabase.CreateFolder("Assets", "PhysicsSlides");
        }

        // If the folder already exists in Unity, append a number so we don't overwrite
        if (AssetDatabase.IsValidFolder(unityFolderPath))
        {
            unityFolderPath += "_" + System.DateTime.Now.ToString("HHmmss");
        }

        string parentFolder = Path.GetDirectoryName(unityFolderPath).Replace("\\", "/");
        string newFolderName = Path.GetFileName(unityFolderPath);
        AssetDatabase.CreateFolder(parentFolder, newFolderName);

        // Copy all PNG/JPG files from the dropped folder into the Unity project
        string[] files = Directory.GetFiles(sourcePath, "*.*")
            .Where(f => f.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (files.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No PNG or JPG images found in the dropped folder.", "OK");
            return;
        }

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string destPath = $"{unityFolderPath}/{fileName}";
            File.Copy(file, destPath);
        }

        // Force Unity to import the new images
        AssetDatabase.Refresh();

        // Now generate the ScriptableObjects using the newly imported folder
        GenerateLesson(unityFolderPath);
    }

    private void GenerateLesson(string unityFolderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { unityFolderPath });
        List<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToList();

        LessonPack newLesson = ScriptableObject.CreateInstance<LessonPack>();
        newLesson.lessonTitle = lessonTitle;

        string lessonAssetPath = $"{unityFolderPath}/{lessonTitle}_Pack.asset";
        AssetDatabase.CreateAsset(newLesson, lessonAssetPath);

        for (int i = 0; i < paths.Count; i++)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(paths[i]);

            SlideData newSlide = ScriptableObject.CreateInstance<SlideData>();
            newSlide.slideName = $"{lessonTitle}_Slide_{i + 1}";
            newSlide.originalIndex = (i + 1).ToString();
            newSlide.slideTexture = tex;

            string slideAssetPath = $"{unityFolderPath}/{newSlide.slideName}.asset";
            AssetDatabase.CreateAsset(newSlide, slideAssetPath);

            newLesson.slides.Add(newSlide);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newLesson;

        Debug.Log($"[Lesson Importer] Successfully imported and generated '{lessonTitle}' with {paths.Count} slides!");
    }
}