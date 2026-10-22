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
        try
        {
            var effect = prefab.GetComponent<ShotEffect2D>();
            var smoke = prefab.GetComponentInChildren<MuzzleSmoke2D>(true);
            if (smoke == null)
            {
                using (var data = new SerializedObject(effect))
                {
                    var plume = (SpriteRenderer)data.FindProperty("plume").objectReferenceValue;
                    var child = new GameObject("Muzzle smoke", typeof(SpriteRenderer), typeof(MuzzleSmoke2D));
                    child.transform.SetParent(prefab.transform, false);
                    var renderer = child.GetComponent<SpriteRenderer>();
                    renderer.sprite = plume.sprite;
                    renderer.sharedMaterial = material;
                    renderer.sortingLayerID = plume.sortingLayerID;
                    renderer.sortingOrder = plume.sortingOrder - 1;
                    renderer.enabled = false;
                    smoke = child.GetComponent<MuzzleSmoke2D>();
                    smoke.Configure(renderer);
                }
            }
            PrefabUtility.SaveAsPrefabAsset(smoke.gameObject, smokePath);
            UnityEngine.Object.DestroyImmediate(smoke.gameObject);
            var smokeTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(smokePath).GetComponent<MuzzleSmoke2D>();
            effect.ConfigureSmokePrefab(smokeTemplate);
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        foreach (string name in new[] { "PistolDefinition", "PumpShotgun" })
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(root + "Data/Weapons/" + name + ".asset");
            var style = weapon.Visuals;
            bool shotgun = name == "PumpShotgun";
            using (var data = new SerializedObject(style))
            {
                data.FindProperty("smokeOpacity").floatValue = shotgun ? .15f : .12f;
                data.FindProperty("smokeSize").vector2Value = shotgun ? new Vector2(.65f, .85f) : new Vector2(.5f, .65f);
                data.FindProperty("smokeSeconds").floatValue = shotgun ? .52f : .42f;
                data.FindProperty("smokeDispersion").floatValue = .65f;
                data.FindProperty("maximumSmokeClouds").intValue = 4;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            style.Validate();
            EditorUtility.SetDirty(style);
        }
        var playerAudio = AssetDatabase.LoadAssetAtPath<PlayerAudioSet>(root + "Audio/PistolIndoor/PistolIndoorAudio.asset");
        ReplaceBank(playerAudio, "concreteImpact", "impact-concrete-01", "impact-concrete-02");
        ReplaceBank(playerAudio, "woodImpact", "impact-wood-01");
        ReplaceBank(playerAudio, "fleshImpact", "stalker_hit_01", "stalker_hit_02", "stalker_hit_03", "stalker_hit_04");
        var stalker = AssetDatabase.LoadAssetAtPath<StalkerAudioSet>(root + "Audio/Stalker/StalkerMembrane.asset");
        ReplaceBank(stalker, "hit", "stalker_hit_01", "stalker_hit_02", "stalker_hit_03", "stalker_hit_04");
        AssetDatabase.SaveAssets();
        return new { prefabPath, materialPath, smokeLimit = 4, scenesModified = false };
    }

    private static void ReplaceBank(UnityEngine.Object asset, string field, params string[] names)
    {
        using (var data = new SerializedObject(asset))
        {
            var bank = data.FindProperty(field);
            bank.arraySize = names.Length;
            for (int index = 0; index < names.Length; index++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/EmergencyShift/Audio/WeaponImpacts02/" + names[index] + ".wav");
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
