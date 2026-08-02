using Core.Module.Input;
using Core.Module.Loading;

namespace Core.Module.Cutscene
{
    /// <summary>
    /// Túi dependency truyền xuống mọi task. SO không nhận DI nên phải đi qua tham số.
    /// </summary>
    public sealed class CutsceneContext
    {
        public readonly IAssetLoader Loader;
        public readonly ICutsceneView View;
        public readonly IInputService Input;
        public readonly CutsceneRuntimeState State;
        public readonly string CutsceneId;

        public CutsceneContext(
            IAssetLoader loader,
            ICutsceneView view,
            IInputService input,
            CutsceneRuntimeState state,
            string cutsceneId)
        {
            Loader = loader;
            View = view;
            Input = input;
            State = state;
            CutsceneId = cutsceneId;
        }
    }
}
