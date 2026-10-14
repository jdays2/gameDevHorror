using UnityEngine;
namespace EmergencyShift
{
    /// <summary>Shared weapon data; ammunition/ownership stay on the player, never in this asset.</summary>
    [CreateAssetMenu(menuName="Emergency Shift/Weapon definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public string displayName="Пистолет";
        [Min(1)] public int damage=1,maximumAmmo=120;
        [Min(.05f)] public float secondsBetweenShots=.3f;
        [Min(.5f)] public float range=10;
        public ShotVisualProfile visuals;
        public bool automatic;
    }
}
