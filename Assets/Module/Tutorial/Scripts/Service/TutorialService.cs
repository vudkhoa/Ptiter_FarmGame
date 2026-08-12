using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Core.Module.Tutorial
{
    /// <summary>
    /// Owns "which step is on screen". Flows run strictly in catalog order and each finished step
    /// is written to disk immediately, so a crash mid-tutorial resumes on the next step instead
    /// of replaying the whole thing.
    /// </summary>
    public sealed class TutorialService : ITutorialService, IAsyncStartable, IDisposable
    {
        private readonly TutorialCatalogSO _catalog;
        private readonly ITutorialRepository _repository;
        private readonly ITutorialView _view;
        private readonly IPublisher<TutorialStepStartedPayload> _stepStartedPub;
        private readonly IPublisher<TutorialStepCompletedPayload> _stepCompletedPub;
        private readonly IPublisher<TutorialFlowCompletedPayload> _flowCompletedPub;

        private TutorialSaveData _progress;
        private TutorialFlowSO _currentFlow;
        private TutorialStepSO _currentStep;
        private bool _flowArmed;
        private bool _initialized;
        private bool _finished;
        private bool _disposed;

        public bool IsRunning => _currentStep != null;
        public string CurrentFlowId => _currentFlow != null ? _currentFlow.flowId : null;
        public string CurrentStepId => _currentStep != null ? _currentStep.stepId : null;

        #region VContainer
        // Takes ITutorialView, not a provider: the container lives in the Preloading scene and is
        // resolved once at boot. A missing container binds NullTutorialView instead of failing the scope.
        public TutorialService(
            TutorialCatalogSO catalog,
            ITutorialRepository repository,
            ITutorialView view,
            IPublisher<TutorialStepStartedPayload> stepStartedPub,
            IPublisher<TutorialStepCompletedPayload> stepCompletedPub,
            IPublisher<TutorialFlowCompletedPayload> flowCompletedPub)
        {
            _catalog = catalog;
            _repository = repository;
            _view = view;
            _stepStartedPub = stepStartedPub;
            _stepCompletedPub = stepCompletedPub;
            _flowCompletedPub = flowCompletedPub;
        }

        /// <summary>
        /// The save file is read by another IAsyncStartable during the same container start, so
        /// the tutorial waits for it rather than racing it and reading empty progress.
        /// </summary>
        public async UniTask StartAsync(CancellationToken cancellation)
        {
            try
            {
                await _repository.WaitUntilLoadedAsync(cancellation);
            }
            catch (OperationCanceledException)
            {
                // Quitting or reloading before the save arrived - nothing to run.
                return;
            }

            Initialize();
        }

        public void Dispose()
        {
            _disposed = true;
            _view?.Hide();
        }
        #endregion

        #region Initialization
        private void Initialize()
        {
            if (_initialized || _disposed) return;

            if (_catalog == null)
            {
                Debug.LogWarning("[TutorialService] No TutorialCatalogSO assigned - tutorial disabled.");
                _initialized = true;
                _finished = true;
                return;
            }

            _initialized = true;
            _progress = _repository.LoadTutorial() ?? new TutorialSaveData();

            if (!_catalog.tutorialEnabled)
            {
                _finished = true;
                return;
            }

            SeedProgressForExistingSave();
            Advance();
        }

        /// <summary>
        /// First contact with a save file. A player who already has a farm must never be shown
        /// "place your first plot", so their untouched tutorial data is retired on sight.
        /// </summary>
        private void SeedProgressForExistingSave()
        {
            if (_progress.initialized) return;

            _progress.initialized = true;
            bool retireForVeteran = _catalog.skipForExistingSaves && !_repository.IsNewPlayer;

            if (retireForVeteran && _catalog.flows != null)
            {
                for (int i = 0; i < _catalog.flows.Count; i++)
                {
                    TutorialFlowSO flow = _catalog.flows[i];
                    if (flow != null && flow.IsValid) _progress.MarkFlowCompleted(flow.flowId);
                }
            }

            Persist();
        }
        #endregion

        #region Flow control
        /// <summary>
        /// Walks forward to the first thing that needs the player: an armed step to display, a
        /// flow waiting on its start signal, or the end of the catalog. Loops instead of recursing
        /// because a step with no completion signal finishes the instant it is reached.
        /// </summary>
        private void Advance()
        {
            if (_finished || _disposed) return;

            // Bounded by the number of authored steps; the guard only catches a malformed catalog.
            int safety = 0;
            const int maxIterations = 512;

            while (safety++ < maxIterations)
            {
                TutorialFlowSO flow = _catalog.GetFirstIncompleteFlow(_progress);
                if (flow == null)
                {
                    Finish();
                    return;
                }

                if (flow != _currentFlow)
                {
                    _currentFlow = flow;
                    _flowArmed = flow.startSignal == TutorialSignal.None;
                }

                if (!_flowArmed)
                {
                    // Waiting for the trigger - e.g. the harvest flow sleeps until a crop ripens.
                    ClearCurrentStep();
                    return;
                }

                TutorialStepSO step = FindNextStep(flow);
                if (step == null)
                {
                    CompleteFlow(flow);
                    continue;
                }

                ShowStep(flow, step);

                // A step with no completion signal is a pass-through beat: consume it and keep going.
                if (step.completionSignal != TutorialSignal.None) return;
                CompleteStep(step);
            }

            Debug.LogError(
                $"[TutorialService] Aborted after {maxIterations} iterations. " +
                "Check the catalog for steps with duplicate ids or an empty completion signal.");
            Finish();
        }

        private TutorialStepSO FindNextStep(TutorialFlowSO flow)
        {
            for (int i = 0; i < flow.steps.Count; i++)
            {
                TutorialStepSO step = flow.steps[i];
                if (step == null) continue;

                if (!step.IsValid)
                {
                    Debug.LogWarning(
                        $"[TutorialService] Step '{step.name}' in flow '{flow.flowId}' has no step id - skipped.",
                        step);
                    continue;
                }

                if (!_progress.IsStepCompleted(step.stepId)) return step;
            }

            return null;
        }

        private void ShowStep(TutorialFlowSO flow, TutorialStepSO step)
        {
            _currentStep = step;
            _view?.ShowStep(step);

            _stepStartedPub.Publish(new TutorialStepStartedPayload(
                flow.flowId, step.stepId, flow.steps.IndexOf(step), flow.steps.Count));
        }

        private void CompleteStep(TutorialStepSO step)
        {
            string flowId = _currentFlow != null ? _currentFlow.flowId : null;
            ClearCurrentStep();

            if (!_progress.MarkStepCompleted(step.stepId)) return;

            Persist();
            _stepCompletedPub.Publish(new TutorialStepCompletedPayload(flowId, step.stepId));
        }

        private void CompleteFlow(TutorialFlowSO flow)
        {
            ClearCurrentStep();
            _currentFlow = null;
            _flowArmed = false;

            if (!_progress.MarkFlowCompleted(flow.flowId)) return;

            Persist();

            bool isLast = _catalog.GetFirstIncompleteFlow(_progress) == null;
            _flowCompletedPub.Publish(new TutorialFlowCompletedPayload(flow.flowId, isLast));
        }

        private void ClearCurrentStep()
        {
            _currentStep = null;
            _view?.Hide();
        }

        private void Finish()
        {
            _finished = true;
            _currentFlow = null;
            ClearCurrentStep();
        }
        #endregion

        #region Signals
        public void ReportSignal(TutorialSignal signal)
        {
            if (signal == TutorialSignal.None || _finished || !_initialized || _disposed) return;

            if (_currentStep != null)
            {
                if (_currentStep.completionSignal != signal) return;

                CompleteStep(_currentStep);
                Advance();
                return;
            }

            if (_currentFlow == null || _flowArmed) return;
            if (_currentFlow.startSignal != signal) return;

            _flowArmed = true;
            Advance();
        }

        public void ResetProgress()
        {
            _progress = new TutorialSaveData { initialized = true };
            Persist();

            _currentFlow = null;
            _currentStep = null;
            _flowArmed = false;
            _finished = false;
            _initialized = true;
            _view?.Hide();

            if (_catalog != null && _catalog.tutorialEnabled) Advance();
        }

        private void Persist()
        {
            if (_repository.SaveTutorial(_progress)) return;

            // Losing the write means the step replays next launch - annoying, never fatal.
            Debug.LogError("[TutorialService] Failed to persist tutorial progress.");
        }
        #endregion
    }
}
