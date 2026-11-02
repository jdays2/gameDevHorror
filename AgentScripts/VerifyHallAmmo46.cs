using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmergencyShift;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// In-memory Pipeline probe, outside Assets. Never attached to an authored scene.
public static class VerifyHallAmmo46
{
    private const string ScenePath = "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity";
    private const string TargetPath = "10_World/R04_Hall/Gameplay/Route pistol reserve - 8 cartridges";
    private const string Output = "Docs/Validation/HallAmmo46/";
    private const string PistolPath = "Assets/EmergencyShift/Data/Weapons/PistolDefinition.asset";
    private const string ShotgunPath = "Assets/EmergencyShift/Data/Weapons/PumpShotgun.asset";

    public static object Open()
    {
        Require(!EditorApplication.isPlaying && !EditorApplication.isCompiling, "Editor must be stopped and ready.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Require(!SceneManager.GetSceneAt(i).isDirty, "Refusing to close an unsaved scene.");
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return Inspect("native-before");
    }

    public static object Inspect(string label)
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        WeaponLootPickup pickup = Find(scene);
        using (var data = new SerializedObject(pickup))
        {
            var result = new
            {
                utc = DateTime.UtcNow,
                scene = scene.path,
                scene.isDirty,
                play = EditorApplication.isPlaying,
                playModeStartScene = EditorSceneManager.playModeStartScene == null ? null : AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),
                path = PathOf(pickup.transform),
                id = GlobalObjectId.GetGlobalObjectIdSlow(pickup).ToString(),
                position = new[] { pickup.transform.position.x, pickup.transform.position.y, pickup.transform.position.z },
                kind = pickup.Kind.ToString(),
                cartridges = data.FindProperty("cartridges").intValue,
                weaponDefinition = AssetDatabase.GetAssetPath(data.FindProperty("weaponDefinition").objectReferenceValue),
                prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(pickup),
                components = pickup.GetComponentsInChildren<Component>(true).Select(c => c == null ? "MISSING" : c.GetType().FullName).ToArray(),
                overrides = (PrefabUtility.GetPropertyModifications(pickup) ?? Array.Empty<PropertyModification>()).Select(m => new
                {
                    m.propertyPath,
                    m.value,
                    reference = m.objectReference == null ? null : GlobalObjectId.GetGlobalObjectIdSlow(m.objectReference).ToString()
                }).ToArray()
            };
            Write(label, result);
            return result;
        }
    }

    public static object Fix()
    {
        Require(!EditorApplication.isPlaying && !EditorApplication.isCompiling, "Editor must be stopped and ready.");
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        Require(!scene.isDirty, "Refusing to save unrelated unsaved scene edits.");
        WeaponLootPickup pickup = Find(scene);
        using (var data = new SerializedObject(pickup))
        {
            Require(pickup.Kind == WeaponLootPickup.LootKind.Ammo && data.FindProperty("cartridges").intValue == 8, "Unexpected hall box configuration.");
            SerializedProperty target = data.FindProperty("weaponDefinition");
            Require(target.objectReferenceValue == null, "Hall box target changed since diagnosis.");
            target.objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(PistolPath);
            Require(target.objectReferenceValue != null, "Missing pistol definition.");
            data.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(pickup);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene), "Scene save failed.");
        return Inspect("native-after");
    }

    public static object PreserveUnsaved()
    {
        Require(!EditorApplication.isPlaying, "Stop Play before taking a preservation copy.");
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        const string copy = "work/bug046-baseline/unsaved-after-tests.unity";
        Require(EditorSceneManager.SaveScene(scene, copy, true), "Could not preserve the unsaved scene copy.");
        return new { copy, scene.path, scene.isDirty };
    }

    public static object SaveUnchanged()
    {
        PreserveUnsaved();
        const string copy = "work/bug046-baseline/unsaved-after-tests.unity";
        Require(File.ReadAllBytes(ScenePath).SequenceEqual(File.ReadAllBytes(copy)), "Unsaved data differs: preserve it and refuse saving.");
        Require(EditorSceneManager.SaveScene(SceneManager.GetSceneByPath(ScenePath)), "Scene save failed.");
        return new { serializedBytesIdentical = true, isDirty = SceneManager.GetSceneByPath(ScenePath).isDirty };
    }

    public static object RestoreEditor()
    {
        Require(!EditorApplication.isPlaying && !EditorApplication.isCompiling, "Editor must be stopped and ready.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Require(!SceneManager.GetSceneAt(i).isDirty, "Preserve unsaved scene edits before returning to the original map.");
        }

        Inspect("native-final");
        Scene scene = EditorSceneManager.OpenScene("Assets/EmergencyShift/Scenes/MechanicsSandbox.unity", OpenSceneMode.Single);
        var result = new { scene = scene.path, scene.isDirty, sceneCount = SceneManager.sceneCount, playModeStartScene = EditorSceneManager.playModeStartScene == null ? null : AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) };
        Write("editor-final", result);
        return result;
    }

    public static object Play(string label, bool fixedTarget)
    {
        Require(EditorApplication.isPlaying, "A normal Play session is required.");
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        WeaponLootPickup pickup = Find(scene);
        LevelContext2D context = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LevelContext2D>(true)).Single();
        PlayerInteraction actor = context.Player.GetComponent<PlayerInteraction>();
        PlayerWeapon2D weapon = actor.Weapon;
        WeaponDefinition pistol = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(PistolPath);
        WeaponDefinition shotgun = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(ShotgunPath);
        WeaponInventorySnapshot originalInventory = weapon.CaptureInventory();
        Require(originalInventory.weapons.Length == 0, "Probe expects a fresh MorgueRouteTest session.");
        Vector3 originalPosition = actor.transform.position;
        var records = new List<object>();
        using (var data = new SerializedObject(pickup))
        {
            UnityEngine.Object target = data.FindProperty("weaponDefinition").objectReferenceValue;
            Require(fixedTarget ? target == pistol : target == null, "Unexpected target in the actual Play scene.");
        }

        // All cases run synchronously in one Editor callback: enemies cannot take a turn
        // between seeding inventory and the real physics-overlap auto-pickup call.
        try
        {
            Run("shotgun-selected", 24, 12, 1, fixedTarget ? 8 : 0, fixedTarget ? 0 : 8, 0);
            Run("full-shotgun-incomplete-pistol", 24, shotgun.MaximumAmmo, 1, fixedTarget ? 8 : 0, 0, fixedTarget ? 0 : 8);
            Run("pistol-selected", 24, 12, 0, 8, 0, 0);
            if (fixedTarget)
            {
                Run("pistol-three-spaces", pistol.MaximumAmmo - 3, 12, 1, 3, 0, 5);
                Run("full-pistol-shotgun-has-space", pistol.MaximumAmmo, 12, 1, 0, 0, 8);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Loot/AmmoPickup.prefab");
            GameObject fixture = UnityEngine.Object.Instantiate(prefab);
            try
            {
                SceneManager.MoveGameObjectToScene(fixture, scene);
                var untyped = fixture.GetComponent<WeaponLootPickup>();
                context.InjectScoped(untyped);
                untyped.Configure(WeaponLootPickup.LootKind.Ammo, 8);
                fixture.transform.position = originalPosition + Vector3.up * 2;
                Seed(24, 12, 1);
                pickup.gameObject.SetActive(false);
                actor.transform.position = fixture.transform.position;
                Physics2D.SyncTransforms();
                WeaponInventorySnapshot before = weapon.CaptureInventory();
                actor.CollectNearbyLoot();
                WeaponInventorySnapshot after = weapon.CaptureInventory();
                Require(Total(after, pistol.ContentId) - Total(before, pistol.ContentId) == 0, "Untyped control changed pistol.");
                Require(Total(after, shotgun.ContentId) - Total(before, shotgun.ContentId) == 8 && untyped.Collected, "Untyped control no longer fills selected shotgun.");
                records.Add(new { name = "untyped-prefab-control", before, after, untyped.Collected, remaining = untyped.RemainingCartridges });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fixture);
            }
        }
        finally
        {
            pickup.gameObject.SetActive(true);
            pickup.RestoreState(false, 8);
            actor.transform.position = originalPosition;
            // Restore the initial empty loadout using its existing pistol asset.
            weapon.RestoreState(false, 0, pistol);
            Physics2D.SyncTransforms();
        }

        Require(!EditorUtility.IsDirty(pistol) && !EditorUtility.IsDirty(shotgun), "Runtime dirtied shared weapon definitions.");
        var result = new { passed = true, utc = DateTime.UtcNow, frame = Time.frameCount, scene = scene.path, fixedTarget, method = "Normal Play; real PlayerInteraction.CollectNearbyLoot with controlled inventory and runtime-only relocation; no route traversal.", records };
        Write(label, result);
        return result;

        void Seed(int pistolAmmo, int shotgunAmmo, int selectedSlot)
        {
            weapon.RestoreState(true, pistolAmmo, pistol);
            Require(weapon.Acquire(shotgunAmmo, shotgun), "Could not seed shotgun.");
            if (selectedSlot == 0)
            {
                Require(weapon.TryEquip(0), "Could not select pistol.");
            }
        }

        void Run(string name, int pistolAmmo, int shotgunAmmo, int selectedSlot, int pistolDelta, int shotgunDelta, int remaining)
        {
            pickup.gameObject.SetActive(false);
            pickup.RestoreState(false, 8);
            Seed(pistolAmmo, shotgunAmmo, selectedSlot);
            actor.transform.position = pickup.transform.position;
            pickup.gameObject.SetActive(true);
            Physics2D.SyncTransforms();
            WeaponInventorySnapshot before = weapon.CaptureInventory();
            bool preview = pickup.CanReceive(actor);
            actor.CollectNearbyLoot();
            WeaponInventorySnapshot after = weapon.CaptureInventory();
            Require(Total(after, pistol.ContentId) - Total(before, pistol.ContentId) == pistolDelta, name + ": wrong pistol delta.");
            Require(Total(after, shotgun.ContentId) - Total(before, shotgun.ContentId) == shotgunDelta, name + ": wrong shotgun delta.");
            Require(pickup.RemainingCartridges == remaining && pickup.Collected == (remaining == 0), name + ": wrong box state.");
            Require(after.activeId == before.activeId, name + ": active weapon changed.");
            records.Add(new { name, preview, before, after, remaining = pickup.RemainingCartridges, pickup.Collected, active = pickup.gameObject.activeSelf });
        }
    }

    private static int Total(WeaponInventorySnapshot snapshot, string id) => snapshot.weapons.Single(w => w.contentId == id).cycle.ammo;

    private static WeaponLootPickup Find(Scene scene)
    {
        Require(scene.IsValid() && scene.isLoaded, "MorgueRouteTest must be open.");
        return scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WeaponLootPickup>(true)).Single(p => PathOf(p.transform) == TargetPath);
    }

    private static string PathOf(Transform item) => item.parent == null ? item.name : PathOf(item.parent) + "/" + item.name;

    private static void Write(string name, object value)
    {
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + name + ".json", JsonConvert.SerializeObject(value, Formatting.Indented));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
