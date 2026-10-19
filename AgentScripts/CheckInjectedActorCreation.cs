using System;
using EmergencyShift;
using UnityEditor;
using UnityEngine;

public static class CheckInjectedActorCreation
{
    public static object Check()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("This lifecycle check requires Play; it never saves a scene.");
        }

        LevelContext2D context = UnityEngine.Object.FindAnyObjectByType<LevelContext2D>();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
        GameObject first = null;
        GameObject second = null;
        try
        {
            first = context.CreateInstance(prefab, new Vector3(10000, 10000, 0), Quaternion.identity);
            second = context.CreateInstance(prefab, new Vector3(10004, 10000, 0), Quaternion.identity);
            EnemyHealth2D firstHealth = first.GetComponent<EnemyHealth2D>();
            EnemyHealth2D secondHealth = second.GetComponent<EnemyHealth2D>();
            int otherHealthBefore = secondHealth.Current;
            bool initializedBeforeFactoryReturned = firstHealth.IsAlive && secondHealth.IsAlive;
            bool injectedTargets = first.GetComponent<ShadowStalker2D>().Player == context.Player.transform &&
                first.GetComponent<ShadowStalker2D>().LightSource == context.Flashlight;
            firstHealth.TakeDamage(1);
            bool independent = secondHealth.Current == otherHealthBefore;
            bool correctScene = first.scene == context.gameObject.scene && second.scene == context.gameObject.scene;
            if (!initializedBeforeFactoryReturned || !injectedTargets || !independent || !correctScene)
            {
                throw new InvalidOperationException("Dynamic actor composition failed its lifecycle/ownership contract.");
            }

            return new { initializedBeforeFactoryReturned, injectedTargets, independent, correctScene, userSceneSaved = false };
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
        }
    }
}
