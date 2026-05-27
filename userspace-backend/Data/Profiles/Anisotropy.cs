namespace userspace_backend.Data.Profiles
{
    public class Anisotropy
    {
        public Vector2 Domain { get; set; } = new Vector2 { X = 1, Y = 1 };

        public Vector2 Range { get; set; } = new Vector2 { X = 1, Y = 1 };

        public double LPNorm { get; set; } = 2.0;

        public bool CombineXYComponents { get; set; }
    }
}
