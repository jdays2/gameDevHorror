#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Temporary scripted pilot: real physics, proximity pickups, doors and bullets. Never saved/built.</summary>
    public sealed class FirstLevelProbe : MonoBehaviour
    {
        [Serializable] private sealed class Check { public string name; public bool passed; }
        [Serializable] private sealed class Report
        { public bool finished; public string failure; public Check[] checks; public int health,shots,killed,modules; public float seconds; }
        private readonly List<Check> checks=new List<Check>();
        private PlayerLife2D player; private PlayerInteraction interaction; private PlayerWeapon2D weapon;
        private PlayerAccessCards2D cards; private Flashlight2D light; private Rigidbody2D body;
        private DemoLevelFlow2D flow; private ShadowStalker2D[] enemies; private Door2D[] doors;
        private string folder; private float began;
        public void Begin(string output)
        {
            folder=output;Directory.CreateDirectory(folder);began=Time.time;
            player=FindAnyObjectByType<PlayerLife2D>();interaction=player.GetComponent<PlayerInteraction>();
            weapon=player.GetComponent<PlayerWeapon2D>();cards=player.GetComponent<PlayerAccessCards2D>();
            light=player.GetComponent<Flashlight2D>();body=player.GetComponent<Rigidbody2D>();flow=FindAnyObjectByType<DemoLevelFlow2D>();
            enemies=FindObjectsByType<ShadowStalker2D>(FindObjectsInactive.Include);doors=FindObjectsByType<Door2D>();
            player.GetComponent<PlayerMotor2D>().enabled=false;light.SetPower(true);
            StartCoroutine(Guard(Run()));
        }
        private void Verify(string name,bool value)
        {checks.Add(new Check{name=name,passed=value});if(!value)throw new InvalidOperationException(name);}
        private void Fight()
        {
            if(player.IsDead)throw new InvalidOperationException("Pilot died; actual combat must be assessed before accepting route.");
            ShadowStalker2D target=null;float nearest=light.Range;
            foreach(var enemy in enemies)
            {
                if(!enemy.isActiveAndEnabled||!enemy.IsAlive)continue;
                float distance=Vector2.Distance(body.position,enemy.transform.position);
                if(distance>=nearest||Physics2D.Linecast(body.position,enemy.transform.position,1<<8).collider!=null)continue;
                target=enemy;nearest=distance;
            }
            if(target!=null){light.AimAt(target.transform.position);if(weapon.HasWeapon)weapon.TryFire();}
        }
        private IEnumerator Move(float x,float y)
        {
            Vector2 goal=new Vector2(x,y);float deadline=Time.time+Vector2.Distance(body.position,goal)/2+4;
            light.AimAt(goal);
            while(Vector2.Distance(body.position,goal)>.13f && !flow.Completed)
            {
                Fight();if(Time.time>deadline)throw new InvalidOperationException("Route blocked heading to "+goal+" from "+body.position);
                body.linearVelocity=(goal-body.position).normalized*Mathf.Min(3.5f,Vector2.Distance(body.position,goal)/Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }
            body.linearVelocity=Vector2.zero;
        }
        private IEnumerator Door(string prefix)
        {
            var door=Array.Find(doors,d=>d.name.StartsWith(prefix));
            Verify("Interactable door "+prefix,door!=null&&door.Interact(interaction));
            float end=Time.time+.45f;while(Time.time<end){Fight();yield return null;}
            Verify("Door motion ended "+prefix,!door.IsMoving);
        }
        private IEnumerator Save(string name)
        {
            yield return new WaitForEndOfFrame();
            var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());Destroy(image);
        }
        private IEnumerator Run()
        {
            yield return new WaitForSeconds(.4f);
            Verify("Fresh start position",Vector2.Distance(body.position,new Vector2(4,6))<.01f);
            Verify("Scene player references and authored enemy count",cards!=null&&enemies.Length==19&&doors.Length==8);
            Verify("Single listener and adaptive director",FindObjectsByType<AudioListener>().Length==1&&FindObjectsByType<LevelAudioDirector2D>().Length==1);
            Verify("Exit rejects missing evacuation card",!flow.TryFinish(player)&&!flow.Completed);
            Verify("Two active introductory enemies; later groups dormant",FindObjectsByType<ShadowStalker2D>().Length==2);
            foreach(var door in doors)if(door.GetComponent<DoorAccess2D>()!=null)
            {door.Toggle();Verify("Public toggle cannot bypass "+door.name,!door.IsOpen);}
            var first=Array.Find(enemies,e=>e.name.StartsWith("01"));
            Verify("Closed room blocks initial detection",!first.HasAggro&&Physics2D.Linecast(body.position,first.transform.position,1<<8).collider!=null);
            var patrol=Array.Find(enemies,e=>e.name.StartsWith("02"));Vector2 before=patrol.transform.position;
            yield return new WaitForSeconds(2.5f);
            Verify("Introductory patrol moves without aggro",!patrol.HasAggro&&Vector2.Distance(before,patrol.transform.position)>.05f);
            yield return Save("start");
            yield return Move(6,10.8f);yield return Door("01");yield return Move(6,15);yield return Move(3,15);yield return Move(3,21);
            yield return null;interaction.CollectNearbyLoot();
            Verify("Service card auto-pickup and duplicate protection",cards.Has("service")&&!cards.Add("service"));
            Verify("Access card plays existing pickup cue",player.GetComponent<PlayerAudio2D>().PickupCount(PickupKind.AccessCard)==1);
            yield return Move(3,15);yield return Move(6,15);yield return Move(6,10.8f);yield return Door("01");
            yield return Move(10.8f,6);yield return Door("02");yield return Move(15.5f,6);yield return Move(15.5f,4);yield return Move(16.5f,4);
            yield return null;interaction.CollectNearbyLoot();Verify("Weapon and ammunition collected",weapon.HasWeapon&&weapon.Ammo>=12);
            // A card is also reusable: the same token can unlock multiple authored doors.
            yield return Move(16.5f,6);yield return Move(18,10.8f);yield return Door("03");
            Verify("Service lock unlocked through ordinary door interaction",Array.Find(doors,d=>d.name.StartsWith("03")).GetComponent<DoorAccess2D>().IsUnlocked);
            yield return Move(18,15);yield return Move(18,20);yield return Move(16,20);yield return null;interaction.CollectNearbyLoot();
            Verify("First locked-room module improves flashlight",player.GetComponent<FlashlightUpgrades>().InstalledCount==1&&light.Range>4.7f);
            yield return Move(20,20);yield return Move(18,15);yield return Move(18,10.8f);yield return Door("03");
            yield return Move(22.8f,6);yield return Door("04");yield return Move(27,6);yield return Move(26.5f,9);
            Verify("Transfer encounter activated by real entry",Array.Find(enemies,e=>e.name.StartsWith("03")).gameObject.activeSelf);
            yield return Move(27,6);yield return Move(32,6);yield return Move(32,10.8f);yield return Move(30,10.8f);yield return Door("05");
            yield return Move(30,14.5f);yield return Move(34.8f,14.5f);yield return Move(34.8f,21);yield return Move(33,21);
            yield return null;interaction.CollectNearbyLoot();Verify("Evacuation card found in escalating room",cards.Has("evacuation"));
            yield return Move(27,21);yield return Move(27,15);yield return Move(27,14);yield return Move(30,14);yield return Move(30,10.8f);yield return Door("05");
            yield return Move(32,6);yield return Move(34.8f,6);yield return Door("06");yield return Move(39,6);
            Verify("Main locked gate accepts evacuation card",Array.Find(doors,d=>d.name.StartsWith("06")).GetComponent<DoorAccess2D>().IsUnlocked);
            yield return Move(40,6);yield return Move(40,10.8f);yield return Move(42,10.8f);yield return Door("07");
            yield return Move(42,14.5f);yield return Move(39,14.5f);yield return Move(39,21);yield return Move(45,21);yield return Move(42,21);
            yield return null;interaction.CollectNearbyLoot();Verify("Three cumulative modules from two locked rooms",player.GetComponent<FlashlightUpgrades>().InstalledCount==3);
            yield return Save("reserve");
            yield return Move(45,14.5f);yield return Move(42,14.5f);yield return Move(42,10.8f);yield return Door("07");
            yield return Move(47,6);yield return Move(49.5f,6.7f);yield return Move(52.5f,6.7f);yield return Move(57,6.7f);yield return Move(58.8f,6);yield return Door("08");
            Verify("All four authored groups activated",FindObjectsByType<ShadowStalker2D>().Length==19);
            yield return Save("exit-approach");yield return Move(64,6);yield return Move(68,6);yield return null;
            Verify("Physical green threshold completes level without death",flow.Completed&&!player.IsDead&&!weapon.enabled&&!body.simulated);
            Verify("Exit completion is one-shot",!flow.TryFinish(player));
            int killed=0;foreach(var enemy in enemies)if(!enemy.IsAlive)killed++;
            Verify("Real combat killed multiple Stalkers",killed>=4&&player.GetComponent<PlayerAudio2D>().ShotAudioCount>=8);
            yield return Save("completed");
        }
        private IEnumerator Guard(IEnumerator routine)
        {
            // Unity nested enumerators are driven here too so failures reach one report.
            var stack=new Stack<IEnumerator>();stack.Push(routine);string failure=null;
            while(stack.Count>0)
            {
                object current=null;bool more=false;
                try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}
                catch(Exception e){failure=e.ToString();break;}
                if(!more){stack.Pop();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}
                yield return current;
            }
            body.linearVelocity=Vector2.zero;int killed=0;foreach(var enemy in enemies)if(!enemy.IsAlive)killed++;
            File.WriteAllText(Path.Combine(folder,"report.json"),JsonUtility.ToJson(new Report{finished=true,failure=failure,checks=checks.ToArray(),health=player.Health,
                shots=player.GetComponent<PlayerAudio2D>().ShotAudioCount,killed=killed,modules=player.GetComponent<FlashlightUpgrades>().InstalledCount,seconds=Time.time-began},true));
        }
    }
}
#endif
