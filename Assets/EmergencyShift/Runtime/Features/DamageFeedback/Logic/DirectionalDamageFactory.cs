using EmergencyShift.Features.DamageFeedback.Config;

namespace EmergencyShift.Features.DamageFeedback.Logic
{
    /// <summary>Each HUD owns its model and presentation lifetime, even with a shared style.</summary>
    public sealed class DirectionalDamageFactory
    {
        public DirectionalDamageController Create(DamageFeedbackSettings settings, IDirectionalDamageView view)
        {
            return new DirectionalDamageController(new DirectionalDamageModel(settings), view);
        }
    }
}
