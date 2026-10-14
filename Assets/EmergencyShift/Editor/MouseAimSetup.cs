using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>Links cursor aiming and adds one saved reticle; retains level and gameplay tuning.</summary>
    public static class MouseAimSetup
    {
        [MenuItem("Emergency Shift/Weapons/Apply mouse target aiming")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != PrototypeSceneBuilder.ScenePath) throw new InvalidOperationException("Open BasementPrototype.");
            const string path = "Assets/EmergencyShift/Prefabs/Player.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try { ConfigurePlayer(root, null); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            foreach (var gun in UnityEngine.Object.FindObjectsByType<PlayerWeapon2D>(FindObjectsInactive.Include)) ConfigurePlayer(gun.gameObject, Camera.main);
            foreach (var hud in UnityEngine.Object.FindObjectsByType<PrototypeHud>(FindObjectsInactive.Include)) ConfigureReticle(hud);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        public static void ConfigurePlayer(GameObject root, Camera camera)
        {
            var aim = root.GetComponent<PlayerAim2D>() ?? root.AddComponent<PlayerAim2D>();
            aim.Configure(camera);
            var gun = root.GetComponent<PlayerWeapon2D>(); gun.ConfigureAim(aim);
            var light = root.GetComponent<Flashlight2D>(); light.ConfigureAim(aim);
            foreach (var component in new Component[] { aim, gun, light })
            {
                EditorUtility.SetDirty(component);
                if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }
        public static void ConfigureReticle(PrototypeHud hud)
        {
            var gun = UnityEngine.Object.FindFirstObjectByType<PlayerWeapon2D>();
            var existing = hud.transform.Find("Aim reticle");
            var reticle = existing != null ? existing.GetComponent<AimReticle2D>() : new GameObject("Aim reticle", typeof(RectTransform), typeof(UnityEngine.CanvasRenderer), typeof(AimReticle2D)).GetComponent<AimReticle2D>();
            reticle.transform.SetParent(hud.transform, false);
            reticle.rectTransform.anchorMin = reticle.rectTransform.anchorMax = reticle.rectTransform.pivot = new Vector2(.5f, .5f);
            reticle.rectTransform.sizeDelta = new Vector2(32, 32); reticle.color = new Color(.86f, .89f, .88f);
            reticle.Configure(gun.GetComponent<PlayerAim2D>(), gun); EditorUtility.SetDirty(reticle);
        }
    }
}
