namespace EmergencyShift
{
    public interface IWeaponShotView
    {
        void PresentShot(WeaponShotPlan plan);
        void PresentEmptyTrigger();
    }
}
