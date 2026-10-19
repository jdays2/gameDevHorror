using System;

namespace EmergencyShift
{
    /// <summary>Validates the acquisition boundary; duplicate pickup is an ordinary rejection.</summary>
    public sealed class AcquireFlashlightCommand
    {
        private readonly FlashlightOwnership ownership;

        public AcquireFlashlightCommand(FlashlightOwnership ownership)
        {
            this.ownership = ownership ?? throw new ArgumentNullException(nameof(ownership));
        }

        public bool CanExecute => !ownership.IsOwned;

        public bool Execute()
        {
            if (!CanExecute)
            {
                return false;
            }

            ownership.Acquire();
            return true;
        }
    }
}
