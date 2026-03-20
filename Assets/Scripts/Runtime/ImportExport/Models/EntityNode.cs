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

        public EntityNode()
        {
            Components = new List<ComponentData>();
            Dependencies = new List<string>();
        }
    }
}