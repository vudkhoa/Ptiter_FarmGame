using System;

namespace Core.Module.Time
{
    public interface IServerTimeProvider
    {
        DateTime UtcNow { get; }
        bool IsSynced { get; }
        TimeSpan Offset { get; }
        DateTime LastSyncedAt { get; }
    }
}