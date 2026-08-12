#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using VContainer.Unity;
using UInput = UnityEngine.Input;

namespace Core.Module.Tutorial
{
    /// <summary>
    /// Editor and dev-build only. F9 replays the tutorial from step one without wiping the rest
    /// of the save, so a flow can be re-tested in seconds instead of by deleting playerdata.json.
    /// </summary>
    public sealed class TutorialDebugController : ITickable
    {
        private const KeyCode ResetKey = KeyCode.F9;

        private readonly ITutorialService _tutorial;

        public TutorialDebugController(ITutorialService tutorial) => _tutorial = tutorial;

        public void Tick()
        {
            if (!UInput.GetKeyDown(ResetKey)) return;

            _tutorial.ResetProgress();
            Debug.Log($"[TutorialDebug] Progress reset - restarting from the first flow. ({ResetKey})");
        }
    }
}
#endif
