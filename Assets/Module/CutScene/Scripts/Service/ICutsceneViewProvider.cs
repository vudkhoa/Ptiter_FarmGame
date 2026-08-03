namespace Core.Module.Cutscene
{
    /// <summary>
    /// Cấp view theo yêu cầu. Instance chỉ sinh ở lần Play đầu tiên, không nằm sẵn trong scene.
    /// </summary>
    public interface ICutsceneViewProvider
    {
        /// <summary>Trả null nếu window chưa được cấu hình - service phải tự bỏ qua cutscene.</summary>
        ICutsceneView Acquire();
    }
}
