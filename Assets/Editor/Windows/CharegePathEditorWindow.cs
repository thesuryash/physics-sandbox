#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ChargePathEditorWindow : EditorWindow
{
    private static string PackageRoot()
    {
        var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ChargePathEditorWindow).Assembly);
        return info != null ? $"{info.assetPath}/Assets" : "Assets";
    }

    //[MenuItem("Physics Sandbox/Charge Path Editor")]
    public static void Open()
    {
        var w = GetWindow<ChargePathEditorWindow>("Charge Path Editor");
        w.minSize = new Vector2(420, 320);
        w.Show();
    }

    public void CreateGUI()
    {
        string uxmlPath = $"{PackageRoot()}/Editor/UXML/ChargePathEditorWindow.uxml";
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);

        if (uxml == null)
        {
            rootVisualElement.Add(new Label($"Missing UXML: {uxmlPath}"));
            return;
        }

        rootVisualElement.Add(uxml.CloneTree());
    }
}

#endif
