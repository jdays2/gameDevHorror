#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>Opt-in behavioural regression. Created only at runtime; never saved in a scene.</summary>
    public sealed class SleeperRegressionProbe : MonoBehaviour
    {
        [Serializable]
        private sealed class Report
        {
            public bool finished;
            public string failure;
            public List<string> passed = new List<string>();
        }
        private readonly Report report = new Report();
        private string output;
        private GameObject prefab;
        private BulletProjectile2D projectilePrefab;
        private ShotVisualProfile shotStyle;
        private GameObject fixture;
        private readonly Vector2 origin = new Vector2(1000, 1000);

        public void Begin(GameObject source, BulletProjectile2D bullet, ShotVisualProfile visuals, string reportPath)
        {
            prefab = source;
            projectilePrefab = bullet;
            shotStyle = visuals;
            output = reportPath;
            StartCoroutine(RunSafely());
        }

        private IEnumerator RunSafely()
        {
            var run = Run();
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
                catch (Exception exception)
                {
                    report.failure = exception.ToString();
                    break;
                }
                yield return next;
            }
            report.finished = true;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            if (fixture != null)
            {
                Destroy(fixture);
            }
        }

        private void Check(string name, bool condition)
        {
            if (!condition)
            {
                throw new InvalidOperationException(name);
            }
            report.passed.Add(name);
        }

        private IEnumerator Run()
        {
            fixture = new GameObject("Disposable Sleeper regression");
            var targetObject = new GameObject("Damage receiver", typeof(CombatProbeTarget2D));
            targetObject.transform.SetParent(fixture.transform);
            var target = targetObject.GetComponent<CombatProbeTarget2D>();
            targetObject.GetComponent<CircleCollider2D>().radius = .25f;
            targetObject.transform.position = origin + Vector2.down * 3;
            var lightObject = new GameObject("Directed test flashlight", typeof(Flashlight2D));
            lightObject.transform.SetParent(targetObject.transform, false);
            var beamObject = new GameObject("Beam", typeof(Light2D));
            beamObject.transform.SetParent(lightObject.transform, false);
            var lamp = lightObject.GetComponent<Flashlight2D>();
            lamp.Configure(null, beamObject.transform, beamObject.GetComponent<Light2D>());
            lamp.enabled = false;
            lamp.AimAt(origin);
            lamp.SetPower(false);

            var first = FindAnyObjectByType<LevelContext2D>().CreateInstance(prefab, origin, Quaternion.identity, fixture.transform).GetComponent<Sleeper2D>();
            var second = FindAnyObjectByType<LevelContext2D>().CreateInstance(prefab, origin + Vector2.right * 8, Quaternion.identity, fixture.transform).GetComponent<Sleeper2D>();
            first.Configure(target.transform, lamp, 1 << 8);
            second.Configure(target.transform, lamp, 1 << 8);
            var firstAudio = first.GetComponent<SleeperAudio2D>();
            var secondAudio = second.GetComponent<SleeperAudio2D>();
            var decor = new GameObject("Ordinary lamp", typeof(Light2D));
            decor.transform.SetParent(fixture.transform);
            decor.transform.position = origin;
            yield return new WaitForSeconds(.8f);
            Check("Sleeping ignores proximity and decorative lights", first.State == Sleeper2D.BehaviourState.Sleeping && !first.HasAggro);
            Check("Sleeping uses compact pose, closed eye and membrane loop", first.GetComponent<CreatureSpriteAnimator2D>().CurrentFrame == 3 && !first.GetComponent<CreatureSpriteAnimator2D>().EyeOpen && firstAudio.BreathingSource.isPlaying);

            lamp.SetPower(true);
            yield return new WaitForSeconds(.22f);
            Check("Brief beam accumulates irritation without instant wake", first.ExposureProgress > 0 && first.State == Sleeper2D.BehaviourState.Sleeping);
            lamp.SetPower(false);
            yield return new WaitForSeconds(.5f);
            Check("Interrupted exposure decays", first.ExposureProgress == 0);
            var wall = new GameObject("Solid wall", typeof(BoxCollider2D));
            wall.transform.SetParent(fixture.transform);
            wall.layer = 8;
            wall.transform.position = origin + Vector2.down * 1.5f;
            wall.GetComponent<BoxCollider2D>().size = new Vector2(4, .15f);
            Physics2D.SyncTransforms();
            lamp.SetPower(true);
            yield return new WaitForSeconds(.8f);
            Check("Opaque wall prevents light wake", first.State == Sleeper2D.BehaviourState.Sleeping);
            wall.SetActive(false);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.75f);
            Check("Sustained directed beam wakes exactly once", first.State == Sleeper2D.BehaviourState.Waking && firstAudio.WakeCount == 1 && !firstAudio.BreathingSource.isPlaying);
            Check("Shared profiles do not share wake or HP", second.State == Sleeper2D.BehaviourState.Sleeping && second.Health == 8 && first.Definition == second.Definition);
            lamp.SetPower(false);
            yield return new WaitForSeconds(1);
            Check("Aggro persists after switching the light off", first.HasAggro && first.State != Sleeper2D.BehaviourState.Sleeping);

            float deadline = Time.time + 3;
            while (first.State != Sleeper2D.BehaviourState.Attacking && Time.time < deadline)
            {
                yield return null;
            }
            Check("Pursuit enters a telegraphed attack", first.State == Sleeper2D.BehaviourState.Attacking && first.AttackProgress < .38f);
            Vector2 heading = first.AttackHeading;
            Vector2 start = first.transform.position;
            targetObject.transform.position = start + Vector2.right * 3;
            yield return new WaitForSeconds(1.07f);
            Vector2 moved = (Vector2)first.transform.position - start;
            Check("Lunge keeps committed heading and can be dodged", target.Hits == 0 && Vector2.Dot(moved, heading) > .3f && Mathf.Abs(Vector2.Dot(moved, new Vector2(-heading.y, heading.x))) < .1f);
            Check("Attack ends in a recovery window without repeating the scream", first.State == Sleeper2D.BehaviourState.Recovering && firstAudio.WakeCount == 1);

            ResetHunter(first, origin);
            targetObject.transform.position = origin + Vector2.down * 2;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Check("Second attack begins with clear line of sight", first.State == Sleeper2D.BehaviourState.Attacking);
            wall.transform.position = origin + Vector2.down;
            wall.SetActive(true);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(1.08f);
            Check("Committed lunge stops at a wall and cannot hit through it (hits=" + target.Hits + ", y=" + first.transform.position.y + ")", target.Hits == 0 && first.transform.position.y > origin.y - .7f);
            wall.SetActive(false);
            ResetHunter(first, origin);
            targetObject.transform.position = origin + Vector2.down * 1.5f;
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(1.1f);
            Check("Real melee delivers exactly one hit per attack", target.Hits == 1 && target.TotalDamage == 1);

            second.AttacksEnabled = false;
            var bullet = Instantiate(projectilePrefab, second.transform.position + Vector3.left * 2, Quaternion.identity, fixture.transform);
            bullet.Launch(target.transform, Vector2.right, shotStyle, null, 1 << second.gameObject.layer, 1, 5);
            yield return new WaitForSeconds(.15f);
            Check("A real unlit projectile hits the shared damage contract and wakes once", second.Health == 7 && second.HasAggro && secondAudio.WakeCount == 1);
            second.ReceiveBulletHit(1, second.transform.position, Vector2.right);
            second.ReceiveBulletHit(1, second.transform.position, Vector2.right);
            Check("Wake voice survives the initial pellet volley", secondAudio.WakeCount == 1 && secondAudio.PainCount == 0);
            yield return new WaitForSeconds(1.6f);
            second.ReceiveBulletHit(1, second.transform.position, Vector2.right);
            second.ReceiveBulletHit(1, second.transform.position, Vector2.right);
            Check("Pellet pain cues have a cooldown", secondAudio.PainCount == 1);
            Check("Damage remains instance-owned", first.Health == 8 && second.Health == 3 && first.Definition.maximumHealth == 8);

            var checkpoint = second.GetComponent<SleeperCheckpoint2D>();
            string live = checkpoint.CaptureCheckpoint();
            checkpoint.ValidateCheckpoint(live);
            int wakeBefore = secondAudio.WakeCount;
            checkpoint.RestoreCheckpoint(live);
            Check("Live checkpoint retains damage and aggro without another wake", second.Health == 3 && second.HasAggro && secondAudio.WakeCount == wakeBefore && !secondAudio.VoiceSource.isPlaying);
            second.ReceiveBulletHit(999, second.transform.position, Vector2.right);
            second.ReceiveBulletHit(999, second.transform.position, Vector2.right);
            Check("Death is single and removes living physics", secondAudio.DeathCount == 1 && !second.IsAlive && !second.GetComponent<CircleCollider2D>().enabled && !second.GetComponent<Rigidbody2D>().simulated && !second.GetComponent<CrowdAgent2D>().enabled);
            var gore = second.GetComponent<GoreBurst2D>();
            deadline = Time.time + 8;
            while ((!gore.IsSettled || second.DeathTime < 1) && Time.time < deadline)
            {
                yield return null;
            }
            Check("Death leaves settled independent remains", gore.IsSettled && gore.CaptureRemains() != null && gore.CaptureRemains().parts.Length > 0);
            string dead = checkpoint.CaptureCheckpoint();
            File.WriteAllText(output + ".dead.json", dead);
            checkpoint.ValidateCheckpoint(dead);
            string remains = JsonUtility.ToJson(gore.CaptureRemains());
            checkpoint.RestoreCheckpoint(dead);
            yield return null;
            File.WriteAllText(output + ".expected-remains.json", remains);
            File.WriteAllText(output + ".actual-remains.json", JsonUtility.ToJson(gore.CaptureRemains()));
            Check("Dead checkpoint is silent and restores settled parts within float tolerance", !second.IsAlive && !secondAudio.enabled && secondAudio.DeathCount == 1 && !secondAudio.VoiceSource.isPlaying && SameRemains(JsonUtility.FromJson<RemainsSnapshot>(remains), gore.CaptureRemains()));
            int ownRemains = 0;
            foreach (var record in FindObjectsByType<RemainsRecord2D>())
            {
                if (Vector2.Distance(record.transform.position, origin) < 20)
                {
                    ownRemains++;
                }
            }
            Check("Restore does not duplicate the remains record", ownRemains == 1);
            target.ReceiveBulletHit(999, target.transform.position, Vector2.up);
            yield return new WaitForSeconds(.1f);
            Check("Target death stops pursuit and attacks", first.State == Sleeper2D.BehaviourState.TargetDead && first.Velocity.sqrMagnitude < .001f);
        }

        private static void ResetHunter(Sleeper2D actor, Vector2 position)
        {
            var saved = actor.CaptureState();
            saved.state = Sleeper2D.BehaviourState.Hunting;
            saved.aggro = true;
            saved.age = 0;
            saved.staggerLeft = 0;
            saved.melee = new EnemyMeleeAttack2D.Snapshot();
            actor.transform.position = position;
            actor.GetComponent<Rigidbody2D>().position = position;
            actor.RestoreState(saved);
        }

        private static bool SameSprite(RemainsSpriteState a, RemainsSpriteState b)
        {
            var rotationA = new Vector4(a.rotation.x, a.rotation.y, a.rotation.z, a.rotation.w);
            var rotationB = new Vector4(b.rotation.x, b.rotation.y, b.rotation.z, b.rotation.w);
            return a.visible == b.visible && Vector3.Distance(a.position, b.position) < .0001f &&
                Vector3.Distance(a.scale, b.scale) < .0001f &&
                Mathf.Min((rotationA - rotationB).magnitude, (rotationA + rotationB).magnitude) < .0001f &&
                ((Vector4)a.color - (Vector4)b.color).magnitude < .0001f;
        }

        private static bool SameRemains(RemainsSnapshot a, RemainsSnapshot b)
        {
            if (a.variantId != b.variantId || a.hasPool != b.hasPool || a.parts.Length != b.parts.Length || Vector3.Distance(a.position, b.position) > .0001f)
            {
                return false;
            }
            if (a.hasPool)
            {
                if (a.pool.Length != b.pool.Length)
                {
                    return false;
                }
                for (int i = 0; i < a.pool.Length; i++)
                {
                    if (!SameSprite(a.pool[i], b.pool[i]))
                    {
                        return false;
                    }
                }
            }
            for (int i = 0; i < a.parts.Length; i++)
            {
                var first = a.parts[i];
                var second = b.parts[i];
                if (first.spriteIndex != second.spriteIndex || first.hasShadow != second.hasShadow || Vector3.Distance(first.position, second.position) > .0001f || !SameSprite(first.body, second.body))
                {
                    return false;
                }
                if (first.hasShadow && !SameSprite(first.shadow, second.shadow))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
#endif
