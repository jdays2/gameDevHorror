using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmergencyShift.Editor
{
    /// <summary>One-time authoring tool. The resulting scene is ordinary editable geometry, not a runtime generator.</summary>
    public static class FirstLevelBuilder
    {
        private const string Root = "Assets/EmergencyShift";
        public const string ScenePath = Root + "/Scenes/FirstShift.unity";
        private static Transform geometry, props, loot, actors, lamps;
        private static readonly List<ShadowStalker2D> enemies = new List<ShadowStalker2D>();
        private static Font font;
        private static Material lit, unlit;
        private static Sprite square;
        private static Scene scene;

        [MenuItem("Emergency Shift/Levels/Create First Shift (once)")]
        public static void Create()
        {
            if (Application.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stop Play/compilation first.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("FirstShift already exists. Edit its objects; this tool never replaces a level.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save existing scene edits first.");
            var previous = SceneManager.GetActiveScene();
            lit = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/FlatLit.mat");
            unlit = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/FlatUnlit.mat");
            square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Shared/Shapes/Rectangle.asset");
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                geometry = Group("01 Geometry — rooms and opaque walls");
                props = Group("02 Cover — arranged routes and hiding places");
                loot = Group("03 Loot — cards weapon ammo modules");
                actors = Group("04 Stalkers — waits patrols groups");
                lamps = Group("05 Practical lights — deliberately sparse");
                var rig = Instance("LevelRig", "Level rig", null, new Vector2(4, 6));
                var player = rig.GetComponentInChildren<PlayerLife2D>();
                // Camera is positioned at the spawn as well; it must not fly in from the template origin.
                rig.GetComponentInChildren<Camera>().transform.position = new Vector3(4, 6, -10);
                Set(player, "maximumHealth", 3);
                var cards = player.gameObject.AddComponent<PlayerAccessCards2D>();
                var hud = rig.GetComponentInChildren<PrototypeHud>();
                font = hud.GetComponentInChildren<Text>(true).font;
                var objective = Label(hud.transform, "Current objective", new Vector2(24, -48), new Vector2(760, 34), 18);
                objective.color = new Color(.69f, .75f, .72f);
                Label(hud.transform, "Sector title", new Vector2(24, -80), new Vector2(780, 26), 13).text = "СЕКТОР 01 · СЛУЖЕБНЫЙ ДОСТУП → ЭВАКУАЦИЯ";
                var flow = new GameObject("06 Level flow — objective and terminal exit").AddComponent<DemoLevelFlow2D>();
                var panel = new GameObject("Completion panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panel.transform.SetParent(hud.transform, false);
                var rect = (RectTransform)panel.transform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                panel.GetComponent<Image>().color = new Color(.015f, .022f, .023f, .94f);
                var result = Label(panel.transform, "Completion text", Vector2.zero, new Vector2(850, 400), 28);
                var resultRect = result.rectTransform; resultRect.anchorMin = resultRect.anchorMax = new Vector2(.5f,.5f);
                resultRect.pivot = new Vector2(.5f,.5f); result.alignment = TextAnchor.MiddleCenter;
                panel.SetActive(false);
                BuildRooms();
                BuildProps();
                BuildLoot();
                BuildEnemies();
                BuildLights();
                flow.Configure(player, cards, objective, panel, result, enemies.ToArray());
                var exit = new GameObject("Exit — cross the green threshold", typeof(BoxCollider2D), typeof(LevelExit2D));
                exit.transform.position = new Vector3(68,6,0); exit.GetComponent<BoxCollider2D>().size = new Vector2(2.5f,5);
                exit.GetComponent<LevelExit2D>().Configure(flow);
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var c in root.GetComponentsInChildren<Component>(true))
                        if (c != null && PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                EditorSceneManager.SaveScene(scene, ScenePath);
                var builds = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath,true) };
                foreach (var entry in EditorBuildSettings.scenes) if (entry.path != ScenePath) builds.Add(entry);
                EditorBuildSettings.scenes = builds.ToArray();
                AssetDatabase.SaveAssets();
                // Only close clean source scenes after the new authored scene has safely been saved.
                for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
                { var old = SceneManager.GetSceneAt(i); if (old != scene) EditorSceneManager.CloseScene(old,true); }
                Selection.activeGameObject = player.gameObject;
            }
            catch
            {
                SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true); throw;
            }
        }

        private static Transform Group(string name) => new GameObject(name).transform;
        private static string PrefabPath(string name)
        {
            switch(name)
            {
                case "AccessCardPickup": return "Assets/EmergencyShift/Prefabs/Loot/AccessCardPickup.prefab";
                case "AmmoPickup": return "Assets/EmergencyShift/Prefabs/Loot/AmmoPickup.prefab";
                case "BloodPool": return "Assets/EmergencyShift/Prefabs/Enemies/BloodPool.prefab";
                case "CoverCabinet": return "Assets/EmergencyShift/Prefabs/World/CoverCabinet.prefab";
                case "CoverCrate": return "Assets/EmergencyShift/Prefabs/World/CoverCrate.prefab";
                case "Door": return "Assets/EmergencyShift/Prefabs/World/Door.prefab";
                case "EmergencyLamp": return "Assets/EmergencyShift/Prefabs/World/EmergencyLamp.prefab";
                case "EncounterZone": return "Assets/EmergencyShift/Prefabs/Levels/EncounterZone.prefab";
                case "FlashlightRangeUpgrade": return "Assets/EmergencyShift/Prefabs/Loot/FlashlightRangeUpgrade.prefab";
                case "LevelAudio": return "Assets/EmergencyShift/Prefabs/Levels/LevelAudio.prefab";
                case "LevelRig": return "Assets/EmergencyShift/Prefabs/Levels/LevelRig.prefab";
                case "LowBench": return "Assets/EmergencyShift/Prefabs/World/LowBench.prefab";
                case "PistolBullet": return "Assets/EmergencyShift/Prefabs/Combat/PistolBullet.prefab";
                case "PistolCasing": return "Assets/EmergencyShift/Prefabs/Combat/PistolCasing.prefab";
                case "PistolPickup": return "Assets/EmergencyShift/Prefabs/Loot/PistolPickup.prefab";
                case "PistolShotEffect": return "Assets/EmergencyShift/Prefabs/Combat/PistolShotEffect.prefab";
                case "Player": return "Assets/EmergencyShift/Prefabs/Player/Player.prefab";
                case "ShadowStalker": return "Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab";
                case "StalkerRemainsTest": return "Assets/EmergencyShift/Prefabs/Enemies/StalkerRemainsTest.prefab";
                case "Wall": return "Assets/EmergencyShift/Prefabs/World/Wall.prefab";
                default: throw new ArgumentException("Unknown prefab "+name);
            }
        }
        private static GameObject Instance(string prefab, string name, Transform parent, Vector2 position)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(prefab));
            if (asset == null) throw new InvalidOperationException("Missing prefab " + prefab);
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(asset,scene);
            obj.name = name; if (parent != null) obj.transform.SetParent(parent,false);
            obj.transform.position = new Vector3(position.x, position.y, 0); return obj;
        }
        private static void Set(UnityEngine.Object target, string field, object value)
        {
            var data = new SerializedObject(target); var p = data.FindProperty(field);
            if (p == null) throw new InvalidOperationException(target.name + "." + field);
            if (value is float f) p.floatValue=f; else if (value is int n) p.intValue=n;
            else if (value is bool b) p.boolValue=b; else if (value is Vector2 v) p.vector2Value=v;
            else if (value is string s) p.stringValue=s; else p.objectReferenceValue=(UnityEngine.Object)value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static SpriteRenderer Shape(string name, Transform parent, Vector2 at, Vector2 size, Color tint, bool emissive=false, int order=-10)
        {
            var obj = new GameObject(name,typeof(SpriteRenderer)); obj.transform.SetParent(parent,false);
            obj.transform.position = at; obj.transform.localScale = new Vector3(size.x,size.y,1);
            var r = obj.GetComponent<SpriteRenderer>(); r.sprite=square; r.sharedMaterial=emissive?unlit:lit; r.color=tint; r.sortingOrder=order; return r;
        }
        private static Text Label(Transform parent,string name,Vector2 at,Vector2 size,int fontSize)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));obj.transform.SetParent(parent,false);
            var text=obj.GetComponent<Text>();text.font=font;text.fontSize=fontSize;text.color=new Color(.72f,.78f,.76f);
            text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;
            var r=text.rectTransform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=at;r.sizeDelta=size;return text;
        }
        private static void Sign(string text,Vector2 at,Color color)
        {
            // UGUI tracks dynamic-font atlas rebuilds; legacy TextMesh can leave the shared HUD with stale glyph UVs.
            var obj = new GameObject("Sign — "+text,typeof(RectTransform),typeof(Canvas));obj.transform.SetParent(geometry,false);
            obj.transform.position=at;obj.transform.localScale=Vector3.one*.006f;
            var canvas=obj.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=4;
            ((RectTransform)obj.transform).sizeDelta=new Vector2(900,100);
            var label=Label(obj.transform,"Label",Vector2.zero,new Vector2(900,100),48);
            label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(.5f,.5f);label.rectTransform.pivot=new Vector2(.5f,.5f);
            label.alignment=TextAnchor.MiddleCenter;label.text=text;label.color=color;
        }
        private static string Edge(int x,int y,bool horizontal) => x+":"+y+":"+horizontal;
        private static void BuildRooms()
        {
            var edges = new Dictionary<string,Vector3>();
            for (int x=0;x<6;x++) Cell(x,0,edges);
            for (int x=0;x<4;x++) Cell(x,1,edges);
            var doors = new Dictionary<string,string> {
                {Edge(0,12,true),"01 Service room"}, {Edge(12,0,false),"02 Distribution"},
                {Edge(12,12,true),"03 Locked module store"}, {Edge(24,0,false),"04 Transfer room"},
                {Edge(24,12,true),"05 Control room"}, {Edge(36,0,false),"06 Evacuation gate"},
                {Edge(36,12,true),"07 Locked reserve"}, {Edge(60,0,false),"08 Exit vestibule"} };
            foreach (var pair in edges)
            {
                if (pair.Key==Edge(48,0,false)) continue; // Two cells form one large hall.
                var e=pair.Value;bool horizontal=e.z>0;Vector2 start=new Vector2(e.x,e.y);Vector2 axis=horizontal?Vector2.right:Vector2.up;
                if (!doors.TryGetValue(pair.Key,out string label)) Wall("Opaque boundary",start+axis*6,horizontal?new Vector2(12.36f,.36f):new Vector2(.36f,12.36f));
                else
                {
                    Wall(label+" — frame A",start+axis*2.25f,horizontal?new Vector2(4.86f,.36f):new Vector2(.36f,4.86f));
                    Wall(label+" — frame B",start+axis*9.75f,horizontal?new Vector2(4.86f,.36f):new Vector2(.36f,4.86f));
                    var door=Instance("Door",label,geometry,start+axis*6);
                    if(horizontal)door.transform.rotation=Quaternion.Euler(0,0,90);
                    if(label.StartsWith("03"))door.AddComponent<DoorAccess2D>().Configure("service","Служебная карта");
                    if(label.StartsWith("06")||label.StartsWith("07"))door.AddComponent<DoorAccess2D>().Configure("evacuation","Эвакуационная карта");
                }
            }
            Sign("ВХОД",new Vector2(3,8.5f),new Color(.26f,.33f,.31f));
            Sign("СЛУЖЕБНАЯ ↑",new Vector2(6,10.7f),new Color(.28f,.3f,.29f));
            Sign("МОДУЛЬ · ДОСТУП",new Vector2(18,13),new Color(.22f,.32f,.35f));
            Sign("УПРАВЛЕНИЕ ↑",new Vector2(30,10.7f),new Color(.27f,.3f,.29f));
            Sign("ЭВАКУАЦИЯ →",new Vector2(34,7.6f),new Color(.25f,.34f,.3f));
            Sign("РЕЗЕРВ · ДОСТУП",new Vector2(42,13),new Color(.22f,.32f,.35f));
            Sign("ВЫХОД →",new Vector2(65,8),new Color(.25f,.42f,.31f));
            Shape("Exit threshold",geometry,new Vector2(68,6),new Vector2(.18f,5),new Color(.12f,.28f,.2f),true,-8);
        }
        private static void Cell(int x,int y,Dictionary<string,Vector3> edges)
        {
            Shape("Floor "+x+","+y,geometry,new Vector2(x*12+6,y*12+6),new Vector2(12,12),new Color(.19f,.205f,.21f));
            edges[Edge(x*12,y*12,true)]=new Vector3(x*12,y*12,1);
            edges[Edge(x*12,y*12+12,true)]=new Vector3(x*12,y*12+12,1);
            edges[Edge(x*12,y*12,false)]=new Vector3(x*12,y*12,0);
            edges[Edge(x*12+12,y*12,false)]=new Vector3(x*12+12,y*12,0);
        }
        private static void Wall(string name,Vector2 at,Vector2 size)
        {
            var obj=Instance("Wall",name,geometry,at);obj.transform.localScale=new Vector3(size.x,size.y,1);
            var r=obj.GetComponent<SpriteRenderer>();r.sharedMaterial=unlit;r.color=new Color(.055f,.063f,.068f);
        }
        private static void Prop(string type,string name,Vector2 at,Vector2 size)
        {var obj=Instance(type,name,props,at);obj.transform.localScale=new Vector3(size.x,size.y,1);}
        private static void BuildProps()
        {
            Prop("LowBench","Entry bench",new Vector2(4,3),new Vector2(3,1));
            Prop("CoverCabinet","Entry cabinet",new Vector2(9.5f,8),new Vector2(1.3f,2.6f));
            Prop("CoverCrate","First blind corner",new Vector2(5.5f,18),new Vector2(2.2f,1.6f));
            Prop("CoverCabinet","Service aisle",new Vector2(8.8f,19),new Vector2(1.3f,4));
            Prop("LowBench","Distribution low cover",new Vector2(19,5),new Vector2(3,1));
            Prop("CoverCrate","Distribution crate",new Vector2(22,2.5f),new Vector2(1.5f,1.5f));
            Prop("CoverCabinet","Module racks west",new Vector2(14.5f,17.5f),new Vector2(1.2f,3.5f));
            Prop("CoverCabinet","Module racks east",new Vector2(21,17.5f),new Vector2(1.2f,3.5f));
            Prop("CoverCabinet","Transfer blind corner",new Vector2(29.5f,8.5f),new Vector2(1.4f,3.2f));
            Prop("CoverCrate","Transfer southern cover",new Vector2(31,3),new Vector2(1.5f,1.5f));
            Prop("CoverCabinet","Control central partition",new Vector2(30,18.2f),new Vector2(1.4f,4.2f));
            Prop("CoverCrate","Control waiting corner",new Vector2(33,16),new Vector2(1.8f,1.6f));
            Prop("CoverCabinet","Hall western barrier",new Vector2(42,3),new Vector2(3,1.2f));
            Prop("CoverCrate","Hall stack A",new Vector2(46,9),new Vector2(1.7f,1.5f));
            Prop("CoverCrate","Hall stack B",new Vector2(48,9),new Vector2(1.7f,1.5f));
            Prop("CoverCabinet","Hall second blind corner",new Vector2(51,4),new Vector2(1.4f,4));
            Prop("LowBench","Hall low bench",new Vector2(55,8),new Vector2(3,1.2f));
            Prop("CoverCabinet","Reserve island",new Vector2(42,17),new Vector2(1.4f,3.2f));
            Prop("LowBench","Exit bench",new Vector2(64,3),new Vector2(3,1));
            FloorSound(new Vector2(6,18),new Vector2(11,11),GroundSoundSurface.Tile);
            FloorSound(new Vector2(18,18),new Vector2(11,11),GroundSoundSurface.Wood);
        }
        private static void FloorSound(Vector2 at,Vector2 size,GroundSoundSurface material)
        {
            var obj=new GameObject("Floor sound — "+material,typeof(BoxCollider2D),typeof(GroundAudioSurface2D));obj.transform.SetParent(geometry,false);obj.transform.position=at;
            obj.GetComponent<BoxCollider2D>().isTrigger=true;obj.GetComponent<BoxCollider2D>().size=size;Set(obj.GetComponent<GroundAudioSurface2D>(),"surface",(int)material);
        }
        private static void BuildLoot()
        {
            CreateCardPrefab();
            Instance("AccessCardPickup","Service card",loot,new Vector2(3,21)).GetComponent<AccessCardPickup2D>().Configure("service","Служебная карта");
            var card=Instance("AccessCardPickup","Evacuation card",loot,new Vector2(33,21));card.GetComponent<AccessCardPickup2D>().Configure("evacuation","Эвакуационная карта");
            foreach(var r in card.GetComponentsInChildren<SpriteRenderer>())r.color=new Color(.55f,.78f,.58f);
            Instance("PistolPickup","Pistol — before group encounters",loot,new Vector2(15.5f,4));
            Ammo("First ammunition",new Vector2(16.5f,4),12);Ammo("Module store ammunition",new Vector2(20,20),8);
            Ammo("Transfer ammunition",new Vector2(26.5f,9),8);Ammo("Control ammunition",new Vector2(27,15),12);
            Ammo("Hall ammunition",new Vector2(54,10),12);Ammo("Reserve ammunition",new Vector2(42,21),12);
            Instance("FlashlightRangeUpgrade","Module 1 — service lock reward",loot,new Vector2(16,20));
            Instance("FlashlightRangeUpgrade","Module 2 — reserve west",loot,new Vector2(39,21));
            Instance("FlashlightRangeUpgrade","Module 3 — reserve east",loot,new Vector2(45,21));
        }
        private static void Ammo(string label,Vector2 at,int amount)
        {Instance("AmmoPickup",label,loot,at).GetComponent<WeaponLootPickup>().Configure(WeaponLootPickup.LootKind.Ammo,amount);}
        private static void CreateCardPrefab()
        {
            string path=Root+"/Prefabs/Loot/AccessCardPickup.prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)return;
            var obj=new GameObject("AccessCardPickup",typeof(CircleCollider2D),typeof(AccessCardPickup2D));
            obj.GetComponent<CircleCollider2D>().isTrigger=true;obj.GetComponent<CircleCollider2D>().radius=.28f;
            Shape("Card body",obj.transform,Vector2.zero,new Vector2(.5f,.32f),new Color(.62f,.7f,.75f),false,3);
            Shape("Magnetic strip",obj.transform,new Vector2(0,.055f),new Vector2(.45f,.055f),new Color(.13f,.22f,.25f),false,4);
            PrefabUtility.SaveAsPrefabAsset(obj,path);UnityEngine.Object.DestroyImmediate(obj);
        }
        private static ShadowStalker2D Enemy(string name,Vector2 at,bool patrol,Vector2 patrolSize,float detection=3.2f)
        {
            var obj=Instance("ShadowStalker",name,actors,at);var ai=obj.GetComponent<ShadowStalker2D>();Set(ai,"habitatRadius",detection);
            var route=obj.GetComponent<EnemyPatrol2D>();Set(route,"patrolEnabled",patrol);Set(route,"usePatrolArea",true);Set(route,"areaSize",patrolSize);Set(route,"profile",AssetDatabase.LoadAssetAtPath<EnemyPatrolProfile>(Root+"/Data/Enemies/Patrol-FirstShift.asset"));
            enemies.Add(ai);return ai;
        }
        private static void Zone(string name,Vector2 center,Vector2 size,params ShadowStalker2D[] group)
        {
            var obj=Instance("EncounterZone",name,actors,center);obj.GetComponent<BoxCollider2D>().size=size;
            var targets=new GameObject[group.Length];for(int i=0;i<group.Length;i++){targets[i]=group[i].gameObject;targets[i].SetActive(false);}
            obj.GetComponent<EncounterActivation2D>().Configure(targets);
        }
        private static void BuildEnemies()
        {
            enemies.Clear();
            Enemy("01 Wait — service card corner",new Vector2(5.5f,20.5f),false,new Vector2(3,3),2.9f);
            Enemy("02 Patrol — distribution",new Vector2(21,8.5f),true,new Vector2(3,3));
            Zone("Encounter 1 — transfer pair",new Vector2(30,6),new Vector2(10,10),
                Enemy("03 Wait — behind transfer cabinet",new Vector2(32,9),false,new Vector2(3,3)),
                Enemy("04 Patrol — transfer south",new Vector2(33,3.5f),true,new Vector2(3,3)));
            Zone("Encounter 2 — control trio",new Vector2(30,18),new Vector2(10,10),
                Enemy("05 Wait — control island",new Vector2(30,21.5f),false,new Vector2(3,3)),
                Enemy("06 Patrol — control west",new Vector2(27,20),true,new Vector2(3,4)),
                Enemy("07 Wait — control east",new Vector2(33,18.5f),false,new Vector2(3,3)));
            Zone("Encounter 3 — hall group",new Vector2(48,6),new Vector2(22,10),
                Enemy("08 Patrol — hall approach",new Vector2(43,5.5f),true,new Vector2(3,3),3.5f),
                Enemy("09 Wait — hall stack",new Vector2(44,8),false,new Vector2(3,3),3.5f),
                Enemy("10 Patrol — hall center",new Vector2(46.5f,4.5f),true,new Vector2(3,3),3.5f),
                Enemy("11 Wait — hall center north",new Vector2(48.5f,7),false,new Vector2(3,3),3.5f),
                Enemy("12 Wait — second blind corner",new Vector2(53,4),false,new Vector2(3,3),3.5f),
                Enemy("13 Wait — hall southern flank",new Vector2(46,2),false,new Vector2(3,3),3.5f),
                Enemy("14 Patrol — hall exit approach",new Vector2(55,4),true,new Vector2(3,3),3.5f),
                Enemy("15 Wait — hall exit corner",new Vector2(57,9.5f),false,new Vector2(3,3),3.5f));
            Zone("Encounter 4 — optional reserve",new Vector2(42,18),new Vector2(10,10),
                Enemy("16 Wait — reserve west",new Vector2(39.5f,18.8f),false,new Vector2(3,3)),
                Enemy("17 Patrol — reserve east",new Vector2(45,19),true,new Vector2(3,3)),
                Enemy("18 Wait — reserve reward corner",new Vector2(38.2f,21.5f),false,new Vector2(3,3)),
                Enemy("19 Patrol — reserve door flank",new Vector2(46,15.8f),true,new Vector2(2,2)));
        }
        private static void BuildLights()
        {
            PlaceLamp("Entry practical",new Vector2(2,9),new Color(.68f,.72f,.65f),.55f,2.5f);
            PlaceLamp("Service unstable",new Vector2(10.5f,21),new Color(.67f,.53f,.34f),.28f,1.7f,true);
            PlaceLamp("Distribution lamp",new Vector2(14,9.5f),new Color(.55f,.65f,.69f),.35f,1.9f);
            PlaceLamp("Module lamp",new Vector2(18,22.5f),new Color(.48f,.65f,.7f),.32f,1.8f);
            PlaceLamp("Transfer unstable",new Vector2(34,10.5f),new Color(.7f,.46f,.29f),.3f,2,true);
            PlaceLamp("Control green",new Vector2(26,22.5f),new Color(.42f,.65f,.53f),.36f,2);
            PlaceLamp("Evacuation gate indicator",new Vector2(36.6f,8.5f),new Color(.6f,.36f,.24f),.28f,1.4f);
            PlaceLamp("Hall unstable",new Vector2(47,1),new Color(.7f,.49f,.3f),.36f,2.2f,true);
            PlaceLamp("Reserve practical",new Vector2(42,22.5f),new Color(.48f,.65f,.7f),.3f,1.8f);
            PlaceLamp("Exit corridor",new Vector2(59,9.5f),new Color(.43f,.7f,.52f),.45f,2.2f);
            PlaceLamp("Exit beacon",new Vector2(68,10),new Color(.43f,.7f,.52f),.6f,2.6f);
        }
        private static void PlaceLamp(string name,Vector2 at,Color tint,float intensity,float radius,bool flicker=false)
        {
            var obj=Instance("EmergencyLamp",name,lamps,at);var lamp=obj.GetComponent<LocalLamp2D>();
            var data=new SerializedObject(lamp);data.FindProperty("color").colorValue=tint;data.FindProperty("intensity").floatValue=intensity;
            data.FindProperty("radius").floatValue=radius;data.FindProperty("flickers").boolValue=flicker;data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
