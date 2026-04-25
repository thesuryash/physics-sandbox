using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ImportExport.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SceneManifest
    {
        // 1. The Fingerprint: Guarantees this JSON belongs to your project
        [JsonProperty("projectSignature", Required = Required.Always)]
        public string ProjectSignature { get; set; } = "PhysicsSandbox_ExportData";

        [JsonProperty("exportVersion")]
        public string ExportVersion { get; set; }

        [JsonProperty("exportDate")]
        public DateTime ExportDate { get; set; }

        // 2. The Strict Bouncer: Throws an error if the entities array is missing
        [JsonProperty("entities", Required = Required.Always)]
        public List<EntityNode> Entities { get; set; }

        public SceneManifest()
        {
            Entities = new List<EntityNode>();
        }
    }
}