#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ChargePathEditorWindow : EditorWindow
{
    //[MenuItem("Physics Sandbox/Charge Path Editor")]
    public static void Open()
    {
        var w = GetWindow<ChargePathEditorWindow>("Charge Path Editor");
        w.minSize = new Vector2(420, 320);
        w.Show();
    }

    public void CreateGUI()
    {
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/Editor/UXML/ChargePathEditorWindow.uxml"
        );

        if (uxml == null)
        {
            rootVisualElement.Add(new Label("Missing UXML: Assets/Editor/UXML/ChargePathEditorWindow.uxml"));
            return;
        }

        rootVisualElement.Add(uxml.CloneTree());
    }
}
#endif
