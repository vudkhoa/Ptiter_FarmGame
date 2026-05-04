using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace Core.Module.Time
{
    public interface ITimeSyncSource
    {
        UniTask<SyncResult> SyncAsync(CancellationToken ct);
    }

    public readonly struct SyncResult
    {
        public readonly bool Success;
        public readonly TimeSpan Offset;
        public readonly DateTime SyncedAtUtc;
        public readonly string ErrorMessage;

        private SyncResult(bool success, TimeSpan offset, DateTime syncedAtUtc, string errorMessage)
        {
            Success = success;
            Offset = offset;
            SyncedAtUtc = syncedAtUtc;
            ErrorMessage = errorMessage;
        }

        public static SyncResult SyncSuccess(TimeSpan offset, DateTime syncedAtUtc) => new SyncResult(
            success: true,
            offset,
            syncedAtUtc,
            errorMessage: null);

        public static SyncResult SyncFailure(string errorMessage) => new SyncResult(
            success: false,
            offset: TimeSpan.Zero,
            syncedAtUtc: default,
            errorMessage);
    }
}