namespace userspace_backend.Data.Profiles.Accel
{
    public class NoAcceleration : Acceleration
    {
        public override AccelerationDefinitionType Type => AccelerationDefinitionType.None;
    }
}
