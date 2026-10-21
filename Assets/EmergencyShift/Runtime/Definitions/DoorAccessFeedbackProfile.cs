using System;
using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Door access feedback")]
    public sealed class DoorAccessFeedbackProfile : ScriptableObject
    {
        [SerializeField]
        private AudioClip denied;
        [SerializeField]
        private AudioClip granted;
        [SerializeField, Min(0)]
        private float confirmationDelay = .24f;
        [SerializeField, Min(.1f)]
        private float denialCooldown = .8f;
        [SerializeField, Range(0, 1)]
        private float volume = .32f;
        [SerializeField, Min(1)]
        private float hearingDistance = 7;
        [SerializeField, Min(0)]
        private float fullVolumeRadius = 2;
        [SerializeField, Min(.01f)]
        private float panDistance = 6;
        [SerializeField, Range(0, 1)]
        private float maximumPan = .75f;

        public AudioClip Denied => denied;
        public AudioClip Granted => granted;
        public float ConfirmationDelay => confirmationDelay;
        public float DenialCooldown => denialCooldown;
        public float Volume => volume;
        public float HearingDistance => hearingDistance;
        public float FullVolumeRadius => fullVolumeRadius;
        public float PanDistance => panDistance;
        public float MaximumPan => maximumPan;

        public void Validate()
        {
            if (!float.IsFinite(confirmationDelay) || confirmationDelay < 0 ||
                !float.IsFinite(denialCooldown) || denialCooldown < .1f ||
                !float.IsFinite(volume) || volume < 0 || volume > 1 ||
                !float.IsFinite(fullVolumeRadius) || fullVolumeRadius < 0 ||
                !float.IsFinite(hearingDistance) || hearingDistance < 1 || hearingDistance <= fullVolumeRadius ||
                !float.IsFinite(panDistance) || panDistance <= 0 ||
                !float.IsFinite(maximumPan) || maximumPan < 0 || maximumPan > 1)
            {
                throw new InvalidOperationException("Invalid door access feedback profile: " + name);
            }
        }
    }
}
