#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ChargeFlowPreviewWindow : EditorWindow
{
    //[MenuItem("Physics Sandbox/Charge Flow Preview")]
    public static void Open()
    {
        var w = GetWindow<ChargeFlowPreviewWindow>("Charge Flow Preview");
        w.minSize = new Vector2(420, 260);
        w.Show();
    }

    public void CreateGUI()
    {
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/Editor/UXML/ChargeFlowPreviewWindow.uxml"
        );

        if (uxml == null)
        {
            rootVisualElement.Add(new Label("Missing UXML: Assets/Editor/UXML/ChargeFlowPreviewWindow.uxml"));
            return;
        }

        rootVisualElement.Add(uxml.CloneTree());
    }
}
#endif
