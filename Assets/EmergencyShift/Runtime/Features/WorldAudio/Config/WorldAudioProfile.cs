using System;
using UnityEngine;
using UnityEngine.Audio;

namespace EmergencyShift.Features.WorldAudio.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/Audio/World death transition")]
    public sealed class WorldAudioProfile : ScriptableObject
    {
        [SerializeField] private AudioMixerGroup worldOutput;
        [SerializeField] private string attenuationParameter = "MyExposedParam";
        [SerializeField, Min(.05f)] private float fadeOutSeconds = 1.4f;
        [SerializeField, Min(.05f)] private float restoreSeconds = .35f;
        [SerializeField] private WorldAudioFadeCurve curve = WorldAudioFadeCurve.SmoothStep;

        public AudioMixerGroup WorldOutput => worldOutput;
        public string AttenuationParameter => attenuationParameter;

        public WorldAudioSettings ToSettings() => new WorldAudioSettings(fadeOutSeconds, restoreSeconds, curve);

        public void Validate()
        {
            ToSettings();
            if (worldOutput == null || string.IsNullOrWhiteSpace(attenuationParameter) ||
                !worldOutput.audioMixer.GetFloat(attenuationParameter, out float value) || !float.IsFinite(value))
            {
                throw new InvalidOperationException("World audio requires its mixer output and exposed attenuation parameter.");
            }
        }
    }
}
