using System;
using System.Collections.Generic;
using EmergencyShift.Features.DamageFeedback.Config;
using EmergencyShift.Features.DamageFeedback.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Features.DamageFeedback.View
{
    [DisallowMultipleComponent, RequireComponent(typeof(UnityEngine.UI.RawImage))]
    public sealed class DirectionalDamageFeedbackView : MonoBehaviour, IDirectionalDamageView
    {
        private static readonly int PulsesId = Shader.PropertyToID("_DamagePulses");
        private static readonly int BloodColorId = Shader.PropertyToID("_BloodColor");
        private static readonly int DepthId = Shader.PropertyToID("_Depth");
        private static readonly int SpreadId = Shader.PropertyToID("_Spread");
        [SerializeField]
        private DirectionalDamageStyle style;
        private readonly Vector4[] shaderPulses = new Vector4[DirectionalDamageModel.Capacity];
        private UnityEngine.UI.RawImage image;
        private Material ownedMaterial;
        private PlayerLife2D life;
        private Flashlight2D cameraSource;
        private DirectionalDamageFactory factory;
        private DirectionalDamageController controller;
        private bool damagePending;
        private bool subscribed;

        [Inject]
        private void Construct(PlayerLife2D player, Flashlight2D sceneLight, DirectionalDamageFactory controllers)
        {
            life = player;
            cameraSource = sceneLight;
            factory = controllers;
        }

        private void Awake()
        {
            if (life == null || cameraSource == null || factory == null || style == null || style.Material == null)
            {
                throw new InvalidOperationException("Directional damage needs level composition and its authored style/material.");
            }

            image = GetComponent<UnityEngine.UI.RawImage>();
            image.raycastTarget = false;
            ownedMaterial = new Material(style.Material) { name = "Directional damage (HUD instance)" };
            ownedMaterial.SetColor(BloodColorId, style.BloodColor);
            ownedMaterial.SetFloat(DepthId, style.Depth);
            ownedMaterial.SetFloat(SpreadId, style.Spread);
            image.material = ownedMaterial;
            controller = factory.Create(style.CreateSettings(), this);
            controller.Reset();
        }

        private void OnEnable()
        {
            if (life == null || controller == null || subscribed)
            {
                return;
            }

            life.Damaged += OnDamaged;
            life.HealthChanged += OnHealthChanged;
            subscribed = true;
            damagePending = false;
            controller.Reset();
        }

        private void Update()
        {
            controller?.Tick(Time.unscaledDeltaTime);
        }

        private void OnDamaged(int amount, Vector2 incomingDirection)
        {
            damagePending = true;
            if (incomingDirection == Vector2.zero || float.IsNaN(incomingDirection.x) || float.IsInfinity(incomingDirection.x) ||
                float.IsNaN(incomingDirection.y) || float.IsInfinity(incomingDirection.y))
            {
                return;
            }

            Camera worldCamera = cameraSource.WorldCamera;
            if (worldCamera == null)
            {
                return;
            }

            Vector3 origin = life.transform.position;
            Vector3 playerPoint = worldCamera.WorldToViewportPoint(origin);
            Vector3 sourcePoint = worldCamera.WorldToViewportPoint(origin - (Vector3)incomingDirection);
            controller.Show(amount, (Vector2)(sourcePoint - playerPoint));
        }

        private void OnHealthChanged(int health)
        {
            if (!damagePending)
            {
                controller.Reset();
            }

            damagePending = false;
        }

        public void Render(IReadOnlyList<DamagePulse> pulses)
        {
            bool visible = false;
            for (int i = 0; i < shaderPulses.Length; i++)
            {
                DamagePulse pulse = pulses[i];
                shaderPulses[i] = new Vector4(pulse.EdgePoint.x, pulse.EdgePoint.y, pulse.Opacity, pulse.Seed);
                visible |= pulse.Opacity > 0;
            }

            ownedMaterial.SetVectorArray(PulsesId, shaderPulses);
            image.enabled = visible;
        }

        private void OnDisable()
        {
            if (subscribed)
            {
                life.Damaged -= OnDamaged;
                life.HealthChanged -= OnHealthChanged;
                subscribed = false;
            }

            damagePending = false;
            controller?.Reset();
        }

        private void OnDestroy()
        {
            if (ownedMaterial != null)
            {
                Destroy(ownedMaterial);
            }
        }
    }
}
