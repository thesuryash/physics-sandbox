using UnityEngine;
using ImportExport.Models;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace ImportExport.Export
{
    public class DataExtractor
    {
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

        public List<ComponentData> ExtractComponents(GameObject obj)
        {
            var componentsList = new List<ComponentData>();

            // Get every component attached to this object
            Component[] allComponents = obj.GetComponents<Component>();

            foreach (var comp in allComponents)
            {
                if (comp == null || comp is Transform) continue;

                Type type = comp.GetType();
                var compData = new ComponentData { Type = type.AssemblyQualifiedName };

                // Use Reflection to find all public fields (variables) in the script
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    // For now, we only grab simple types (numbers, strings, bools)
                    if (field.FieldType.IsPrimitive || field.FieldType == typeof(string))
                    {
                        compData.Properties[field.Name] = field.GetValue(comp);
                    }
                }

                if (compData.Properties.Count > 0 || comp is Rigidbody)
                {
                    // Special case for Rigidbody since it uses properties, not fields
                    if (comp is Rigidbody rb)
                    {
                        compData.Properties["mass"] = rb.mass;
                        compData.Properties["useGravity"] = rb.useGravity;
                        compData.Properties["isKinematic"] = rb.isKinematic;
                    }
                    componentsList.Add(compData);
                }
            }
            return componentsList;
        }
    }
}