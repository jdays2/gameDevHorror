using UnityEngine;
namespace EmergencyShift
{
    /// <summary>Species adapter; a different AI can provide the same presentation facts.</summary>
    [DisallowMultipleComponent,RequireComponent(typeof(ShadowStalker2D))]
    public sealed class StalkerPresentationSource2D : EnemyPresentationSource2D
    {
        private ShadowStalker2D actor;
        private void Awake(){actor=GetComponent<ShadowStalker2D>();}
        public override bool IsDead=>!actor.IsAlive;
        public override float AttackProgress=>actor.AttackProgress;
        public override float DeathTime=>actor.DeathTime;
        public override IThreatLight2D LightSource=>actor.LightSource;
        public override LayerMask BlockingGeometry=>actor.BlockingGeometry;
    }
}
