using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ReviewFleshTrial
{
    public static Task<object> Burst(string output)
    {
        return BurstCore(output, 1, false);
    }

    public static Task<object> Variants(string output)
    {
        return BurstCore(output, 3, true);
    }

    private static async Task<object> BurstCore(string output, int magazines, bool checkVariants)
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Run the temporary contact review in Play.");
        }
        var context = Object.FindAnyObjectByType<LevelContext2D>();
        var player = context.Player;
        var weapon = player.GetComponent<PlayerWeapon2D>();
        var playback = player.GetComponent<PlayerAudio2D>();
        playback.SetPreviewSeed(42);
        var channels = new List<AudioSource>();
        using (var data = new SerializedObject(playback))
        {
            var sources = data.FindProperty("impactSources");
            for (int i = 0; i < sources.arraySize; i++)
            {
                channels.Add((AudioSource)sources.GetArrayElementAtIndex(i).objectReferenceValue);
            }
        }
        var aim = player.GetComponent<PlayerAim2D>();
        player.GetComponent<PlayerMotor2D>().enabled = false;
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        foreach (var stalker in Object.FindObjectsByType<ShadowStalker2D>())
        {
            stalker.AttacksEnabled = false;
        }
        var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PistolDefinition.asset");
        weapon.Acquire(40, definition);
        weapon.TryEquip(definition.SelectionSlot);
        if (weapon.LoadedAmmo < 8)
        {
            weapon.TryReload();
            await Task.Delay(1600);
        }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
        Vector3 position = player.transform.position + Vector3.right * 2.6f;
        var enemy = context.CreateInstance(prefab, position, Quaternion.identity);
        try
        {
            enemy.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            enemy.GetComponent<ShadowStalker2D>().AttacksEnabled = false;
            enemy.GetComponent<EnemyHealth2D>().Initialize(100);
            aim.SetScriptedTarget(position);
            Physics2D.SyncTransforms();
            await Task.Delay(200);
            int before = playback.ImpactAudioCount;
            int contactsBefore = playback.ResolvedImpactCount;
            int fired = 0;
            var clips = new List<string>();
            float started = Time.time;
            for (int magazine = 0; magazine < magazines; magazine++)
            {
                if (magazine > 0)
                {
                    await Task.Delay(1600);
                }
                for (int i = 0; i < 8; i++)
                {
                    int priorSounds = playback.ImpactAudioCount;
                    if (weapon.TryFire())
                    {
                        fired++;
                    }
                    await Task.Delay(110);
                    if (playback.ImpactAudioCount == priorSounds + 1)
                    {
                        var newest = channels.Where(source => source != null && source.isPlaying).OrderBy(source => source.timeSamples).FirstOrDefault();
                        clips.Add(newest != null && newest.clip != null ? newest.clip.name : "unobserved");
                    }
                }
            }
            await Task.Delay(150);
            int sounds = playback.ImpactAudioCount - before;
            int contacts = playback.ResolvedImpactCount - contactsBefore;
            bool noAdjacentRepeats = !clips.Zip(clips.Skip(1), (first, second) => first == second).Any(repeated => repeated);
            bool observedAllSounds = clips.Count == sounds && clips.All(name => name.StartsWith("flesh-contact-B"));
            bool passed = fired == magazines * 8 && sounds == fired && contacts == fired && observedAllSounds;
            if (checkVariants)
            {
                passed &= noAdjacentRepeats && clips.Distinct().Count() > 1;
            }
            var result = new { passed, fired, sounds, contacts, clips, distinctClips = clips.Distinct().Count(), noAdjacentRepeats, boundedChannels = channels.Count, elapsedScaledSeconds = Time.time - started, hpAfter = enemy.GetComponent<EnemyHealth2D>().Current };
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonConvert.SerializeObject(result, Formatting.Indented));
            if (!result.passed)
            {
                throw new InvalidOperationException("Contact variation review failed: " + JsonConvert.SerializeObject(result));
            }
            return result;
        }
        finally
        {
            Object.Destroy(enemy);
        }
    }
}
