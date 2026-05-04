using System;

namespace Core.Module.Time
{
    public readonly struct ClockManipulationDetectedPayload
    {
        public readonly DateTime ExpectedUtc;
        public readonly DateTime ActualUtc;
        public readonly TimeSpan Drift;

        public ClockManipulationDetectedPayload(DateTime expectedUtc, DateTime actualUtc)
        {
            ExpectedUtc = expectedUtc;
            ActualUtc = actualUtc;
            Drift = actualUtc - expectedUtc;
        }
    }
}