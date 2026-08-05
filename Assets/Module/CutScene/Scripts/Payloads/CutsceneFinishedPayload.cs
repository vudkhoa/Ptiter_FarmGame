namespace Core.Module.Cutscene
{
    public readonly struct CutsceneFinishedPayload
    {
        public readonly string CutsceneId;
        public readonly CutsceneEndReason EndReason;

        public CutsceneFinishedPayload(string cutsceneId, CutsceneEndReason endReason)
        {
            CutsceneId = cutsceneId;
            EndReason = endReason;
        }
    }
}
