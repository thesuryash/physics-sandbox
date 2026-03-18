using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class WidgetCard : VisualElement
{
    private VisualElement _contentContainer;
    private Button _minimizeBtn;
    private bool _isMinimized = false;
    public VisualElement ContentContainer => _contentContainer;


    // The Dashboard Manager will listen to this action to delete the actual GameObjects/Charts
    public Action OnCloseClicked;

    public WidgetCard(string title)
    {
        // 1. Automatically find and load the UXML Template
        string[] guids = AssetDatabase.FindAssets("WidgetCardTemplate t:VisualTreeAsset");
        if (guids.Length == 0)
        {
            Debug.LogError("Could not find WidgetCardTemplate.uxml in the project!");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
        visualTree.CloneTree(this);

        // 2. Grab references to the UI elements inside the template
        _contentContainer = this.Q<VisualElement>("content-container");
        _minimizeBtn = this.Q<Button>("minimize-btn");
        var closeBtn = this.Q<Button>("close-btn");
        var titleLabel = this.Q<Label>("title-label");

        // 3. Set the Title
        titleLabel.text = title;

        // 4. Bind the buttons to our local methods
        _minimizeBtn.clicked += ToggleMinimize;
        closeBtn.clicked += CloseWidget;
    }

    private void ToggleMinimize()
    {
        _isMinimized = !_isMinimized;

        // Hide/Show the content using Flexbox DisplayStyle
        _contentContainer.style.display = _isMinimized ? DisplayStyle.None : DisplayStyle.Flex;

        // Change the arrow icon
        _minimizeBtn.text = _isMinimized ? "▶" : "▼";
    }

    private void CloseWidget()
    {
        // 1. Tell the main Manager to destroy the associated Unity GameObjects (like the Chart)
        OnCloseClicked?.Invoke();

        // 2. Remove this entire UI card from the dashboard smoothly
        this.RemoveFromHierarchy();
    }

    // A helper method for your manager to easily inject custom UI into this card
    public void InjectContent(VisualElement customUI)
    {
        _contentContainer.Add(customUI);
    }
}