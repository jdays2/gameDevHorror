using System;
using EmergencyShift.Features.AccessControl;
using EmergencyShift.Features.AccessControl.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Only access tokens for this run; not a general inventory or a save system.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAccessCards2D : MonoBehaviour, IAccessCardView
    {
        private AccessControlFactory factory;
        private AccessCardController controller;

        public int Count => RequireController().Count;
        public event Action Changed;

        [Inject]
        public void Construct(AccessControlFactory controllerFactory)
        {
            factory = controllerFactory ?? throw new ArgumentNullException(nameof(controllerFactory));
        }

        public bool Has(string id) => RequireController().Has(id);
        public bool Add(string id) => RequireController().Add(id).Success;
        public string[] Capture() => RequireController().Capture();

        public void Restore(string[] values)
        {
            var result = RequireController().Restore(values);
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Reason);
            }
        }

        void IAccessCardView.CardsChanged() => Changed?.Invoke();

        private AccessCardController RequireController()
        {
            if (factory == null)
            {
                throw new InvalidOperationException("Access cards require their level composition.");
            }

            return controller ??= factory.CreateCards(this);
        }
    }
}
