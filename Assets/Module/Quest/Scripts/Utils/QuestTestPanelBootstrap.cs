#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Core.Module.Quest.View;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core.Module.Quest.Utils
{
    /// <summary>Creates and injects the runtime debug view; contains no quest presentation logic.</summary>
    public sealed class QuestTestPanelBootstrap : IStartable, IDisposable
    {
        private readonly IObjectResolver _resolver;
        private QuestTestPanelView _view;

        public QuestTestPanelBootstrap(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public void Start()
        {
            var viewObject = new GameObject("[Quest Test Panel]");
            UnityEngine.Object.DontDestroyOnLoad(viewObject);
            _view = viewObject.AddComponent<QuestTestPanelView>();
            _resolver.Inject(_view);
        }

        public void Dispose()
        {
            if (_view != null)
                UnityEngine.Object.Destroy(_view.gameObject);
        }
    }
}
#endif
