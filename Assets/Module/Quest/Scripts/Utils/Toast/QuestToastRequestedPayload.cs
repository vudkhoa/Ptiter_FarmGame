namespace Core.Module.Quest.Utils
{
    public enum QuestToastStyle
    {
        Info = 0,
        Success = 1
    }

    public readonly struct QuestToastRequestedPayload
    {
        public readonly string Message;
        public readonly QuestToastStyle Style;
        public readonly float Duration;

        public QuestToastRequestedPayload(
            string message,
            QuestToastStyle style = QuestToastStyle.Info,
            float duration = 1.8f)
        {
            Message = message;
            Style = style;
            Duration = duration;
        }
    }
}
