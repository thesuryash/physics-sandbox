using UnityEngine;
using ImportExport.Models;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Collections;

namespace ImportExport.Export
{
    public class DataExtractor
    {
        private IdRegistry idRegistry;

        // Pass the IdRegistry into the constructor so we can generate UUIDs for references
        public DataExtractor(IdRegistry registry)
        {
            this.idRegistry = registry;
        }

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

        // --- THE NEW MATERIAL EXTRACTION METHOD ---
        // Notice we explicitly state "ImportExport.Models.MaterialData" here:
        public ImportExport.Models.MaterialData ExtractMaterial(GameObject obj)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null) return null;

            Material mat = renderer.sharedMaterial;

            // And we explicitly state it here too:
            ImportExport.Models.MaterialData matData = new ImportExport.Models.MaterialData
            {
                ShaderName = mat.shader.name,
                Color = mat.HasProperty("_Color") ? new float[] { mat.color.r, mat.color.g, mat.color.b, mat.color.a } : new float[] { 1, 1, 1, 1 }
            };

            if (mat.mainTexture != null && mat.mainTexture is Texture2D mainTex)
            {
                try
                {
                    RenderTexture tmp = RenderTexture.GetTemporary(mainTex.width, mainTex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.sRGB);
                    Graphics.Blit(mainTex, tmp);

                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = tmp;

                    Texture2D readableTex = new Texture2D(mainTex.width, mainTex.height);
                    readableTex.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                    readableTex.Apply();

                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(tmp);

                    byte[] bytes = readableTex.EncodeToPNG();
                    matData.TextureBase64 = Convert.ToBase64String(bytes);

                    if (Application.isPlaying) UnityEngine.Object.Destroy(readableTex);
                    else UnityEngine.Object.DestroyImmediate(readableTex);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to extract texture for {obj.name}: {e.Message}");
                }
            }

            return matData;
        }

        public MeshData ExtractMesh(GameObject obj)
        {
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null) return null;

            Mesh mesh = meshFilter.sharedMesh;

            // 1. Flatten Vertices (Vector3 to float[])
            float[] verts = new float[mesh.vertices.Length * 3];
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                verts[i * 3] = mesh.vertices[i].x;
                verts[i * 3 + 1] = mesh.vertices[i].y;
                verts[i * 3 + 2] = mesh.vertices[i].z;
            }

            // 2. Flatten Normals
            float[] normals = new float[mesh.normals.Length * 3];
            for (int i = 0; i < mesh.normals.Length; i++)
            {
                normals[i * 3] = mesh.normals[i].x;
                normals[i * 3 + 1] = mesh.normals[i].y;
                normals[i * 3 + 2] = mesh.normals[i].z;
            }

            // 3. Flatten UVs (Vector2 to float[])
            float[] uvs = new float[mesh.uv.Length * 2];
            for (int i = 0; i < mesh.uv.Length; i++)
            {
                uvs[i * 2] = mesh.uv[i].x;
                uvs[i * 2 + 1] = mesh.uv[i].y;
            }

            return new MeshData
            {
                Vertices = verts,
                Triangles = mesh.triangles, // Triangles are already an int[], so we just copy them!
                Normals = normals,
                UVs = uvs
            };
        }


        public List<ComponentData> ExtractComponents(GameObject obj)
        {
            var componentsList = new List<ComponentData>();
            Component[] allComponents = obj.GetComponents<Component>();

            foreach (var comp in allComponents)
            {
                if (comp == null || comp is Transform) continue;

                Type type = comp.GetType();
                var compData = new ComponentData { Type = type.AssemblyQualifiedName };

                // Get public AND private fields (so we catch [SerializeField] private fields)
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

                foreach (var field in fields)
                {
                    // Ignore private fields UNLESS they have the [SerializeField] attribute
                    if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null) continue;

                    object value = field.GetValue(comp);

                    // Safely skip if it's a standard C# null OR if it is a Unity "Fake Null" (an unassigned Inspector slot)
                    if (value == null || (value is UnityEngine.Object unityObj && unityObj == null)) continue;

                    Type fieldType = field.FieldType;

                    // 1. Simple Types (Numbers, Strings, Bools)
                    if (fieldType.IsPrimitive || fieldType == typeof(string))
                    {
                        compData.Properties[field.Name] = value;
                    }
                    // 2. GameObject References
                    else if (typeof(GameObject).IsAssignableFrom(fieldType))
                    {
                        GameObject targetGo = (GameObject)value;
                        compData.Properties[field.Name] = idRegistry.GetOrAssignId(targetGo);
                    }
                    // 3. Component References (Rigidbody, MeshRenderer, Custom Scripts)
                    else if (typeof(Component).IsAssignableFrom(fieldType))
                    {
                        Component targetComp = (Component)value;
                        compData.Properties[field.Name] = idRegistry.GetOrAssignId(targetComp.gameObject);
                    }
                    // 4. Arrays of GameObjects or Components (Crucial for physics multi-body setups)
                    else if (fieldType.IsArray)
                    {
                        Type elementType = fieldType.GetElementType();
                        if (typeof(GameObject).IsAssignableFrom(elementType) || typeof(Component).IsAssignableFrom(elementType))
                        {
                            Array sourceArray = (Array)value;
                            List<string> uuidList = new List<string>();
                            foreach (object item in sourceArray)
                            {
                                if (item is GameObject go) uuidList.Add(idRegistry.GetOrAssignId(go));
                                else if (item is Component componentItem) uuidList.Add(idRegistry.GetOrAssignId(componentItem.gameObject));
                            }
                            compData.Properties[field.Name] = uuidList.ToArray();
                        }
                    }
                }

                // Special case for Rigidbody properties
                if (comp is Rigidbody rb)
                {
                    compData.Properties["mass"] = rb.mass;
                    compData.Properties["useGravity"] = rb.useGravity;
                    compData.Properties["isKinematic"] = rb.isKinematic;
                }

                if (compData.Properties.Count > 0 || comp is Rigidbody)
                {
                    componentsList.Add(compData);
                }
            }
            return componentsList;
        }
    }
}