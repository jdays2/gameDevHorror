using UnityEngine;

namespace EmergencyShift.Features.Doors.Logic
{
    public interface IDoorCrushEnvironment
    {
        void CrushContacts(Vector2 displacement);
    }
}
