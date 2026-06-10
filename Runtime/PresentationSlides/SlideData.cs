using UnityEngine;

// This attribute adds a new option in Unity's "Create" menu
[CreateAssetMenu(fileName = "NewSlide", menuName = "Physics Sandbox/Slide Data")]
public class SlideData : ScriptableObject
{
    public string slideName;      // E.g., "Newton_Law_1"
    public string originalIndex;  // E.g., "Slide 3"
    public Texture2D slideTexture;

    [TextArea(3, 10)] // Makes the text box bigger in the Inspector
    public string description;
}