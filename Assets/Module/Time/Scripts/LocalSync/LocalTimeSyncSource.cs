using System;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Core.Module.Time
{
    public sealed class LocalTimeSyncSource : ITimeSyncSource
    {
        public async UniTask<SyncResult> SyncAsync(CancellationToken ct)
        {
            await UniTask.Yield();
            return SyncResult.SyncSuccess(TimeSpan.Zero, DateTime.UtcNow);
        }
    }
}