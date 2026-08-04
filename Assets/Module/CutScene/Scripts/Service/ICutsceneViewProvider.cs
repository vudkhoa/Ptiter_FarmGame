namespace Core.Module.Cutscene
{
    /// <summary>
    /// Sở hữu vòng đời window cutscene. Instance chỉ sinh ở lần Play đầu tiên, không nằm sẵn trong scene.
    /// </summary>
    public interface ICutsceneViewProvider
    {
        /// <summary>Mở window. Trả null nếu chưa cấu hình - service phải tự bỏ qua cutscene.</summary>
        ICutsceneView Acquire();

        /// <summary>Đóng window. Animation ra do transitionOut trong prefab lo.</summary>
        void Hide();
    }
}
