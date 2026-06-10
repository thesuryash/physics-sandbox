using UnityEngine;
using System.Collections.Generic;
using TMPro; // Remove if using UI Toolkit

public class CameraDropdownManager : MonoBehaviour
{
    [Header("References")]
    public CameraOrbitSafe cameraScript;
    public TMP_Dropdown targetDropdown;

    private List<FocusableObject> activeObjects = new List<FocusableObject>();

    private void Start()
    {
        RefreshDropdownList();
    }

    // Call this method via code whenever a new object is spawned into the sandbox!
    public void RefreshDropdownList()
    {
        // 1. Find all objects in the scene with the marker script
        activeObjects.Clear();
        activeObjects.AddRange(FindObjectsByType<FocusableObject>(FindObjectsSortMode.None));

        // 2. Clear the old UI dropdown
        targetDropdown.ClearOptions();
        List<string> options = new List<string>();

        // 3. Add a default "View Everything" option at the very top
        options.Add("View Entire Sandbox");

        // 4. Add the names of all the individual objects
        foreach (FocusableObject obj in activeObjects)
        {
            options.Add(obj.displayName);
        }

        // 5. Push the text to the UI and listen for clicks
        targetDropdown.AddOptions(options);
        targetDropdown.onValueChanged.RemoveAllListeners();
        targetDropdown.onValueChanged.AddListener(OnDropdownSelectionChanged);

        // 6. Force the camera to look at whatever is currently selected
        OnDropdownSelectionChanged(targetDropdown.value);
    }

    private void OnDropdownSelectionChanged(int selectedIndex)
    {
        cameraScript.ClearAllTargets();

        if (selectedIndex == 0)
        {
            // The user selected "View Entire Sandbox"
            // Tell the camera to look at every single focusable object at once
            foreach (FocusableObject obj in activeObjects)
            {
                cameraScript.AddTarget(obj.transform);
            }
        }
        else
        {
            // The user selected a specific object. 
            // We subtract 1 because index 0 is our "View Entire Sandbox" option.
            FocusableObject selectedObject = activeObjects[selectedIndex - 1];
            cameraScript.AddTarget(selectedObject.transform);
        }
    }
}