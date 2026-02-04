using UnityEngine;
using System;

public class SlideAnchor : MonoBehaviour
{
    [Header("Reference Settings")]
    public string linkedSlideName; // The name defined in the PresentationManager
    public bool autoDisplayOnStart = true;

    [Header("Visual Implementation")]
    public Renderer displayQuad; // A simple Quad mesh to show the slide

    void Start()
    {
        if (autoDisplayOnStart)
        {
            ApplySlide();
        }
    }

    public void ApplySlide()
    {
        if (PresentationManager.Instance == null) return;

        SlideData data = PresentationManager.Instance.GetSlide(linkedSlideName);

        if (data != null && displayQuad != null)
        {
            // Apply the slide texture to the 3D object's material
            displayQuad.material.mainTexture = data.slideTexture;
            Debug.Log($"[SlideAnchor] {name} linked to: {linkedSlideName}");
        }
        else
        {
            throw new Exception($"Slide '{linkedSlideName}' not found in Library or DisplayQuad missing.");
        }
    }
}