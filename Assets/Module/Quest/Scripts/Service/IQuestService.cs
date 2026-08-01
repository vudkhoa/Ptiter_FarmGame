using System.Collections.Generic;

namespace Core.Module.Quest
{
    public interface IQuestService
    {
        IReadOnlyList<QuestRuntimeState> ActiveQuests { get; }

        bool ActivateQuest(string runtimeId, string definitionId, QuestRuntimeSnapshot snapshot = null);
        bool DeactivateQuest(string runtimeId);
        int DeactivateQuests(IEnumerable<string> runtimeIds);
        bool ReportEvent(QuestProgressEvent progressEvent);
        QuestRuntimeState GetQuestState(string runtimeId);
        QuestRuntimeSnapshot CreateSnapshot(string runtimeId);
    }
}
