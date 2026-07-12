using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using MessagePipe;
using MyOwn.ServiceHarness;
using VContainer.Unity;
using UnityEngine;

namespace myOwn.Firebase
{
    public class FirebaseCloudService : IService, IAsyncStartable, IDisposable
    {
        private const string COLLECTION = "player_data"; 
        
        private readonly IFirebaseGate _gate;
        private readonly ISubscriber<FirebaseReadyPayload> _readySub;
        private readonly PlayerDataHolder _holder;
        
        // IDisposable => Register that needs clean up while done.
        private IDisposable _readySubscription;

        public FirebaseCloudService(
            IFirebaseGate gate,
            ISubscriber<FirebaseReadyPayload> readySub,
            PlayerDataHolder holder)
        {
            _gate = gate;
            _readySub = readySub;
            _holder = holder;
        }
        
        public UniTask StartAsync(CancellationToken cancellation = new CancellationToken())
        {
            if (_gate.isReady) AcquireIdAsync().Forget();
            else _readySubscription = _readySub.Subscribe(_ => AcquireIdAsync().Forget());
            return UniTask.CompletedTask;
        }

        private async UniTaskVoid AcquireIdAsync()
        {
            try
            {
                PlayerData playerData = _holder.Data;
                if (playerData == null) return;

                // 1. Get UID from Auth of Firebase.
                if (string.IsNullOrEmpty(playerData.PlayerId))
                {
                    var auth = FirebaseAuth.DefaultInstance;
                    if (auth.CurrentUser == null)
                        await auth.SignInAnonymouslyAsync();

                    playerData.PlayerId = auth.CurrentUser.UserId;
                    // _holder.SaveImmediate(); // Waiting Merge code.
                }

                // 2. Writing to Firestore.
                var db = FirebaseFirestore.DefaultInstance;
                await db.Collection(COLLECTION)
                        .Document(playerData.PlayerId)
                        .SetAsync(
                            new Dictionary<string, object>
                            {
                                { "playerId", playerData.PlayerId }, 
                                { "time", Timestamp.GetCurrentTimestamp()}
                            }, SetOptions.MergeAll);
            }
            catch (Exception e) { Debug.LogError($"[FirebaseCloud] {e}"); }
        }

        public void Dispose() => _readySubscription?.Dispose();
    }
}