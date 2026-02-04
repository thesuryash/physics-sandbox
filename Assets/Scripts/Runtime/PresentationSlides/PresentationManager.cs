using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PresentationManager : MonoBehaviour
{
    public static PresentationManager Instance { get; private set; }

    // The central library: Key is the 'slideName'
    private Dictionary<string, SlideData> _slideLibrary = new Dictionary<string, SlideData>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Registers a slide into the library. 
    /// Called during the "Ingestion Phase" (Upload/Import).
    /// </summary>
    public void RegisterSlide(SlideData data)
    {
        if (!_slideLibrary.ContainsKey(data.slideName))
            _slideLibrary.Add(data.slideName, data);
    }

    public SlideData GetSlide(string name)
    {
        _slideLibrary.TryGetValue(name, out SlideData data);
        return data;
    }
}