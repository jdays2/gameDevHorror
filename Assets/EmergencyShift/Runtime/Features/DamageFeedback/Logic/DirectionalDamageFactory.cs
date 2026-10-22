using EmergencyShift.Features.DamageFeedback.Config;

namespace EmergencyShift.Features.DamageFeedback.Logic
{
    /// <summary>Each HUD owns its model and presentation lifetime, even with a shared style.</summary>
    public sealed class DirectionalDamageFactory
    {
        private readonly ICameraImpactView camera;

        public DirectionalDamageFactory(ICameraImpactView camera)
        {
            this.camera = camera ?? throw new System.ArgumentNullException(nameof(camera));
        }

        public DirectionalDamageController Create(DamageFeedbackSettings settings, IDirectionalDamageView view)
        {
            return new DirectionalDamageController(new DirectionalDamageModel(settings), view, camera);
        }
    }
}
