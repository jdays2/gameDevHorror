using System;
using System.Collections.Generic;
using EmergencyShift.Core.Composition;
using EmergencyShift.Features.AudioMood.Logic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Features.AudioMood.View
{
    public sealed class AudioThreatQuery2D
    {
        private readonly SceneRegistry<AudioThreatEmitter2D> sources;
        private readonly LocalIlluminationQuery2D lights;
        public int RegisteredCount => sources.Members.Count;

        public AudioThreatQuery2D(SceneRegistry<AudioThreatEmitter2D> sources, LocalIlluminationQuery2D lights)
        {
            this.sources = sources ?? throw new ArgumentNullException(nameof(sources));
            this.lights = lights ?? throw new ArgumentNullException(nameof(lights));
        }

        public void Collect(Scene scene, Transform listener, Flashlight2D flashlight, float awarenessDistance,
            List<AudioThreatObservation> observations)
        {
            observations.Clear();
            float maximumSquared = awarenessDistance * awarenessDistance;
            for (int index = 0; index < sources.Members.Count; index++)
            {
                AudioThreatEmitter2D source = sources.Members[index];
                if (source == null)
                {
                    continue;
                }

                bool alive = source.isActiveAndEnabled && source.IsAlive;
                float distanceSquared = ((Vector2)source.transform.position - (Vector2)listener.position).sqrMagnitude;
                bool eligible = alive && source.gameObject.scene == scene &&
                    (source.Target == null || source.Target == listener) && distanceSquared <= maximumSquared;
                bool seen = eligible && IsVisible(scene, listener, flashlight, source);
                observations.Add(new AudioThreatObservation(source.ObservationId, alive, eligible, distanceSquared,
                    seen, source.HeardUntil, source.UrgentUntil, source.IsEngaged, source.IsAttacking));
            }
        }

        private bool IsVisible(Scene scene, Transform listener, Flashlight2D flashlight, AudioThreatEmitter2D source)
        {
            Vector2 point = source.transform.position;
            if (flashlight != null && flashlight.WorldCamera != null)
            {
                Vector3 viewport = flashlight.WorldCamera.WorldToViewportPoint(source.transform.position);
                if (viewport.z <= 0 || viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1)
                {
                    return false;
                }
            }

            if (flashlight != null && flashlight.Illuminates(point, source.Obstacles))
            {
                return true;
            }

            if (Physics2D.Linecast(listener.position, point, source.Obstacles).collider != null)
            {
                return false;
            }

            return lights.AnyLampIlluminates(scene, point, source.Obstacles);
        }
    }
}
