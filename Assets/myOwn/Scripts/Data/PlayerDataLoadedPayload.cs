namespace MyOwn.ServiceHarness
{
    /// <summary>MessagePipe event: PlayerDataHolder.Load() hoàn tất. Subscribers init state ban đầu.</summary>
    public readonly struct PlayerDataLoadedPayload
    {
        /// <summary>True khi không tìm thấy save file → tạo PlayerData mặc định.</summary>
        public readonly bool IsNewlyCreated;

        public PlayerDataLoadedPayload(bool isNewlyCreated)
        {
            IsNewlyCreated = isNewlyCreated;
        }
    }
}
