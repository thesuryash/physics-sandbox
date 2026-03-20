using Newtonsoft.Json;

namespace ImportExport.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TransformState
    {
        [JsonProperty("position")]
        public float[] Position { get; set; }

        [JsonProperty("rotation")]
        public float[] Rotation { get; set; }

        [JsonProperty("scale")]
        public float[] Scale { get; set; }

        public TransformState()
        {
            Position = new float[3];
            Rotation = new float[3];
            Scale = new float[3] { 1f, 1f, 1f };
        }
    }
}