using UnityEngine;
namespace EmergencyShift
{
    [CreateAssetMenu(menuName="Emergency Shift/Enemies/Stalker behaviour")]
    public sealed class StalkerBehaviourProfile : ScriptableObject
    {
        [Min(.1f)] public float retreatSpeed=3.8f, standOffDistance=1.05f;
        [Min(.1f)] public float recoilSeconds=.35f, retreatSeconds=1.05f, cooldownSeconds=3;
        [Min(.1f)] public float lightFearDistance=3.2f;
        [Min(.05f)] public float reactionDelay=.45f;
    }
}
