using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Crashlytics;
using MessagePipe;
using MyOwn.ServiceHarness;
using VContainer.Unity;
namespace myOwn.Firebase
{
    public class FirebaseInitService : IAsyncStartable, IFirebaseGate, IService
    {
        public bool isReady { get; private set; }
        private readonly IPublisher<FirebaseReadyPayload> _readyPub;

        // DI -> Call
        public FirebaseInitService(IPublisher<FirebaseReadyPayload> readyPub)
        {
            _readyPub = readyPub;
        }

        public async UniTask StartAsync(CancellationToken cancellation = new CancellationToken())
        {
            // await result
            var status = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (status != DependencyStatus.Available)
            {
                Debug.LogError($"[FirebaseInit] Dependencies not available: {status}");
                return;
            }

            // Report Crashlytics
            Crashlytics.ReportUncaughtExceptionsAsFatal = true;

            isReady = true;
            _readyPub.Publish(new FirebaseReadyPayload());
        }
    }
}