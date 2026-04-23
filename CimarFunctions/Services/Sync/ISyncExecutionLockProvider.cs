namespace CimarFunctions.Services.Sync;

public interface ISyncExecutionLockProvider
{
    Task<ISyncExecutionLock> TryAcquireAsync(
        string resourceName,
        CancellationToken cancellationToken = default);
}
