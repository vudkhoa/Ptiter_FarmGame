using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Module.Quest
{
    public interface IFoodRecipeService
    {
        bool IsReady { get; }
        UniTask EnsureInitializedAsync(
            CancellationToken cancellationToken = default);
        FoodRecipeViewState GetViewState();
        UniTask<FoodRecipeUnlockResult> TryUnlockAsync(
            string recipeId,
            CancellationToken cancellationToken = default);
    }
}
