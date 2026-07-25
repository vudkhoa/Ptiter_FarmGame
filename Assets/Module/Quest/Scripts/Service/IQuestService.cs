using System.Collections.Generic;

namespace Core.Module.Quest
{
    public interface IQuestService
    {
        IReadOnlyList<QuestRuntimeState> ActiveQuests { get; }

        bool AcceptQuest(string questId);
        bool ReportEvent(QuestProgressEvent progressEvent);
        QuestRuntimeState GetQuestState(string questId);
    }
}
