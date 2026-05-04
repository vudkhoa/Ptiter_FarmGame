using System;

namespace Core.Module.Time
{
    /// <summary>
    /// MessagePipe event: ClockService publish mỗi 1s. readonly struct → zero-alloc.
    /// </summary>
    public readonly struct ClockTickPayload
    {
        /// <summary>Số tick từ lúc service start.</summary>
        public readonly int TickCount;

        /// <summary>UTC time tại thời điểm tick.</summary>
        public readonly DateTime UtcNow;

        public ClockTickPayload(int tickCount, DateTime utcNow)
        {
            TickCount = tickCount;
            UtcNow = utcNow;
        }
    }
}
