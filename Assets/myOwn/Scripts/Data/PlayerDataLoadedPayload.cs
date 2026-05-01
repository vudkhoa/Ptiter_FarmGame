namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// MessagePipe event: published khi PlayerDataHolder.Load() hoàn tất.
    /// Subscribers (UI, gameplay services) react để khởi tạo state ban đầu.
    /// </summary>
    public readonly struct PlayerDataLoadedPayload
    {
        /// <summary>True nếu Load() KHÔNG tìm thấy save file → tạo PlayerData mặc định.</summary>
        public readonly bool IsNewlyCreated;

        public PlayerDataLoadedPayload(bool isNewlyCreated)
        {
            IsNewlyCreated = isNewlyCreated;
        }
    }
}
