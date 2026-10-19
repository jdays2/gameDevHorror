using System;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Core.Composition;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public static class CheckGroundAudioScope
{
    private sealed class Probe
    {
        public GroundAudioSurfaceQuery2D Surfaces { get; private set; }
        public SceneRegistry<IGroundAudioRegion> Registry { get; private set; }
        public bool CasingLanded { get; private set; }

        [Inject]
        private void Construct(GroundAudioSurfaceQuery2D surfaces, SceneRegistry<IGroundAudioRegion> registry)
        {
            Surfaces = surfaces;
            Registry = registry;
        }

        public void OnLanded(ShellCasing2D casing)
        {
            CasingLanded = true;
        }
    }

    public static async Task<object> Check()
    {
        if (!Application.isPlaying || Time.timeScale <= 0)
        {
            throw new InvalidOperationException("This isolated lifecycle check requires unpaused Play.");
        }

        LevelContext2D context = UnityEngine.Object.FindAnyObjectByType<LevelContext2D>();
        var probe = new Probe();
        context.InjectScoped(probe);
        int baseline = probe.Registry.Members.Count;
        GameObject floor = null;
        ShellCasing2D casing = null;
        Scene originalActiveScene = SceneManager.GetActiveScene();
        Scene otherScene = default;
        SceneRegistry<IGroundAudioRegion> otherRegistry = null;
        bool registered = false;
        bool disabledRemoved = false;
        bool enabledOnce = false;
        try
        {
            Vector2 point = new Vector2(10000, 10000);
            floor = new GameObject("Ground audio lifecycle fixture");
            SceneManager.MoveGameObjectToScene(floor, context.gameObject.scene);
            floor.transform.position = point;
            GroundAudioSurface2D surface = floor.AddComponent<GroundAudioSurface2D>();
            floor.GetComponent<BoxCollider2D>().size = Vector2.one * 4;
            var settings = new SerializedObject(surface);
            settings.FindProperty("surface").intValue = (int)GroundSoundSurface.Wood;
            settings.FindProperty("priority").intValue = int.MaxValue;
            settings.ApplyModifiedPropertiesWithoutUndo();
            context.InjectScoped(surface);
            Physics2D.SyncTransforms();
            registered = probe.Registry.Members.Count == baseline + 1 && probe.Surfaces.At(point) == GroundSoundSurface.Wood;
            floor.SetActive(false);
            disabledRemoved = probe.Registry.Members.Count == baseline;
            floor.SetActive(true);
            enabledOnce = probe.Registry.Members.Count == baseline + 1;

            otherScene = SceneManager.CreateScene("Ground audio foreign fixture " + Guid.NewGuid());
            var otherFloor = new GameObject("Foreign surface at the same point");
            SceneManager.MoveGameObjectToScene(otherFloor, otherScene);
            otherFloor.transform.position = point;
            GroundAudioSurface2D otherSurface = otherFloor.AddComponent<GroundAudioSurface2D>();
            otherFloor.GetComponent<BoxCollider2D>().size = Vector2.one * 4;
            var otherSettings = new SerializedObject(otherSurface);
            otherSettings.FindProperty("surface").intValue = (int)GroundSoundSurface.Tile;
            otherSettings.FindProperty("priority").intValue = int.MaxValue;
            otherSettings.ApplyModifiedPropertiesWithoutUndo();
            otherRegistry = new SceneRegistry<IGroundAudioRegion>();
            var otherContainer = new DiContainer();
            otherContainer.Bind<SceneRegistry<IGroundAudioRegion>>().FromInstance(otherRegistry);
            otherContainer.Inject(otherSurface);
            await Task.Delay(50);
            Physics2D.SyncTransforms();
            SceneManager.SetActiveScene(otherScene);
            bool scopeIsolated = probe.Surfaces.At(point) == GroundSoundSurface.Wood &&
                new GroundAudioSurfaceQuery2D(otherRegistry).At(point) == GroundSoundSurface.Tile;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/PistolCasing.prefab");
            casing = context.CreateInstance(prefab, point, Quaternion.identity).GetComponent<ShellCasing2D>();
            casing.FirstLanded += probe.OnLanded;
            casing.Launch(point, Vector2.zero, .1f, 0, context.Player.GetComponent<PlayerAudio2D>(), 0);
            DateTime deadline = DateTime.UtcNow.AddSeconds(3);
            while (!casing.Settled && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
            }

            bool casingUsedOwnScene = casing.LastSurface == GroundSoundSurface.Wood && casing.gameObject.scene == floor.scene;
            if (!registered || !disabledRemoved || !enabledOnce || !scopeIsolated ||
                !probe.CasingLanded || !casing.Settled || !casingUsedOwnScene)
            {
                throw new InvalidOperationException("Ground surface/casing scope lifecycle failed: registered=" + registered +
                    "; disabledRemoved=" + disabledRemoved + "; enabledOnce=" + enabledOnce +
                    "; scopeIsolated=" + scopeIsolated + "; casingLanded=" + probe.CasingLanded +
                    "; casingSettled=" + casing.Settled + "; casingUsedOwnScene=" + casingUsedOwnScene +
                    "; lastSurface=" + casing.LastSurface + "; ownRegionContains=" + surface.Contains(point) +
                    "; ownNativeOverlap=" + floor.GetComponent<BoxCollider2D>().OverlapPoint(point));
            }

            return new { registered, disabledRemoved, enabledOnce, scopeIsolated, casingUsedOwnScene,
                casingLanded = probe.CasingLanded, casingSettled = casing.Settled, scenesSaved = false };
        }
        finally
        {
            SceneManager.SetActiveScene(originalActiveScene);
            if (casing != null)
            {
                casing.FirstLanded -= probe.OnLanded;
                UnityEngine.Object.Destroy(casing.gameObject);
            }

            if (floor != null)
            {
                UnityEngine.Object.Destroy(floor);
            }

            await Task.Delay(50);
            if (otherScene.IsValid() && otherScene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(otherScene);
                while (unload != null && !unload.isDone)
                {
                    await Task.Delay(25);
                }
            }

            otherRegistry?.Dispose();
            if (probe.Registry.Members.Count != baseline)
            {
                throw new InvalidOperationException("A temporary ground surface leaked into the level registry.");
            }
        }
    }
}
