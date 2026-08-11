using System;
using System.Collections.Generic;
using MessagePipe;

namespace Core.Module.Quest.Cooking.UI
{
    public sealed class FoodCookingPanelPresenter : IDisposable
    {
        private readonly FoodCookingPanelView _view;
        private readonly ICookingService _service;
        private readonly string _recipeId;
        private readonly Action _closeAction;
        private readonly List<IDisposable> _subscriptions =
            new List<IDisposable>();

        private int _quantity = 1;
        private bool _starting;
        private bool _introPlaying;
        private bool _completionPlaying;
        private bool _showingCountdown;
        private bool _disposed;

        public FoodCookingPanelPresenter(
            FoodCookingPanelView view,
            ICookingService service,
            ISubscriber<CookingStateChangedPayload> stateSubscriber,
            ISubscriber<CookingCompletedPayload> completedSubscriber,
            string recipeId,
            Action closeAction)
        {
            _view = view;
            _service = service;
            _recipeId = recipeId;
            _closeAction = closeAction;

            _view.CloseRequested += OnCloseRequested;
            _view.MinusRequested += OnMinusRequested;
            _view.PlusRequested += OnPlusRequested;
            _view.CookRequested += OnCookRequested;
            _view.IntroCompleted += OnIntroCompleted;
            _view.CompletionFinished += OnCompletionFinished;
            _subscriptions.Add(stateSubscriber.Subscribe(OnStateChanged));
            _subscriptions.Add(completedSubscriber.Subscribe(OnCompleted));

            _view.ShowDetailImmediate();
            RenderCurrent(forceVisualState: true);
        }

        private void OnCloseRequested()
        {
            if (!_disposed)
                _closeAction?.Invoke();
        }

        private void OnMinusRequested()
        {
            if (_disposed || _introPlaying || _completionPlaying) return;

            _quantity = Math.Max(1, _quantity - 1);
            RenderCurrent(forceVisualState: false);
        }

        private void OnPlusRequested()
        {
            if (_disposed || _introPlaying || _completionPlaying) return;

            CookingRecipeState state =
                _service.GetRecipeState(_recipeId, _quantity);
            int maximum = Math.Min(
                state.MaxQuantity,
                Math.Max(1, state.MaxCraftable));
            _quantity = Math.Min(maximum, _quantity + 1);
            RenderCurrent(forceVisualState: false);
        }

        private void OnCookRequested()
        {
            if (_disposed || _starting || _introPlaying ||
                _completionPlaying)
                return;

            _starting = true;
            CookingStartResult result =
                _service.TryStartCooking(_recipeId, _quantity);
            _starting = false;

            if (!result.IsSuccess)
            {
                RenderCurrent(forceVisualState: false);
                return;
            }

            CookingRecipeState state = result.State;
            _quantity = Math.Max(1, state.CookingQuantity);
            _introPlaying = true;
            _showingCountdown = false;
            _view.PlayIntro(
                _quantity,
                state.RemainingSeconds);
        }

        private void OnIntroCompleted()
        {
            if (_disposed) return;

            _introPlaying = false;
            RenderCurrent(forceVisualState: true);
        }

        private void OnCompleted(CookingCompletedPayload payload)
        {
            if (_disposed || !string.Equals(
                    payload.RecipeId,
                    _recipeId,
                    StringComparison.Ordinal))
                return;

            _introPlaying = false;
            _showingCountdown = false;
            _completionPlaying = true;
            _view.PlayCompletion(Math.Max(1, payload.Quantity));
        }

        private void OnCompletionFinished()
        {
            if (_disposed) return;

            _completionPlaying = false;
            _quantity = 1;
            RenderCurrent(forceVisualState: false);
        }

        private void OnStateChanged(CookingStateChangedPayload payload)
        {
            if (_disposed || _starting || _introPlaying ||
                _completionPlaying)
                return;
            if (!string.IsNullOrEmpty(payload.RecipeId) &&
                !string.Equals(
                    payload.RecipeId,
                    _recipeId,
                    StringComparison.Ordinal))
                return;

            RenderCurrent(forceVisualState: false);
        }

        private void RenderCurrent(bool forceVisualState)
        {
            CookingRecipeState state =
                _service.GetRecipeState(_recipeId, _quantity);
            if (state.IsCooking)
            {
                _quantity = Math.Max(1, state.CookingQuantity);
                if (!_showingCountdown || forceVisualState)
                {
                    _showingCountdown = true;
                    _view.ShowCountdown(
                        _quantity,
                        state.RemainingSeconds);
                }
                else
                {
                    _view.UpdateCountdown(
                        _quantity,
                        state.RemainingSeconds);
                }
                return;
            }

            if (_showingCountdown || forceVisualState)
            {
                _showingCountdown = false;
                _view.ShowDetailImmediate();
            }

            int maximum = Math.Min(
                state.MaxQuantity,
                Math.Max(1, state.MaxCraftable));
            _quantity = Math.Max(1, Math.Min(_quantity, maximum));
            state = _service.GetRecipeState(_recipeId, _quantity);
            _view.RenderDetail(state, _quantity);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _view.CloseRequested -= OnCloseRequested;
            _view.MinusRequested -= OnMinusRequested;
            _view.PlusRequested -= OnPlusRequested;
            _view.CookRequested -= OnCookRequested;
            _view.IntroCompleted -= OnIntroCompleted;
            _view.CompletionFinished -= OnCompletionFinished;
            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();
            _subscriptions.Clear();
        }
    }
}
