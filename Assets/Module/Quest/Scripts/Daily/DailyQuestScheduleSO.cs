using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Quest
{
    [CreateAssetMenu(fileName = "DailyQuestSchedule", menuName = "GDD/Quest/Daily Quest Schedule")]
    public sealed class DailyQuestScheduleSO : ScriptableObject
    {
        public int utcOffsetHours = 7;
        public List<DailyQuestSetSO> sets = new List<DailyQuestSetSO>();

        public string GetDayKey(DateTime utcNow)
        {
            return utcNow.AddHours(utcOffsetHours).ToString("yyyy-MM-dd");
        }

        public DateTime GetNextResetUtc(DateTime utcNow)
        {
            DateTime localNow = utcNow.AddHours(utcOffsetHours);
            DateTime nextLocalMidnight = localNow.Date.AddDays(1);
            return DateTime.SpecifyKind(nextLocalMidnight.AddHours(-utcOffsetHours), DateTimeKind.Utc);
        }

        public DailyQuestSetSO SelectSet(DateTime utcNow)
        {
            if (sets == null || sets.Count == 0) return null;
            DateTime localDate = utcNow.AddHours(utcOffsetHours).Date;
            long dayNumber = (long)(localDate - new DateTime(1970, 1, 1)).TotalDays;
            int index = (int)(((dayNumber % sets.Count) + sets.Count) % sets.Count);
            return sets[index];
        }

        public DailyQuestSetSO GetSetById(string setId)
        {
            if (string.IsNullOrWhiteSpace(setId) || sets == null) return null;
            for (int i = 0; i < sets.Count; i++)
            {
                DailyQuestSetSO set = sets[i];
                if (set != null && string.Equals(set.setId, setId, StringComparison.Ordinal))
                    return set;
            }
            return null;
        }
    }
}
