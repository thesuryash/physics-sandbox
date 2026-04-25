using System.Collections.Generic;
using Newtonsoft.Json;

namespace ImportExport.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ComponentData
    {
        // The fully qualified type name of the script/component
        [JsonProperty("type")]
        public string Type { get; set; }

        // Holds the actual variables (e.g., "mass": 5.0, "drag": 1.2)
        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; }

        public Dictionary<string, string> References { get; set; }

        public ComponentData()
        {
            Properties = new Dictionary<string, object>();
        }
    }
}