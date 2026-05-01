using System;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// MessagePipe event: published mỗi 1s bởi ClockService.
    /// Dùng readonly struct để zero-alloc khi publish (struct được copy theo value).
    /// </summary>
    public readonly struct ClockTickPayload
    {
        /// <summary>Số tick đã phát từ lúc service start.</summary>
        public readonly int TickCount;

        /// <summary>Server-time UTC tại thời điểm tick (fallback DateTime.UtcNow nếu chưa có ServerTimeService).</summary>
        public readonly DateTime UtcNow;

        public ClockTickPayload(int tickCount, DateTime utcNow)
        {
            TickCount = tickCount;
            UtcNow = utcNow;
        }
    }
}
