using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace EmergencyShift.Editor
{
    /// <summary>Creates reusable scene-authored scaffolding once; never replaces the user's playable level.</summary>
    public static class LevelAuthoringSetup
    {
        private const string Root="Assets/EmergencyShift";
        public const string RigPath=Root+"/Prefabs/LevelRig.prefab";
        public const string TemplatePath=Root+"/Scenes/LevelTemplate.unity";
        [MenuItem("Emergency Shift/Preparation/Create level rig and template")]
        public static void Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
            var source=SceneManager.GetActiveScene();
            if(source.path!=PrototypeSceneBuilder.ScenePath)throw new InvalidOperationException("Open BasementPrototype as the source rig.");
            if(AssetDatabase.LoadAssetAtPath<GameObject>(RigPath)==null)CreateRig();
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(TemplatePath)==null)
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(scene);PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RigPath),scene);
                    var geometry=new GameObject("Level geometry — edit in Scene");
                    var floor=new GameObject("Floor",typeof(SpriteRenderer));floor.transform.SetParent(geometry.transform,false);floor.transform.localScale=new Vector3(18,12,1);floor.transform.position=new Vector3(0,0,.1f);
                    var image=floor.GetComponent<SpriteRenderer>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Shapes/Rectangle.asset");image.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/FlatLit.mat");image.color=new Color(.18f,.20f,.21f);image.sortingOrder=-10;
                    Wall(geometry.transform,"North",new Vector2(0,6),new Vector2(18.3f,.3f));Wall(geometry.transform,"South",new Vector2(0,-6),new Vector2(18.3f,.3f));
                    Wall(geometry.transform,"West",new Vector2(-9,0),new Vector2(.3f,12));Wall(geometry.transform,"East",new Vector2(9,0),new Vector2(.3f,12));
                    EditorSceneManager.SaveScene(scene,TemplatePath);
                }
                finally{SceneManager.SetActiveScene(source);EditorSceneManager.CloseScene(scene,true);}
            }
            const string zonePath=Root+"/Prefabs/EncounterZone.prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(zonePath)==null)
            {
                var zone=new GameObject("Encounter zone",typeof(BoxCollider2D),typeof(EncounterActivation2D));
                try{var box=zone.GetComponent<BoxCollider2D>();box.isTrigger=true;box.size=new Vector2(4,4);PrefabUtility.SaveAsPrefabAsset(zone,zonePath);}
                finally{UnityEngine.Object.DestroyImmediate(zone);}
            }
            var builds=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if(!builds.Exists(s=>s.path==TemplatePath)){builds.Add(new EditorBuildSettingsScene(TemplatePath,true));EditorBuildSettings.scenes=builds.ToArray();}
            AssetDatabase.SaveAssets();
        }
        private static void CreateRig()
        {
            var sourceCamera=Camera.main;var sourceHud=UnityEngine.Object.FindFirstObjectByType<PrototypeHud>();
            var rig=new GameObject("Level rig — player camera HUD context");
            try
            {
                var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Player.prefab"));player.transform.SetParent(rig.transform,false);player.transform.localPosition=Vector3.zero;
                var camera=UnityEngine.Object.Instantiate(sourceCamera.gameObject,rig.transform).GetComponent<Camera>();camera.name="Main Camera";camera.transform.localPosition=new Vector3(0,0,-10);camera.GetComponent<CameraFollow2D>().Configure(player.transform);camera.backgroundColor=Color.black;
                var hud=UnityEngine.Object.Instantiate(sourceHud.gameObject,rig.transform).GetComponent<PrototypeHud>();hud.name="HUD — minimal";
                var light=player.GetComponent<Flashlight2D>();var interaction=player.GetComponent<PlayerInteraction>();var life=player.GetComponent<PlayerLife2D>();var weapon=player.GetComponent<PlayerWeapon2D>();
                player.GetComponent<PlayerAim2D>().Configure(camera);
                var data=new SerializedObject(light);data.FindProperty("worldCamera").objectReferenceValue=camera;data.ApplyModifiedPropertiesWithoutUndo();
                interaction.Configure(Array.Empty<Door2D>());
                data=new SerializedObject(hud);
                hud.Configure(light,interaction,(Text)data.FindProperty("flashlightStatus").objectReferenceValue,(Text)data.FindProperty("doorPrompt").objectReferenceValue);
                hud.ConfigureLife(life,(Text)data.FindProperty("healthStatus").objectReferenceValue,(Text)data.FindProperty("defeatPrompt").objectReferenceValue);
                hud.ConfigureWeapon(weapon,(Text)data.FindProperty("ammoStatus").objectReferenceValue);
                hud.GetComponentInChildren<AimReticle2D>(true).Configure(player.GetComponent<PlayerAim2D>(),weapon);
                var context=rig.AddComponent<LevelContext2D>();context.Configure(life,light);
                foreach(var l in UnityEngine.Object.FindObjectsByType<Light2D>())if(l.lightType==Light2D.LightType.Global)
                {var ambient=UnityEngine.Object.Instantiate(l.gameObject,rig.transform);ambient.name="Darkness — ambient";break;}
                foreach(var c in player.GetComponents<Component>())if(c!=null && PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                PrefabUtility.SaveAsPrefabAsset(rig,RigPath);
            }
            finally{UnityEngine.Object.DestroyImmediate(rig);}
        }
        private static void Wall(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Wall.prefab"));obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=new Vector3(size.x,size.y,1);
            var r=obj.GetComponent<SpriteRenderer>();r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/FlatUnlit.mat");r.color=new Color(.055f,.06f,.065f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        }
    }
}
