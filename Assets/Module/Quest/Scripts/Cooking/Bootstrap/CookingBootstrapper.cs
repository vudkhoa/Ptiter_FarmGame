using VContainer.Unity;

namespace Core.Module.Quest.Cooking
{
    public sealed class CookingBootstrapper : IStartable
    {
        private readonly ICookingService _service;

        public CookingBootstrapper(ICookingService service)
        {
            _service = service;
        }

        public void Start()
        {
            _service.Initialize();
        }
    }
}
