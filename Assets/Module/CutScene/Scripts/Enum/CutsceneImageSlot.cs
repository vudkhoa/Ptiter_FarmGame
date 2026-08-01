namespace Core.Module.Cutscene
{
    /// <summary>Giá trị = index vào mảng _slots của CutsceneView nên thứ tự khai báo phải khớp thứ tự kéo slot trong Inspector. Số lớn hơn vẽ đè lên trên.</summary>
    public enum CutsceneImageSlot
    {
        Background = 0, // Lớp dưới cùng: ảnh nền
        Foreground = 1, // Lớp giữa: nhân vật / vật thể chính
        Overlay = 2,    // Lớp trên cùng: fade, khung viền, hiệu ứng phủ
    }
}
