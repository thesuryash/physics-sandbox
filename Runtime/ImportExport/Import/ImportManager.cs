using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using ImportExport.Models;
using System.Reflection;

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

            try
            {
                string jsonText = File.ReadAllText(filePath);
                SceneManifest manifest = JsonConvert.DeserializeObject<SceneManifest>(jsonText);

                List<EntityNode> allEntities = manifest.Entities;

                // Pass 1: Spawn all empty GameObjects and attach base components
                var spawnedObjects = entityBuilder.BuildEntities(allEntities);

                // Pass 1.5: Sculpt the 3D Meshes from the raw JSON data
                RebuildMeshes(spawnedObjects, allEntities);

                // Pass 2: Wire up the inter-component Inspector references
                referenceResolver.ResolveReferences(spawnedObjects, allEntities);

                // Sweep native Unity Engine warnings under the rug
                //ClearConsole();


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
        // --- NEW MESH RECONSTRUCTION PASS ---
        private void RebuildMeshes(Dictionary<string, GameObject> spawnedObjects, List<EntityNode> entities)
        {
            int rebuiltCount = 0;

            foreach (var node in entities)
            {
                // Skip if this object didn't have a mesh saved in the JSON
                if (node.Mesh == null || node.Mesh.Vertices == null || node.Mesh.Vertices.Length == 0)
                    continue;

                // Ensure the base GameObject was actually created by the EntityBuilder
                if (!spawnedObjects.TryGetValue(node.Id, out GameObject targetObj) || targetObj == null)
                    continue;

                Mesh newMesh = new Mesh();
                newMesh.name = node.Name + "_ImportedMesh";

                // 1. Unflatten Vertices (Float array -> Vector3 array)
                Vector3[] verts = new Vector3[node.Mesh.Vertices.Length / 3];
                for (int i = 0; i < verts.Length; i++)
                {
                    verts[i] = new Vector3(
                        node.Mesh.Vertices[i * 3],
                        node.Mesh.Vertices[i * 3 + 1],
                        node.Mesh.Vertices[i * 3 + 2]
                    );
                }
                newMesh.vertices = verts;

                // 2. Restore Triangles (already an int array)
                newMesh.triangles = node.Mesh.Triangles;

                // 3. Unflatten Normals
                if (node.Mesh.Normals != null && node.Mesh.Normals.Length > 0)
                {
                    Vector3[] normals = new Vector3[node.Mesh.Normals.Length / 3];
                    for (int i = 0; i < normals.Length; i++)
                    {
                        normals[i] = new Vector3(
                            node.Mesh.Normals[i * 3],
                            node.Mesh.Normals[i * 3 + 1],
                            node.Mesh.Normals[i * 3 + 2]
                        );
                    }
                    newMesh.normals = normals;
                }
                else
                {
                    newMesh.RecalculateNormals(); // Fallback to calculate them automatically
                }

                // 4. Unflatten UVs
                if (node.Mesh.UVs != null && node.Mesh.UVs.Length > 0)
                {
                    Vector2[] uvs = new Vector2[node.Mesh.UVs.Length / 2];
                    for (int i = 0; i < uvs.Length; i++)
                    {
                        uvs[i] = new Vector2(
                            node.Mesh.UVs[i * 2],
                            node.Mesh.UVs[i * 2 + 1]
                        );
                    }
                    newMesh.uv = uvs;
                }

                newMesh.RecalculateBounds();

                // 5. Attach the Mesh to the GameObject
                MeshFilter mf = targetObj.GetComponent<MeshFilter>();
                if (mf == null) mf = targetObj.AddComponent<MeshFilter>();
                mf.sharedMesh = newMesh;

                //MeshRenderer mr = targetObj.GetComponent<MeshRenderer>();
                //if (mr == null) mr = targetObj.AddComponent<MeshRenderer>();

                //// Assign a default material so it isn't invisible/pink
                //mr.sharedMaterial = new Material(Shader.Find("Standard"));

                MeshRenderer mr = targetObj.GetComponent<MeshRenderer>();
                if (mr == null) mr = targetObj.AddComponent<MeshRenderer>();

                // --- REBUILD THE MATERIAL AND TEXTURE ---
                if (node.Material != null)
                {
                    // 1. Find the right shader
                    Shader shader = Shader.Find(node.Material.ShaderName);
                    if (shader == null) shader = Shader.Find("Standard"); // Fallback

                    Material newMat = new Material(shader);

                    // 2. Restore the Base Color
                    if (node.Material.Color != null && node.Material.Color.Length == 4)
                    {
                        newMat.color = new Color(
                            node.Material.Color[0],
                            node.Material.Color[1],
                            node.Material.Color[2],
                            node.Material.Color[3]
                        );
                    }

                    // 3. Decode the Base64 String back into an Image
                    if (!string.IsNullOrEmpty(node.Material.TextureBase64))
                    {
                        byte[] imageBytes = Convert.FromBase64String(node.Material.TextureBase64);
                        Texture2D decodedTexture = new Texture2D(2, 2); // Size will auto-resize when loading
                        decodedTexture.LoadImage(imageBytes);
                        newMat.mainTexture = decodedTexture;
                    }

                    mr.sharedMaterial = newMat;
                }
                else
                {
                    mr.sharedMaterial = new Material(Shader.Find("Standard"));
                }

                // --- NEW INDIVIDUAL DEBUG PRINT ---
                Debug.Log($"<color=magenta>Mesh Reconstructed:</color> Successfully built '{targetObj.name}' with {newMesh.vertexCount} vertices.");
                rebuiltCount++;
            }

            // --- NEW SUMMARY DEBUG PRINT ---
            if (rebuiltCount > 0)
            {
                Debug.Log($"<color=cyan>Mesh Pass Complete:</color> Rebuilt {rebuiltCount} meshes from JSON.");
            }
            else
            {
                Debug.LogWarning("<color=orange>Mesh Pass Warning:</color> No meshes were found in the JSON or successfully rebuilt.");
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
                // Silently fail if Unity changes their internal API
            }
#endif
        }
    }
}