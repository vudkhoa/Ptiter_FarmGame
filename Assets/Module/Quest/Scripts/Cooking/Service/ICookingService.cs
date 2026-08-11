namespace Core.Module.Quest.Cooking
{
    public interface ICookingService
    {
        CookingRecipeState GetRecipeState(
            string recipeId,
            int requestedQuantity = 1);

        CookingStartResult TryStartCooking(
            string recipeId,
            int quantity);

        void Initialize();
        void Refresh();
    }
}
