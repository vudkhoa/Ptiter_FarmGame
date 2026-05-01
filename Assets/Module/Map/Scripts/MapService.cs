using Cysharp.Threading.Tasks;
using MyOwn.ServiceHarness;
using System;
using System.Threading;
using VContainer.Unity;

namespace Core.Module.MapService
{
    public class MapService : IService, IAsyncStartable, IDisposable
    {
        public MapService()
        {

        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {

        }

        public void Dispose()
        {

        }
    }
}