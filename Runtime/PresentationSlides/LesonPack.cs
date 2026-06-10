using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLessonPack", menuName = "Physics Sandbox/Lesson Pack")]
public class LessonPack : ScriptableObject
{
    public string lessonTitle;
    public string educatorNotes;
    public List<SlideData> slides = new List<SlideData>();
}