using System;
using UnityEngine;

public class PreviewSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _previewOffset = 0.06f;

    [SerializeField] private CursorIndicatorBehavior _cursorIndicator;
    private GameObject previewObject;

    [SerializeField] private Material _previewMaterialPrefab;
    private Material previewMaterialInstance;

    // Runtime
    MaterialPropertyBlock materialPropertyBlock;

    #region Unity Life Cycle
    private void Start()
    {
        if (_previewMaterialPrefab != null)
            previewMaterialInstance = new Material(_previewMaterialPrefab);

        if (_cursorIndicator != null && _cursorIndicator.GameObject != null)
            _cursorIndicator.GameObject.SetActive(false);

        if (materialPropertyBlock == null)
        {
            materialPropertyBlock = new MaterialPropertyBlock();
        }
    }
    #endregion

    #region Main Logic
    public void StartShowingPlacementPreview(GameObject prefab, Vector2Int size)
    {
        previewObject = Instantiate(prefab);
        PreparePreview(previewObject);
        PrepareCursor(size);
        _cursorIndicator.GameObject.SetActive(true);
    }

    private void PrepareCursor(Vector2Int size)
    {
        if (size.x > 0 && size.y > 0)
        {
            _cursorIndicator.transform.localScale = new Vector3(size.x, 1, size.y);

            _cursorIndicator.Renderer.GetPropertyBlock(materialPropertyBlock);
            materialPropertyBlock.SetVector("_MainTex_ST", new Vector4(size.x, size.y, 0f, 0f));
            _cursorIndicator.Renderer.SetPropertyBlock(materialPropertyBlock);
        }
    }

    private void PreparePreview(GameObject previewObject)
    {
        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++) 
        {
            Material[] materials = renderers[i].materials;
            for (int j = 0; j < materials.Length; j++)
            {
                materials[j] = previewMaterialInstance;
            }
            renderers[i].materials = materials;
        }
    }

    public void StopShowingPreview()
    {
        _cursorIndicator.GameObject.SetActive(false);
        Destroy(previewObject);
    }

    public void UpdatePosition(Vector3 position, bool validity)
    {
        MovePreview(position);
        MoveCursor(position);
        ApplyFeedback(validity);
    }

    private void ApplyFeedback(bool validity)
    {
        Color c = validity ? Color.white : Color.red;

        _cursorIndicator.Renderer.GetPropertyBlock(materialPropertyBlock);
        materialPropertyBlock.SetColor("_Color", c);
        _cursorIndicator.Renderer.SetPropertyBlock(materialPropertyBlock);
    }

    private void MoveCursor(Vector3 position)
    {
        _cursorIndicator.transform.position = position;
    }

    private void MovePreview(Vector3 position)
    {
        previewObject.transform.position = new Vector3(
            position.x, 
            position.y + _previewOffset, 
            position.z
            );
    }
    #endregion
}
