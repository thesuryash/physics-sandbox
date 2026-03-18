using UnityEngine;
using XCharts.Runtime;

[RequireComponent(typeof(BarChart))]
public class EnergyBarGrapher : MonoBehaviour
{
    [Header("Physics Targets")]
    public PhysicsBody targetBody;
    public Mass targetMass;

    [Header("Reference Ground")]
    [Tooltip("The Y-position considered to be zero Potential Energy.")]
    public float groundHeight = 0f;

    private BarChart _chart;
    private Rigidbody _rb;

    // We store the initial maximum energy to lock the graph scale
    private float _initialTotalEnergy;

    void Start()
    {
        _chart = GetComponent<BarChart>();
        if (targetBody != null) _rb = targetBody.GetComponent<Rigidbody>();

        // We MUST calculate the starting energy before we configure the chart
        CalculateInitialEnergy();
        ConfigureChart();
    }

    private void CalculateInitialEnergy()
    {
        if (_rb == null || targetMass == null) return;

        float mass = targetMass.TotalMass;

        // 1. Initial KE
        float ke = 0.5f * mass * _rb.velocity.sqrMagnitude;

        // 2. Initial PE
        float height = Mathf.Max(0, targetBody.transform.position.y - groundHeight);
        float pe = mass * Mathf.Abs(Physics.gravity.y) * height;

        // 3. Initial TE
        _initialTotalEnergy = ke + pe;

        // Safety check: If starting with 0 energy (resting on the ground), give the graph a minimum scale of 10
        if (_initialTotalEnergy < 0.01f) _initialTotalEnergy = 10f;
    }

    private void ConfigureChart()
    {
        _chart.RemoveData();

        // 1. Title
        XCharts.Runtime.Title chartTitle = _chart.EnsureChartComponent<XCharts.Runtime.Title>();
        chartTitle.show = true;
        chartTitle.text = "System Energy";

        // 2. X-Axis (Categories)
        var xAxis = _chart.EnsureChartComponent<XAxis>();
        xAxis.type = Axis.AxisType.Category;
        xAxis.data.Clear();
        xAxis.data.Add("KE");
        xAxis.data.Add("PE");
        xAxis.data.Add("TE");

        // 3. Y-Axis (Values & Locked Scale)
        var yAxis = _chart.EnsureChartComponent<YAxis>();
        yAxis.type = Axis.AxisType.Value;
        yAxis.axisName.show = true;
        yAxis.axisName.name = "Energy";

        yAxis.axisLabel.show = true;
        yAxis.axisLabel.numericFormatter = "F0";
        yAxis.axisLabel.formatter = "{value} J";

        // --- THE MAGIC: Lock the Y-Axis to the Initial Total Energy + a 15% visual buffer ---
        yAxis.minMaxType = Axis.AxisMinMaxType.Custom;
        yAxis.min = 0;
        yAxis.max = _initialTotalEnergy * 1.15f;

        // 4. Data Series Setup
        var serie = _chart.AddSerie<Bar>("Energy");

        var label = serie.EnsureComponent<LabelStyle>();
        label.show = true;
        label.position = LabelStyle.Position.Top;
        label.numericFormatter = "F1";
        label.formatter = "{value} J";

        // --- THE ANIMATION: Ensure the smooth "Rise" effect is turned on ---
        serie.animation.enable = true;
        serie.animation.fadeIn.duration = 1000; // Takes 1 second (1000ms) to rise to full height

        // 5. Inject the real physics data at the very beginning of the launch!
        // Instead of starting at (0, 0), we recalculate the exact values for Frame 1
        float mass = targetMass.TotalMass;
        float initialKE = 0.5f * mass * _rb.velocity.sqrMagnitude;
        float initialHeight = Mathf.Max(0, targetBody.transform.position.y - groundHeight);
        float initialPE = mass * Mathf.Abs(Physics.gravity.y) * initialHeight;

        _chart.AddData(0, initialKE); // Index 0: KE
        _chart.AddData(0, initialPE); // Index 1: PE
        _chart.AddData(0, _initialTotalEnergy); // Index 2: TE
    }

    void FixedUpdate()
    {
        if (_rb == null || targetMass == null) return;

        float mass = targetMass.TotalMass;

        float ke = 0.5f * mass * _rb.velocity.sqrMagnitude;

        float height = targetBody.transform.position.y - groundHeight;
        float pe = mass * Mathf.Abs(Physics.gravity.y) * height;
        pe = Mathf.Max(0, pe);

        float te = ke + pe;

        // Update the existing bars dynamically
        _chart.UpdateData(0, 0, ke);
        _chart.UpdateData(0, 1, pe);
        _chart.UpdateData(0, 2, te);
    }
}