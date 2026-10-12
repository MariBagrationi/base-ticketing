namespace TixFlow.Workers.Contract;

public interface INonceService
{
    Task InitializeAsync();
    long GetNextNonce();

    /// <summary>Re-reads the pending nonce from the chain after a failed send, so no gap is left behind.</summary>
    Task ResyncAsync() => InitializeAsync();
}
