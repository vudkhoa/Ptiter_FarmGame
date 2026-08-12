using System;
using System.Collections.Generic;
using Core.Module.Farm;
using Core.Module.Map;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Core.Module.Tutorial.Integration.Farm
{
    /// The only place that knows both Farm/Map payloads and TutorialSignal. Also pins the world
    /// anchors the hand points at. Scene-scoped: it needs IMapService for cell to world conversion.
    public sealed class TutorialGameplaySignalBridge : IStartable, IDisposable
    {
        /// Cells searched outward from the middle of the view before giving up.
        private const int FreePlotSearchRadius = 12;

        /// Viewport band the suggested plot must land in. Keeps it clear of the object picker, the
        /// HUD and the cancel button, all of which are back on screen for the NEXT step.
        private static readonly Rect SafePlotViewport = Rect.MinMaxRect(0.38f, 0.30f, 0.80f, 0.72f);

        // Matches FarmVisualizer's soil offset so the hand lands on the crop, not the ground under it.
        private static readonly Vector3 CropVisualOffset = new Vector3(0f, 0.6f, 0f);

        private readonly ITutorialService _tutorial;
        private readonly IMapService _mapService;
        private readonly IFarmService _farmService;
        private readonly FarmDatabaseSO _farmDatabase;
        private readonly ObjectDatabaseSO _objectDatabase;
        private readonly IDisposable _subscriptions;

        /// Squared cell distance of the restored plot currently pinned; nearest one wins.
        private float _restoredPlotDistance = float.MaxValue;

        /// Search origin cached across the restore burst so the camera ray is cast once.
        private Vector3Int? _searchOrigin;

        public TutorialGameplaySignalBridge(
            ITutorialService tutorial,
            IMapService mapService,
            IFarmService farmService,
            FarmDatabaseSO farmDatabase,
            ObjectDatabaseSO objectDatabase,
            ISubscriber<MapPlacementStartedPayload> placementStartedSub,
            ISubscriber<MapFurnitureAddedPayload> furnitureAddedSub,
            ISubscriber<OpenFarmSelectorUIPayload> selectorOpenedSub,
            ISubscriber<FarmEntityPlantedPayload> plantedSub,
            ISubscriber<FarmEntityRipePayload> ripeSub,
            ISubscriber<FarmEntityHarvestedPayload> harvestedSub)
        {
            _tutorial = tutorial;
            _mapService = mapService;
            _farmService = farmService;
            _farmDatabase = farmDatabase;
            _objectDatabase = objectDatabase;

            var bag = DisposableBag.CreateBuilder();
            placementStartedSub.Subscribe(OnPlacementStarted).AddTo(bag);
            furnitureAddedSub.Subscribe(OnFurnitureAdded).AddTo(bag);
            selectorOpenedSub.Subscribe(OnSelectorOpened).AddTo(bag);
            plantedSub.Subscribe(OnPlanted).AddTo(bag);
            ripeSub.Subscribe(OnRipe).AddTo(bag);
            harvestedSub.Subscribe(OnHarvested).AddTo(bag);
            _subscriptions = bag.Build();
        }

        #region Lifecycle
        /// FarmEntityRipePayload only fires the moment a crop finishes growing, so a player who quit
        /// before harvesting comes back to a ripe farm and no event. Replay the signal once.
        public void Start()
        {
            if (!TryFindRipeCrop(out Vector3Int cell)) return;

            TutorialAnchorRegistry.SetWorldPoint(
                TutorialAnchorIds.RipeCrop,
                _mapService.CellToWorld(cell) + CropVisualOffset,
                CellHalfExtent(cell));
            _tutorial.ReportSignal(TutorialSignal.CropRipe);
        }

        public void Dispose()
        {
            _subscriptions?.Dispose();

            // The anchors point into a scene that is going away; a stale world point would
            // park the hand over whatever happens to be at those coordinates next.
            TutorialAnchorRegistry.ClearWorldPoint(TutorialAnchorIds.NewPlot);
            TutorialAnchorRegistry.ClearWorldPoint(TutorialAnchorIds.RipeCrop);
            TutorialAnchorRegistry.ClearWorldPoint(TutorialAnchorIds.FreePlot);
        }
        #endregion

        #region Event Handlers
        private void OnPlacementStarted(MapPlacementStartedPayload payload)
        {
            if (!IsSoil(payload.ObjectId)) return;

            // Pin BEFORE reporting: this signal completes the previous step, so the next one goes
            // on screen inside this same call and would otherwise find nothing to point at.
            PinFreePlotAnchor(payload.ObjectId);
            _tutorial.ReportSignal(TutorialSignal.LandPlacementStarted);
        }

        private void OnFurnitureAdded(MapFurnitureAddedPayload payload)
        {
            if (!IsSoil(payload.ObjectId)) return;

            // The same payload replays for every saved placement at scene load. Only a placement
            // the player just performed animates, and only that one is a tutorial beat.
            if (!payload.AnimatePlacement)
            {
                ConsiderRestoredPlot(payload);
                return;
            }

            TutorialAnchorRegistry.ClearWorldPoint(TutorialAnchorIds.FreePlot);
            TutorialAnchorRegistry.SetWorldPoint(
                TutorialAnchorIds.NewPlot,
                payload.SnappedWorld + CropVisualOffset,
                CellHalfExtent(payload.Cell));

            // Soil paints continuously, so the next tap would drop a second plot instead of opening
            // the seed picker. Free play keeps the brush - only a running tutorial hands it in.
            if (_tutorial.IsRunning) _mapService.StopPlacement(false);

            _tutorial.ReportSignal(TutorialSignal.LandPlaced);
        }

        private void OnSelectorOpened(OpenFarmSelectorUIPayload payload)
        {
            if (payload.IsAnimal) return;

            _tutorial.ReportSignal(TutorialSignal.SeedSelectorOpened);
        }

        private void OnPlanted(FarmEntityPlantedPayload payload)
        {
            if (payload.EntityType != FarmEntityType.Crop) return;

            // The plot is no longer bare, so nothing should be pointing at it as one.
            TutorialAnchorRegistry.ClearWorldPoint(TutorialAnchorIds.NewPlot);
            _tutorial.ReportSignal(TutorialSignal.SeedPlanted);
        }

        private void OnRipe(FarmEntityRipePayload payload)
        {
            if (payload.EntityType != FarmEntityType.Crop) return;

            TutorialAnchorRegistry.SetWorldPoint(
                TutorialAnchorIds.RipeCrop,
                _mapService.CellToWorld(payload.Cell) + CropVisualOffset,
                CellHalfExtent(payload.Cell));
            _tutorial.ReportSignal(TutorialSignal.CropRipe);
        }

        private void OnHarvested(FarmEntityHarvestedPayload payload)
        {
            if (payload.EntityType != FarmEntityType.Crop) return;

            TutorialAnchorRegistry.ClearWorldPoint(TutorialAnchorIds.RipeCrop);
            _tutorial.ReportSignal(TutorialSignal.CropHarvested);
        }
        #endregion

        #region Private Methods
        private bool TryFindRipeCrop(out Vector3Int cell)
        {
            cell = default;

            IReadOnlyList<FarmSlotSaveData> slots = _farmService?.ActiveSlots;
            if (slots == null || _farmDatabase == null) return false;

            for (int i = 0; i < slots.Count; i++)
            {
                FarmSlotSaveData slot = slots[i];
                if (slot == null || slot.state != FarmSlotState.Ripe) continue;

                FarmEntityData entity = _farmDatabase.GetEntityById(slot.entityId);
                if (entity == null || entity.entityType != FarmEntityType.Crop) continue;

                cell = new Vector3Int(slot.cellX, slot.cellY, slot.cellZ);
                return true;
            }

            return false;
        }

        /// Points at a cell the player can genuinely build on. A fixed screen position used to land
        /// on the shop roof, telling them to tap somewhere placement would refuse.
        private void PinFreePlotAnchor(int objectId)
        {
            if (!TryFindPlaceableCell(objectId, out Vector3Int cell))
            {
                TutorialAnchorRegistry.ClearWorldPoint(TutorialAnchorIds.FreePlot);
                Debug.LogWarning(
                    "[TutorialGameplaySignalBridge] No buildable cell found near the view centre; " +
                    "the place-land step will wait until one is on screen.");
                return;
            }

            // CellToWorld is exactly where MapPreviewView parks its own grid cursor and where the
            // soil prefab lands, so the marker sits on the spot the game considers "this cell".
            TutorialAnchorRegistry.SetWorldPoint(
                TutorialAnchorIds.FreePlot, _mapService.CellToWorld(cell), CellHalfExtent(cell));
        }

        /// Half the world footprint of one grid cell. Every world anchor ships it so the view sizes
        /// the ring by projection instead of a guessed pixel size that breaks when the camera zooms.
        private Vector3 CellHalfExtent(Vector3Int cell)
        {
            Vector3 origin = _mapService.CellToWorld(cell);
            Vector3 diagonal = _mapService.CellToWorld(cell + new Vector3Int(1, 0, 1)) - origin;
            return new Vector3(Mathf.Abs(diagonal.x), 0f, Mathf.Abs(diagonal.z)) * 0.5f;
        }

        /// Rings outward from whatever the camera is looking at, nearest cell wins. Runs twice: the
        /// second pass drops the safe-viewport demand, since an awkward plot beats no guidance.
        private bool TryFindPlaceableCell(int objectId, out Vector3Int result)
        {
            if (TryFindPlaceableCell(objectId, true, out result)) return true;

            return TryFindPlaceableCell(objectId, false, out result);
        }

        private bool TryFindPlaceableCell(int objectId, bool requireSafeViewport, out Vector3Int result)
        {
            Vector3Int origin = ResolveSearchOrigin();

            for (int radius = 0; radius <= FreePlotSearchRadius; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        // Only the ring itself: inner cells were already rejected on earlier passes.
                        if (radius > 0 && Mathf.Abs(dx) != radius && Mathf.Abs(dz) != radius) continue;

                        Vector3Int candidate = origin + new Vector3Int(dx, 0, dz);
                        if (!_mapService.CanPlaceObjectAt(objectId, candidate)) continue;
                        if (requireSafeViewport && !IsInSafeViewport(candidate)) continue;

                        result = candidate;
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        private bool IsInSafeViewport(Vector3Int cell)
        {
            Camera camera = Camera.main;
            // No camera to judge with: let the cell through rather than rejecting everything.
            if (camera == null) return true;

            Vector3 viewport = camera.WorldToViewportPoint(_mapService.CellToWorld(cell));
            if (viewport.z <= 0f) return false;

            return SafePlotViewport.Contains(new Vector2(viewport.x, viewport.y));
        }

        private Vector3Int ResolveSearchOrigin()
        {
            Camera camera = Camera.main;
            if (camera == null) return Vector3Int.zero;

            // Where the middle of the screen meets the ground plane - the patch of map the player
            // is actually looking at, so the hand never points off screen.
            Ray ray = camera.ScreenPointToRay(
                new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            Plane ground = new Plane(Vector3.up, Vector3.zero);

            return ground.Raycast(ray, out float distance)
                ? _mapService.WorldToCell(ray.GetPoint(distance))
                : Vector3Int.zero;
        }

        /// Re-pins the new-plot anchor onto a plot restored from the save, so the plant-seed step
        /// still has a target. Nearest to the middle of the view wins, which keeps the hand on screen.
        private void ConsiderRestoredPlot(in MapFurnitureAddedPayload payload)
        {
            if (HasCrop(payload.Cell)) return;

            Vector3Int offset = payload.Cell - RestoreSearchOrigin();
            float distance = offset.x * offset.x + offset.z * offset.z;
            if (distance >= _restoredPlotDistance) return;

            _restoredPlotDistance = distance;
            TutorialAnchorRegistry.SetWorldPoint(
                TutorialAnchorIds.NewPlot,
                payload.SnappedWorld + CropVisualOffset,
                CellHalfExtent(payload.Cell));
        }

        private bool HasCrop(Vector3Int cell)
        {
            FarmSlotSaveData slot = _farmService?.GetSlotAt(cell);
            return slot != null && !string.IsNullOrEmpty(slot.entityId);
        }

        /// Cached on purpose: the restore replays every saved placement back to back and the camera
        /// cannot move in between. PinFreePlotAnchor keeps using the live origin instead.
        private Vector3Int RestoreSearchOrigin()
        {
            _searchOrigin ??= ResolveSearchOrigin();
            return _searchOrigin.Value;
        }

        private bool IsSoil(int objectId)
        {
            if (_objectDatabase == null) return false;
            if (!_objectDatabase.TryGetById(objectId, out ObjectData data, out _)) return false;

            return data.FarmRole == FarmObjectRole.Soil;
        }
        #endregion
    }
}
