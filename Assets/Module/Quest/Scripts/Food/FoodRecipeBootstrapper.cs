using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace Core.Module.Quest
{
    public sealed class FoodRecipeBootstrapper : IAsyncStartable
    {
        private readonly IFoodRecipeService _service;

        public FoodRecipeBootstrapper(IFoodRecipeService service)
        {
            _service = service;
        }

        public UniTask StartAsync(CancellationToken cancellation)
        {
            return _service.EnsureInitializedAsync(cancellation);
        }
    }
}
