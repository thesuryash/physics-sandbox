using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(RectTransform))]
public class UIProxyTracker : MonoBehaviour
{
    public VisualElement TargetElement { get; set; }

    private RectTransform _rectTransform;
    private Canvas _canvas;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();

        // Force the RectTransform to scale from the top-left so the math matches UI Toolkit perfectly
        _rectTransform.pivot = new Vector2(0, 1);
        _rectTransform.anchorMin = new Vector2(0, 1);
        _rectTransform.anchorMax = new Vector2(0, 1);
    }

    void LateUpdate()
    {
        if (TargetElement == null || TargetElement.panel == null) return;

        // 1. Handle Minimize: If the UI Toolkit box is hidden, hide the Canvas Chart
        if (TargetElement.resolvedStyle.display == DisplayStyle.None)
        {
            _rectTransform.localScale = Vector3.zero;
            return;
        }

        _rectTransform.localScale = Vector3.one;

        // 2. Sync Position & Size
        Rect worldBound = TargetElement.worldBound;

        // Convert UI Toolkit top-left coordinates to Canvas bottom-left coordinates
        float canvasScale = _canvas.scaleFactor;

        _rectTransform.position = new Vector3(worldBound.x, Screen.height - worldBound.y, 0);
        _rectTransform.sizeDelta = new Vector2(worldBound.width / canvasScale, worldBound.height / canvasScale);
    }
}