using System;

namespace Core.Module.Time
{
    public interface IServerTimeProvider
    {
        // 4 Properties
        DateTime UtcNow { get; }
        bool IsSynced { get; }
        TimeSpan Offset { get; }
        DateTime LastSyncedAt { get; }
    }
}