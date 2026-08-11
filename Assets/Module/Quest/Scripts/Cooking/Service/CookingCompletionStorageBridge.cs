using System;
using MessagePipe;
using VContainer.Unity;

namespace Core.Module.Quest.Cooking
{
    public sealed class CookingCompletionStorageBridge :
        IStartable,
        IDisposable
    {
        private readonly ICookingJobRepository _repository;
        private readonly IPublisher<CookingCompletionCommittedPayload>
            _committedPublisher;
        private readonly IDisposable _subscription;

        public CookingCompletionStorageBridge(
            ICookingJobRepository repository,
            ISubscriber<CookingCompletedPayload> completedSubscriber,
            IPublisher<CookingCompletionCommittedPayload>
                committedPublisher)
        {
            _repository = repository;
            _committedPublisher = committedPublisher;
            _subscription = completedSubscriber.Subscribe(OnCompleted);
        }

        public void Start()
        {
            // Subscription is established by construction.
        }

        private void OnCompleted(CookingCompletedPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.TransactionId) ||
                string.IsNullOrWhiteSpace(payload.OutputItemId) ||
                payload.Amount <= 0)
                return;

            if (!_repository.TryCommitCookingCompletion(payload))
                return;

            _committedPublisher.Publish(
                new CookingCompletionCommittedPayload(
                    payload.TransactionId,
                    payload.RecipeId));
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }
}
