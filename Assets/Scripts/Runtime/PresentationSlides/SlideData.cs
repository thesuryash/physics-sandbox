using UnityEngine;

[System.Serializable]
public class SlideData
{
    public string slideName;      // The name the educator assigns
    public string originalIndex;  // Slide number from original PPT
    public Texture2D slideTexture; // The image of the slide
    public string description;    // Any metadata or alt-text
}