namespace EmergencyShift
{
    public readonly struct WeaponInputIntent
    {
        public bool PrimarySlot { get; }
        public bool SecondarySlot { get; }
        public bool Reload { get; }
        public bool FirePressed { get; }
        public bool FireHeld { get; }

        public WeaponInputIntent(bool primarySlot, bool secondarySlot, bool reload, bool firePressed, bool fireHeld)
        {
            PrimarySlot = primarySlot;
            SecondarySlot = secondarySlot;
            Reload = reload;
            FirePressed = firePressed;
            FireHeld = fireHeld;
        }
    }
}
