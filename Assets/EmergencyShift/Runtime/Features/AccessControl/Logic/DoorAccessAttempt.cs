namespace EmergencyShift.Features.AccessControl.Logic
{
    public readonly struct DoorAccessAttempt
    {
        public DoorAccessAttempt(bool granted, bool hasFeedback, bool showNotice)
        {
            Granted = granted;
            HasFeedback = hasFeedback;
            ShowNotice = showNotice;
        }

        public bool Granted { get; }
        public bool HasFeedback { get; }
        public bool ShowNotice { get; }
    }
}
