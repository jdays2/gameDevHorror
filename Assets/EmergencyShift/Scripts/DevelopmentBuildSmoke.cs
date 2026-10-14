#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EmergencyShift
{
    /// <summary>Only runs with --es-smoke-test in a development player. Ordinary game startup is unchanged.</summary>
    public sealed class DevelopmentBuildSmoke : MonoBehaviour
    {
        [Serializable] private sealed class Result {public bool passed;public string[] checks,errors;public string unity,scene;}
        private readonly List<string> checks=new List<string>(),errors=new List<string>();
        private string output;
        private bool finished;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (!Debug.isDebugBuild) return;
            string[] args=Environment.GetCommandLineArgs();if(Array.IndexOf(args,"--es-smoke-test")<0)return;
            int path=Array.IndexOf(args,"--es-smoke-output");if(path<0||path+1>=args.Length)return;
            var r=new GameObject("Development smoke test").AddComponent<DevelopmentBuildSmoke>();r.output=args[path+1];DontDestroyOnLoad(r.gameObject);r.StartCoroutine(r.Run());
        }
        private bool Check(bool value,string label)
        {checks.Add(label+": "+value);if(!value){errors.Add(label);Finish(false);}return value;}
        private IEnumerator Run()
        {
            Application.logMessageReceived+=OnLog;
            yield return null;
            var original=FindAnyObjectByType<PlayerLife2D>();
            if(!Check(original!=null && LevelContext2D.For(original)!=null,"Initial level context"))yield break;
            SceneManager.LoadScene("LevelTemplate");yield return null;yield return null;
            var player=FindAnyObjectByType<PlayerLife2D>();var context=LevelContext2D.For(player);
            if(!Check(context!=null && context.Player==player,"New level context"))yield break;
            if(!Check(FindObjectsByType<AudioListener>().Length==1,"One listener after scene switch"))yield break;
            if(!Check(Door2D.Active.Count==0,"Old door registry cleared on unload"))yield break;
            if(!Check(player.GetComponentsInChildren<AudioSource>(true).Length==25,"Complete player audio prefab"))yield break;
            player.GetComponent<PlayerMotor2D>().enabled=false;player.GetComponent<PlayerInteraction>().enabled=false;player.GetComponent<Rigidbody2D>().simulated=false;player.transform.position=Vector3.zero;
            var weapon=player.GetComponent<PlayerWeapon2D>();weapon.Acquire(20);var aim=player.GetComponent<PlayerAim2D>();aim.SetScriptedTarget(new Vector2(3,0));
            var target=new GameObject("Generic damage contract target",typeof(CombatProbeTarget2D)).GetComponent<CombatProbeTarget2D>();target.transform.position=new Vector3(3,0,0);target.GetComponent<CircleCollider2D>().radius=.35f;Physics2D.SyncTransforms();
            if(!Check(weapon.TryFire(),"Fire from authored rig"))yield break;yield return new WaitForSeconds(.3f);
            if(!Check(target.Hits==1 && target.TotalDamage==1,"Non-Stalker receives one bullet"))yield break;
            var definition=Instantiate(weapon.Definition);definition.damage=3;definition.secondsBetweenShots=.05f;definition.maximumAmmo=77;definition.range=8;weapon.SetDefinition(definition);weapon.TryAddAmmo(200);
            if(!Check(weapon.Ammo==77 && weapon.Damage==3 && Mathf.Approximately(weapon.ShotInterval,.05f),"Weapon data controls stats"))yield break;
            if(!Check(weapon.TryFire(),"Fire with alternate weapon data"))yield break;yield return new WaitForSeconds(.3f);
            if(!Check(target.Hits==2 && target.TotalDamage==4,"Alternate damage reaches generic target"))yield break;
            var wall=new GameObject("Thin wall",typeof(BoxCollider2D));wall.layer=8;wall.transform.position=new Vector3(1.5f,0,0);wall.GetComponent<BoxCollider2D>().size=new Vector2(.015f,2);Physics2D.SyncTransforms();weapon.TryFire();yield return new WaitForSeconds(.3f);
            if(!Check(target.Hits==2,"Thin wall blocks new weapon"))yield break;
            var actor=new GameObject("Authored inactive actor");actor.SetActive(false);var zone=new GameObject("Encounter zone",typeof(EncounterActivation2D)).GetComponent<EncounterActivation2D>();zone.Configure(new[]{actor});
            if(!Check(zone.Activate() && actor.activeSelf && !zone.Activate(),"One-shot encounter activation"))yield break;
            Finish(errors.Count==0);
        }
        private void OnLog(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors.Add(message);Finish(false);}}
        private void Finish(bool passed)
        {
            if(finished)return;finished=true;
            Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(new Result {passed=passed,checks=checks.ToArray(),errors=errors.ToArray(),unity=Application.unityVersion,scene=SceneManager.GetActiveScene().path},true));
            Application.Quit(passed?0:2);
        }
        private void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
#endif
