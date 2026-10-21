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
        private Mouse testMouse;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--sandbox-regression");
            bool shotgun = false;
            bool pistol = false;
            if (index < 0)
            {
                index = Array.IndexOf(args, "--shotgun-regression");
                shotgun = true;
            }
            if (index < 0)
            {
                index = Array.IndexOf(args, "--pistol-regression");
                shotgun = false;
                pistol = true;
            }
            if (!Debug.isDebugBuild || index < 0 || index + 1 >= args.Length || FindAnyObjectByType<SandboxRegressionProbe>() != null)
            {
                return;
            }

            new GameObject("Temporary sandbox regression").AddComponent<SandboxRegressionProbe>().Begin(args[index + 1], shotgun, pistol);
        }

        public void Begin(string path, bool shotgun = false, bool pistol = false)
        {
            output = path;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            DontDestroyOnLoad(gameObject);
            testKeyboard = InputSystem.AddDevice<Keyboard>("Sandbox regression keyboard");
            IEnumerator route = Run();
            if (shotgun)
            {
                route = RunShotgun();
            }
            if (pistol)
            {
                route = RunPistol();
            }
            StartCoroutine(Guard(route));
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

            if (testMouse != null)
            {
                InputSystem.RemoveDevice(testMouse);
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

        private IEnumerator RunPistol()
        {
            yield return new WaitForSeconds(.4f);
            Bind();
            yield return Move(50, -3);
            yield return Move(50, -1.1f);
            yield return Open(Entry(3));
            yield return Move(48.5f, 2);
            yield return null;
            var pistol = weapon.Definition;
            Check("Pistol pickup loads only available cartridges", weapon.MagazineCapacity == 8 && weapon.LoadedAmmo == Mathf.Min(weapon.Ammo, 8));
            yield return Move(46, 2);
            yield return null;
            var shotgun = weapon.Definition;
            Check("Both weapons share flying projectile presentation", shotgun.Visuals.projectile == pistol.Visuals.projectile);
            yield return KeyPress(Key.Digit1);
            yield return Move(48.5f, 5);
            player.GetComponent<PlayerAim2D>().SetScriptedTarget(new Vector2(48.5f, 14));
            yield return null;
            int loaded = weapon.LoadedAmmo;
            weapon.AddAmmo(30, pistol);
            int total = weapon.Ammo;
            Check("Ammo pickup adds reserve without filling magazine", weapon.LoadedAmmo == loaded && weapon.ReserveAmmo == total - loaded);
            var audio = player.GetComponent<PlayerAudio2D>();
            var utility = player.GetComponent<PlayerUtilityAudio2D>();
            int empty = utility.EmptyTriggersPlayed;
            int sounds = audio.ShotAudioCount;
            for (int index = 0; index < loaded; index++)
            {
                Check("Loaded pistol shot " + (index + 1), weapon.TryFire());
                yield return new WaitForSeconds(.13f);
            }
            Check("Successful pistol shots never add empty clicks", utility.EmptyTriggersPlayed == empty && audio.ShotAudioCount == sounds + loaded);
            Check("Pistol cannot shoot reserve directly", weapon.LoadedAmmo == 0 && weapon.Ammo == total - loaded && !weapon.TryFire());
            Check("Empty magazine gives exactly one dry click", utility.EmptyTriggersPlayed == empty + 1);
            int mechanisms = audio.MechanismAudioCount;
            yield return KeyPress(Key.X);
            Check("X starts magazine reload and its foley", weapon.Phase == WeaponPhase.Loading && audio.MechanismAudioCount == mechanisms + 1);
            weapon.TryFire();
            Check("Reload suppresses dry clicks and repeated reload cues", utility.EmptyTriggersPlayed == empty + 1 && !weapon.TryReload() && audio.MechanismAudioCount == mechanisms + 1);
            yield return new WaitForSeconds(.5f);
            Check("Magazine does not fill early", weapon.LoadedAmmo == 0);
            yield return new WaitForSeconds(1);
            Check("Reload transfers reserve once", weapon.LoadedAmmo == 8 && weapon.Ammo == total - loaded && weapon.Phase == WeaponPhase.Ready);
            Check("Reloaded pistol fires", weapon.TryFire());
            yield return new WaitForSeconds(.15f);
            weapon.TryReload();
            yield return new WaitForSeconds(.3f);
            yield return KeyPress(Key.Digit2);
            yield return KeyPress(Key.Digit1);
            Check("Switching cancels reload without free cartridges", weapon.LoadedAmmo == 7 && weapon.Phase == WeaponPhase.Ready);
            weapon.TryReload();
            yield return new WaitForSeconds(.3f);
            var catalog = ScriptableObject.CreateInstance<WeaponCatalog>();
            catalog.weapons = new[] { pistol, shotgun };
            var adapter = player.gameObject.AddComponent<PlayerCheckpoint2D>();
            adapter.Configure(catalog);
            string slot = Path.Combine(Path.GetDirectoryName(output), "isolated-pistol-payload.json");
            File.WriteAllText(slot, adapter.CaptureCheckpoint());
            int savedAmmo = weapon.Ammo;
            player.TakeDamage(1000);
            Check("Death stops a pending magazine reload", player.IsDead && !weapon.enabled);
            yield return KeyPress(Key.R);
            yield return new WaitForSeconds(.4f);
            Bind();
            adapter = player.gameObject.AddComponent<PlayerCheckpoint2D>();
            adapter.Configure(catalog);
            adapter.RestoreCheckpoint(File.ReadAllText(slot));
            player.GetComponent<PlayerMotor2D>().enabled = false;
            Check("Disk restores exact magazine reserve and remaining reload", weapon.LoadedAmmo == 7 && weapon.Ammo == savedAmmo && weapon.Phase == WeaponPhase.Loading);
            yield return new WaitForSeconds(1.3f);
            Check("Restored magazine reload commits once", weapon.LoadedAmmo == 8 && weapon.Ammo == savedAmmo && weapon.Phase == WeaponPhase.Ready);
            File.Delete(slot);
            Destroy(catalog);
        }

        private IEnumerator RunShotgun()
        {
            yield return new WaitForSeconds(.4f);
            Bind();
            testMouse = InputSystem.AddDevice<Mouse>("Shotgun regression mouse");
            Check("Shotgun route starts in fresh sandbox", SceneManager.GetActiveScene().name == "MechanicsSandbox" && !weapon.HasWeapon);
            yield return Move(50, -3);
            yield return Move(50, -1.1f);
            yield return Open(Entry(3));
            yield return Move(48.5f, 2);
            yield return null;
            var pistol = weapon.Definition;
            int pistolAmmo = weapon.Ammo;
            Check("Pistol pickup has a loaded magazine", weapon.HasWeapon && pistol.ContentId == "pistol" && weapon.MagazineCapacity > 0 && weapon.LoadedAmmo == Mathf.Min(weapon.Ammo, weapon.MagazineCapacity));
            yield return Move(46, 2);
            yield return null;
            var shotgun = weapon.Definition;
            var catalog = ScriptableObject.CreateInstance<WeaponCatalog>();
            catalog.weapons = new[] { pistol, shotgun };
            Check("Shotgun auto pickup owns both weapons", weapon.OwnedCount == 2 && shotgun.ContentId == "pump-shotgun");
            Check("Shotgun starts with four loaded and twelve reserve", weapon.LoadedAmmo == 4 && weapon.ReserveAmmo == 12 && weapon.Ammo == 16);
            yield return Move(46, 5);
            yield return Move(55, 6.4f);
            var aim = player.GetComponent<PlayerAim2D>();
            aim.SetScriptedTarget(new Vector2(55, 9));
            player.GetComponent<Flashlight2D>().SetPower(true);
            yield return null;
            var audio = player.GetComponent<PlayerAudio2D>();
            var ejector = player.GetComponent<PlayerCasingEjector2D>();
            int shots = weapon.ShotsFired;
            int shotSounds = audio.ShotAudioCount;
            int ejected = ejector.EmittedCount;
            testMouse.MakeCurrent();
            InputSystem.QueueStateEvent(testMouse, new MouseState { buttons = 1 });
            yield return null;
            yield return new WaitForFixedUpdate();
            Check("One click creates eight real projectiles", FindObjectsByType<BulletProjectile2D>().Length == shotgun.ProjectileCount);
            Check("One volley consumes one shell and plays one shot", weapon.LoadedAmmo == 3 && weapon.Ammo == 15 && audio.ShotAudioCount == shotSounds + 1);
            Check("Shotgun selects its own recorded shot", Array.Exists(shotgun.AudioProfile.shots, clip => clip != null && clip.name == audio.LastShotClip));
            Check("Hull is not ejected at muzzle event", ejector.EmittedCount == ejected);
            Check("Pump blocks immediate second shot and switching", !weapon.TryFire() && !weapon.TryEquip(0));
            yield return new WaitForSeconds(1.15f);
            Check("Holding mouse does not fire after pump", weapon.ShotsFired == shots + 1 && weapon.LoadedAmmo == 3);
            Check("Pump ejects one hull and plays its mechanical recording", ejector.EmittedCount == ejected + 1 && audio.MechanismAudioCount > 0);
            Check("Hull landing uses shotgun recording", audio.LastCasingClip == "hull_concrete_01");
            InputSystem.QueueStateEvent(testMouse, new MouseState());
            yield return null;
            var target = Array.Find(FindObjectsByType<SandboxStationaryStalker2D>(), a => a.GetComponent<EnemyHealth2D>().Maximum == 20);
            Check("Real pellets damage anchored target", target.GetComponent<EnemyHealth2D>().Current < 20);
            for (int i = 0; i < 3; i++)
            {
                Check("Separate shotgun shot " + (i + 2), weapon.TryFire());
                yield return new WaitForSeconds(1);
            }

            Check("Exactly four shots exhaust magazine", weapon.LoadedAmmo == 0 && weapon.Ammo == 12 && !weapon.TryFire());
            Check("Lethal pellet volley produces one settled remains record", !target.GetComponent<EnemyHealth2D>().IsAlive && FindObjectsByType<RemainsRecord2D>().Length == 1 && target.GetComponent<GoreBurst2D>().IsSettled);
            int empty = player.GetComponent<PlayerUtilityAudio2D>().EmptyTriggersPlayed;
            yield return new WaitForSeconds(.3f);
            weapon.TryFire();
            Check("Empty trigger has feedback", player.GetComponent<PlayerUtilityAudio2D>().EmptyTriggersPlayed > empty);
            yield return KeyPress(Key.X);
            Check("X begins single shell loading", weapon.Phase == WeaponPhase.Loading);
            yield return new WaitForSeconds(.9f);
            Check("One shell commits from reserve", weapon.LoadedAmmo == 1 && weapon.Ammo == 12 && weapon.ReserveAmmo == 11);
            Check("Loaded shell can interrupt reload to fire", weapon.TryFire());
            yield return new WaitForSeconds(1);
            Check("Interrupted insertion creates no free shell", weapon.LoadedAmmo == 0 && weapon.Phase == WeaponPhase.Ready && weapon.Ammo == 11);
            yield return KeyPress(Key.X);
            yield return new WaitForSeconds(3.4f);
            Check("Four shells refill sequentially without changing total", weapon.LoadedAmmo == 4 && weapon.Ammo == 11 && weapon.Phase == WeaponPhase.Ready);
            yield return KeyPress(Key.Digit1);
            Check("1 equips original pistol and preserves its ammo", weapon.Definition == pistol && weapon.Ammo == pistolAmmo);
            yield return KeyPress(Key.Digit2);
            Check("2 restores shotgun magazine and reserve", weapon.Definition == shotgun && weapon.LoadedAmmo == 4 && weapon.Ammo == 11);
            yield return Capture("shotgun-range");

            // Two generic receivers in one cone verify that the firing code has no Stalker dependency.
            yield return Move(50, 11);
            var left = new GameObject("Volley receiver left", typeof(CombatProbeTarget2D)).GetComponent<CombatProbeTarget2D>();
            var right = new GameObject("Volley receiver right", typeof(CombatProbeTarget2D)).GetComponent<CombatProbeTarget2D>();
            left.transform.position = new Vector3(49.75f, 14, 0);
            right.transform.position = new Vector3(50.25f, 14, 0);
            left.GetComponent<CircleCollider2D>().radius = .18f;
            right.GetComponent<CircleCollider2D>().radius = .18f;
            aim.SetScriptedTarget(new Vector2(50, 15));
            yield return null;
            Physics2D.SyncTransforms();
            var normalVisuals = weapon.VisualProfile;
            var hiddenVisuals = Instantiate(normalVisuals);
            hiddenVisuals.projectile = Instantiate(normalVisuals.projectile);
            hiddenVisuals.projectile.projectileVisibility = 0;
            weapon.SetVisualProfile(hiddenVisuals);
            UnityEngine.Random.InitState(4471);
            Check("Generic two-target volley fires", weapon.TryFire());
            Check("Hidden projectile presentation disables sprites only", Array.TrueForAll(FindObjectsByType<BulletProjectile2D>(), bullet => Array.TrueForAll(bullet.GetComponentsInChildren<SpriteRenderer>(), renderer => !renderer.enabled)));
            yield return new WaitForSeconds(1);
            Check("One cone can hit two independent damage receivers", left.Hits > 0 && right.Hits > 0);
            weapon.SetVisualProfile(normalVisuals);
            Destroy(hiddenVisuals.projectile);
            Destroy(hiddenVisuals);
            int leftHits = left.Hits;
            int rightHits = right.Hits;
            var wall = new GameObject("Temporary pellet blocker", typeof(BoxCollider2D));
            wall.layer = 8;
            wall.transform.position = new Vector3(50, 13, 0);
            wall.GetComponent<BoxCollider2D>().size = new Vector2(4, .03f);
            Physics2D.SyncTransforms();
            int impactsBefore = audio.ImpactAudioCount;
            Check("Blocked volley fires", weapon.TryFire());
            yield return new WaitForSeconds(1);
            Check("Thin wall blocks every pellet", left.Hits == leftHits && right.Hits == rightHits);
            Check("Wall does not play eight full impact sounds", audio.ImpactAudioCount - impactsBefore <= 1);
            Destroy(wall);
            Destroy(left.gameObject);
            Destroy(right.gameObject);
            yield return Move(50, 2);
            yield return KeyPress(Key.Digit1);
            yield return Move(50, 4);
            yield return Move(54, 4);
            yield return Move(54, 2);
            yield return null;
            Check("Typed shotgun ammo leaves equipped pistol unchanged", weapon.Definition == pistol && weapon.Ammo == pistolAmmo);
            yield return KeyPress(Key.Digit2);
            Check("Typed ammo refills the holstered shotgun", weapon.Ammo == 21);
            yield return KeyPress(Key.T);
            Check("Sandbox T fills selected weapon total", weapon.Ammo == shotgun.MaximumAmmo);
            yield return Move(50, 2);
            yield return Move(50, -2);
            aim.SetScriptedTarget(new Vector2(50, -5));
            yield return null;
            Check("Corridor shot prepares checkpoint fixture", weapon.TryFire());
            yield return new WaitForSeconds(1);
            Check("Checkpoint fixture starts loading", weapon.TryReload());
            yield return new WaitForSeconds(.25f);
            var adapter = player.GetComponent<PlayerCheckpoint2D>();
            if (adapter == null)
            {
                adapter = player.gameObject.AddComponent<PlayerCheckpoint2D>();
            }

            adapter.Configure(catalog);
            var captured = adapter.CaptureCheckpoint();
            var store = new CheckpointStore(Path.Combine(Path.GetDirectoryName(output), "isolated-weapon-slot.json"));
            store.Write(new CheckpointData
            {
                levelId = "shotgun-regression",
                levelRevision = 1,
                checkpointId = "weapon-payload",
                entities = new[]
                {
                    new CheckpointEntityState
                    {
                        id = "test-player",
                        active = true,
                        position = player.transform.position,
                        rotation = player.transform.rotation,
                        parts = new[] { new CheckpointPart { key = adapter.SaveKey, json = captured } }
                    }
                }
            });
            int savedAmmo = weapon.Ammo;
            int savedLoaded = weapon.LoadedAmmo;
            player.TakeDamage(1000);
            Check("Death stops a player during loading", player.IsDead && !weapon.enabled);
            yield return KeyPress(Key.R);
            yield return new WaitForSeconds(.4f);
            Bind();
            Check("Scene reload starts with clean inventory", !weapon.HasWeapon);
            adapter = player.GetComponent<PlayerCheckpoint2D>();
            if (adapter == null)
            {
                adapter = player.gameObject.AddComponent<PlayerCheckpoint2D>();
            }

            adapter.Configure(catalog);
            var saved = store.Read().entities[0];
            adapter.ValidateCheckpoint(saved.parts[0].json);
            body.position = saved.position;
            player.transform.position = saved.position;
            adapter.RestoreCheckpoint(saved.parts[0].json);
            player.GetComponent<PlayerMotor2D>().enabled = false;
            Check("Disk payload restores both weapons and exact loaded reserve state", weapon.OwnedCount == 2 && weapon.Ammo == savedAmmo && weapon.LoadedAmmo == savedLoaded && weapon.Phase == WeaponPhase.Loading);
            yield return new WaitForSeconds(.65f);
            Check("Restored pending insertion commits once", weapon.LoadedAmmo == savedLoaded + 1 && weapon.Ammo == savedAmmo);
            yield return KeyPress(Key.Digit1);
            Check("Pistol survives death/disk/reload independently", weapon.Definition == pistol && weapon.Ammo == pistolAmmo);
            var legacy = new PlayerCheckpoint2D.State
            {
                format = 1, health = player.Health, weapon = true, weaponId = pistol.ContentId, ammo = 7,
                cards = Array.Empty<string>(), modules = Array.Empty<FlashlightUpgrades.ModuleState>(), direction = Vector2.up
            };
            adapter.RestoreCheckpoint(JsonUtility.ToJson(legacy));
            Check("Legacy pistol payload remains compatible", weapon.OwnedCount == 1 && weapon.Definition == pistol && weapon.Ammo == 7);
            store.Delete();
            Destroy(catalog);
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
            Check("Restart leaves one listener and four doors", FindObjectsByType<AudioListener>().Length == 1 && FindObjectsByType<Door2D>().Length == 4);
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
