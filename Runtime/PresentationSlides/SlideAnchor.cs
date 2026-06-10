using UnityEngine;

public class SlideAnchor : MonoBehaviour
{
    [Header("Visual Implementation")]
    public Renderer displayQuad;

    void OnEnable()
    {
        // Listen for the manager's broadcast
        PresentationManager.SlideChangedEvent += ApplySlide;
    }

    void OnDisable()
    {
        // Stop listening if this screen is destroyed or turned off
        PresentationManager.SlideChangedEvent -= ApplySlide;
    }

    private void ApplySlide(Texture2D newTexture)
    {
        if (displayQuad != null)
        {
            // Apply the texture to the material
            displayQuad.material.mainTexture = newTexture;
        }
    }
}