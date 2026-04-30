using UnityEngine;

public class CursorIndicatorBehavior : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Renderer _renderer;
    [SerializeField] private GameObject _gameObject;

    public Renderer Renderer => _renderer;
    public GameObject GameObject => _gameObject;
}
