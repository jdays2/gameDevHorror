using UnityEngine;
namespace EmergencyShift
{
    [CreateAssetMenu(menuName="Emergency Shift/Enemies/Patrol profile")]
    public sealed class EnemyPatrolProfile : ScriptableObject
    {
        [Min(.1f)] public float speed=.6f;
        [Min(.4f)] public float wanderStepDistance=1.6f;
        public Vector2 pauseSeconds=new Vector2(.6f,1.8f);
    }
}
