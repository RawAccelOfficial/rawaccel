using userspace_backend.Model.EditableSettings;

namespace userspace_backend_tests.ModelTests
{
    /// <summary>
    /// Shared parser instances for model tests. These were lost during the DI
    /// refactor, which left the EditableSettings* test files uncompilable (and
    /// therefore excluded from the build). Restoring them revives that coverage.
    /// </summary>
    internal static class UserInputParsers
    {
        public static IUserInputParser<int> IntParser { get; } = new IntParser();

        public static IUserInputParser<string> StringParser { get; } = new StringParser();
    }

    internal static class ModelValueValidators
    {
        public static IModelValueValidator<int> DefaultIntValidator { get; } = new DefaultModelValueValidator<int>();

        public static IModelValueValidator<string> DefaultStringValidator { get; } = new DefaultModelValueValidator<string>();
    }
}
