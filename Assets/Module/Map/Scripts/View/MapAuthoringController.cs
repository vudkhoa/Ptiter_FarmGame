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
            _workingEntries.Clear();
            if (_layout != null && _layout.Objects != null)
            {
                foreach (MapLayoutEntry sourceEntry in _layout.Objects)
                {
                    MapLayoutEntry entry = sourceEntry;
                    if (string.IsNullOrEmpty(entry.InstanceId))
                        entry.InstanceId = Guid.NewGuid().ToString("N");
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
                        _map?.StartPlacement(data.ID);
                    }
                }
            }

            GUILayout.Space(10f);
            if (GUILayout.Button(IsEraseMode ? "Erase mode: ON" : "Erase object"))
            {
                IsEraseMode = !IsEraseMode;
                _map?.StopPlacement();
            }

            if (GUILayout.Button("Cancel placement"))
            {
                IsEraseMode = false;
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
                _status = "Working layout cleared (not saved)";
            }

            GUILayout.Space(8f);
            GUILayout.Label("Authoring does not write PlayerData.");
            GUILayout.EndArea();
        }

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
