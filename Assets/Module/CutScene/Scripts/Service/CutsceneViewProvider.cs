using BrunoMikoski.UIManager;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core.Module.Cutscene
{
    /// <summary>
    /// Mở window cutscene y như mọi UI khác: lấy UIWindowIndirectReference rồi gọi Open().
    /// Open() tự Instantiate prefab ở lần đầu (đồng bộ), nên không cần WindowsManager lẫn await.
    /// Reference nằm trong CutsceneCatalogSO chứ không phải scene: scene nào cũng chiếu được, không phải kéo tay.
    /// </summary>
    public sealed class CutsceneViewProvider : ICutsceneViewProvider
    {
        private readonly ICutsceneCatalogProvider _catalogProvider;
        private readonly IObjectResolver _resolver;

        private CutsceneWindowController _view;

        #region VContainer
        public CutsceneViewProvider(ICutsceneCatalogProvider catalogProvider, IObjectResolver resolver)
        {
            _catalogProvider = catalogProvider;
            _resolver = resolver;
        }
        #endregion

        public ICutsceneView Acquire()
        {
            UIWindow window = _catalogProvider?.Catalog?.cutsceneWindow?.Ref;
            if (window == null)
            {
                Debug.LogError(
                    "[CutsceneViewProvider] CutsceneCatalogSO chưa gán Cutscene Window.");
                return null;
            }

            bool firstOpen = !window.HasWindowInstance;
            window.Open();

            if (window.WindowInstance is not CutsceneWindowController controller)
            {
                Debug.LogError($"[CutsceneViewProvider] '{window.name}' chưa trỏ tới prefab có CutsceneWindowController.");
                return null;
            }

            // Prefab sinh lúc runtime nên VContainer chưa đụng tới; inject tay cả cây con
            // để CutsceneSkipInput nhận được ICutsceneService. Chỉ cần làm 1 lần.
            if (firstOpen) _resolver.InjectGameObject(controller.gameObject);

            _view = controller;
            return _view;
        }
    }
}
