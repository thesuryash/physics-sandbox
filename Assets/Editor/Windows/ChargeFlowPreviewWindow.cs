#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ChargeFlowPreviewWindow : EditorWindow
{
    private static string PackageRoot()
    {
        var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ChargeFlowPreviewWindow).Assembly);
        return info != null ? info.assetPath : "Assets";
    }

    //[MenuItem("Physics Sandbox/Charge Flow Preview")]
    public static void Open()
    {
        var w = GetWindow<ChargeFlowPreviewWindow>("Charge Flow Preview");
        w.minSize = new Vector2(420, 260);
        w.Show();
    }

    public void CreateGUI()
    {
        string uxmlPath = $"{PackageRoot()}/Editor/UXML/ChargeFlowPreviewWindow.uxml";
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
