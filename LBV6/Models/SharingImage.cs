namespace LBV6.Models
{
    /// <summary>
    /// Represents an image being used for social sharing, i.e. open-graph, twitter cards, etc.
    /// Can describe attachment images (legacy) and photos (new).
    /// </summary>
    public class SharingImage
    {
        public string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}