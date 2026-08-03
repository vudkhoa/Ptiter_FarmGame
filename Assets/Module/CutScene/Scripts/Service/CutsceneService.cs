using System;
using System.Threading;
using Core.Module.Input;
using Core.Module.Loading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;

namespace Core.Module.Cutscene
{
    public sealed class CutsceneService : ICutsceneService, IDisposable
    {
        private readonly ICutsceneCatalogProvider _catalogProvider;
        private readonly CutsceneRunner _runner;
        private readonly IAssetLoader _loader;
        private readonly ICutsceneViewProvider _viewProvider;
        private readonly IInputService _input;
        private readonly IPublisher<CutsceneStartedPayload> _startedPub;
        private readonly IPublisher<CutsceneStepChangedPayload> _stepPub;
        private readonly IPublisher<CutsceneFinishedPayload> _finishedPub;

        private CancellationTokenSource _cts;
        private CutsceneRuntimeState _state;
        private CutsceneSO _current;
        private bool _disposed;

        public bool IsPlaying { get; private set; }

        #region VContainer
        // Nhận provider chứ không nhận CutsceneCatalogSO: thiếu catalog chỉ mất cutscene,
        // không được làm scope gameplay build hỏng (khác Farm/Quest - hai thằng đó fail-fast).
        public CutsceneService(
            ICutsceneCatalogProvider catalogProvider,
            CutsceneRunner runner,
            IAssetLoader loader,
            ICutsceneViewProvider viewProvider,
            IInputService input,
            IPublisher<CutsceneStartedPayload> startedPub,
            IPublisher<CutsceneStepChangedPayload> stepPub,
            IPublisher<CutsceneFinishedPayload> finishedPub)
        {
            _catalogProvider = catalogProvider;
            _runner = runner;
            _loader = loader;
            _viewProvider = viewProvider;
            _input = input;
            _startedPub = startedPub;
            _stepPub = stepPub;
            _finishedPub = finishedPub;
            Debug.Log($"[CutsceneService] VContainer initialized.");
        }

        public void Dispose()
        {
            // Đặt cờ trước khi Cancel: PlayAsync đang chạy sẽ vào finally và không publish lên broker đã chết.
            _disposed = true;
            if (_cts == null) return;

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        #endregion

        public async UniTask PlayAsync(string cutsceneId, CancellationToken ct = default)
        {
            if (IsPlaying)
            {
                Debug.LogWarning($"[CutsceneService] Đang phát '{_current?.cutsceneId}' - bỏ qua yêu cầu '{cutsceneId}'.");
                return;
            }

            var catalog = _catalogProvider?.Catalog;
            var cutscene = catalog != null ? catalog.GetById(cutsceneId) : null;
            if (cutscene == null)
            {
                Debug.LogError($"[CutsceneService] Không tìm thấy cutscene '{cutsceneId}' trong catalog.");
                return;
            }

            _current = cutscene;
            _state = new CutsceneRuntimeState();
            _state.Reset(cutscene.steps?.Count ?? 0);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var token = _cts.Token;

            // Window chỉ Instantiate ở lần Play đầu tiên nên phải lấy view TRƯỚC khi coi là đang phát.
            // Hỏng ở bước này thì chưa publish Started -> cũng không nợ ai Finished.
            ICutsceneView view = _viewProvider.Acquire();
            if (view == null)
            {
                ClearPlaybackState();
                return;
            }

            var ctx = new CutsceneContext(_loader, view, _input, _state, cutsceneId);

            IsPlaying = true;
            _startedPub.Publish(new CutsceneStartedPayload(cutsceneId, _state.TotalSteps));

            try
            {
                await view.ShowAsync(token);
                view.ResetSlots();

                await _runner.PrepareAllAsync(cutscene, ctx, token);
                await _runner.RunAsync(cutscene, ctx, token, PublishStepChanged);
            }
            catch (OperationCanceledException)
            {
                // Skip đã tự set Skipped; huỷ từ ngoài (đổi scene, Dispose) thì mới là Cancelled.
                if (_state.EndReason == CutsceneEndReason.Completed)
                    _state.EndReason = CutsceneEndReason.Cancelled;

                // Snap mọi task về khung cuối, tránh đứng hình giữa chừng.
                _runner.CompleteAll(cutscene, ctx);
            }
            catch (Exception e)
            {
                _state.EndReason = CutsceneEndReason.Failed;
                Debug.LogError($"[CutsceneService] Cutscene '{cutsceneId}' hỏng: {e}");
            }
            finally
            {
                var endReason = _state.EndReason;

                _runner.ReleaseAll(cutscene, ctx);

                // None thay vì token: bị skip vẫn phải fade panel ra, không tắt cụp.
                // Bọc try vì lúc teardown scene DOTween huỷ tween -> ném; kẹt ở đây là IsPlaying = true vĩnh viễn.
                try
                {
                    await view.HideAsync(CancellationToken.None);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CutsceneService] Đóng panel lỗi: {e.Message}");
                }

                IsPlaying = false;
                ClearPlaybackState();

                if (!_disposed) _finishedPub.Publish(new CutsceneFinishedPayload(cutsceneId, endReason));
            }
        }

        private void ClearPlaybackState()
        {
            _current = null;
            _state = null;
            _cts?.Dispose();
            _cts = null;
        }

        public void Skip()
        {
            if (!IsPlaying || _current == null || !_current.allowSkip) return;

            // Gán lý do TRƯỚC khi Cancel, nếu không catch sẽ hiểu nhầm thành Cancelled.
            if (_state != null) _state.EndReason = CutsceneEndReason.Skipped;
            _cts?.Cancel();
        }

        public void RequestNextStep()
        {
            if (!IsPlaying || _state == null) return;
            _state.NextStepRequested = true;
        }

        private void PublishStepChanged(int stepIndex, CutsceneStepSO step)
            => _stepPub.Publish(new CutsceneStepChangedPayload(_current.cutsceneId, stepIndex, step.stepId));
    }
}
