using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Map
{
    [DisallowMultipleComponent]
    public sealed class MapAuthoringController : MonoBehaviour
    {
        [Header("Authoring")]
        [SerializeField] private bool _enableInEditor = true;
        [SerializeField] private MapLayoutSO _layout;

        private readonly List<MapLayoutEntry> _workingEntries = new();
        private readonly Rect _toggleRect = new(16f, 16f, 160f, 36f);
        private readonly Rect _toolbarRect = new(16f, 60f, 280f, 640f);
        private ObjectDatabaseSO _database;
        private MapService _map;
        private string _status = "Ready";
        private bool _isPanelVisible = true;

        public bool IsAuthoringMode
        {
            get
            {
#if UNITY_EDITOR
                return _enableInEditor;
#else
                return false;
#endif
            }
        }

        public MapLayoutSO Layout => _layout;
        public IReadOnlyList<MapLayoutEntry> WorkingEntries => _workingEntries;
        public bool IsEraseMode { get; private set; }
        public bool IsSelectMode { get; private set; }
        public string SelectedInstanceId { get; private set; }
        public float SelectedScale { get; private set; } = 1f;
        public string SelectedLabel { get; private set; }

        public void Initialize(MapService map, ObjectDatabaseSO database)
        {
            _map = map;
            _database = database;
            ReloadWorkingCopy();
        }

        public void RecordGridPlacement(string instanceId, int objectId, Vector3Int originCell)
        {
            if (!IsAuthoringMode) return;
            _workingEntries.Add(MapLayoutEntry.Grid(instanceId, objectId, originCell));
            _status = $"Unsaved: {_workingEntries.Count} objects";
        }

        public void RecordFreePlacement(string instanceId, int objectId, Vector3 worldPosition)
        {
            if (!IsAuthoringMode) return;
            _workingEntries.Add(MapLayoutEntry.Free(instanceId, objectId, worldPosition));
            _status = $"Unsaved: {_workingEntries.Count} objects";
        }

        public void RecordRemoval(string instanceId)
        {
            if (!IsAuthoringMode) return;
            int index = _workingEntries.FindIndex(entry => entry.InstanceId == instanceId);
            if (index >= 0) _workingEntries.RemoveAt(index);
            _status = $"Unsaved: {_workingEntries.Count} objects";
            if (SelectedInstanceId == instanceId) ClearSelection();
        }

        public void SetSelection(string instanceId, string label, float uniformScale)
        {
            SelectedInstanceId = instanceId;
            SelectedLabel = label;
            SelectedScale = NormalizeScale(uniformScale);
            _status = $"Selected: {label}";
        }

        public void ClearSelection()
        {
            SelectedInstanceId = null;
            SelectedLabel = null;
            SelectedScale = 1f;
        }

        public void UpdateSelectedGridPosition(Vector3Int originCell)
        {
            int index = FindSelectedEntryIndex();
            if (index < 0) return;
            MapLayoutEntry entry = _workingEntries[index];
            entry.OriginCell = originCell;
            _workingEntries[index] = entry;
            MarkUnsaved();
        }

        public void UpdateSelectedFreePosition(Vector3 worldPosition)
        {
            int index = FindSelectedEntryIndex();
            if (index < 0) return;
            MapLayoutEntry entry = _workingEntries[index];
            entry.WorldPosition = worldPosition;
            _workingEntries[index] = entry;
            MarkUnsaved();
        }

        public void UpdateSelectedScale(float uniformScale)
        {
            SelectedScale = NormalizeScale(uniformScale);
            int index = FindSelectedEntryIndex();
            if (index < 0) return;
            MapLayoutEntry entry = _workingEntries[index];
            entry.UniformScale = SelectedScale;
            _workingEntries[index] = entry;
            MarkUnsaved();
        }

        private int FindSelectedEntryIndex()
        {
            return string.IsNullOrEmpty(SelectedInstanceId)
                ? -1
                : _workingEntries.FindIndex(entry => entry.InstanceId == SelectedInstanceId);
        }

        private void MarkUnsaved()
        {
            _status = $"Unsaved: {_workingEntries.Count} objects";
        }

        public bool IsPointerOverToolbar(Vector2 screenPosition)
        {
            if (!IsAuthoringMode) return false;
            var guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            return _toggleRect.Contains(guiPosition)
                || (_isPanelVisible && _toolbarRect.Contains(guiPosition));
        }

        private void ReloadWorkingCopy()
        {
            IsEraseMode = false;
            IsSelectMode = false;
            ClearSelection();
            _workingEntries.Clear();
            if (_layout != null && _layout.Objects != null)
            {
                foreach (MapLayoutEntry sourceEntry in _layout.Objects)
                {
                    MapLayoutEntry entry = sourceEntry;
                    if (string.IsNullOrEmpty(entry.InstanceId))
                        entry.InstanceId = Guid.NewGuid().ToString("N");
                    entry.UniformScale = NormalizeScale(entry.UniformScale);
                    _workingEntries.Add(entry);
                }
            }
            _status = $"Loaded: {_workingEntries.Count} objects";
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!IsAuthoringMode) return;

            if (GUI.Button(_toggleRect, _isPanelVisible ? "Hide authoring" : "Show authoring"))
                _isPanelVisible = !_isPanelVisible;

            if (!_isPanelVisible) return;

            GUILayout.BeginArea(_toolbarRect, GUI.skin.window);
            GUILayout.Label("MAP AUTHORING", GUI.skin.box);
            GUILayout.Label(_layout != null ? _layout.name : "No MapLayout assigned");
            GUILayout.Label(_status);

            if (_database?.Objects != null)
            {
                GUILayout.Space(6f);
                GUILayout.Label("Place object");
                foreach (ObjectData data in _database.Objects)
                {
                    string placementLabel = data.PositionMode == PlacementPositionMode.Grid
                        ? $"Grid {data.Size.x}x{data.Size.y}"
                        : data.FreeSnapStep > 0f ? $"Free {data.FreeSnapStep:0.##}" : "Free";
                    if (GUILayout.Button($"{data.name}  [{placementLabel}]"))
                    {
                        IsEraseMode = false;
                        IsSelectMode = false;
                        ClearSelection();
                        _map?.StartPlacement(data.ID);
                    }
                }
            }

            GUILayout.Space(10f);
            if (GUILayout.Button(IsSelectMode ? "Select / Move: ON" : "Select / Move"))
            {
                IsSelectMode = !IsSelectMode;
                IsEraseMode = false;
                if (!IsSelectMode) ClearSelection();
                _map?.StopPlacement();
            }

            if (GUILayout.Button(IsEraseMode ? "Erase mode: ON" : "Erase object"))
            {
                IsEraseMode = !IsEraseMode;
                IsSelectMode = false;
                ClearSelection();
                _map?.StopPlacement();
            }

            if (IsSelectMode && !string.IsNullOrEmpty(SelectedInstanceId))
            {
                GUILayout.Space(6f);
                GUILayout.Label($"Selected: {SelectedLabel}");
                GUILayout.Label($"Scale: {SelectedScale:0.00}");
                float nextScale = GUILayout.HorizontalSlider(SelectedScale, 0.25f, 4f);
                if (!Mathf.Approximately(nextScale, SelectedScale))
                    _map?.SetSelectedAuthoringScale(nextScale);
            }

            if (GUILayout.Button("Cancel placement"))
            {
                IsEraseMode = false;
                IsSelectMode = false;
                ClearSelection();
                _map?.StopPlacement();
            }

            if (GUILayout.Button("Save layout"))
                SaveLayout();

            if (GUILayout.Button("Reload saved layout"))
            {
                ReloadWorkingCopy();
                _map?.ReloadAuthoringLayout();
            }

            if (GUILayout.Button("Clear working layout"))
            {
                _workingEntries.Clear();
                _map?.ClearAllPlacements();
                IsEraseMode = false;
                IsSelectMode = false;
                ClearSelection();
                _status = "Working layout cleared (not saved)";
            }

            GUILayout.Space(8f);
            GUILayout.Label("Authoring does not write PlayerData.");
            GUILayout.EndArea();
        }

        private static float NormalizeScale(float scale) => scale > 0f ? scale : 1f;

        private void SaveLayout()
        {
            if (_layout == null)
            {
                _status = "Assign a MapLayoutSO first";
                return;
            }

            _layout.Objects = new List<MapLayoutEntry>(_workingEntries);
            UnityEditor.EditorUtility.SetDirty(_layout);
            UnityEditor.AssetDatabase.SaveAssets();
            _status = $"Saved: {_workingEntries.Count} objects";
        }
#endif
    }
}
