using System;
using System.Threading;
using Core.Module.Loading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Module.Farm
{
    // Loads FarmDatabaseSO during the boot sequence (IBootPreloader) and keeps it for scene consumers.
    public sealed class FarmDatabaseProvider : IFarmDatabaseProvider, IBootPreloader
    {
        private readonly FarmDatabaseReference _reference;   // RegisterInstance at root
        public FarmDatabaseSO Database { get; private set; }
        public string DisplayName => "Loading farm data...";

        public FarmDatabaseProvider(FarmDatabaseReference reference) => _reference = reference;

        public async UniTask PreloadAsync(IAssetLoader loader, CancellationToken ct)
        {
            if (_reference == null || !_reference.RuntimeKeyIsValid())
            {
                Debug.LogError("[FarmDatabaseProvider] FarmDatabaseReference is unassigned or its key is invalid.");
                return;
            }

            try
            {
                Database = await loader.LoadTrackerAsync(_reference);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FarmDatabaseProvider] Failed to load FarmDatabase: {e.Message}");
            }
        }
    }
}