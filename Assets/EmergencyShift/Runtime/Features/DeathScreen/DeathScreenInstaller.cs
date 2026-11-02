using System;
using EmergencyShift.Features.DeathScreen.Config;
using EmergencyShift.Features.DeathScreen.Logic;
using EmergencyShift.Features.DeathScreen.View;
using EmergencyShift.Features.PauseMenu.View;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Features.DeathScreen
{
    public static class DeathScreenInstaller
    {
        public static void Install(DiContainer container, DeathScreenEnvironment environment)
        {
            if (environment.Profile == null)
            {
                throw new InvalidOperationException("The authored death screen needs its transition profile.");
            }
            GameObject pauseRoot = null;
            DialogueSession dialogue = null;
            LevelCheckpointController2D checkpoint = null;
            // Cold composition of exactly this scene, never a consumer's per-frame search.
            foreach (GameObject root in environment.gameObject.scene.GetRootGameObjects())
            {
                foreach (PauseMenuMusic music in root.GetComponentsInChildren<PauseMenuMusic>(true))
                {
                    if (pauseRoot != null || music.gameObject == environment.gameObject ||
                        music.GetComponent<LevelContext2D>() != null || music.GetComponent<PlayerLife2D>() != null)
                    {
                        throw new InvalidOperationException("Choose one separate pause UI root for this level.");
                    }
                    pauseRoot = music.gameObject;
                }
                foreach (DialogueSession session in root.GetComponentsInChildren<DialogueSession>(true))
                {
                    RequireUnique(dialogue, nameof(DialogueSession));
                    dialogue = session;
                }
                foreach (LevelCheckpointController2D controller in root.GetComponentsInChildren<LevelCheckpointController2D>(true))
                {
                    RequireUnique(checkpoint, nameof(LevelCheckpointController2D));
                    checkpoint = controller;
                }
            }
            environment.BindScene(pauseRoot, dialogue, checkpoint);
            container.Bind<DeathScreenSettings>().FromInstance(environment.Profile.ToSettings());
            container.Bind<IDeathScreenEnvironment>().FromInstance(environment);
            container.Bind<DeathScreenModel>().AsSingle();
            container.BindInterfacesAndSelfTo<DeathScreenController>().AsSingle();
        }

        private static void RequireUnique(UnityEngine.Object existing, string type)
        {
            if (existing != null)
            {
                throw new InvalidOperationException("Choose one " + type + " for the death screen's scene.");
            }
        }
    }
}
