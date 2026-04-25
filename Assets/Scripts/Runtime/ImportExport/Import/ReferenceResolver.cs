using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ImportExport.Models;
using Newtonsoft.Json.Linq; // Needed to unwrap JSON arrays

namespace ImportExport.Import
{
    public class ReferenceResolver
    {
        // Claude's Point #5: The Resolution Report
        private class ImportReport
        {
            public int resolvedCount = 0;
            public List<string> missingTargets = new List<string>();
            public List<string> missingComponents = new List<string>();
            public List<string> typeErrors = new List<string>();

            public void PrintReport()
            {
                if (resolvedCount > 0)
                    Debug.Log($"<color=cyan>Reference Resolver:</color> Successfully mapped {resolvedCount} references.");

                foreach (var err in missingTargets) Debug.LogWarning($"<color=orange>Missing Target:</color> {err}");
                foreach (var err in missingComponents) Debug.LogWarning($"<color=red>Missing Component:</color> {err}");
                foreach (var err in typeErrors) Debug.LogError($"<color=red>Type Error:</color> {err}");
            }
        }

        public void ResolveReferences(Dictionary<string, GameObject> spawnedObjects, List<EntityNode> nodes)
        {
            ImportReport report = new ImportReport();

            foreach (var node in nodes)
            {
                if (!spawnedObjects.TryGetValue(node.Id, out GameObject currentObj) || currentObj == null) continue;
                if (node.Components == null) continue;

                foreach (var compData in node.Components)
                {
                    Type compType = FindType(compData.Type);
                    if (compType == null) continue;

                    Component comp = currentObj.GetComponent(compType);
                    if (comp == null) continue;

                    // Parse the Properties dictionary for string IDs or Arrays of IDs
                    if (compData.Properties != null)
                    {
                        foreach (var prop in compData.Properties)
                        {
                            ResolveField(comp, compType, prop.Key, prop.Value, spawnedObjects, report);
                        }
                    }
                }
            }

            // Fire the audit report at the very end
            report.PrintReport();
        }

        private void ResolveField(Component comp, Type compType, string fieldName, object jsonValue, Dictionary<string, GameObject> spawnedObjects, ImportReport report)
        {
            if (jsonValue == null) return;

            FieldInfo field = compType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return;

            Type fieldType = field.FieldType;

            try
            {
                // --- CLAUDE'S POINT #3: HANDLE ARRAYS ---
                if (fieldType.IsArray && jsonValue is JArray jArray)
                {
                    Type elementType = fieldType.GetElementType();
                    Array newArray = Array.CreateInstance(elementType, jArray.Count);

                    for (int i = 0; i < jArray.Count; i++)
                    {
                        object resolvedObj = FetchTargetObject(jArray[i].ToString(), elementType, spawnedObjects, compType.Name, fieldName, report);
                        newArray.SetValue(resolvedObj, i);
                    }
                    field.SetValue(comp, newArray);
                    report.resolvedCount++;
                }
                // --- HANDLE GENERIC LISTS (List<T>) ---
                else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>) && jsonValue is JArray jListArray)
                {
                    Type elementType = fieldType.GetGenericArguments()[0];
                    IList newList = (IList)Activator.CreateInstance(fieldType);

                    foreach (var item in jListArray)
                    {
                        object resolvedObj = FetchTargetObject(item.ToString(), elementType, spawnedObjects, compType.Name, fieldName, report);
                        if (resolvedObj != null) newList.Add(resolvedObj);
                    }
                    field.SetValue(comp, newList);
                    report.resolvedCount++;
                }
                // --- HANDLE SINGLE REFERENCES ---
                else if (jsonValue is string targetStringId)
                {
                    // Only process if it's actually asking for a Unity Object
                    if (typeof(GameObject).IsAssignableFrom(fieldType) || typeof(Component).IsAssignableFrom(fieldType))
                    {
                        object resolvedObj = FetchTargetObject(targetStringId, fieldType, spawnedObjects, compType.Name, fieldName, report);
                        if (resolvedObj != null)
                        {
                            field.SetValue(comp, resolvedObj);
                            report.resolvedCount++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                report.typeErrors.Add($"Crash resolving '{fieldName}' on '{compType.Name}': {ex.Message}");
            }
        }

        // Claude's Point #2: The Field Tells You Everything
        private object FetchTargetObject(string targetId, Type expectedType, Dictionary<string, GameObject> spawnedObjects, string sourceComp, string sourceField, ImportReport report)
        {
            if (string.IsNullOrEmpty(targetId)) return null;

            if (!spawnedObjects.TryGetValue(targetId, out GameObject targetGo))
            {
                report.missingTargets.Add($"{sourceComp}.{sourceField} looked for UUID '{targetId}', but it doesn't exist in the scene.");
                return null;
            }

            if (typeof(GameObject).IsAssignableFrom(expectedType))
            {
                return targetGo;
            }

            if (typeof(Component).IsAssignableFrom(expectedType))
            {
                Component targetComp = targetGo.GetComponent(expectedType);
                if (targetComp != null)
                {
                    return targetComp;
                }
                else
                {
                    report.missingComponents.Add($"{sourceComp}.{sourceField} found the GameObject '{targetGo.name}', but it has no '{expectedType.Name}' attached.");
                    return null;
                }
            }

            return null;
        }

        private Type FindType(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName.Split(',')[0]);
                if (type != null) return type;
            }
            return null;
        }
    }
}