using UnityEngine;
using UnityEngine.UI;

public class ClockUI : MonoBehaviour
{
    [Header("Core Reference")]
    [SerializeField] private Clock clock;

    [Header("UI Controls")]
    [SerializeField] private Slider speedSlider;
    [SerializeField] private Text speedLabel; // Swap to TMPro.TextMeshProUGUI if using TextMeshPro!

    private void Start()
    {
        // Auto-find the clock if you forgot to drag it in
        if (clock == null) clock = FindFirstObjectByType<Clock>();

        if (speedSlider != null)
        {
            // Set sandbox slider limits (0x to 3x speed is usually a good range)
            speedSlider.minValue = 0f;
            speedSlider.maxValue = 3f;

            // Match the slider to the starting state
            speedSlider.value = Time.timeScale;
            UpdateLabel(Time.timeScale);

            // Listen for slider drags
            speedSlider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    // Call this from your "Play" Button's OnClick event
    public void OnPlayClicked()
    {
        clock.Play();
        if (speedSlider != null) speedSlider.value = 1f;
    }

    // Call this from your "Pause" Button's OnClick event
    public void OnPauseClicked()
    {
        clock.Pause();
        if (speedSlider != null) speedSlider.value = 0f;
    }

    // Called automatically when you drag the slider
    private void OnSliderChanged(float value)
    {
        clock.SetSpeed(value);
        UpdateLabel(value);
    }

    private void UpdateLabel(float value)
    {
        if (speedLabel != null)
        {
            // Formats the number to 1 decimal place (e.g., "Speed: 1.5x")
            speedLabel.text = $"Speed: {value:F1}x";
        }
    }
}