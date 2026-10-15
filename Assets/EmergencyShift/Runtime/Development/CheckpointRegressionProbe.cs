#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift
{
    /// <summary>Opt-in end-to-end disk save/death/reload regression. Never authored in a scene.</summary>
    public sealed class CheckpointRegressionProbe : MonoBehaviour
    {
        [Serializable]
        private sealed class Result
        {
            public string failure;
            public List<string> passed = new List<string>();
        }

        private Result result = new Result();
        private string output;
        private string deadId;
        private string chasingId;
        private string doorId;
        private string lootId;
        private string remainsJson;
        private int count;
        private Quaternion deadRotation;
        private int expectedAmmo;
        private CheckpointData saved;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--checkpoint-regression");
            if (!Debug.isDebugBuild || index < 0 || index + 1 >= args.Length || FindAnyObjectByType<CheckpointRegressionProbe>() != null)
            {
                return;
            }

            var probe = new GameObject("Temporary checkpoint regression").AddComponent<CheckpointRegressionProbe>();
            DontDestroyOnLoad(probe.gameObject);
            probe.output = args[index + 1];
            var controller = FindAnyObjectByType<LevelCheckpointController2D>();
            controller.UseStore(Path.Combine(Path.GetDirectoryName(probe.output), "checkpoint-test-slot.json"));
            controller.Clear();
            probe.StartCoroutine(probe.Guard(probe.Run()));
        }

        private IEnumerator Guard(IEnumerator run)
        {
            while (true)
            {
                object next;
                try
                {
                    if (!run.MoveNext())
                    {
                        break;
                    }

                    next = run.Current;
                }
                catch (Exception error)
                {
                    result.failure = error.ToString();
                    Finish();
                    yield break;
                }

                yield return next;
            }

            Finish();
        }

        private void Check(string label, bool value)
        {
            if (!value)
            {
                throw new InvalidOperationException(label);
            }

            result.passed.Add(label);
        }

        private void Finish()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(result, true));
            Application.Quit(string.IsNullOrEmpty(result.failure) ? 0 : 1);
        }

        private PersistentEntity2D Entity(string id)
        {
            return PersistentEntity2D.Collect(SceneManager.GetActiveScene())[id];
        }

        private static bool SameRotation(Quaternion a, Quaternion b)
        {
            var first = new Vector4(a.x, a.y, a.z, a.w); var second = new Vector4(b.x, b.y, b.z, b.w);
            return Mathf.Min((first - second).magnitude, (first + second).magnitude) < .00001f;
        }
        private static bool SameSprite(RemainsSpriteState a, RemainsSpriteState b)
        {
            return a.visible == b.visible && Vector3.Distance(a.position, b.position) < .00001f
                && Vector3.Distance(a.scale, b.scale) < .00001f && SameRotation(a.rotation, b.rotation)
                && ((Vector4)a.color - (Vector4)b.color).magnitude < .00001f;
        }
        private static bool SameRemains(RemainsSnapshot a, RemainsSnapshot b)
        {
            if (a.variantId != b.variantId || a.hasPool != b.hasPool || a.parts.Length != b.parts.Length || Vector3.Distance(a.position, b.position) > .00001f) return false;
            if (a.hasPool)
            {
                if (a.pool.Length != b.pool.Length) return false;
                for (int i = 0; i < a.pool.Length; i++) if (!SameSprite(a.pool[i], b.pool[i])) return false;
            }
            for (int i = 0; i < a.parts.Length; i++)
            {
                var first = a.parts[i]; var second = b.parts[i];
                if (first.spriteIndex != second.spriteIndex || first.hasShadow != second.hasShadow || Vector3.Distance(first.position, second.position) > .00001f || !SameSprite(first.body, second.body)) return false;
                if (first.hasShadow && !SameSprite(first.shadow, second.shadow)) return false;
            }
            return true;
        }

        private IEnumerator Run()
        {
            yield return null;
            var player = FindAnyObjectByType<PlayerLife2D>();
            var controller = LevelCheckpointController2D.For(player);
            var motor = player.GetComponent<PlayerMotor2D>();
            motor.enabled = false;
            player.GetComponent<Flashlight2D>().SetPower(false);
            var actors = FindObjectsByType<ShadowStalker2D>();
            foreach (var actor in actors)
            {
                actor.AttacksEnabled = false;
            }

            Check("fresh level has eight authored checkpoint volumes", FindObjectsByType<CheckpointVolume2D>().Length == 8);
            var entities = PersistentEntity2D.Collect(player.gameObject.scene);
            count = entities.Count;
            Check("unique stable IDs cover 44 authored stateful objects", count == 44);
            player.TakeDamage(1);
            var weapon = player.GetComponent<PlayerWeapon2D>();
            weapon.Acquire(7);
            expectedAmmo = weapon.Ammo;
            player.GetComponent<PlayerAccessCards2D>().Add("service");
            string message;
            player.GetComponent<FlashlightUpgrades>().TryInstall("test-module", "Test", .6f, 6, out message, true);
            var corpse = actors[0];
            deadId = corpse.GetComponent<PersistentEntity2D>().Id;
            corpse.GetComponent<StalkerSpriteAnimator>().RestoreVisualRotation(Quaternion.Euler(0, 0, 67));
            deadRotation = corpse.GetComponent<StalkerSpriteAnimator>().VisualRotation;
            corpse.Kill();
            corpse.Kill();
            var chase = actors[1];
            chasingId = chase.GetComponent<PersistentEntity2D>().Id;
            chase.ApplyBulletHit(1, chase.transform.position, Vector2.right);
            var door = FindObjectsByType<Door2D>()[0];
            doorId = door.GetComponent<PersistentEntity2D>().Id;
            door.RestoreState(true);
            var ammo = Array.Find(FindObjectsByType<WeaponLootPickup>(), item => item.Kind == WeaponLootPickup.LootKind.Ammo);
            lootId = ammo.GetComponent<PersistentEntity2D>().Id;
            ammo.RestoreState(true, 0);
            ammo.gameObject.SetActive(false);
            // Wait for all rupture work to settle before the checkpoint is made.
            yield return new WaitForSeconds(4);
            saved = controller.Capture("regression");
            remainsJson = JsonUtility.ToJson(corpse.GetComponent<GoreBurst2D>().CaptureRemains());
            Check("checkpoint writes atomically", controller.TrySaveNow("regression"));
            Check("re-entering the same checkpoint does not overwrite it", !controller.RequestSave("regression"));
            yield return new WaitForSeconds(.8f);
            player.TakeDamage(999);
            Check("actual player death", player.IsDead);
            player.RestartRoom();
            yield return new WaitForSeconds(1);
            player = FindAnyObjectByType<PlayerLife2D>();
            controller = LevelCheckpointController2D.For(player);
            player.GetComponent<PlayerMotor2D>().enabled = false;
            foreach (var actor in FindObjectsByType<ShadowStalker2D>())
            {
                actor.AttacksEnabled = false;
            }

            Check("death reload restored last checkpoint ID", controller.LastCheckpointId == "regression");
            Check("health restored without a full heal", player.Health == 2);
            Check("weapon and exact ammunition restored", player.GetComponent<PlayerWeapon2D>().HasWeapon && player.GetComponent<PlayerWeapon2D>().Ammo == expectedAmmo);
            Check("cards restored", player.GetComponent<PlayerAccessCards2D>().Has("service"));
            Check("flashlight modules restored", player.GetComponent<FlashlightUpgrades>().InstalledCount == 1);
            Check("collected loot stays consumed", !Entity(lootId).gameObject.activeSelf && Entity(lootId).GetComponent<WeaponLootPickup>().Collected);
            Check("door stays open", Entity(doorId).GetComponent<Door2D>().IsOpen);
            Check("dead enemy stays dead", !Entity(deadId).GetComponent<ShadowStalker2D>().IsAlive);
            Check("corpse orientation restored", SameRotation(Entity(deadId).GetComponent<StalkerSpriteAnimator>().VisualRotation, deadRotation));
            Check("persistent aggro survives reload", Entity(chasingId).GetComponent<ShadowStalker2D>().HasAggro);
            var restoredRemains = Entity(deadId).GetComponent<GoreBurst2D>().CaptureRemains();
            File.WriteAllText(output + ".expected-remains.json", remainsJson);
            File.WriteAllText(output + ".actual-remains.json", JsonUtility.ToJson(restoredRemains));
            Check("settled blood and pieces restored within float tolerance", SameRemains(JsonUtility.FromJson<RemainsSnapshot>(remainsJson), restoredRemains));
            Check("no duplicate entities after reload", PersistentEntity2D.Collect(player.gameObject.scene).Count == count);
            Check("one blood pool after restore", FindObjectsByType<BloodPool2D>().Length == 1);
            Check("eight settled parts after restore", FindObjectsByType<RemainsPartIdentity2D>().Length == 8);
            Check("corpse has no active AI audio or animator", !Entity(deadId).GetComponent<ShadowStalker2D>().enabled && !Entity(deadId).GetComponent<StalkerAudio2D>().enabled && !Entity(deadId).GetComponent<StalkerSpriteAnimator>().enabled);
            var invalid = JsonUtility.FromJson<CheckpointData>(JsonUtility.ToJson(saved));
            invalid.entities[0].id = "missing-spawn";
            var position = player.transform.position;
            bool rejected = false;
            try
            {
                controller.Apply(invalid);
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }

            Check("incompatible snapshot rejected before moving player", rejected && player.transform.position == position);
            controller.Reload();
            yield return new WaitForSeconds(1);
            player = FindAnyObjectByType<PlayerLife2D>();
            Check("second load does not duplicate blood", FindObjectsByType<BloodPool2D>().Length == 1 && FindObjectsByType<RemainsPartIdentity2D>().Length == 8);
            controller = LevelCheckpointController2D.For(player);
            controller.Clear();
            Check("clearing checkpoint removes disk slot", !controller.HasCheckpoint);
            controller.Reload();
            yield return new WaitForSeconds(1);
            player = FindAnyObjectByType<PlayerLife2D>();
            Check("new run is clean", player.Health == 3 && !player.GetComponent<PlayerWeapon2D>().HasWeapon && FindObjectsByType<BloodPool2D>().Length == 0);
        }
    }
}
#endif
