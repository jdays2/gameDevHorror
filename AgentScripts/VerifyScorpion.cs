using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.Scorpion.View;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyScorpion
{
    private const string Folder = "Docs/Validation/ScorpionRoom01/";
    private static Transform[] Nodes() => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    private static Transform Find(string path) => Nodes().First(t => PathOf(t) == path);

    public static object Authoring()
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Editor authoring audit requires stopped Play.");
        }
        var nodes = Nodes();
        JObject before = JObject.Parse(File.ReadAllText(Folder + "before.json"));
        var changed = new List<string>();
        foreach (JObject old in before["nodes"])
        {
            string path = (string)old["path"];
            var oldPosition = new Vector3((float)old["position"][0], (float)old["position"][1], (float)old["position"][2]);
            var candidates = nodes.Where(t => PathOf(t) == path).ToArray();
            Transform node = candidates.FirstOrDefault(t => Vector3.Distance(t.position, oldPosition) < .0001f) ?? candidates.FirstOrDefault();
            if (node == null)
            {
                changed.Add("Missing: " + path);
                continue;
            }
            var position = new Vector3((float)old["position"][0], (float)old["position"][1], (float)old["position"][2]);
            var scale = new Vector3((float)old["scale"][0], (float)old["scale"][1], (float)old["scale"][2]);
            if (Vector3.Distance(position, node.position) > .0001f || Vector3.Distance(scale, node.lossyScale) > .0001f)
            {
                changed.Add("Transform: " + path);
            }
            if (node.gameObject.activeSelf != (bool)old["active"])
            {
                changed.Add("Active: " + path);
            }
        }
        Transform room = Find("10_World/R04_ScorpionPackTest");
        var pack = room.GetComponentInChildren<ScorpionPack2D>(true);
        var ids = nodes.Select(t => t.GetComponent<PersistentEntity2D>()).Where(x => x != null).ToArray();
        var report = new
        {
            scene = SceneManager.GetActiveScene().path,
            dirty = SceneManager.GetActiveScene().isDirty,
            previousObjects = before["nodes"].Count(), currentObjects = nodes.Length,
            previousChanges = changed,
            missingScripts = nodes.Sum(n => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(n.gameObject)),
            packMembers = pack.Members.Select(m => new { m.name, parent = PathOf(m.transform.parent), prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(m.gameObject), position = m.transform.position.ToString() }),
            door = room.GetComponentInChildren<Door2D>(true).Blocker.bounds.ToString(),
            cameras = nodes.Count(n => n.GetComponent<Camera>() != null),
            listeners = nodes.Count(n => n.GetComponent<AudioListener>() != null),
            persistentEntities = ids.Length,
            duplicateIds = ids.GroupBy(x => x.Id).Where(group => group.Count() > 1 || string.IsNullOrWhiteSpace(group.Key)).Select(group => group.Key).ToArray(),
            audioSourcesInRoom = room.GetComponentsInChildren<AudioSource>(true).Length,
            colliders = room.GetComponentsInChildren<Collider2D>(true).Length,
            notes = "Sandbox has no checkpoint controller. New pack and actors intentionally have no persistence payload/IDs. All inactive nodes included; only the shared south corridor boundary transform is intentionally changed."
        };
        File.WriteAllText(Folder + "authoring.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    public static async Task<object> DoorScenario()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires native Play.");
        }
        var context = Nodes().Select(n => n.GetComponent<LevelContext2D>()).First(c => c != null);
        var player = context.Player;
        player.SetHealthCapacity(100, 100);
        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        playerBody.position = new Vector2(50,-4);
        player.transform.position = new Vector3(50,-4,0);
        playerBody.linearVelocity = Vector2.zero;
        Transform room = Find("10_World/R04_ScorpionPackTest");
        var pack = room.GetComponentInChildren<ScorpionPack2D>();
        var members = pack.Members;
        Vector2[] original = members.Select(m => (Vector2)m.transform.position).ToArray();
        await Task.Delay(400);
        bool waiting = members.All(m => !m.HasAggro && m.Velocity.sqrMagnitude < .001f);
        bool eyesOccluded = members.All(m => !m.transform.Find("Visual/Eyes").GetComponent<SpriteRenderer>().enabled);
        Capture(new Vector2(50,-12), 7, Folder + "room-idle.png");
        var door = room.GetComponentInChildren<Door2D>();
        door.Toggle();
        await Task.Delay(600);
        bool packAlert = pack.IsAlerted && members.All(m => m.HasAggro);
        Capture(new Vector2(50,-10), 5.5f, Folder + "pack-emerging.png");
        int alarms = pack.AlarmCount;
        var motion = new List<object>();
        int beforeHealth = player.Health;
        float until = Time.time + 7;
        bool movingAttack = false;
        while (Time.time < until)
        {
            foreach (Scorpion2D member in members)
            {
                if (member.IsAttacking && member.Velocity.magnitude > .1f)
                {
                    movingAttack = true;
                }
            }
            motion.Add(members.Select(m => new { m.name, position = m.transform.position.ToString(), speed = m.Velocity.magnitude, m.IsAttacking, m.AttackProgress, m.GaitPhase }).ToArray());
            await Task.Delay(50);
        }
        Capture(new Vector2(50,-6), 4, Folder + "pack-charge.png");
        bool tookDamage = player.Health < beforeHealth;
        door.Toggle();
        await Task.Delay(400);
        door.Toggle();
        await Task.Delay(400);
        bool oneAlarm = pack.AlarmCount == alarms && alarms == 1;
        var victim = members[0];
        Sprite displayed = victim.transform.Find("Visual/Body").GetComponent<SpriteRenderer>().sprite;
        victim.ReceiveBulletHit(99, victim.transform.position, Vector2.up);
        await Task.Delay(1100);
        var remains = victim.GetComponent<GoreBurst2D>().CaptureRemains();
        Transform carcass = Nodes().First(n => n.name == "Persistent remains — " + victim.name);
        bool posePreserved = carcass.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.sprite == displayed);
        bool deadPhysics = !victim.IsAlive && !victim.GetComponent<Rigidbody2D>().simulated && !victim.GetComponent<CircleCollider2D>().enabled;
        var report = new
        {
            waiting, eyesOccluded, packAlert, oneAlarm, movingAttack, tookDamage,
            positionsMoved = members.Select((m,i) => Vector2.Distance(m.transform.position, original[i])).ToArray(),
            healthBefore = beforeHealth, healthAfter = player.Health,
            death = new { deadPhysics, posePreserved, pool = remains != null && remains.hasPool, parts = remains?.parts?.Length, displayedAtDeath = displayed.name },
            sounds = members.Select(m => {var a=m.GetComponent<ScorpionAudio2D>(); return new {m.name,a.StepsPlayed,a.AlarmsPlayed,a.AttacksPlayed,a.HitsPlayed};}).ToArray(),
            motion,
            scope = "Functional native Editor Play with four authored actors. No stress, FPS, profiling or 10/20-enemy test."
        };
        File.WriteAllText(Folder + "door-play.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return new {waiting,eyesOccluded,packAlert,oneAlarm,movingAttack,tookDamage,deadPhysics,posePreserved, pool = remains != null && remains.hasPool};
    }

    public static async Task<object> VisionScenario()
    {
        var context = Nodes().Select(n => n.GetComponent<LevelContext2D>()).First(c => c != null);
        context.Player.SetHealthCapacity(100,100);
        var pack = Find("10_World/R04_ScorpionPackTest").GetComponentInChildren<ScorpionPack2D>();
        context.Player.GetComponent<Rigidbody2D>().position = new Vector2(47.5f,-9.3f);
        context.Player.transform.position = new Vector3(47.5f,-9.3f,0);
        await Task.Delay(600);
        Capture(new Vector2(49.5f,-12), 5, Folder + "room-visible.png");
        bool alertedByVision = pack.IsAlerted && pack.Members.All(m => m.HasAggro);
        bool doorClosed = !Find("10_World/R04_ScorpionPackTest").GetComponentInChildren<Door2D>().IsOpen;
        var report = new {alertedByVision,doorClosed,alarmCount=pack.AlarmCount, noFearInput = pack.Members.All(m => m.GetComponent<EnemyPerception2D>().ThreatLight == null)};
        File.WriteAllText(Folder + "vision-play.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    public static async Task<object> SuspensionScenario()
    {
        var context = Nodes().Select(n => n.GetComponent<LevelContext2D>()).First(c => c != null);
        var pack = Find("10_World/R04_ScorpionPackTest").GetComponentInChildren<ScorpionPack2D>();
        Scorpion2D actor = pack.Members[0];
        foreach (Scorpion2D member in pack.Members.Skip(1))
        {
            member.enabled = false;
            member.GetComponent<Rigidbody2D>().simulated = false;
            member.GetComponent<CircleCollider2D>().enabled = false;
        }
        var player = context.Player;
        player.SetHealthCapacity(100,100);
        player.GetComponent<Rigidbody2D>().position = (Vector2)actor.transform.position + actor.Facing;
        float until = Time.time + 4;
        while ((!actor.IsAttacking || actor.AttackProgress > .25f) && Time.time < until)
        {
            await Task.Delay(10);
        }
        bool interruptedWindup = actor.IsAttacking && actor.AttackProgress < .4f;
        int health = player.Health;
        actor.enabled = false;
        await Task.Delay(750);
        bool suspendedNoHit = health == player.Health;
        actor.enabled = true;
        await Task.Delay(40);
        bool noExpiredStrike = player.Health == health && (!actor.IsAttacking || actor.AttackProgress < .3f);
        var report = new {interruptedWindup,suspendedNoHit,noExpiredStrike};
        File.WriteAllText(Folder + "suspend-play.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        return report;
    }

    public static async Task<object> LightScenario()
    {
        var context = Nodes().Select(n => n.GetComponent<LevelContext2D>()).First(c => c != null);
        var pack = Find("10_World/R04_ScorpionPackTest").GetComponentInChildren<ScorpionPack2D>();
        var member = pack.Members[0];
        context.Player.SetHealthCapacity(100,100);
        context.Player.GetComponent<Rigidbody2D>().position = new Vector2(47.5f,-8.7f);
        context.Player.transform.position = new Vector3(47.5f,-8.7f,0);
        context.Flashlight.SetPower(true);
        context.Flashlight.AimAt(member.transform.position);
        await Task.Delay(250);
        context.Flashlight.AimAt(member.transform.position);
        await Task.Delay(100);
        Capture(new Vector2(48.7f,-11.7f),4.5f,Folder+"flashlight-room.png");
        bool noRetreat = member.HasAggro && Vector2.Dot(member.Velocity, (Vector2)context.Player.transform.position - (Vector2)member.transform.position) > 0;
        var report = new {lightOn=context.Flashlight.IsOn,noRetreat,eyesVisible=member.transform.Find("Visual/Eyes").GetComponent<SpriteRenderer>().enabled,
            projectionLength=member.GetComponent<CreatureGroundShadow2D>().ProjectionLength,
            contactVisible=member.transform.Find("Ground shadows/Contact shadow").GetComponent<SpriteRenderer>().enabled};
        File.WriteAllText(Folder+"light-play.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        return report;
    }

    private static void Capture(Vector2 center, float size, string destination)
    {
        Camera camera = Nodes().Select(n => n.GetComponent<Camera>()).First(c => c != null);
        Vector3 oldPosition = camera.transform.position;
        float oldSize = camera.orthographicSize;
        RenderTexture oldTarget = camera.targetTexture;
        RenderTexture oldActive = RenderTexture.active;
        var target = new RenderTexture(1280,900,24);
        var pixels = new Texture2D(1280,900,TextureFormat.RGBA32,false);
        try
        {
            camera.transform.position = new Vector3(center.x,center.y,oldPosition.z);
            camera.orthographicSize = size;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0,0,1280,900),0,0);
            pixels.Apply();
            File.WriteAllBytes(destination,pixels.EncodeToPNG());
        }
        finally
        {
            camera.transform.position = oldPosition;
            camera.orthographicSize = oldSize;
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}
