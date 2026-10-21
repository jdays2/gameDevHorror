using System;
using EmergencyShift.Features.Flashlight.Logic;
using UnityEngine;

namespace EmergencyShift.Tests
{
    /// <summary>Disposable data source to verify the camera's real subscription lifetime.</summary>
    [AddComponentMenu("")]
    public sealed class CameraOpticsTestSource : MonoBehaviour, IFlashlightOpticsSource
    {
        private Action ownedChanged;
        private Action opticsChanged;
        private float range = 4;

        public int Subscribers { get; private set; }
        public int RangeReads { get; private set; }
        public bool IsOwned { get; private set; }
        public bool IsOn => true;
        public float BeamAngle => 80;
        public float Range
        {
            get
            {
                RangeReads++;
                return range;
            }
        }

        public event Action OwnershipChanged
        {
            add
            {
                ownedChanged += value;
                Subscribers++;
            }
            remove
            {
                ownedChanged -= value;
                Subscribers--;
            }
        }

        public event Action OpticsChanged
        {
            add
            {
                opticsChanged += value;
                Subscribers++;
            }
            remove
            {
                opticsChanged -= value;
                Subscribers--;
            }
        }

        public event Action<bool> PowerChanged
        {
            add { }
            remove { }
        }

        public void SetOwned(bool value)
        {
            IsOwned = value;
            ownedChanged?.Invoke();
        }

        public void SetRange(float value)
        {
            range = value;
            opticsChanged?.Invoke();
        }
    }
}
