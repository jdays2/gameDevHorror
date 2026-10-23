using System;
using EmergencyShift;
using UnityEditor;
using UnityEngine;

public static class AuthorWeaponFx
{
    public static object Apply()
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Stop Play before authoring weapon FX.");
        }
        const string root = "Assets/EmergencyShift/";
        const string materialPath = root + "Materials/Effects/MuzzleSmoke.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("EmergencyShift/Muzzle Smoke"));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        const string prefabPath = root + "Prefabs/Combat/PistolShotEffect.prefab";
        const string smokePath = root + "Prefabs/Combat/MuzzleSmoke.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        var smokeRoot = PrefabUtility.LoadPrefabContents(smokePath);
        try
        {
            var effect = prefab.GetComponent<ShotEffect2D>();
            var smoke = smokeRoot.GetComponent<MuzzleSmoke2D>();
            SpriteRenderer renderer;
            using (var data = new SerializedObject(smoke))
            {
                renderer = (SpriteRenderer)data.FindProperty("cloud").objectReferenceValue;
            }
            if (renderer.transform == smokeRoot.transform)
            {
                var child = new GameObject("Forward smoke", typeof(SpriteRenderer));
                child.transform.SetParent(smokeRoot.transform, false);
                var forward = child.GetComponent<SpriteRenderer>();
                EditorUtility.CopySerialized(renderer, forward);
                UnityEngine.Object.DestroyImmediate(renderer);
                renderer = forward;
                smoke.Configure(renderer);
            }
            Transform lateral = smokeRoot.transform.Find("Lateral gas");
            if (lateral == null)
            {
                lateral = new GameObject("Lateral gas", typeof(SpriteRenderer)).transform;
                lateral.SetParent(smokeRoot.transform, false);
            }
            var side = lateral.GetComponent<SpriteRenderer>();
            side.sprite = renderer.sprite;
            side.sharedMaterial = material;
            side.sortingLayerID = renderer.sortingLayerID;
            side.sortingOrder = renderer.sortingOrder;
            side.enabled = false;
            smoke.ConfigureSideBurst(side);
            PrefabUtility.SaveAsPrefabAsset(smokeRoot, smokePath);
            var smokeTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(smokePath).GetComponent<MuzzleSmoke2D>();
            effect.ConfigureSmokePrefab(smokeTemplate);
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
            PrefabUtility.UnloadPrefabContents(smokeRoot);
        }
        foreach (string name in new[] { "PistolDefinition", "PumpShotgun" })
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(root + "Data/Weapons/" + name + ".asset");
            var style = weapon.Visuals;
            bool shotgun = name == "PumpShotgun";
            using (var data = new SerializedObject(style))
            {
                data.FindProperty("smokeOpacity").floatValue = shotgun ? .34f : .30f;
                data.FindProperty("smokeColor").colorValue = new Color(.64f, .66f, .68f, 1);
                data.FindProperty("smokeSize").vector2Value = shotgun ? new Vector2(1.05f, 1.35f) : new Vector2(.85f, 1.1f);
                data.FindProperty("smokeSeconds").floatValue = shotgun ? .82f : .68f;
                data.FindProperty("smokeDispersion").floatValue = .65f;
                data.FindProperty("maximumSmokeClouds").intValue = 4;
                data.FindProperty("sideSmokeOpacity").floatValue = shotgun ? .23f : .18f;
                data.FindProperty("sideSmokeSize").vector2Value = shotgun ? new Vector2(1.5f, .5f) : new Vector2(.95f, .32f);
                data.FindProperty("sideSmokeSeconds").floatValue = shotgun ? .22f : .16f;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            style.Validate();
            EditorUtility.SetDirty(style);
        }
        var playerAudio = AssetDatabase.LoadAssetAtPath<PlayerAudioSet>(root + "Audio/PistolIndoor/PistolIndoorAudio.asset");
        ReplaceBank(playerAudio, "concreteImpact", "impact-concrete-01", "impact-concrete-02");
        ReplaceBank(playerAudio, "woodImpact", "impact-wood-01");
        ReplaceBankFromFolder(playerAudio, "fleshImpact", "FleshContacts03", "flesh-contact-01", "flesh-contact-02", "flesh-contact-03", "flesh-contact-04");
        using (var data = new SerializedObject(playerAudio))
        {
            data.FindProperty("fleshImpactVolume").floatValue = .4f;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        var stalker = AssetDatabase.LoadAssetAtPath<StalkerAudioSet>(root + "Audio/Stalker/StalkerMembrane.asset");
        ReplaceBank(stalker, "hit", "stalker_hit_01", "stalker_hit_02", "stalker_hit_03", "stalker_hit_04");
        AssetDatabase.SaveAssets();
        return new { prefabPath, materialPath, smokeLimit = 4, scenesModified = false };
    }

    private static void ReplaceBank(UnityEngine.Object asset, string field, params string[] names)
    {
        ReplaceBankFromFolder(asset, field, "WeaponImpacts02", names);
    }

    private static void ReplaceBankFromFolder(UnityEngine.Object asset, string field, string folder, params string[] names)
    {
        using (var data = new SerializedObject(asset))
        {
            var bank = data.FindProperty(field);
            bank.arraySize = names.Length;
            for (int index = 0; index < names.Length; index++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/EmergencyShift/Audio/" + folder + "/" + names[index] + ".wav");
                if (clip == null)
                {
                    throw new InvalidOperationException("Missing impact derivative " + names[index]);
                }
                bank.GetArrayElementAtIndex(index).objectReferenceValue = clip;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorUtility.SetDirty(asset);
    }
}
