using System;
using System.Threading;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace Core.Module.Time
{
    public sealed class WebTimeSyncSource : ITimeSyncSource
    {
        private const string TIME_API_URL = "https://worldtimeapi.org/api/timezone/Etc/UTC";

        [Serializable]
        private class WorldTimeApiResponse
        {
            public string utc_datetime;
        }

        public async UniTask<SyncResult> SyncAsync(CancellationToken ct)
        {
            using (var request = UnityWebRequest.Get(TIME_API_URL))
            {
                try
                {
                    await request.SendWebRequest().WithCancellation(ct);

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var json = request.downloadHandler.text;
                        var response = UnityEngine.JsonUtility.FromJson<WorldTimeApiResponse>(json);

                        if (response != null && DateTime.TryParse(response.utc_datetime, out var utcTime))
                        {
                            TimeSpan offset = utcTime - DateTime.UtcNow;
                            return SyncResult.SyncSuccess(offset, utcTime);
                        }
                    }
                    return SyncResult.SyncFailure($"API request failed: {request.error}");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    return SyncResult.SyncFailure($"Failed to fetch server time: {e.Message}");
                }
            }
        }
    }
}
