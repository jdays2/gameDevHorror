using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EmergencyShift.Editor
{
    /// <summary>Small migrations toward authored levels; no movement/AI tuning or level rebuild.</summary>
    public static class ProjectReadinessSetup
    {
        public const string DefinitionPath="Assets/EmergencyShift/Settings/PistolDefinition.asset";
        [MenuItem("Emergency Shift/Preparation/Apply level and combat foundations")]
        public static void Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
            if(SceneManager.GetActiveScene().path!=PrototypeSceneBuilder.ScenePath)
                throw new InvalidOperationException("Open BasementPrototype before running this one-time migration.");
            var definition=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(DefinitionPath);
            if(definition==null)
            {
                definition=ScriptableObject.CreateInstance<WeaponDefinition>();definition.visuals=AssetDatabase.LoadAssetAtPath<ShotVisualProfile>(WeaponPrototypeSetup.ProfilePath(0));
                var current=UnityEngine.Object.FindFirstObjectByType<PlayerWeapon2D>();
                definition.damage=current.Damage;definition.maximumAmmo=current.MaximumAmmo;definition.secondsBetweenShots=current.ShotInterval;definition.range=current.BulletRange;
                AssetDatabase.CreateAsset(definition,DefinitionPath);
            }
            Edit("Assets/EmergencyShift/Prefabs/Player.prefab",r=>{var w=r.GetComponent<PlayerWeapon2D>();if(w.Definition==null)w.SetDefinition(definition,true,false);});
            Edit("Assets/EmergencyShift/Prefabs/PistolPickup.prefab",r=>{var s=new SerializedObject(r.GetComponent<WeaponLootPickup>());if(s.FindProperty("weaponDefinition").objectReferenceValue==null)s.FindProperty("weaponDefinition").objectReferenceValue=definition;s.ApplyModifiedPropertiesWithoutUndo();});
            Edit("Assets/EmergencyShift/Prefabs/ShadowStalker.prefab",r=>{if(r.GetComponent<CrowdAgent2D>()==null)r.AddComponent<CrowdAgent2D>();});
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerLife2D>();
            var gun=player.GetComponent<PlayerWeapon2D>();
            // Explicitly keep the current scene's Inspector stats, including the user's .1 s fire interval.
            gun.SetDefinition(definition,false,false);EditorUtility.SetDirty(gun);PrefabUtility.RecordPrefabInstancePropertyModifications(gun);
            var root=GameObject.Find("Level context");if(root==null)root=new GameObject("Level context");
            var context=root.GetComponent<LevelContext2D>();if(context==null)context=root.AddComponent<LevelContext2D>();
            context.Configure(player,player.GetComponent<Flashlight2D>());EditorUtility.SetDirty(context);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        private static void Edit(string path,Action<GameObject> action)
        {var r=PrefabUtility.LoadPrefabContents(path);try{action(r);PrefabUtility.SaveAsPrefabAsset(r,path);}finally{PrefabUtility.UnloadPrefabContents(r);}}
    }
}
