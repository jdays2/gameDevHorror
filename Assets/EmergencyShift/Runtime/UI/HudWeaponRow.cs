using UnityEngine;

namespace EmergencyShift
{
    public sealed class HudWeaponRow : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition definition;
        [SerializeField] private CanvasGroup opacity;
        [SerializeField] private UnityEngine.UI.Text ammunition;
        [SerializeField] private UnityEngine.UI.Text operation;
        private int previousLoaded = -1;
        private int previousReserve = -1;
        private WeaponPhase previousPhase;
        public WeaponDefinition Definition => definition;

        public void Present(bool owned, bool selected, RectTransform activeSlot, RectTransform spareSlot, float spareAlpha)
        {
            gameObject.SetActive(owned);
            if (!owned)
            {
                return;
            }

            var slot = selected ? activeSlot : spareSlot;
            var rect = (RectTransform)transform;
            if (rect.parent != slot)
            {
                rect.SetParent(slot, false);
                rect.anchoredPosition = Vector2.zero;
            }

            opacity.alpha = selected ? 1 : spareAlpha;
            ammunition.gameObject.SetActive(selected);
            operation.gameObject.SetActive(selected);
            previousLoaded = -1;
        }

        public void RefreshAmmunition(PlayerWeapon2D weapon)
        {
            if (previousLoaded != weapon.LoadedAmmo || previousReserve != weapon.ReserveAmmo)
            {
                ammunition.text = weapon.LoadedAmmo + " / " + weapon.ReserveAmmo;
                previousLoaded = weapon.LoadedAmmo;
                previousReserve = weapon.ReserveAmmo;
            }

            if (previousPhase != weapon.Phase || string.IsNullOrEmpty(operation.text))
            {
                previousPhase = weapon.Phase;
                switch (weapon.Phase)
                {
                    case WeaponPhase.Pumping:
                        operation.text = "ПОМПА";
                        break;
                    case WeaponPhase.Loading:
                        operation.text = "ЗАРЯДКА";
                        break;
                    default:
                        operation.text = " ";
                        break;
                }
            }
        }
    }
}
