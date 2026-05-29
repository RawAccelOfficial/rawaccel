namespace userspace_backend.Data.Profiles.Accel.Formula
{
    // Single source of truth for formula defaults: used by BackEndComposer DI and the Data DTOs.
    // UI defaults intentionally differ from native/Contracts; do not unify with Contracts.
    public static class FormulaDefaults
    {
        // Synchronous
        public const double SyncSpeed = 15;
        public const double Motivity = 1.4;
        public const double Gamma = 1;
        public const double Smoothness = 0.5;

        // Linear
        public const double LinearAcceleration = 0.01;
        public const double LinearOffset = 0;
        public const double LinearCap = 0;

        // Classic
        public const double ClassicAcceleration = 0.01;
        public const double ClassicExponent = 2;
        public const double ClassicOffset = 0;
        public const double ClassicCap = 0;

        // Power
        public const double PowerScale = 1;
        public const double PowerExponent = 0.05;
        public const double PowerOutputOffset = 0;
        public const double PowerCap = 0;

        // Jump
        public const double JumpSmooth = 0.5;
        public const double JumpInput = 15;
        public const double JumpOutput = 1.5;

        // Natural
        public const double NaturalDecayRate = 0.1;
        public const double NaturalInputOffset = 0;
        public const double NaturalLimit = 1.5;
    }
}
