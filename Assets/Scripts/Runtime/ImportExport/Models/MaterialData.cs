namespace ImportExport.Models
{
    [System.Serializable]
    public class MaterialData
    {
        public string ShaderName;
        public float[] Color; // RGBA color array
        public string TextureBase64; // The actual image encoded as text
    }
}