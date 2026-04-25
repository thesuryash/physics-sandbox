using UnityEngine;
using XCharts.Runtime;

[RequireComponent(typeof(LineChart))]
public class EnergyGrapher : MonoBehaviour
{
    [Header("Physics Targets")]
    public PhysicsBody targetBody;
    public Mass targetMass;

    [Header("Graph Settings")]
    public int maxDataPoints = 200; // Creates a "rolling" window effect
    
    private LineChart _chart;
    private float _timePassed = 0f;
    private Rigidbody _rb; // Assuming PhysicsBody uses a Rigidbody under the hood

    void Start()
    {
        _chart = GetComponent<LineChart>();
        if (targetBody != null) _rb = targetBody.GetComponent<Rigidbody>();

        ConfigureChart();
    }

    private void ConfigureChart()
    {
        // 1. Clear out the default placeholder data
        _chart.RemoveData();

        // 2. Setup the Title (Explicitly forcing the XCharts Title type to fix the CS1061 error)
        XCharts.Runtime.Title chartTitle = _chart.EnsureChartComponent<XCharts.Runtime.Title>();
        chartTitle.show = true;
        chartTitle.text = "Kinetic Energy over Time";

        // 3. Configure X-Axis for Continuous Time
        var xAxis = _chart.EnsureChartComponent<XAxis>();
        xAxis.type = Axis.AxisType.Value;

        // FIX: 'title' is now 'axisName', and 'text' is now 'name'
        xAxis.axisName.show = true;
        xAxis.axisName.name = "Time (s)";
        xAxis.splitNumber = 5;

        // 4. Configure Y-Axis for Energy
        var yAxis = _chart.EnsureChartComponent<YAxis>();
        yAxis.type = Axis.AxisType.Value;

        // FIX: Updated to the new axisName API
        yAxis.axisName.show = true;
        yAxis.axisName.name = "Energy (J)";

        // 5. Add our Data Series
        var serie = _chart.AddSerie<Line>("Kinetic Energy");

        // Prevent infinite squeezing by clamping the data points
        serie.maxCache = maxDataPoints;
        serie.lineType = LineType.Smooth;
    }
    void FixedUpdate()
    {
        if (_rb == null || targetMass == null) return;

        // Track our simulation time
        _timePassed += Time.fixedDeltaTime;

        // Calculate Kinetic Energy
        // Using sqrMagnitude is faster than magnitude and perfectly fits the v^2 formula!
        float velocitySq = _rb.velocity.sqrMagnitude; 
        float mass = targetMass.TotalMass;
        float kineticEnergy = 0.5f * mass * velocitySq;

        // Feed the data to XCharts: AddData(serieIndex, xValue, yValue)
        _chart.AddData(0, _timePassed, kineticEnergy);
    }
}