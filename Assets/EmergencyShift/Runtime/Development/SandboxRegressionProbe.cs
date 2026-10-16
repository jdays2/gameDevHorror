#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace EmergencyShift
{
    /// <summary>Opt-in physical sandbox route, shooting and restart checks. Never saved in a scene.</summary>
    public sealed class SandboxRegressionProbe : MonoBehaviour
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
        private PlayerLife2D player;
        private Rigidbody2D body;
        private PlayerInteraction interaction;
        private PlayerWeapon2D weapon;
        private Keyboard testKeyboard;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--sandbox-regression");
            if (!Debug.isDebugBuild || index < 0 || index + 1 >= args.Length || FindAnyObjectByType<SandboxRegressionProbe>() != null)
            {
                return;
            }

            new GameObject("Temporary sandbox regression").AddComponent<SandboxRegressionProbe>().Begin(args[index + 1]);
        }

        public void Begin(string path)
        {
            output = path;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            DontDestroyOnLoad(gameObject);
            testKeyboard = InputSystem.AddDevice<Keyboard>("Sandbox regression keyboard");
            StartCoroutine(Guard(Run()));
        }

        private IEnumerator Guard(IEnumerator route)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(route);
            while (stack.Count > 0)
            {
                object next = null;
                try
                {
                    if (!stack.Peek().MoveNext())
                    {
                        stack.Pop();
                        continue;
                    }

                    next = stack.Peek().Current;
                }
                catch (Exception error)
                {
                    report.failure = error.ToString();
                    break;
                }

                if (next is IEnumerator nested)
                {
                    stack.Push(nested);
                    continue;
                }

                yield return next;
            }

            report.finished = true;
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            if (testKeyboard != null)
            {
                InputSystem.RemoveDevice(testKeyboard);
            }
            if (!Application.isEditor)
            {
                Application.Quit(string.IsNullOrEmpty(report.failure) ? 0 : 1);
            }
        }

        private void Check(string name, bool passed)
        {
            if (!passed)
            {
                throw new InvalidOperationException(name);
            }

            report.passed.Add(name);
        }

        private void Bind()
        {
            player = FindAnyObjectByType<PlayerLife2D>();
            body = player.GetComponent<Rigidbody2D>();
            interaction = player.GetComponent<PlayerInteraction>();
            weapon = player.GetComponent<PlayerWeapon2D>();
            player.GetComponent<PlayerMotor2D>().enabled = false;
        }

        private IEnumerator Move(float x, float y)
        {
            Vector2 goal = new Vector2(x, y);
            float deadline = Time.time + Vector2.Distance(body.position, goal) / 3 + 3;
            while (Vector2.Distance(body.position, goal) > .1f)
            {
                if (Time.time > deadline || player.IsDead)
                {
                    throw new InvalidOperationException("Route blocked at " + body.position + " toward " + goal);
                }

                body.linearVelocity = (goal - body.position).normalized * Mathf.Min(3.5f, Vector2.Distance(body.position, goal) / Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }

            body.linearVelocity = Vector2.zero;
        }

        private Door2D Entry(int room)
        {
            float x = room * 20 - 10;
            return Array.Find(FindObjectsByType<Door2D>(), door => Vector2.Distance(door.transform.position, new Vector2(x, 0)) < .1f);
        }

        private IEnumerator Open(Door2D door)
        {
            Check("Interact " + door.transform.parent.name + " / " + door.name, door.Interact(interaction));
            yield return new WaitForSeconds(.4f);
            Check("Door fully open", door.IsOpen && !door.IsMoving);
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForSeconds(.4f);
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(output), name + ".png"), texture.EncodeToPNG());
            Destroy(texture);
        }

        private IEnumerator KeyPress(Key key)
        {
            // A separate device avoids editor/OS key resets racing the injected test input.
            testKeyboard.MakeCurrent();
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            yield return null;
            yield return null;
        }

        private IEnumerator Run()
        {
            yield return new WaitForSeconds(.4f);
            Bind();
            Check("Sandbox is startup scene", SceneManager.GetActiveScene().name == "MechanicsSandbox");
            Check("Fresh player 3/3 HP", player.Health == 3 && player.MaximumHealth == 3);
            Check("No objectives, exit or checkpoint slot", FindAnyObjectByType<LevelRunController2D>() == null && FindAnyObjectByType<LevelCheckpointController2D>() == null);
            Check("One listener and accepted adaptive director", FindObjectsByType<AudioListener>().Length == 1 && FindObjectsByType<LevelAudioDirector2D>().Length == 1);
            Check("Two mobile Stalkers and three anchored targets", FindObjectsByType<ShadowStalker2D>().Length == 5 && FindObjectsByType<SandboxStationaryStalker2D>().Length == 3);
            Check("Room 01 starts behind blocking geometry", !Array.Find(FindObjectsByType<ShadowStalker2D>(), a => a.name == "Stalker · waiting").HasAggro);

            yield return Move(30, -3);
            yield return Move(30, -1.1f);
            yield return Open(Entry(2));
            yield return Move(30, 7.2f);
            var locked = FindAnyObjectByType<DoorAccess2D>();
            Check("Locked door rejects missing card", !locked.GetComponent<Door2D>().Interact(interaction) && !locked.IsUnlocked);
            body.linearVelocity = Vector2.up * 3.5f;
            yield return new WaitForSeconds(.6f);
            body.linearVelocity = Vector2.zero;
            Check("Closed door physically blocks movement", body.position.y < 8.2f);
            yield return Move(30, 5);
            yield return Move(24, 4);
            yield return null;
            Check("Physical route auto-picks card", player.GetComponent<PlayerAccessCards2D>().Has("sandbox-card"));
            yield return Move(30, 7.2f);
            yield return Open(locked.GetComponent<Door2D>());
            yield return Move(30, 10);
            Check("Card unlocks only test door", locked.IsUnlocked && player.GetComponent<PlayerAccessCards2D>().Count == 1);
            yield return Capture("card-room");
            yield return Move(30, -3);
            yield return Move(50, -3);
            yield return Move(50, -1.1f);
            yield return Open(Entry(3));
            Check("Damage outside range uses normal HP", player.TakeDamage(1) && player.Health == 2);
            yield return Move(50, 1.5f);
            Check("Range entry raises player to 100/100", player.Health == 100 && player.MaximumHealth == 100);
            yield return Move(48.5f, 2);
            yield return null;
            Check("Pistol collected via proximity", weapon.HasWeapon);
            yield return Move(51.5f, 2);
            yield return Move(45, 5.5f);
            var target = Array.Find(FindObjectsByType<SandboxStationaryStalker2D>(), a => a.GetComponent<EnemyHealth2D>().Maximum == 2);
            var brain = target.GetComponent<ShadowStalker2D>();
            Vector2 targetPosition = target.transform.position;
            var aim = player.GetComponent<PlayerAim2D>();
            aim.SetScriptedTarget(targetPosition);
            player.GetComponent<Flashlight2D>().SetPower(true);
            yield return new WaitForSeconds(.8f);
            Check("Target remains anchored during light response", Vector2.Distance(target.transform.position, targetPosition) < .01f && !brain.AttacksEnabled);
            int beforeAmmo = weapon.Ammo;
            Check("First actual projectile fires", weapon.TryFire());
            yield return new WaitForSeconds(.4f);
            Check("Bullet damages target without killing", brain.Health == 1 && brain.IsAlive && weapon.Ammo == beforeAmmo - 1);
            Check("Target stays anchored after bullet push", Vector2.Distance(target.transform.position, targetPosition) < .01f);
            Check("Second actual projectile fires", weapon.TryFire());
            yield return new WaitForSeconds(2.5f);
            Check("Target dead and body collision removed", !brain.IsAlive && !target.GetComponent<Collider2D>().enabled);
            Check("One independent remains root", FindObjectsByType<RemainsRecord2D>().Length == 1 && target.GetComponent<GoreBurst2D>().IsSettled);
            brain.Kill();
            Check("Repeated kill does not duplicate blood", FindObjectsByType<RemainsRecord2D>().Length == 1);
            Check("Other targets retain independent HP", Array.Find(FindObjectsByType<SandboxStationaryStalker2D>(), a => a.GetComponent<EnemyHealth2D>().Maximum == 6).GetComponent<EnemyHealth2D>().Current == 6);
            yield return KeyPress(Key.T);
            Check("T refills existing weapon", weapon.Ammo == weapon.MaximumAmmo);
            yield return Capture("shooting-range");
            yield return Move(50, 2);
            yield return Move(50, -2);
            Check("Leaving range restores original 2/3 HP", player.MaximumHealth == 3 && player.Health == 2);
            yield return Move(50, 2);
            Check("Reentering range reapplies 100 HP", player.MaximumHealth == 100 && player.Health == 100);
            yield return Move(50, -2);
            Check("Repeated entry does not heal outside", player.MaximumHealth == 3 && player.Health == 2);
            aim.ClearScriptedTarget();
            yield return KeyPress(Key.R);
            yield return new WaitForSeconds(.4f);
            Bind();
            Check("R restarts while alive", player.Health == 3 && !weapon.HasWeapon && Vector2.Distance(body.position, new Vector2(10, -3)) < .1f);
            Check("Restart restores targets, loot and doors", FindObjectsByType<ShadowStalker2D>().Length == 5 && FindObjectsByType<RemainsRecord2D>().Length == 0 && !FindAnyObjectByType<DoorAccess2D>().IsUnlocked);
            Check("Restart leaves one listener and four doors", FindObjectsByType<AudioListener>().Length == 1 && Door2D.Active.Count == 4);
            yield return Move(10, -1.1f);
            yield return Open(Entry(1));
            yield return Move(5, 4);
            var waiting = Array.Find(FindObjectsByType<ShadowStalker2D>(), a => a.name == "Stalker · waiting");
            player.GetComponent<PlayerAim2D>().SetScriptedTarget(waiting.transform.position);
            player.GetComponent<Flashlight2D>().SetPower(true);
            yield return Move(5, 5.7f);
            yield return new WaitForSeconds(.8f);
            Check("Mobile Stalker detects real nearby player", waiting.HasAggro);
            Check("Mobile Stalker reacts to flashlight", waiting.State == ShadowStalker2D.BehaviourState.Recoiling || waiting.State == ShadowStalker2D.BehaviourState.Retreating || waiting.State == ShadowStalker2D.BehaviourState.Waiting);
            yield return Capture("stalker-room");
            yield return Move(5, 2);
            Check("Room 01 keeps permanent aggro", waiting.HasAggro);
            yield return new WaitForSeconds(.8f);
            player.TakeDamage(1000);
            Check("Normal player can still be defeated", player.IsDead);
            yield return KeyPress(Key.R);
            yield return new WaitForSeconds(.4f);
            Bind();
            Check("R restarts after death", player.Health == 3 && !player.IsDead && Vector2.Distance(body.position, new Vector2(10, -3)) < .1f);
        }
    }
}
#endif
