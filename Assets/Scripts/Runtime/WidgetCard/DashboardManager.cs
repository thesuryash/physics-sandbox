using UnityEngine;
using UnityEngine.UIElements;

public class DashboardManager : MonoBehaviour
{
    [Header("UI Toolkit")]
    public UIDocument dashboardDocument; // Drag your main UI Document here

    [Header("Prefabs")]
    public GameObject energyChartPrefab; // Drag your XCharts Prefab here
    public Canvas mainCanvas; // Drag your scene's standard Canvas here

    [Header("Test Targets")]
    public PhysicsBody testBody;
    public Mass testMass;

    private ScrollView _mainScrollView;

    void Start()
    {
        var root = dashboardDocument.rootVisualElement;
        _mainScrollView = root.Q<ScrollView>();

        if (_mainScrollView == null)
        {
            Debug.LogError("DashboardManager: Could not find a ScrollView in the UIDocument!");
            return;
        }

        // ADD THIS LINE: Force it to spawn the widget the second the game starts!
        SpawnEnergyChartWidget();
    }

    // Call this via a UI Button, or just put it in Start() to test it immediately
    [ContextMenu("Spawn Energy Chart Widget")]
    public void SpawnEnergyChartWidget()
    {
        // 1. Create the UI Toolkit Card
        WidgetCard newCard = new WidgetCard("Energy Chart: " + testBody.gameObject.name);

        // Give the content container a fixed height so the Flexbox knows how much space to make for the chart
        newCard.ContentContainer.style.height = 250;

        // Add the card to the dashboard list
        _mainScrollView.Add(newCard);

        // 2. Spawn the XCharts Canvas Prefab
        GameObject chartObj = Instantiate(energyChartPrefab, mainCanvas.transform);

        // 3. Setup the Physics Data
        var grapher = chartObj.GetComponent<EnergyBarGrapher>();
        grapher.targetBody = testBody;
        grapher.targetMass = testMass;

        // 4. Link them together using our Tracker
        var tracker = chartObj.AddComponent<UIProxyTracker>();
        tracker.TargetElement = newCard.ContentContainer;

        // 5. Setup Destruction Logic
        newCard.OnCloseClicked = () =>
        {
            // When the user clicks 'X' on the Card, destroy the UGUI Chart as well!
            if (chartObj != null) Destroy(chartObj);
        };

        Debug.Log("Successfully spawned and linked a hybrid Widget!");
    }
}