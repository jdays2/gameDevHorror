using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.PlayerHitAudio.Config;
using EmergencyShift.Features.PlayerHitAudio.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyPlayerHit
{
    private static readonly string[] Prefabs = { "ShadowStalker", "Sleeper", "Scorpion", "ClawPrototype" };

    public static async Task<object> MeleeMatrix()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires ordinary Play with the game lease.");
        }
        Scene scene = SceneManager.GetActiveScene();
        var context = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LevelContext2D>(true)).Single();
        PlayerLife2D player = context.Player;
        foreach (EnemyHealth2D enemy in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemyHealth2D>(true)))
        {
            enemy.gameObject.SetActive(false);
        }
        player.GetComponent<PlayerMotor2D>().enabled = false;
        player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        player.SetHealthCapacity(100, 100);
        context.Flashlight.SetPower(false);
        var audio = player.GetComponent<PlayerHitAudioPresenter>();
        Require(audio != null, "Runtime hit presenter composed");
        var profile = AssetDatabase.LoadAssetAtPath<PlayerHitAudioProfile>("Assets/EmergencyShift/Data/Audio/PlayerHitAudio.asset");
        string profileBefore = JsonUtility.ToJson(profile);
        var rows = new List<object>();
        await Task.Delay(400);

        foreach (string name in Prefabs)
        {
            GameObject actor = Spawn(context, player, name, .25f);
            var melee = actor.GetComponent<EnemyMeleeAttack2D>();
            bool? landed = null;
            Action<bool> resolution = value => landed = value;
            melee.Resolved += resolution;
            try
            {
                actor.AddComponent<BoxCollider2D>().size = Vector2.one * .1f;
                player.RestoreHealth(100);
                int count = audio.PlayedCount;
                player.SetHealthCapacity(100, 100);
                Require(audio.PlayedCount == count, name + " silent HP restore/capacity");
                Fresh(melee);
                melee.Step(0, actor.transform.position, Vector2.left, true);
                Require(audio.PlayedCount == count, name + " preparation has no player hit");
                int health = player.Health;
                melee.Step(1, actor.transform.position, Vector2.left, true);
                Require(landed == true && player.Health < health && audio.PlayedCount == count + 1, name + " accepted real melee");
                AudioClip previous = audio.LastClip;
                int healthAfter = player.Health;
                melee.Step(1, actor.transform.position, Vector2.left, true);
                melee.Step(1, actor.transform.position, Vector2.left, true);
                Require(player.Health == healthAfter && audio.PlayedCount == count + 1, name + " repeated presentation/multiple colliders");
                Fresh(melee);
                landed = null;
                melee.Step(1, actor.transform.position, Vector2.left, true);
                Require(landed == false && audio.PlayedCount == count + 1, name + " protection rejects a new strike");

                player.RestoreHealth(100);
                actor.transform.position = player.transform.position + Vector3.right * 4;
                Physics2D.SyncTransforms();
                Fresh(melee);
                landed = null;
                melee.Step(1, actor.transform.position, Vector2.left, true);
                Require(landed == false && player.Health == 100 && audio.PlayedCount == count + 1, name + " miss is silent");

                actor.transform.position = player.transform.position + Vector3.right * .25f;
                Physics2D.SyncTransforms();
                player.RestoreHealth(100);
                var selections = new List<string>();
                for (int index = 0; index < 6; index++)
                {
                    Fresh(melee);
                    landed = null;
                    int before = audio.PlayedCount;
                    melee.Step(1, actor.transform.position, Vector2.left, true);
                    Require(landed == true && audio.PlayedCount == before + 1, name + " series accepted " + index);
                    Require(audio.LastClip != previous, name + " no adjacent repeated sample");
                    Require(audio.ActiveVoiceCount <= 2, name + " bounded player voices");
                    previous = audio.LastClip;
                    selections.Add(previous.name);
                    await Task.Delay(750);
                }
                rows.Add(new { actor = name, acceptedCues = audio.PlayedCount - count,
                    colliders = actor.GetComponents<Collider2D>().Length, selections,
                    checkedCases = new[] { "restore/capacity", "preparation", "accepted", "repeat Step and colliders", "protection", "miss", "six frequent hits", "no adjacent repeat", "bounded voices" } });
            }
            finally
            {
                melee.Resolved -= resolution;
                Object.Destroy(actor);
            }
            await Task.Delay(40);
        }

        player.RestoreHealth(100);
        GameObject first = Spawn(context, player, "ShadowStalker", .25f);
        GameObject second = Spawn(context, player, "Scorpion", -.25f);
        int pairedBefore = audio.PlayedCount;
        int pairedHealth = player.Health;
        try
        {
            EnemyMeleeAttack2D one = first.GetComponent<EnemyMeleeAttack2D>();
            EnemyMeleeAttack2D two = second.GetComponent<EnemyMeleeAttack2D>();
            Fresh(one);
            Fresh(two);
            one.Step(1, first.transform.position, Vector2.left, true);
            two.Step(1, second.transform.position, Vector2.right, true);
            Require(audio.PlayedCount == pairedBefore + 1 && player.Health < pairedHealth,
                "Nearly simultaneous attacks produce only the health-authorized cue");
        }
        finally
        {
            Object.Destroy(first);
            Object.Destroy(second);
        }
        Require(JsonUtility.ToJson(profile) == profileBefore, "Shared profile unchanged");
        Require(player.GetComponents<PlayerHitAudioPresenter>().Length == 1, "Single presenter");
        var sources = player.transform.Find("Player hit voices (runtime)").GetComponents<AudioSource>();
        Require(sources.Length == 2 && sources.All(source => source.outputAudioMixerGroup != null), "Two routed sources, no accumulation");
        var result = new { scene.path, rows, nearlySimultaneousCueCount = audio.PlayedCount - pairedBefore,
            totalCues = audio.PlayedCount, playerSources = sources.Length, immutableProfile = true,
            method = "Real composed prefabs and common native melee delivery; brains disabled and cooldown snapshots reset only in Play fixtures. Not an autonomous AI playthrough or a balance change." };
        Directory.CreateDirectory("Docs/Validation/PlayerHitTask009");
        File.WriteAllText("Docs/Validation/PlayerHitTask009/native-melee.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    private static GameObject Spawn(LevelContext2D context, PlayerLife2D player, string name, float offset)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/" + name + ".prefab");
        GameObject actor = context.CreateInstance(prefab, player.transform.position + Vector3.right * offset, Quaternion.identity);
        actor.GetComponent<Damageable2D>().enabled = false;
        actor.GetComponent<Rigidbody2D>().simulated = false;
        Physics2D.SyncTransforms();
        return actor;
    }

    private static void Fresh(EnemyMeleeAttack2D melee)
    {
        melee.RestoreState(new EnemyMeleeAttack2D.Snapshot { struck = false, lunged = false, cooldown = 0 });
        melee.Begin();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
