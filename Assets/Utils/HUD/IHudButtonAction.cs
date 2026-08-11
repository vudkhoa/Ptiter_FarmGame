namespace Shared.Utils.HUD
{
    /// <summary>Action contract executed by a reusable HUD button.</summary>
    public interface IHudButtonAction
    {
        bool CanExecute();
        bool TryExecute();
    }
}
