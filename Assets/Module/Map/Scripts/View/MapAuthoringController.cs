using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Map
{
    [DisallowMultipleComponent]
    public sealed class MapAuthoringController : MonoBehaviour
    {
        [Header("Authoring")]
#if UNITY_EDITOR
        [SerializeField] private bool _enableInEditor = true;
#endif
        [SerializeField] private MapLayoutSO _layout;

        private readonly List<MapLayoutEntry> _workingEntries = new();
        private readonly HashSet<Vector3Int> _workingDecorBlockedCells = new();
        private readonly Rect _toggleRect = new(16f, 16f, 160f, 36f);
        private readonly Rect _toolbarRect = new(16f, 60f, 280f, 760f);
        private ObjectDatabaseSO _database;
        private MapService _map;
        private GameObject _decorBlockerOverlay;
        private Mesh _decorBlockerMesh;
        private Material _decorBlockerMaterial;
        private string _status = "Ready";
        private bool _isPanelVisible = true;
        private bool _showDecorBlockers = true;
        private Vector2 _toolbarScroll;

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
        public IReadOnlyCollection<Vector3Int> WorkingDecorBlockedCells => _workingDecorBlockedCells;
        public bool IsEraseMode { get; private set; }
        public bool IsSelectMode { get; private set; }
        public bool IsDecorBlockerPaintMode { get; private set; }
        public bool IsDecorBlockerEraseMode { get; private set; }
        public bool IsDecorBlockerMode => IsDecorBlockerPaintMode || IsDecorBlockerEraseMode;
        public string SelectedInstanceId { get; private set; }
        public float SelectedScale { get; private set; } = 1f;
        public string SelectedLabel { get; private set; }

        public void Initialize(MapService map, ObjectDatabaseSO database)
        {
            _map = map;
            _database = database;
            ReloadWorkingCopy();
            RebuildDecorBlockerOverlay();
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

        public void RecordDecorBlocker(Vector3Int cell, bool blocked)
        {
            if (!IsAuthoringMode) return;

            bool changed = blocked
                ? _workingDecorBlockedCells.Add(cell)
                : _workingDecorBlockedCells.Remove(cell);
            if (!changed) return;

            _status = $"Unsaved: {_workingDecorBlockedCells.Count} decor blocker cells";
            RebuildDecorBlockerOverlay();
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
            IsDecorBlockerPaintMode = false;
            IsDecorBlockerEraseMode = false;
            ClearSelection();
            _workingEntries.Clear();
            _workingDecorBlockedCells.Clear();
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
            if (_layout != null && _layout.DecorBlockedCells != null)
            {
                foreach (Vector3Int cell in _layout.DecorBlockedCells)
                    _workingDecorBlockedCells.Add(new Vector3Int(cell.x, 0, cell.z));
            }
            _status = $"Loaded: {_workingEntries.Count} objects, {_workingDecorBlockedCells.Count} blockers";
            RebuildDecorBlockerOverlay();
        }

        private void OnDestroy()
        {
            if (_decorBlockerOverlay != null) Destroy(_decorBlockerOverlay);
            if (_decorBlockerMesh != null) Destroy(_decorBlockerMesh);
            if (_decorBlockerMaterial != null) Destroy(_decorBlockerMaterial);
        }

        private void RebuildDecorBlockerOverlay()
        {
            if (!IsAuthoringMode || _map == null)
            {
                if (_decorBlockerOverlay != null) _decorBlockerOverlay.SetActive(false);
                return;
            }

            EnsureDecorBlockerOverlay();
            _decorBlockerOverlay.SetActive(_showDecorBlockers && _workingDecorBlockedCells.Count > 0);
            if (_decorBlockerMesh == null) return;

            _decorBlockerMesh.Clear();
            if (_workingDecorBlockedCells.Count == 0) return;

            var vertices = new List<Vector3>(_workingDecorBlockedCells.Count * 4);
            var triangles = new List<int>(_workingDecorBlockedCells.Count * 6);
            var colors = new List<Color>(_workingDecorBlockedCells.Count * 4);
            Color overlayColor = new(1f, 0.12f, 0.08f, 0.38f);

            foreach (Vector3Int cell in _workingDecorBlockedCells)
            {
                int first = vertices.Count;
                Vector3 a = _map.CellToWorld(cell);
                Vector3 b = _map.CellToWorld(cell + new Vector3Int(1, 0, 0));
                Vector3 c = _map.CellToWorld(cell + new Vector3Int(1, 0, 1));
                Vector3 d = _map.CellToWorld(cell + new Vector3Int(0, 0, 1));
                const float overlayY = 0.035f;
                a.y += overlayY;
                b.y += overlayY;
                c.y += overlayY;
                d.y += overlayY;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                vertices.Add(d);
                colors.Add(overlayColor);
                colors.Add(overlayColor);
                colors.Add(overlayColor);
                colors.Add(overlayColor);
                triangles.Add(first);
                triangles.Add(first + 2);
                triangles.Add(first + 1);
                triangles.Add(first);
                triangles.Add(first + 3);
                triangles.Add(first + 2);
            }

            _decorBlockerMesh.SetVertices(vertices);
            _decorBlockerMesh.SetColors(colors);
            _decorBlockerMesh.SetTriangles(triangles, 0);
            _decorBlockerMesh.RecalculateBounds();
        }

        private void EnsureDecorBlockerOverlay()
        {
            if (_decorBlockerOverlay != null) return;

            _decorBlockerOverlay = new GameObject("DecorBlocker Overlay");
            _decorBlockerOverlay.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var filter = _decorBlockerOverlay.AddComponent<MeshFilter>();
            var renderer = _decorBlockerOverlay.AddComponent<MeshRenderer>();
            _decorBlockerMesh = new Mesh { name = "DecorBlocker Overlay Mesh" };
            filter.sharedMesh = _decorBlockerMesh;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                _decorBlockerMaterial = new Material(shader) { name = "DecorBlocker Overlay Material" };
                renderer.sharedMaterial = _decorBlockerMaterial;
            }
            renderer.sortingOrder = -7;
        }

        private static float NormalizeScale(float scale) => scale > 0f ? scale : 1f;

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!IsAuthoringMode) return;

            if (GUI.Button(_toggleRect, _isPanelVisible ? "Hide authoring" : "Show authoring"))
                _isPanelVisible = !_isPanelVisible;

            if (!_isPanelVisible) return;

            GUILayout.BeginArea(_toolbarRect, GUI.skin.window);
            _toolbarScroll = GUILayout.BeginScrollView(_toolbarScroll);
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
                        IsDecorBlockerPaintMode = false;
                        IsDecorBlockerEraseMode = false;
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
                IsDecorBlockerPaintMode = false;
                IsDecorBlockerEraseMode = false;
                if (!IsSelectMode) ClearSelection();
                _map?.StopPlacement();
            }

            if (GUILayout.Button(IsEraseMode ? "Erase mode: ON" : "Erase object"))
            {
                IsEraseMode = !IsEraseMode;
                IsSelectMode = false;
                IsDecorBlockerPaintMode = false;
                IsDecorBlockerEraseMode = false;
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

            GUILayout.Space(10f);
            GUILayout.Label($"Decor blockers: {_workingDecorBlockedCells.Count}");
            if (GUILayout.Button(IsDecorBlockerPaintMode ? "Paint blocker: ON" : "Paint decor blocker"))
            {
                IsDecorBlockerPaintMode = !IsDecorBlockerPaintMode;
                IsDecorBlockerEraseMode = false;
                IsEraseMode = false;
                IsSelectMode = false;
                ClearSelection();
                _map?.StopPlacement();
            }
            if (GUILayout.Button(IsDecorBlockerEraseMode ? "Erase blocker: ON" : "Erase decor blocker"))
            {
                IsDecorBlockerEraseMode = !IsDecorBlockerEraseMode;
                IsDecorBlockerPaintMode = false;
                IsEraseMode = false;
                IsSelectMode = false;
                ClearSelection();
                _map?.StopPlacement();
            }
            if (GUILayout.Button(_showDecorBlockers ? "Hide blocker overlay" : "Show blocker overlay"))
            {
                _showDecorBlockers = !_showDecorBlockers;
                RebuildDecorBlockerOverlay();
            }

            if (GUILayout.Button("Cancel placement"))
            {
                IsEraseMode = false;
                IsSelectMode = false;
                IsDecorBlockerPaintMode = false;
                IsDecorBlockerEraseMode = false;
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
                _workingDecorBlockedCells.Clear();
                _map?.ClearAllPlacements();
                _map?.ClearAllDecorBlockers();
                IsEraseMode = false;
                IsSelectMode = false;
                IsDecorBlockerPaintMode = false;
                IsDecorBlockerEraseMode = false;
                ClearSelection();
                RebuildDecorBlockerOverlay();
                _status = "Working layout cleared (not saved)";
            }

            GUILayout.Space(8f);
            GUILayout.Label("Authoring does not write PlayerData.");
            GUILayout.EndScrollView();
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
            var sortedBlockers = new List<Vector3Int>(_workingDecorBlockedCells);
            sortedBlockers.Sort((left, right) =>
            {
                int xComparison = left.x.CompareTo(right.x);
                return xComparison != 0 ? xComparison : left.z.CompareTo(right.z);
            });
            _layout.DecorBlockedCells = sortedBlockers;
            UnityEditor.EditorUtility.SetDirty(_layout);
            UnityEditor.AssetDatabase.SaveAssets();
            _status = $"Saved: {_workingEntries.Count} objects, {sortedBlockers.Count} blockers";
        }
#endif
    }
}
