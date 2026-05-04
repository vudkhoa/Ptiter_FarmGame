using System;

namespace Core.Module.Time
{
    public readonly struct ServerTimeSyncedPayload
    {
        public readonly TimeSpan Offset;
        public readonly DateTime SyncedAtUtc;

        public ServerTimeSyncedPayload(TimeSpan offset, DateTime syncedAtUtc)
        {
            Offset = offset;
            SyncedAtUtc = syncedAtUtc;
        }
    }
}