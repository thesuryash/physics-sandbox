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
            dataExtractor = new DataExtractor();
            dependencyAnalyzer = new DependencyAnalyzer(idRegistry);
        }

        // The main public method to kick off the export process
        public void ExportScene(List<GameObject> rootObjects, string filePath)
        {
            // Always clear the registry before a new export so we don't hold old data
            idRegistry.Clear();

            SceneManifest manifest = new SceneManifest
            {
                ExportVersion = "1.0",
                ExportDate = System.DateTime.Now
            };

            // Loop through the given objects and flatten them into our list
            foreach (GameObject obj in rootObjects)
            {
                if (obj != null)
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
            if (obj.hideFlags != HideFlags.None) return;

            EntityNode node = new EntityNode();
            node.Id = idRegistry.GetOrAssignId(obj);
            node.ParentId = parentId; // Save the parent's ID
            node.Name = obj.name;
            node.Transform = dataExtractor.ExtractTransform(obj.transform);
            node.Components = dataExtractor.ExtractComponents(obj);
            node.Dependencies = dependencyAnalyzer.GetDependencies(obj);

            entitiesList.Add(node);

            foreach (Transform child in obj.transform)
            {
                // Pass THIS object's ID as the parent for the next level
                ProcessTree(child.gameObject, entitiesList, node.Id);
            }
        }
    }
}