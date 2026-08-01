using WinStream.Core.Persistence;

namespace WinStream.Core.Streaming;

/// <summary>Thrown when a receiver requires a password but none was supplied.</summary>
public sealed class ReceiverPasswordRequiredException : InvalidOperationException
{
    public ReceiverPasswordRequiredException()
        : base(ConnectionFailureCopy.PasswordRequiredDetail)
    {
    }
}

/// <summary>
/// Resolves the password policy independently of WinUI: no password for open
/// receivers, then stored credentials, then one prompt, otherwise a typed failure.
/// </summary>
public static class ReceiverPasswordResolver
{
    public static async Task<string?> ResolveAsync(
        IReceiverPasswordStore store,
        string receiverKey,
        bool requiresPassword,
        Func<CancellationToken, Task<string?>>? promptAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(receiverKey);

        if (!requiresPassword)
        {
            return null;
        }

        if (store.TryGet(receiverKey, out var stored) &&
            !string.IsNullOrWhiteSpace(stored))
        {
            return stored;
        }

        if (promptAsync is not null)
        {
            var prompted = await promptAsync(cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(prompted))
            {
                return prompted.Trim();
            }
        }

        throw new ReceiverPasswordRequiredException();
    }

    /// <summary>Store-only lookup for reconnects that must never display UI.</summary>
    public static string? StoredOrNull(
        IReceiverPasswordStore store,
        string receiverKey,
        bool requiresPassword)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(receiverKey);

        return requiresPassword &&
               store.TryGet(receiverKey, out var stored) &&
               !string.IsNullOrWhiteSpace(stored)
            ? stored
            : null;
    }
}
