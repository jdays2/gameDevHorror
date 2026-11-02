using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.DeathScreen.Logic;
using EmergencyShift.Features.DeathScreen.View;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExerciseDeathScreenCheckpoint
{
    private const string FixturePath = "Assets/EmergencyShift/Tests/Fixtures/DeathScreenTask006/CheckpointScene.unity";
    private const string Evidence = "Docs/Validation/DeathScreenD01/";
    private static string Slot => Path.GetFullPath(Path.Combine(Application.dataPath,
        "../../../work/task006-deathscreen/checkpoint/slot.json"));

    public static object Start()
    {
        if (!Application.isPlaying || SceneManager.GetActiveScene().path != FixturePath)
        {
            throw new InvalidOperationException("Play the isolated checkpoint fixture.");
        }
        Directory.CreateDirectory(Evidence);
        Save("checkpoint-status.json", new { status = "running" });
        EditorApplication.delayCall += async () =>
        {
            try
            {
                object result = await Run();
                Save("native-checkpoint.json", result);
                Save("checkpoint-status.json", new { status = "completed" });
            }
            catch (Exception error)
            {
                Save("checkpoint-status.json", new { status = "failed", error = error.ToString() });
            }
        };
        return new { status = "scheduled" };
    }

    public static object Diagnose()
    {
        try
        {
            return One<LevelCheckpointController2D>().Capture("task006-diagnostic");
        }
        catch (Exception error)
        {
            PlayerLife2D player = One<LevelContext2D>().Player;
            PlayerWeapon2D weapon = player.GetComponent<PlayerWeapon2D>();
            return new { error = error.ToString(), fileExists = File.Exists(Slot),
                cardsPresent = player.GetComponent<PlayerAccessCards2D>() != null,
                weaponPresent = weapon != null, definitionPresent = weapon != null && weapon.Definition != null,
                upgradesPresent = player.GetComponent<FlashlightUpgrades>() != null,
                lightPresent = player.GetComponent<Flashlight2D>() != null };
        }
    }

    private static async Task<object> Run()
    {
        var results = new List<object>();
        LevelCheckpointController2D checkpoint = One<LevelCheckpointController2D>();
        checkpoint.UseStore(Slot, true);
        await Task.Delay(200);
        PlayerLife2D player = One<LevelContext2D>().Player;
        player.RestoreHealth(Mathf.Max(1, player.MaximumHealth - 1));
        player.transform.position += Vector3.right;
        player.GetComponent<Rigidbody2D>().position = player.transform.position;
        Physics2D.SyncTransforms();
        Vector3 savedPosition = player.transform.position;
        int savedHealth = player.Health;
        Require(checkpoint.TrySaveNow("task006-return"), "Real isolated checkpoint save failed: " + checkpoint.LastError);
        string good = File.ReadAllText(Slot);
        Require(checkpoint.TryValidateReload(out string validationError), "Preflight must accept actual saved scene: " + validationError);
        player.transform.position += Vector3.right * 3;
        player.GetComponent<Rigidbody2D>().position = player.transform.position;
        Physics2D.SyncTransforms();
        Kill();
        await Ready();
        Require(Presenter().Controller.Model.HasCheckpoint, "Saved return must be offered.");
        string old = player.GetEntityId().ToString();
        Require(Presenter().Controller.Execute(DeathScreenIntent.Retry), "Checkpoint retry should be accepted.");
        await Reloaded(old);
        player = One<LevelContext2D>().Player;
        Require(player.Health == savedHealth && Vector3.Distance(player.transform.position, savedPosition) < .001f,
            "New scene must restore saved health/pose.");
        Require(File.ReadAllText(Slot) == good, "Return must not rewrite the user's slot.");
        results.Add(new { label = "valid-checkpoint-full-Single-reload", health = player.Health,
            position = new[] { player.transform.position.x, player.transform.position.y, player.transform.position.z },
            actualNewScene = true, fileRetained = true });

        File.WriteAllText(Slot, "{broken-slot");
        Kill();
        await Ready();
        old = One<LevelContext2D>().Player.GetEntityId().ToString();
        Presenter().Controller.Execute(DeathScreenIntent.Retry);
        await Error();
        Require(One<LevelContext2D>().Player.GetEntityId().ToString() == old &&
            One<LevelContext2D>().Player.IsDead && File.ReadAllText(Slot) == "{broken-slot",
            "Corrupt checkpoint must remain intact without loading a fresh world.");
        results.Add(new { label = "corrupt-slot", noSceneReplacement = true, fileRetained = true, blocked = Time.timeScale == 0 });

        File.Delete(Slot);
        Presenter().Controller.Execute(DeathScreenIntent.Retry);
        await Error();
        Require(One<LevelContext2D>().Player.GetEntityId().ToString() == old &&
            One<LevelContext2D>().Player.IsDead && !File.Exists(Slot),
            "A disappeared selected slot must not silently start fresh.");
        results.Add(new { label = "missing-selected-slot", noSceneReplacement = true, noFreshFallback = true });

        JObject incompatible = JObject.Parse(good);
        incompatible["levelRevision"] = incompatible["levelRevision"].Value<int>() + 1;
        string incompatibleText = incompatible.ToString(Formatting.None);
        File.WriteAllText(Slot, incompatibleText);
        Presenter().Controller.Execute(DeathScreenIntent.Retry);
        await Error();
        Require(One<LevelContext2D>().Player.GetEntityId().ToString() == old &&
            File.ReadAllText(Slot) == incompatibleText, "Incompatible revision must remain unchanged.");
        results.Add(new { label = "incompatible-revision", noSceneReplacement = true, fileRetained = true });

        File.WriteAllText(Slot, good);
        Presenter().Controller.Execute(DeathScreenIntent.Retry);
        await Reloaded(old);
        Require(One<LevelContext2D>().Player.Health == savedHealth && File.ReadAllText(Slot) == good,
            "Corrected slot must recover by a full reload.");
        results.Add(new { label = "retry-after-repair", actualNewScene = true, fileRetained = true });

        File.WriteAllText(Slot, "{broken-startup-slot");
        old = One<LevelContext2D>().Player.GetEntityId().ToString();
        AsyncOperation startupReload = SceneManager.LoadSceneAsync(FixturePath, LoadSceneMode.Single);
        while (!startupReload.isDone)
        {
            await Task.Delay(35);
        }
        await Until(() => One<LevelContext2D>().Player.GetEntityId().ToString() != old, "Startup reload");
        await Error();
        Require(!One<LevelContext2D>().Player.IsDead && Presenter().Controller.Model.IsRecoveryError &&
            Time.timeScale == 0 && File.ReadAllText(Slot) == "{broken-startup-slot",
            "Failed startup restore must present recovery error rather than silently enable a default player.");
        var title = Presenter().GetComponentsInChildren<UnityEngine.UI.Text>(true).Single(text => text.name == "Death Title");
        Require(title.text == "ОШИБКА", "Recovery heading must describe a load error.");
        results.Add(new { label = "startup-restore-error", defaultPlayerBlocked = true, title = title.text, fileRetained = true });
        File.WriteAllText(Slot, good);
        old = One<LevelContext2D>().Player.GetEntityId().ToString();
        Presenter().Controller.Execute(DeathScreenIntent.Retry);
        await Reloaded(old);
        Require(File.ReadAllText(Slot) == good, "Successful startup recovery must preserve the slot.");
        results.Add(new { label = "startup-error-recovered", health = One<LevelContext2D>().Player.Health,
            phase = Presenter().Controller.Model.Phase.ToString(), fileRetained = true });
        return new { fixture = FixturePath, slot = Slot, cases = results,
            productionMapsModified = false, userSlotsUsed = false };
    }

    private static async Task Reloaded(string old)
    {
        await Until(() => One<LevelContext2D>().Player.GetEntityId().ToString() != old, "Checkpoint Single reload");
        await Until(() => Presenter().Controller.Model.Phase == DeathScreenPhase.Hidden &&
            !One<LevelContext2D>().Player.IsDead, "Restored scene ready");
        await Task.Delay(120);
    }

    private static void Kill()
    {
        PlayerLife2D player = One<LevelContext2D>().Player;
        Require(player.TakeDamage(player.MaximumHealth + 1000), "Lethal damage was rejected.");
    }

    private static Task Ready() => Until(() => Presenter().Controller.Model.Phase == DeathScreenPhase.Ready, "D ready");
    private static Task Error() => Until(() => Presenter().Controller.Model.Phase == DeathScreenPhase.Error, "Load error");
    private static DeathScreenPresenter Presenter() => One<DeathScreenPresenter>();
    private static T One<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();

    private static async Task Until(Func<bool> condition, string label)
    {
        float deadline = Time.realtimeSinceStartup + 7;
        while (Time.realtimeSinceStartup < deadline)
        {
            try
            {
                if (condition())
                {
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                // The old scene has been disposed; wait for the new composition.
            }
            await Task.Delay(35);
        }
        throw new InvalidOperationException("Timed out: " + label);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Save(string file, object value) =>
        File.WriteAllText(Evidence + file, JsonConvert.SerializeObject(value, Formatting.Indented));
}
