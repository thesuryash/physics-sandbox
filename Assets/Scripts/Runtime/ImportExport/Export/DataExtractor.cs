using UnityEngine;
using ImportExport.Models;
using System.Collections.Generic;

namespace ImportExport.Export
{
    public class DataExtractor
    {
        // Converts a Unity Transform into our TransformState model
        public TransformState ExtractTransform(Transform unityTransform)
        {
            if (unityTransform == null) return new TransformState();

            return new TransformState
            {
                Position = new float[] { unityTransform.position.x, unityTransform.position.y, unityTransform.position.z },
                Rotation = new float[] { unityTransform.eulerAngles.x, unityTransform.eulerAngles.y, unityTransform.eulerAngles.z },
                Scale = new float[] { unityTransform.localScale.x, unityTransform.localScale.y, unityTransform.localScale.z }
            };
        }

        // Scans a GameObject for attached scripts and extracts their variables
        public List<ComponentData> ExtractComponents(GameObject obj)
        {
            var componentsList = new List<ComponentData>();

            // Example: Extracting a basic Rigidbody
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                var rbData = new ComponentData { Type = "UnityEngine.Rigidbody" };

                // Stuff the variables into our flexible dictionary
                rbData.Properties["mass"] = rb.mass;
                rbData.Properties["drag"] = rb.drag;
                rbData.Properties["useGravity"] = rb.useGravity;
                rbData.Properties["isKinematic"] = rb.isKinematic;

                componentsList.Add(rbData);
            }

            // We will add extraction logic for your custom scripts here later!

            return componentsList;
        }
    }
}