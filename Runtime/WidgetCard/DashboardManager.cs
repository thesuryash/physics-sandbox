using UnityEngine;
using UnityEngine.UIElements;

public class DashboardManager : MonoBehaviour
{
    [Header("UI Toolkit")]
    public UIDocument dashboardDocument;

    [Header("Physics Targets")]
    public PhysicsBody testBody;
    public Mass testMass;

    private ScrollView _mainScrollView;

    void Start()
    {
        if (dashboardDocument == null) return;
        var root = dashboardDocument.rootVisualElement;
        _mainScrollView = root.Q<ScrollView>();

        if (_mainScrollView == null)
            Debug.LogError("DashboardManager: Could not find a ScrollView in the UIDocument.");
    }

    public void SpawnWidgetCard(string title)
    {
        if (_mainScrollView == null) return;
        WidgetCard newCard = new WidgetCard(title);
        newCard.ContentContainer.style.height = 250;
        _mainScrollView.Add(newCard);
    }
}
