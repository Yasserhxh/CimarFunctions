namespace CimarFunctions.Services.Sync;

public interface ISyncExecutionLock : IAsyncDisposable
{
    bool Acquired { get; }
}
