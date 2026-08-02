namespace Core.Module.Cutscene
{
    /// <summary>Parallel = 0 để mặc định trong Inspector là chạy song song.</summary>
    public enum CutsceneRunMode
    {
        Parallel = 0,   // Song song
        Sequential = 1, // Tuần tự
    }
}