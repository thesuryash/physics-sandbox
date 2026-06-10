using System.Collections.Generic;
using Newtonsoft.Json;

namespace ImportExport.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class EntityNode
    {
        [JsonProperty("parentId")]
        public string ParentId { get; set; } // The ID of the parent GameObject

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("transform")]
        public TransformState Transform { get; set; }

        [JsonProperty("components")]
        public List<ComponentData> Components { get; set; }

        // This is the list your topological sort will read
        [JsonProperty("dependencies")]
        public List<string> Dependencies { get; set; }

        //// THE FIX: We explicitly tell Newtonsoft it is allowed to save this!
        //[JsonProperty("mesh")]
        //public MeshData Mesh;

        [Newtonsoft.Json.JsonProperty("mesh")]
        public MeshData Mesh;

        // Add this line for the textures!
        [Newtonsoft.Json.JsonProperty("material")]
        public MaterialData Material;
        public EntityNode()
        {
            Components = new List<ComponentData>();
            Dependencies = new List<string>();
        }
    }
}