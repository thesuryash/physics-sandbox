using System.Collections.Generic;
using UnityEngine;
using ImportExport.Models;
using System;
using System.Reflection;

namespace ImportExport.Import
{
    public class EntityBuilder
    {
        public Dictionary<string, GameObject> BuildEntities(List<EntityNode> nodes)
        {
            var spawnedObjects = new Dictionary<string, GameObject>();

            // PASS 1: Create every object as INACTIVE to suppress OnValidate/Awake crashes
            foreach (var node in nodes)
            {
                try
                {
                    GameObject newObj = new GameObject(node.Name);

                    // THE SHIELD: Turn it off before we touch any components
                    newObj.SetActive(false);

                    ApplyTransform(newObj.transform, node.Transform);
                    ApplyComponents(newObj, node.Components);

                    spawnedObjects.Add(node.Id, newObj);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"CRASH building object '{node.Name}': {ex.Message}");
                }
            }

            // PASS 2: Re-establish the Hierarchy
            foreach (var node in nodes)
            {
                try
                {
                    if (!string.IsNullOrEmpty(node.ParentId) && spawnedObjects.TryGetValue(node.ParentId, out GameObject parentObj))
                    {
                        if (spawnedObjects.TryGetValue(node.Id, out GameObject childObj))
                        {
                            childObj.transform.SetParent(parentObj.transform, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"CRASH parenting object '{node.Name}': {ex.Message}");
                }
            }

            // PASS 3: Wake everything up! 
            // Now that the hierarchy is safe, OnValidate and Awake can fire normally.
            foreach (var obj in spawnedObjects.Values)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }

            return spawnedObjects;
        }

        //private void ApplyComponents(GameObject obj, List<ComponentData> components)
        //{
        //    if (components == null) return;

        //    foreach (var compData in components)
        //    {
        //        try
        //        {
        //            // 1. Find the Type
        //            Type compType = Type.GetType(compData.Type);
        //            if (compType == null)
        //            {
        //                // Fallback: search all loaded assemblies
        //                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        //                {
        //                    compType = assembly.GetType(compData.Type.Split(',')[0]);
        //                    if (compType != null) break;
        //                }
        //            }

        //            if (compType == null)
        //            {
        //                Debug.LogWarning($"Skipped missing script: {compData.Type}");
        //                continue;
        //            }

        //            // 2. Add the Component
        //            //Component comp = obj.GetComponent(compType) ?? obj.AddComponent(compType);

        //            Component comp = obj.GetComponent(compType) ?? obj.AddComponent(compType);

        //            // THE FIX: If Unity refuses to add the component (returns null), skip it and don't crash!
        //            if (comp == null)
        //            {
        //                Debug.LogWarning($"Unity blocked adding component '{compType.Name}' to '{obj.name}'. Skipping its variables.");
        //                continue;
        //            }

        //            // 3. Apply Variables Safely
        //            //foreach (var prop in compData.Properties)
        //            //{
        //            //    try
        //            //    {
        //            //        FieldInfo field = compType.GetField(prop.Key, BindingFlags.Public | BindingFlags.Instance);
        //            //        if (field != null)
        //            //        {
        //            //            // Safely cast JSON numbers to the correct C# type (solves float vs double crashes)
        //            //            object value = Convert.ChangeType(prop.Value, field.FieldType);
        //            //            field.SetValue(comp, value);
        //            //        }

        //            //        // Manual hook for Rigidbody properties since they are Properties, not Fields
        //            //        if (comp is Rigidbody rb)
        //            //        {
        //            //            if (prop.Key == "mass") rb.mass = Convert.ToSingle(prop.Value);
        //            //            if (prop.Key == "useGravity") rb.useGravity = Convert.ToBoolean(prop.Value);
        //            //            if (prop.Key == "isKinematic") rb.isKinematic = Convert.ToBoolean(prop.Value);
        //            //        }
        //            //    }
        //            //    catch (Exception fieldEx)
        //            //    {
        //            //        Debug.LogWarning($"Could not set variable '{prop.Key}' on '{compType.Name}': {fieldEx.Message}");
        //            //    }
        //            //}
        //            foreach (var prop in compData.Properties)
        //            {
        //                try
        //                {
        //                    FieldInfo field = compType.GetField(prop.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        //                    if (field != null)
        //                    {
        //                        // Convert JSON number to C# float safely
        //                        object value = (field.FieldType == typeof(float))
        //                            ? Convert.ToSingle(prop.Value)
        //                            : Convert.ChangeType(prop.Value, field.FieldType);

