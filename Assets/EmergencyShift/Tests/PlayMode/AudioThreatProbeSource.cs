using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class AudioThreatProbeSource : AudioThreatEmitter2D
    {
        public override bool IsAlive => true;
        public override bool IsEngaged => false;
        public override bool IsAttacking => false;
        public override Transform Target => null;
        public override LayerMask Obstacles => 1 << 8;
        public void Emit() => ReportHeard(1, 1, true);
    }
}
