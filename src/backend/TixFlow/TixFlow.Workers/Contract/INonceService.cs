namespace TixFlow.Workers.Contract;

public interface INonceService
{
    Task InitializeAsync();
    long GetNextNonce();
}
