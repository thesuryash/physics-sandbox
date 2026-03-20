using System.Collections.Generic;
using UnityEngine;
using ImportExport.Models;

namespace ImportExport.Import
{
    public class EntityBuilder
    {
        // Pass 1: Create GameObjects and attach components
        public Dictionary<string, GameObject> BuildEntities(List<EntityNode> sortedNodes)
        {
            var spawnedObjects = new Dictionary<string, GameObject>();

            // Pass 1: Spawn everything as roots (Same as before)
            foreach (var node in sortedNodes)
            {
                GameObject newObj = new GameObject(node.Name);
                ApplyTransform(newObj.transform, node.Transform);
                ApplyComponents(newObj, node.Components);
                spawnedObjects.Add(node.Id, newObj);
            }

            // Pass 2: Re-establish Hierarchy
            foreach (var node in sortedNodes)
            {
                // If this node has a parent and that parent was spawned...
                if (!string.IsNullOrEmpty(node.ParentId) && spawnedObjects.TryGetValue(node.ParentId, out GameObject parentObj))
                {
                    // Re-parent it
                    spawnedObjects[node.Id].transform.SetParent(parentObj.transform);
                }
            }

            return spawnedObjects;
        }

        private void ApplyTransform(Transform t, TransformState state)
        {
            if (state == null) return;

            t.position = new Vector3(state.Position[0], state.Position[1], state.Position[2]);
            t.eulerAngles = new Vector3(state.Rotation[0], state.Rotation[1], state.Rotation[2]);
            t.localScale = new Vector3(state.Scale[0], state.Scale[1], state.Scale[2]);
        }

        private void ApplyComponents(GameObject obj, List<ComponentData> components)
        {
            if (components == null) return;

            foreach (var compData in components)
            {
                // Special handling for Rigidbody since we saw it in your JSON
                if (compData.Type == "UnityEngine.Rigidbody")
                {
                    Rigidbody rb = obj.AddComponent<Rigidbody>();

                    if (compData.Properties.TryGetValue("mass", out object mass))
                        rb.mass = System.Convert.ToSingle(mass);

                    if (compData.Properties.TryGetValue("drag", out object drag))
                        rb.drag = System.Convert.ToSingle(drag);

                    if (compData.Properties.TryGetValue("useGravity", out object useGrav))
                        rb.useGravity = (bool)useGrav;

                    if (compData.Properties.TryGetValue("isKinematic", out object isKin))
                        rb.isKinematic = (bool)isKin;
                }

                // We will add more component types (Springs, Drag, etc.) here later
            }
        }
    }
}