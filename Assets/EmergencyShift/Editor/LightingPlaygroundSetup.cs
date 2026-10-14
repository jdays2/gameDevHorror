using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>Сохраняет небольшой стенд укрытий, ламп и границ; не перестраивает старый уровень.</summary>
    public static class LightingPlaygroundSetup
    {
        private const string Root="Assets/EmergencyShift";
        public const string GroupName="Occlusion playground";
        [MenuItem("Emergency Shift/Set up lighting playground")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Остановите Play.");
            var scene=SceneManager.GetActiveScene();if(scene.path!=PrototypeSceneBuilder.ScenePath)throw new InvalidOperationException("Откройте BasementPrototype.");
            EnsureLayer();EnsureProp("CoverCabinet",false,false);EnsureProp("CoverCrate",true,false);EnsureProp("LowBench",false,true);EnsureLamp();
            foreach(string prop in new[]{"CoverCabinet","CoverCrate"})EditPrefab(prop,obj=>
            {
                var caster=obj.GetComponent<ShadowCaster2D>();
                if(caster.castingOption==ShadowCaster2D.ShadowCastingOptions.CastAndSelfShadow)
                    caster.castingOption=ShadowCaster2D.ShadowCastingOptions.CastShadow;
            });
            var group=GameObject.Find(GroupName)??new GameObject(GroupName);
            var props=Child(group.transform,"Props");var lights=Child(group.transform,"Local lights");var encounter=Child(group.transform,"Cover encounter");
            Place(props,"Cabinet — Room A cover","CoverCabinet",new Vector2(-3,-2.15f));
            Place(props,"Crate — near ammo","CoverCrate",new Vector2(-.9f,1.1f));
            Place(props,"Low bench — light passes","LowBench",new Vector2(-.6f,-.65f));
            Place(props,"Cabinet — Room B cover","CoverCabinet",new Vector2(13.1f,1.15f));
            var warm=Place(lights,"Warm lamp — pistol corner","EmergencyLamp",new Vector2(-5.7f,2.7f));
            var cold=Place(lights,"Flickering lamp — Room B","EmergencyLamp",new Vector2(15.8f,-2.5f));
            if(warm.created)ConfigureLamp(warm.obj,new Color(.70f,.53f,.31f),false);
            if(cold.created)ConfigureLamp(cold.obj,new Color(.35f,.52f,.62f),true);
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerWeapon2D>();var lamp=player.GetComponent<Flashlight2D>();
            var enemy=Place(encounter,"Cover Stalker — Room B","ShadowStalker",new Vector2(14.3f,1.1f));
            if(enemy.created)
            {
                var actor=enemy.obj.GetComponent<ShadowStalker2D>();actor.Configure(player.transform,lamp,1<<8);
                var so=new SerializedObject(actor);so.FindProperty("habitatRadius").floatValue=1.8f;so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
            }
            Place(encounter,"Ammo — behind Room B cabinet","AmmoPickup",new Vector2(14.3f,1.9f));
            EditPrefab("Player",IncludeLowSolids);
            IncludeLowSolids(player.gameObject);
            AddLamp(lights,"Wall lamp — Room A",new Vector2(.8f,3.78f),new Color(.70f,.48f,.26f),1.5f,.32f,false);
            AddLamp(lights,"Door frame lamp",new Vector2(3.78f,1.28f),new Color(.40f,.58f,.45f),1.3f,.32f,false);
            AddLamp(lights,"Corridor emergency lamp",new Vector2(7.8f,1.28f),new Color(.58f,.24f,.18f),1.15f,.18f,true);
            AddLamp(lights,"East wall lamp — Room B",new Vector2(16.78f,.1f),new Color(.48f,.63f,.72f),2.2f,.8f,false);
            ConfigureGuides();
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light2D>())if(light.lightType==Light2D.LightType.Global)
            {light.intensity=.002f;light.name="Darkness — ambient 0.002";EditorUtility.SetDirty(light);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        private static void EnsureLayer()
        {
            var asset=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];var so=new SerializedObject(asset);var layer=so.FindProperty("layers").GetArrayElementAtIndex(9);
            if(!string.IsNullOrEmpty(layer.stringValue)&&layer.stringValue!="LowObstacles")throw new InvalidOperationException("Слой 9 занят: "+layer.stringValue);
            layer.stringValue="LowObstacles";so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static Transform Child(Transform parent,string name)
        {var t=parent.Find(name);if(t!=null)return t;t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        private static (GameObject obj,bool created) Place(Transform parent,string name,string prefab,Vector2 point)
        {
            var existing=parent.Find(name);if(existing!=null)return(existing.gameObject,false);
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+prefab+".prefab"),parent);
            obj.name=name;obj.transform.position=new Vector3(point.x,point.y,obj.transform.position.z);return(obj,true);
        }
        private static void EditPrefab(string name,Action<GameObject> edit)
        {
            string path=Root+"/Prefabs/"+name+".prefab";var obj=PrefabUtility.LoadPrefabContents(path);
            try{edit(obj);PrefabUtility.SaveAsPrefabAsset(obj,path);}finally{PrefabUtility.UnloadPrefabContents(obj);}
        }
        private static SpriteRenderer Part(Transform root,string name,Vector2 position,Vector2 scale,Color color,int order=2)
        {
            var obj=new GameObject(name,typeof(SpriteRenderer));obj.transform.SetParent(root,false);
            obj.transform.localPosition=new Vector3(position.x,position.y,-.005f);obj.transform.localScale=new Vector3(scale.x,scale.y,1);
            var r=obj.GetComponent<SpriteRenderer>();r.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Shapes/Rectangle.asset");
            r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/FlatLit.mat");r.color=color;r.sortingOrder=order;return r;
        }
        private static void EnsureProp(string name,bool crate,bool low)
        {
            string path=Root+"/Prefabs/"+name+".prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)return;
            var obj=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/Wall.prefab");
            try
            {
                obj.name=name;obj.transform.localScale=low?new Vector3(1.65f,.55f,1):crate?new Vector3(.9f,.9f,1):new Vector3(.65f,1.55f,1);
                obj.layer=low?9:8;
                if(low)obj.transform.localPosition=new Vector3(0,0,-.045f);
                var r=obj.GetComponent<SpriteRenderer>();r.color=low?new Color(.32f,.29f,.24f):new Color(.29f,.34f,.36f);r.sortingOrder=low?0:2;
                if(low)UnityEngine.Object.DestroyImmediate(obj.GetComponent<ShadowCaster2D>());
                Color trim=new Color(.42f,.44f,.43f);int order=low?0:2;
                if(crate)
                {
                    Part(obj.transform,"Top rail",new Vector2(0,.38f),new Vector2(.85f,.07f),trim);
                    Part(obj.transform,"Bottom rail",new Vector2(0,-.38f),new Vector2(.85f,.07f),trim);
                    Part(obj.transform,"Left rail",new Vector2(-.38f,0),new Vector2(.07f,.85f),trim);
                    Part(obj.transform,"Right rail",new Vector2(.38f,0),new Vector2(.07f,.85f),trim);
                }
                else
                {
                    Part(obj.transform,"Inset",Vector2.zero,new Vector2(.78f,.82f),new Color(.23f,.27f,.29f),order);
                    Part(obj.transform,"Seam",Vector2.zero,new Vector2(.035f,.8f),trim,order);
                }
                PrefabUtility.SaveAsPrefabAsset(obj,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(obj);}
        }
        private static void EnsureLamp()
        {
            string path=Root+"/Prefabs/EmergencyLamp.prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)return;
            var obj=new GameObject("EmergencyLamp",typeof(Light2D),typeof(LocalLamp2D));
            try
            {
                Part(obj.transform,"Mount",Vector2.zero,new Vector2(.30f,.19f),new Color(.31f,.34f,.35f));
                var bulb=Part(obj.transform,"Bulb",Vector2.zero,new Vector2(.15f,.065f),Color.white);
                bulb.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/FlatUnlit.mat");
                var light=obj.GetComponent<Light2D>();light.shadowIntensity=1;light.shadowVolumeIntensity=1;
                var so=new SerializedObject(light);var layers=so.FindProperty("m_ApplyToSortingLayers");layers.arraySize=1;layers.GetArrayElementAtIndex(0).intValue=0;so.ApplyModifiedPropertiesWithoutUndo();
                obj.GetComponent<LocalLamp2D>().Configure(light,bulb,new Color(.70f,.53f,.31f),false);
                PrefabUtility.SaveAsPrefabAsset(obj,path);
            }
            finally{UnityEngine.Object.DestroyImmediate(obj);}
        }
        private static void ConfigureLamp(GameObject obj,Color color,bool flicker)
        {
            var c=obj.GetComponent<LocalLamp2D>();c.Configure(obj.GetComponent<Light2D>(),obj.transform.Find("Bulb").GetComponent<SpriteRenderer>(),color,flicker);
            PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        }
        private static void IncludeLowSolids(GameObject obj)
        {
            var weapon=obj.GetComponent<PlayerWeapon2D>();var so=new SerializedObject(weapon);var mask=so.FindProperty("bulletCollisionMask");mask.intValue|=1<<9;so.ApplyModifiedPropertiesWithoutUndo();
            if(PrefabUtility.IsPartOfPrefabInstance(weapon))PrefabUtility.RecordPrefabInstancePropertyModifications(weapon);
        }
        private static void AddLamp(Transform parent,string name,Vector2 point,Color color,float radius,float intensity,bool flicker)
        {
            var placed=Place(parent,name,"EmergencyLamp",point);if(!placed.created)return;
            ConfigureLamp(placed.obj,color,flicker);var c=placed.obj.GetComponent<LocalLamp2D>();
            var so=new SerializedObject(c);so.FindProperty("radius").floatValue=radius;so.FindProperty("intensity").floatValue=intensity;
            so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        }
        private static void ConfigureGuides()
        {
            var walls=GameObject.Find("Level — editable geometry").transform.Find("Walls");
            var sources=new List<SpriteRenderer>();
            foreach(var r in walls.GetComponentsInChildren<SpriteRenderer>(true))if(r.name!="Boundary guide")sources.Add(r);
            foreach(var door in UnityEngine.Object.FindObjectsByType<Door2D>())
            {var leaf=door.transform.Find("Leaf");if(leaf!=null)sources.Add(leaf.GetComponent<SpriteRenderer>());}
            foreach(var source in sources)
            {
                if(source==null)continue;var existing=source.transform.Find("Boundary guide");
                var obj=existing!=null?existing.gameObject:new GameObject("Boundary guide",typeof(SpriteRenderer));
                obj.transform.SetParent(source.transform,false);obj.transform.localPosition=new Vector3(0,0,-.001f);obj.transform.localScale=Vector3.one;
                var guide=obj.GetComponent<SpriteRenderer>();guide.sprite=source.sprite;
                guide.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/TransientGlow.mat");
                guide.color=new Color(.12f,.14f,.17f,1);guide.sortingLayerID=source.sortingLayerID;guide.sortingOrder=source.sortingOrder;
            }
        }
    }
}
