using UnityEngine;
using ImportExport.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace ImportExport.Export
{
    public class ExportManager
    {
        private IdRegistry idRegistry;
        private DataExtractor dataExtractor;
        private DependencyAnalyzer dependencyAnalyzer;

        public ExportManager()
        {
            idRegistry = new IdRegistry();
            // CHANGE THIS LINE: Pass the idRegistry into the DataExtractor
            dataExtractor = new DataExtractor(idRegistry);
            dependencyAnalyzer = new DependencyAnalyzer(idRegistry);
        }

        // The main public method to kick off the export process
        private HashSet<GameObject> processedObjects = new HashSet<GameObject>();
        public void ExportScene(List<GameObject> rootObjects, string filePath)
        {
            // Always clear the registry before a new export so we don't hold old data
            idRegistry.Clear();
            processedObjects.Clear(); // Clear the safety set

            SceneManifest manifest = new SceneManifest
            {
                ExportVersion = "1.0",
                ExportDate = System.DateTime.Now
            };

            // Loop through the given objects and flatten them into our list
            foreach (GameObject obj in rootObjects)
            {
                if (obj != null && !processedObjects.Contains(obj))
                {
                    ProcessTree(obj, manifest.Entities);
                }
            }

            // Convert our C# objects into a formatted JSON string
            string jsonOutput = JsonConvert.SerializeObject(manifest, Formatting.Indented);

            // Write it to the disk
            File.WriteAllText(filePath, jsonOutput);

            Debug.Log($"<color=green>Export Complete!</color> Successfully exported {manifest.Entities.Count} objects to:\n{filePath}");
        }

        // Recursively processes an object and all its children, flattening them into the manifest
        private void ProcessTree(GameObject obj, List<EntityNode> entitiesList, string parentId = null)
        {
            // Skip internal Unity Editor objects
            if (obj.hideFlags != HideFlags.None || processedObjects.Contains(obj)) return;

            processedObjects.Add(obj); // Mark as processed

            EntityNode node = new EntityNode();
            node.Id = idRegistry.GetOrAssignId(obj);
            node.ParentId = parentId; // CRITICAL: This links the child to the parent
            node.Name = obj.name;
            node.Transform = dataExtractor.ExtractTransform(obj.transform);
            node.Components = dataExtractor.ExtractComponents(obj);
            node.Mesh = dataExtractor.ExtractMesh(obj);
            node.Material = dataExtractor.ExtractMaterial(obj);
            node.Dependencies = dependencyAnalyzer.GetDependencies(obj);

            entitiesList.Add(node);

            // Drill down into every child in the hierarchy
            foreach (Transform child in obj.transform)
            {
                // Pass the CURRENT node's ID as the parentId for the next generation
                ProcessTree(child.gameObject, entitiesList, node.Id);
            }
        }
    }
}