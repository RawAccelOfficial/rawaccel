using System.Threading.Tasks;

namespace userinterface.Interfaces
{
    public interface IAsyncInitializable
    {
        Task InitializeAsync();
        bool IsInitialized { get; }
        bool IsInitializing { get; }
    }
}