using System;
using System.IO;
using System.Linq;
using EmergencyShift;
using EmergencyShift.Features.Pickups.Config;
using EmergencyShift.Features.Pickups.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class PickupGlintVerification
{
    private const string Folder = "Docs/Validation/PickupGlint57";
    private const string MaterialPath = "Assets/EmergencyShift/Materials/Effects/Pickups/PickupGlint.mat";
    private const string ProfilePath = "Assets/EmergencyShift/Resources/PickupGlintProfile.asset";

    public static object Prepare()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/EmergencyShift/Materials/Effects/Pickups/PickupGlint.shader");
        if (shader == null || ShaderUtil.ShaderHasError(shader))
        {
            throw new InvalidOperationException("Pickup glint shader is missing or failed compilation.");
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        var profile = AssetDatabase.LoadAssetAtPath<PickupGlintProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<PickupGlintProfile>();
            using (var serialized = new SerializedObject(profile))
            {
                serialized.FindProperty("material").objectReferenceValue = material;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }
        profile.Validate();
        AssetDatabase.SaveAssetIfDirty(material);
        AssetDatabase.SaveAssetIfDirty(profile);
        Scene scene = SceneManager.GetActiveScene();
        var result = new
        {
            scene = scene.path,
            scene.isDirty,
            shaderErrors = ShaderUtil.GetShaderMessages(shader).Select(message => message.message).ToArray(),
            sceneSaved = false,
            duration = profile.Duration,
            rearm = profile.RearmSeconds,
            stripeWidth = profile.StripeWidth,
            intensity = profile.Intensity
        };
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/prepare.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    public static object Inspect()
    {
        var targets = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include)
            .Where(item => item is IPickupGlintTarget).ToArray();
        var result = new
        {
            playing = Application.isPlaying,
            scene = SceneManager.GetActiveScene().path,
            pickups = targets.Length,
            glints = targets.Count(item => item.GetComponent<PickupGlint2D>() != null),
            displaying = targets.Count(item => item.GetComponent<PickupGlint2D>()?.IsShowing == true),
            types = targets.GroupBy(item => item.GetType().Name).Select(group => new { type = group.Key, count = group.Count() }).ToArray()
        };
        File.WriteAllText(Folder + "/inspection.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }
}
