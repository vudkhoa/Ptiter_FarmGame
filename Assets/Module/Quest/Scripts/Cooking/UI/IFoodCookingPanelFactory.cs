using UnityEngine;

namespace Core.Module.Quest.Cooking
{
    public interface IFoodCookingPanelFactory
    {
        bool IsOpen { get; }
        GameObject Open(RectTransform parent, string recipeId);
        void Close();
    }
}
