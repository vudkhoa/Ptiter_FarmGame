using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Module.Cutscene
{
    public interface ICutsceneService
    {
        bool IsPlaying { get; }

        UniTask PlayAsync(string cutsceneId, CancellationToken ct = default);

        /// <summary>Huỷ cả cutscene. No-op nếu CutsceneSO.allowSkip = false.</summary>
        void Skip();

        /// <summary>Tap để qua khung kế tiếp, không huỷ cutscene.</summary>
        void RequestNextStep();
    }
}