        //                        field.SetValue(comp, value);
        //                    }

        //                    // Hook for Rigidbody
        //                    if (comp is Rigidbody rb)
        //                    {
        //                        if (prop.Key == "mass") rb.mass = Convert.ToSingle(prop.Value);
        //                        if (prop.Key == "useGravity") rb.useGravity = (bool)prop.Value;
        //                        if (prop.Key == "isKinematic") rb.isKinematic = (bool)prop.Value;
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    // If one variable fails, log it in yellow but keep importing the rest!
        //                    Debug.LogWarning($"Failed to set '{prop.Key}' on '{compType.Name}': {ex.Message}");
        //                }
        //            }

        //        }
        //        catch (Exception compEx)
        //        {
        //            Debug.LogError($"Could not attach component '{compData.Type}' to '{obj.name}': {compEx.Message}");
        //        }
        //    }
        //}


        private void ApplyComponents(GameObject obj, List<ComponentData> components)
        {
            if (components == null) return;

            foreach (var compData in components)
            {
                try
                {
                    Type compType = Type.GetType(compData.Type);
                    if (compType == null)
                    {
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            compType = assembly.GetType(compData.Type.Split(',')[0]);
                            if (compType != null) break;
                        }
                    }

                    if (compType == null)
                    {
                        Debug.LogWarning($"Skipped missing script: {compData.Type}");
                        continue;
                    }

                    // --- NEW FIX: Auto-Add Required Components (like BoxColliders) ---
                    EnsureRequiredComponents(obj, compType);

                    // Add or Get the Component
                    Component comp = obj.GetComponent(compType);
                    if (comp == null)
                    {
                        comp = obj.AddComponent(compType);
                    }

                    if (comp == null)
                    {
                        Debug.LogWarning($"Unity blocked adding '{compType.Name}' to '{obj.name}'.");
                        continue;
                    }

                    // Apply Variables
                    foreach (var prop in compData.Properties)
                    {
                        try
                        {
                            FieldInfo field = compType.GetField(prop.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                            if (field != null)
                            {
                                object value = (field.FieldType == typeof(float))
                                    ? Convert.ToSingle(prop.Value)
                                    : Convert.ChangeType(prop.Value, field.FieldType);

                                field.SetValue(comp, value);
                            }

                            if (comp is Rigidbody rb)
                            {
                                if (prop.Key == "mass") rb.mass = Convert.ToSingle(prop.Value);
                                if (prop.Key == "useGravity") rb.useGravity = Convert.ToBoolean(prop.Value);
                                if (prop.Key == "isKinematic") rb.isKinematic = Convert.ToBoolean(prop.Value);
                            }
                        }
                        catch { /* Ignore individual variable mapping errors */ }
                    }
                }
                catch (Exception compEx)
                {
                    Debug.LogError($"Could not attach component '{compData.Type}' to '{obj.name}': {compEx.Message}");
                }
            }
        }

        // --- NEW FIX: Reflection helper to read [RequireComponent] attributes ---
        private void EnsureRequiredComponents(GameObject obj, Type compType)
        {
            var requireAttributes = compType.GetCustomAttributes(typeof(RequireComponent), true) as RequireComponent[];
            if (requireAttributes != null)
            {
                foreach (var attr in requireAttributes)
                {
                    TryAddRequired(obj, attr.m_Type0);
                    TryAddRequired(obj, attr.m_Type1);
                    TryAddRequired(obj, attr.m_Type2);
                }
            }
        }

        private void TryAddRequired(GameObject obj, Type type)
        {
            if (type == null) return;

            // If it already has the component, we're good!
            if (obj.GetComponent(type) != null) return;

            // THE FIX: Provide concrete classes for abstract requests
            if (type == typeof(Collider)) type = typeof(BoxCollider);
            else if (type == typeof(Renderer)) type = typeof(MeshRenderer);
            else if (type.IsAbstract || type.IsInterface) return; // Skip other un-buildable types

            try
            {
                obj.AddComponent(type);
            }
            catch { /* Ignore if Unity still refuses to add a specific required type */ }
        }
        private void ApplyTransform(Transform t, TransformState state)
        {
            if (state == null) return;
            t.position = new Vector3(state.Position[0], state.Position[1], state.Position[2]);
            t.eulerAngles = new Vector3(state.Rotation[0], state.Rotation[1], state.Rotation[2]);
            t.localScale = new Vector3(state.Scale[0], state.Scale[1], state.Scale[2]);
        }
    }
}