using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmergencyShift;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class InstallMonsterEyes03
{
    public static string Apply()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Eye authoring requires stopped Play.");
        }

        var log = new List<object>();
        foreach (string name in new[] { "Stalker-Light-Default", "Stalker-Light-BasementPrototype" })
        {
            string path = "Assets/EmergencyShift/Data/Enemies/" + name + ".asset";
            var profile = AssetDatabase.LoadAssetAtPath<StalkerBehaviourProfile>(path);
            var fields = new SerializedObject(profile);
            Set(fields, "recoilSeconds", .2f);
            Set(fields, "lightBurnDistance", 1.9f);
            Set(fields, "eyeBurnSeconds", .3f);
            Set(fields, "eyeRecoverySeconds", 10);
            Set(fields, "eyeWarningSeconds", .3f);
            Set(fields, "eyeOpenRecoilFraction", .7f);
            fields.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(profile);
            log.Add(new { path, profile.ReactionDelay, guid = AssetDatabase.AssetPathToGUID(path) });
        }

        const string livePath = "Assets/EmergencyShift/Materials/Characters/stalker-eye-burn.mat";
        var live = AssetDatabase.LoadAssetAtPath<Material>(livePath);
        live.SetColor("_CloudColor", new Color(.76f, .84f, .86f, 1));
        live.SetFloat("_Redness", .65f);
        EditorUtility.SetDirty(live);
        AssetDatabase.SaveAssetIfDirty(live);
        const string deadPath = "Assets/EmergencyShift/Materials/Characters/remains-eye-cloud.mat";
        var dead = AssetDatabase.LoadAssetAtPath<Material>(deadPath);
        if (dead == null)
        {
            dead = new Material(live) { name = "remains-eye-cloud" };
            AssetDatabase.CreateAsset(dead, deadPath);
        }

        var eye = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EmergencyShift/Art/Enemies/Stalker/Remains04/DetachedEye.asset");
        dead.SetVector("_IrisRadius", new Vector4(eye.bounds.extents.x * .61f, eye.bounds.extents.y * .57f, 0, 0));
        EditorUtility.SetDirty(dead);
        AssetDatabase.SaveAssetIfDirty(dead);

        foreach (string species in new[] { "Stalker", "Sleeper" })
        {
            ConfigureGore(species, dead, log);
            string prefabPath = "Assets/EmergencyShift/Prefabs/Enemies/" + (species == "Stalker" ? "ShadowStalker" : "Sleeper") + ".prefab";
            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var animation = prefab.GetComponent<CreatureSpriteAnimator2D>();
                var fields = new SerializedObject(animation);
                var pupil = (SpriteRenderer)fields.FindProperty("pupil").objectReferenceValue;
                float prior = pupil.transform.localScale.x;
                if (Mathf.Abs(prior - .13f) > .00001f && Mathf.Abs(prior - .1105f) > .00001f)
                {
                    throw new InvalidOperationException("Unexpected authored pupil scale: " + prefabPath + "/" + prior);
                }

                pupil.transform.localScale = new Vector3(.1105f, .1105f, pupil.transform.localScale.z);
                Set(fields, "pupilWarningExpansion", .25f);
                fields.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                log.Add(new { path = prefabPath, previousPupilScale = prior, pupilScale = .1105f });
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        Directory.CreateDirectory("Docs/Validation/MonsterEyes03");
        string json = JsonConvert.SerializeObject(log, Formatting.Indented);
        File.WriteAllText("Docs/Validation/MonsterEyes03/authored.json", json);
        return json;
    }

    private static void ConfigureGore(string species, Material material, List<object> log)
    {
        string path = "Assets/EmergencyShift/Data/Gore/" + species + "Rupture.asset";
        var profile = AssetDatabase.LoadAssetAtPath<GoreProfile>(path);
        var data = JObject.Parse(JsonUtility.ToJson(profile));
        var variants = (JArray)data["variants"];
        if (!variants.Any(value => ((string)value["id"]).EndsWith("-eye03", StringComparison.Ordinal)))
        {
            JObject[] sources = variants.Cast<JObject>().Where(value => !(bool)value["restorationOnly"]).ToArray();
            if (sources.Length != 3)
            {
                throw new InvalidOperationException("Expected the three existing new-death compositions: " + path);
            }

            JObject eyeTemplate = sources.SelectMany(value => (JArray)value["pieces"]).Cast<JObject>()
                .First(value => (int)value["spriteIndex"] == 9);
            for (int index = 0; index < sources.Length; index++)
            {
                JObject source = sources[index];
                source["restorationOnly"] = true;
                foreach (bool withEye in new[] { true, false })
                {
                    var variant = (JObject)source.DeepClone();
                    variant["id"] = (string)source["id"] + (withEye ? "-eye03" : "-flesh03");
                    variant["restorationOnly"] = false;
                    variant["selectionWeight"] = withEye ? 7 : 13;
                    var pieces = new JArray(((JArray)variant["pieces"]).Where(value => (int)value["spriteIndex"] != 9).Select(value => value.DeepClone()));
                    foreach (JObject piece in pieces)
                    {
                        // RetainedEyeCore has a painted-in organ: use the existing open cavity for the new variants.
                        if ((int)piece["spriteIndex"] == 7)
                        {
                            piece["spriteIndex"] = 6;
                        }

                        piece["eyeSurface"] = false;
                    }

                    if (withEye)
                    {
                        var detached = (JObject)eyeTemplate.DeepClone();
                        detached["eyeSurface"] = true;
                        pieces.Add(detached);
                    }
                    else
                    {
                        for (int extra = 0; extra < index + 2; extra++)
                        {
                            var flesh = (JObject)pieces.Cast<JObject>().First(value => (int)value["spriteIndex"] == 10 + extra % 4).DeepClone();
                            flesh["offset"] = new JObject { ["x"] = -.17f + extra * .105f, ["y"] = extra % 2 == 0 ? -.12f : .13f };
                            flesh["scale"] = new JObject { ["x"] = .65f, ["y"] = 1.05f };
                            flesh["impulseMultiplier"] = .15f + extra * .035f;
                            flesh["liftMultiplier"] = .3f;
                            pieces.Add(flesh);
                        }
                    }

                    variant["pieces"] = pieces;
                    variant["fragmentCount"] = pieces.Count;
                    variants.Add(variant);
                }
            }

            JsonUtility.FromJsonOverwrite(new JObject { ["variants"] = variants }.ToString(Formatting.None), profile);
        }

        var fields = new SerializedObject(profile);
        fields.FindProperty("eyeSurfaceMaterial").objectReferenceValue = material;
        fields.ApplyModifiedPropertiesWithoutUndo();
        profile.Validate();
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
        log.Add(new { path, active = Enumerable.Range(0, profile.VariantCount).Select(profile.VariantAt)
            .Where(value => !value.RestorationOnly).Select(value => new { value.Id, value.PieceCount, value.SelectionWeight,
                eyes = Enumerable.Range(0, value.PieceCount).Count(i => value.PieceAt(i).EyeSurface),
                flesh = Enumerable.Range(0, value.PieceCount).Count(i => value.PieceAt(i).SpriteIndex >= 10) }).ToArray() });
    }

    private static void Set(SerializedObject target, string field, float value) => target.FindProperty(field).floatValue = value;
}
