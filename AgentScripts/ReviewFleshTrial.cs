using System;
using System.IO;
using System.Threading.Tasks;
using EmergencyShift;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ReviewFleshTrial
{
    public static async Task<object> Burst(string output)
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Run the temporary contact review in Play.");
        }
        var context = Object.FindAnyObjectByType<LevelContext2D>();
        var player = context.Player;
        var weapon = player.GetComponent<PlayerWeapon2D>();
        var playback = player.GetComponent<PlayerAudio2D>();
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
            float started = Time.time;
            for (int i = 0; i < 8; i++)
            {
                if (weapon.TryFire())
                {
                    fired++;
                }
                await Task.Delay(110);
            }
            await Task.Delay(150);
            int sounds = playback.ImpactAudioCount - before;
            int contacts = playback.ResolvedImpactCount - contactsBefore;
            var result = new { passed = fired == 8 && sounds == 8 && contacts == 8, fired, sounds, contacts, elapsedScaledSeconds = Time.time - started, hpAfter = enemy.GetComponent<EnemyHealth2D>().Current };
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonConvert.SerializeObject(result, Formatting.Indented));
            if (!result.passed)
            {
                throw new InvalidOperationException("A rapid B contact was skipped: " + JsonConvert.SerializeObject(result));
            }
            return result;
        }
        finally
        {
            Object.Destroy(enemy);
        }
    }
}
