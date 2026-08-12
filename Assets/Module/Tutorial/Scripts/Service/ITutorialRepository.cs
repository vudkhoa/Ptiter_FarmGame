using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Module.Tutorial
{
    /// <summary>
    /// Storage contract the tutorial module needs. Implemented by PlayerDataHolder so
    /// tutorial progress rides the same save file as the rest of the player state.
    /// </summary>
    public interface ITutorialRepository
    {
        bool IsLoaded { get; }

        /// <summary>
        /// True when this session created the save file rather than reading one from disk.
        /// Read once, on the first tutorial evaluation, to tell a fresh install apart from a
        /// veteran whose save predates the tutorial.
        /// </summary>
        bool IsNewPlayer { get; }

        /// <summary>Completes once the save file has been read, so the tutorial never evaluates empty data.</summary>
        UniTask WaitUntilLoadedAsync(CancellationToken cancellationToken);

        /// <summary>Returns a defensive copy; never null once the save is loaded.</summary>
        TutorialSaveData LoadTutorial();

        /// <summary>Writes immediately - a replayed tutorial step is worse than a dropped frame.</summary>
        bool SaveTutorial(TutorialSaveData data);
    }
}
