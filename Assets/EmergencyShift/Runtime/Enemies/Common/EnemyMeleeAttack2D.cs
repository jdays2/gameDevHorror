using UnityEngine;
namespace EmergencyShift
{
    /// <summary>Reusable attack timeline and hit delivery. The brain decides when to start/interrupt.</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyMeleeAttack2D : MonoBehaviour
    {
        private EnemyDefinition tuning;private EnemyPerception2D senses;private EnemyMotor2D motor;
        private bool struck,lunged;private float readyAt;
        public event System.Action Lunged;
        public event System.Action<bool> Resolved;
        public void Configure(EnemyDefinition definition,EnemyPerception2D perception,EnemyMotor2D movement)
        {tuning=definition;senses=perception;motor=movement;}
        public bool CanStart(Vector2 at)=>tuning!=null&&senses.TargetHealth!=null&&senses.CanSeeTarget(at)&&senses.Within(at,tuning.attackDistance)&&Time.time>=readyAt;
        public void Begin(){struck=lunged=false;}
        public Vector2 Step(float progress,Vector2 at,Vector2 heading)
        {
            if(!lunged&&progress>=tuning.lungeStart){lunged=true;Lunged?.Invoke();}
            Vector2 velocity=progress>=tuning.lungeStart&&progress<tuning.lungeEnd?motor.ClearDirection(heading)*tuning.lungeSpeed:Vector2.zero;
            if(!struck&&progress>=tuning.strikeAt)
            {
                struck=true;bool landed=false;
                if(senses.TargetHealth!=null&&senses.TargetAlive&&Vector2.Distance(at,senses.Target.position)<tuning.biteDistance&&senses.CanSeeTarget(at))
                    landed=senses.TargetHealth.TryTakeDamage(tuning.attackDamage,at,heading);
                Resolved?.Invoke(landed);
            }
            return velocity;
        }
        public void Finish(){readyAt=Time.time+tuning.attackCooldown;}
        public void Interrupt(float delay){readyAt=Time.time+delay;}
    }
}
