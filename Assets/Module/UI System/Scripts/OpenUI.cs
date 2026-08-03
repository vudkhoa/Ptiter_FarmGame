using BrunoMikoski.UIManager;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn vào bất kỳ Button nào, chọn window ở dropdown Inspector, ấn là mở.
/// Window nào cũng dùng được miễn nó nằm trong UIWindowCollection.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class OpenUI : MonoBehaviour
{
    [SerializeField] private UIWindowIndirectReference _window;
    [SerializeField] private Button _button;

    [Tooltip("Bật = ấn lần nữa thì đóng window đang mở. Tắt = ấn thêm không làm gì.")]
    [SerializeField] private bool _toggle;

    private void OnEnable()
    {
        if (_button != null) _button.onClick.AddListener(Open);
    }

    private void OnDisable()
    {
        if (_button != null) _button.onClick.RemoveListener(Open);
    }

    /// <summary>Public để gọi được từ onClick khác hoặc từ code.</summary>
    public void Open()
    {
        UIWindow window = _window?.Ref;
        if (window == null)
        {
            Debug.LogError($"[OpenUI] '{name}' chưa chọn window.", this);
            return;
        }

        if (window.IsOpen())
        {
            if (_toggle) window.Close();
            return;
        }

        window.Open();
    }

    private void OnValidate() => _button ??= GetComponent<Button>();
}
