using WinStream.Core.Persistence;
using WinStream.Core.Protocol.AirPlay2;

namespace WinStream.Core.Streaming;

/// <summary>
/// Builds <see cref="PairingOptions"/> the orchestrator and tests share: store
/// callbacks, clear-before-attempt, and the transient-pairing mark. Callers own the
/// session map; this helper only invokes the supplied clear/mark actions.
/// </summary>
public static class PairingOptionsFactory
{
    public static PairingOptions Create(
        IPairingCredentialStore store,
        string receiverKey,
        Func<CancellationToken, Task<string?>>? requestPinAsync = null,
        Action? clearTransient = null,
        Action? markTransient = null,
        string? receiverPassword = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(receiverKey);

        // Each attempt re-decides its pairing mode. Clearing first matters on a quality
        // rebuild that reuses a SessionEntry: a previous temporary pairing must not keep
        // claiming Accept-every-time after a successful pair-verify.
        clearTransient?.Invoke();

        // Password-protected receivers must use the password as the transient SRP
        // secret. Persistent pair-verify then SETUP 401s; skip identity/PIN paths.
        if (!string.IsNullOrEmpty(receiverPassword))
        {
            return new PairingOptions { ReceiverPassword = receiverPassword };
        }

        return new PairingOptions
        {
            StoredCredentials = store.TryGet(receiverKey, out var stored) ? stored : null,
            RequestPinAsync = requestPinAsync,
            OnPaired = credentials => store.Save(receiverKey, credentials),
            OnStoredCredentialsRejected = () => store.Remove(receiverKey),
            OnTransientPairing = markTransient
        };
    }
}
