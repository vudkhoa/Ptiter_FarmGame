namespace Core.Module.Cutscene
{
    /// <summary>Module khác publish cái này để xin phát cutscene, không cần ref tới Cutscene service.</summary>
    public readonly struct PlayCutsceneRequestPayload
    {
        public readonly string CutsceneId;

        public PlayCutsceneRequestPayload(string cutsceneId)
        {
            CutsceneId = cutsceneId;
        }
    }
}
