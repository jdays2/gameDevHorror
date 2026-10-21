#if UNITY_EDITOR
using System;
using EmergencyShift.Core.Composition;
using EmergencyShift.Features.PlayerInput.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    [AddComponentMenu("")]
    public sealed class CompositionFailureProbe : MonoBehaviour
    {
        public Door2D Door { get; set; }
        public IPlayerInput CapturedInput { get; private set; }
        public SceneRegistry<Door2D> CapturedRegistry { get; private set; }

        [Inject]
        public void Construct(IPlayerInput input, SceneRegistry<Door2D> registry)
        {
            CapturedInput = input;
            CapturedRegistry = registry;
            registry.Register(Door);
            throw new InvalidOperationException("Expected isolated level injection failure.");
        }
    }
}
#endif
