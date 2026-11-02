using System;
using System.Collections.Generic;
using EmergencyShift.Features.Doors.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Features.Doors.View
{
    /// <summary>Opt-in crushing for the actual rectangular leaf. Health owns death, AI cleanup and gore.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Door2D))]
    public sealed class DoorCrush2D : MonoBehaviour, IDoorCrushEnvironment
    {
        private readonly List<Collider2D> overlaps = new List<Collider2D>();
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
        private readonly HashSet<EnemyHealth2D> targets = new HashSet<EnemyHealth2D>();
        private Door2D door;
        private BoxCollider2D leaf;
        private DoorCrushController controller;

        [Inject]
        public void Construct(DoorCrushControllerFactory factory)
        {
            controller = (factory ?? throw new ArgumentNullException(nameof(factory))).Create(this);
        }

        private void Awake()
        {
            door = GetComponent<Door2D>();
            leaf = door.Blocker as BoxCollider2D;
            if (controller == null || leaf == null || leaf.isTrigger || leaf.edgeRadius != 0)
            {
                throw new InvalidOperationException("Door crushing requires level composition and a solid rectangular leaf with zero edge radius.");
            }
            Shape(out _, out _, out _);
        }

        private void OnEnable() => door.BeforeLeafMoved += OnLeafStep;

        private void OnDisable()
        {
            if (door != null)
            {
                door.BeforeLeafMoved -= OnLeafStep;
            }
            overlaps.Clear();
            hits.Clear();
            targets.Clear();
        }

        private void OnLeafStep(DoorLeafStep step) => controller.Step(step);

        void IDoorCrushEnvironment.CrushContacts(Vector2 displacement)
        {
            if (!isActiveAndEnabled || !door.isActiveAndEnabled || !leaf.enabled || !leaf.gameObject.activeInHierarchy)
            {
                return;
            }

            Shape(out Vector2 center, out Vector2 size, out float angle);
            Physics2D.SyncTransforms();
            var filter = new ContactFilter2D { useTriggers = false };
            // Lists grow to fit every contact; no fixed buffer can silently spare a crowded doorway.
            Physics2D.OverlapBox(center, size, angle, filter, overlaps);
            Physics2D.BoxCast(center, size, angle, displacement.normalized, filter, hits, displacement.magnitude);
            targets.Clear();
            foreach (Collider2D contact in overlaps)
            {
                Crush(contact);
            }
            foreach (RaycastHit2D hit in hits)
            {
                Crush(hit.collider);
            }
            overlaps.Clear();
            hits.Clear();
            targets.Clear();
        }

        private void Crush(Collider2D contact)
        {
            if (contact == null || contact.gameObject.scene != gameObject.scene)
            {
                return;
            }

            // This shared monster health contract is deliberately absent from the player.
            var target = contact.GetComponentInParent<EnemyHealth2D>();
            if (target != null && target.gameObject.scene == gameObject.scene && target.isActiveAndEnabled &&
                target.IsAlive && targets.Add(target))
            {
                // Kill uses DamageHealthCommand and revalidates life; Died/gore remain single-owner events.
                target.Kill();
            }
        }

        private void Shape(out Vector2 center, out Vector2 size, out float angle)
        {
            Vector3 horizontal = leaf.transform.TransformVector(Vector3.right);
            Vector3 vertical = leaf.transform.TransformVector(Vector3.up);
            float width = horizontal.magnitude;
            float height = vertical.magnitude;
            if (width <= 0 || height <= 0 || Mathf.Abs(horizontal.z) > .0001f || Mathf.Abs(vertical.z) > .0001f ||
                Mathf.Abs(Vector3.Dot(horizontal / width, vertical / height)) > .0001f || leaf.edgeRadius != 0)
            {
                throw new InvalidOperationException("Door crushing requires an unskewed XY rectangular leaf.");
            }
            center = leaf.transform.TransformPoint(leaf.offset);
            size = Vector2.Scale(leaf.size, new Vector2(width, height));
            angle = Mathf.Atan2(horizontal.y, horizontal.x) * Mathf.Rad2Deg;
        }
    }
}
