namespace Core.Module.Cutscene
{
    /// <summary>Completed = 0 để Reset() mặc định là chạy trọn, chỉ ghi đè khi có sự cố. Gửi ra ngoài qua CutsceneFinishedPayload.</summary>
    public enum CutsceneEndReason
    {
        Completed = 0, // Chạy hết mọi step
        Skipped = 1,   // Người chơi bấm skip (chỉ khi cutscene bật allowSkip)
        Cancelled = 2, // Bị huỷ từ ngoài: đổi scene, Dispose service, ct của caller cancel
        Failed = 3,    // Lỗi giữa chừng: thiếu asset, task ném exception
    }
}
