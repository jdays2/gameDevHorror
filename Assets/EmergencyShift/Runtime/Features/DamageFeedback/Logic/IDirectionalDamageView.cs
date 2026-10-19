using System.Collections.Generic;

namespace EmergencyShift.Features.DamageFeedback.Logic
{
    public interface IDirectionalDamageView
    {
        void Render(IReadOnlyList<DamagePulse> pulses);
    }
}
