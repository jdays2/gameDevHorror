using EmergencyShift.Core.Randomness;
using EmergencyShift.Core.Time;

namespace EmergencyShift
{
    public sealed class WeaponControllerFactory
    {
        private readonly IGameClock clock;
        private readonly RandomSourceFactory randoms;

        public WeaponControllerFactory(IGameClock clock, RandomSourceFactory randoms)
        {
            this.clock = clock;
            this.randoms = randoms;
        }

        public WeaponController Create(IWeaponUseGate gate, IWeaponShotView view)
        {
            return new WeaponController(new WeaponOwnerModel(randoms.Create()), clock, gate, view);
        }
    }
}
