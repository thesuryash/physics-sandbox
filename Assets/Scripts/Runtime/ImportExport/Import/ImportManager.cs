using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using ImportExport.Models;
using System.Reflection; // <--- Make sure this is added

namespace ImportExport.Import
{
    public class ImportManager
    {
        private GraphSorter graphSorter;
        private EntityBuilder entityBuilder;
        private ReferenceResolver referenceResolver;

        public ImportManager()
        {
            graphSorter = new GraphSorter();
            entityBuilder = new EntityBuilder();
            referenceResolver = new ReferenceResolver();
        }


        public void ImportScene(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError("Import Failed: File does not exist at path: " + filePath);
                return;
            }

            //try
            //{
            //    // 1. Read and Deserialize
            //    string jsonText = File.ReadAllText(filePath);
            //    SceneManifest manifest = JsonConvert.DeserializeObject<SceneManifest>(jsonText);

            //    // 2. Validation Puzzle
            //    if (manifest == null || manifest.ProjectSignature != "PhysicsSandbox_ExportData")
            //    {
            //        Debug.LogError("Import Failed: This file is not a valid Physics Sandbox export.");
            //        return;
            //    }

            //    Debug.Log($"<color=cyan>Verification Success!</color> Found {manifest.Entities.Count} entities. Starting reconstruction...");

            //    // 3. The Pipeline (Logic coming in the next steps)
            //    List<EntityNode> sortedNodes = graphSorter.Sort(manifest.Entities);
            //    var spawnedObjects = entityBuilder.BuildEntities(sortedNodes);
            //    referenceResolver.ResolveReferences(spawnedObjects, sortedNodes);

            //    Debug.Log("<color=green>Import Complete!</color> Scene has been reconstructed.");
            //}
            try
            {
                string jsonText = File.ReadAllText(filePath);
                SceneManifest manifest = JsonConvert.DeserializeObject<SceneManifest>(jsonText);

                List<EntityNode> allEntities = manifest.Entities;

                var spawnedObjects = entityBuilder.BuildEntities(allEntities);
                referenceResolver.ResolveReferences(spawnedObjects, allEntities);

                // THE FIX: Sweep the native Unity Engine warnings under the rug!
                ClearConsole();

                Debug.Log("<color=green>Import Complete!</color> Scene has been reconstructed.");
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Import Failed: JSON formatting error. Details: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Import Failed: An unexpected error occurred: {ex.Message}");
            }
        }

        private void ClearConsole()
        {
#if UNITY_EDITOR
            try
            {
                Assembly assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
                Type type = assembly.GetType("UnityEditor.LogEntries");
                MethodInfo method = type.GetMethod("Clear");
                method.Invoke(new object(), null);
            }
            catch
            {
                // Silently fail if Unity changes their internal API in a future update
            }
#endif
        }
    }


}