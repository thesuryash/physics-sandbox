using UnityEngine;

public class PresentationManager : MonoBehaviour
{
    public static PresentationManager Instance { get; private set; }

    [Header("Active Lesson")]
    public LessonPack currentLesson;
    private int _currentSlideIndex = 0;

    // The event that tells all screens to update
    public delegate void OnSlideChanged(Texture2D newTexture);
    public static event OnSlideChanged SlideChangedEvent;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Broadcast the first slide when the scene starts
        if (currentLesson != null && currentLesson.slides.Count > 0)
        {
            UpdateScreens();
        }
    }

    public void NextSlide()
    {
        if (currentLesson == null || currentLesson.slides.Count == 0) return;

        if (_currentSlideIndex < currentLesson.slides.Count - 1)
        {
            _currentSlideIndex++;
            UpdateScreens();
        }
    }

    public void PreviousSlide()
    {
        if (currentLesson == null || currentLesson.slides.Count == 0) return;

        if (_currentSlideIndex > 0)
        {
            _currentSlideIndex--;
            UpdateScreens();
        }
    }

    private void UpdateScreens()
    {
        SlideData currentSlide = currentLesson.slides[_currentSlideIndex];

        if (currentSlide != null && currentSlide.slideTexture != null)
        {
            // Yell out to the room: "Hey everyone, change to this texture!"
            SlideChangedEvent?.Invoke(currentSlide.slideTexture);
            Debug.Log($"[PresentationManager] Slide changed to: {currentSlide.slideName}");
        }
    }
}