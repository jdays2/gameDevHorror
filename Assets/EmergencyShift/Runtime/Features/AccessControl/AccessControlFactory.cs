using EmergencyShift.Features.AccessControl.Logic;

namespace EmergencyShift.Features.AccessControl
{
    /// <summary>The factory is scoped; inventories are created separately for every actor view.</summary>
    public sealed class AccessControlFactory
    {
        public AccessCardController CreateCards(IAccessCardView view)
        {
            return new AccessCardController(new AccessCardModel(), view);
        }
    }
}
