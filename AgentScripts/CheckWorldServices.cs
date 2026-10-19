using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Core.Composition;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public static class CheckWorldServices
{
    public static async Task<object> Check()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("The world-service lifecycle check requires Play.");
        }

        LevelContext2D context = UnityEngine.Object.FindAnyObjectByType<LevelContext2D>();
        var registries = new RegistryProbe();
        var budgets = new BudgetProbe();
        context.InjectScoped(registries);
        context.InjectScoped(budgets);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
        var beforeRemains = new HashSet<RemainsRecord2D>(UnityEngine.Object.FindObjectsByType<RemainsRecord2D>());
        GameObject first = null;
        GameObject second = null;
        GameObject wall = null;
        RemainsRecord2D ownedRemains = null;
        try
        {
            first = context.CreateInstance(prefab, new Vector3(10000, 10000, 0), Quaternion.identity);
            second = context.CreateInstance(prefab, new Vector3(10004, 10000, 0), Quaternion.identity);
            CrowdAgent2D firstCrowd = first.GetComponent<CrowdAgent2D>();
            StalkerAudio2D firstAudio = first.GetComponent<StalkerAudio2D>();
            bool registered = registries.Crowd.Members.Contains(firstCrowd) && registries.Voices.Members.Contains(firstAudio);
            if (!registered)
            {
                throw new InvalidOperationException("Injected actor adapters did not register in their scene.");
            }

            wall = new GameObject("Disposable navigation obstruction", typeof(BoxCollider2D));
            SceneManager.MoveGameObjectToScene(wall, context.gameObject.scene);
            wall.layer = 8;
            wall.transform.position = new Vector3(10002, 10000, 0);
            wall.GetComponent<BoxCollider2D>().size = new Vector2(.25f, 3);
            Physics2D.SyncTransforms();
            EnemyMotor2D motor = first.GetComponent<EnemyMotor2D>();
            DateTime navigationDeadline = DateTime.UtcNow.AddSeconds(3);
            while (motor.WaypointCount == 0 && DateTime.UtcNow < navigationDeadline)
            {
                motor.Follow(second.transform.position, 1);
                await Task.Delay(30);
            }

            bool pathBuilt = motor.WaypointCount > 0;
            if (!pathBuilt)
            {
                throw new InvalidOperationException("The injected navigation path could not route around the isolated obstruction.");
            }

            EnemyHealth2D health = first.GetComponent<EnemyHealth2D>();
            GoreBurst2D gore = first.GetComponent<GoreBurst2D>();
            health.Kill();
            health.Kill();
            RemainsRecord2D[] created = UnityEngine.Object.FindObjectsByType<RemainsRecord2D>()
                .Where(record => !beforeRemains.Contains(record) && record.gameObject.scene == context.gameObject.scene &&
                    (record.transform.position - first.transform.position).sqrMagnitude < 1).ToArray();
            if (created.Length != 1)
            {
                throw new InvalidOperationException("Repeated death must create exactly one owned remains record.");
            }

            ownedRemains = created[0];
            DateTime deathDeadline = DateTime.UtcNow.AddSeconds(8);
            while (!gore.IsSettled && DateTime.UtcNow < deathDeadline)
            {
                await Task.Delay(50);
            }

            RemainsSnapshot snapshot = gore.CaptureRemains();
            bool settled = gore.IsSettled && snapshot != null && snapshot.parts.Length > 0;
            if (!settled)
            {
                throw new InvalidOperationException("Owned fragment budget did not complete and settle the authored death.");
            }

            UnityEngine.Object.Destroy(first);
            first = null;
            await Task.Delay(100);
            bool unregistered = !registries.Crowd.Members.Contains(firstCrowd) && !registries.Voices.Members.Contains(firstAudio);
            bool remainsSurvivedActor = ownedRemains != null && ownedRemains.IsSettled;
            if (!unregistered || !remainsSurvivedActor)
            {
                throw new InvalidOperationException("Actor teardown leaked registry membership or removed transferred remains.");
            }

            return new { registered, pathBuilt, settled, unregistered, remainsSurvivedActor,
                parts = snapshot.parts.Length, navigationPeak = budgets.Navigation.PeakBuilds,
                fragmentPeak = budgets.Gore.PeakFragments, userSceneSaved = false };
        }
        finally
        {
            if (first != null)
            {
                UnityEngine.Object.Destroy(first);
            }

            if (second != null)
            {
                UnityEngine.Object.Destroy(second);
            }

            if (wall != null)
            {
                UnityEngine.Object.Destroy(wall);
            }

            if (ownedRemains != null)
            {
                UnityEngine.Object.Destroy(ownedRemains.gameObject);
            }
        }
    }

    private sealed class RegistryProbe
    {
        public SceneRegistry<CrowdAgent2D> Crowd { get; private set; }
        public SceneRegistry<StalkerAudio2D> Voices { get; private set; }

        [Inject]
        private void Construct(SceneRegistry<CrowdAgent2D> crowd, SceneRegistry<StalkerAudio2D> voices)
        {
            Crowd = crowd;
            Voices = voices;
        }
    }

    private sealed class BudgetProbe
    {
        public NavigationWorkBudget2D Navigation { get; private set; }
        public GoreSpawnBudget2D Gore { get; private set; }

        [Inject]
        private void Construct(NavigationWorkBudget2D navigation, GoreSpawnBudget2D gore)
        {
            Navigation = navigation;
            Gore = gore;
        }
    }
}
