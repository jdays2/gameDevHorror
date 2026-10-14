using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EmergencyShift.Editor
{
    public static class WorldFoleySetup
    {
        private const string Folder="Assets/EmergencyShift/Audio/WorldFoley";
        [MenuItem("Emergency Shift/Audio/Apply door and pickup foley")]
        public static void Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
            if(SceneManager.GetActiveScene().path!=PrototypeSceneBuilder.ScenePath)
                throw new InvalidOperationException("Open BasementPrototype before running this one-time migration.");
            // The old prototype kept audio as scene-only additions. Promote only these additions,
            // otherwise adding the same components to the prefab would duplicate voices in the instance.
            var existingPlayer=UnityEngine.Object.FindFirstObjectByType<PlayerWeapon2D>();
            const string playerPath="Assets/EmergencyShift/Prefabs/Player.prefab";
            Transform audioRoot=null;
            foreach(Transform child in existingPlayer.transform)
                if(child.name=="Audio" && PrefabUtility.IsAddedGameObjectOverride(child.gameObject))audioRoot=child;
            var addedAudio=existingPlayer.GetComponent<PlayerAudio2D>();
            if(audioRoot!=null && addedAudio!=null && PrefabUtility.IsAddedComponentOverride(addedAudio))
            {
                // Repair the legacy conflict if a prefab already has its own independently authored channels.
                // Keep the scene's authoritative audio additions, then promote them with their references intact.
                var content=PrefabUtility.LoadPrefabContents(playerPath);
                try
                {
                    if(content.GetComponent<PlayerAudio2D>()!=null)
                    {
                        var ejector=content.GetComponent<PlayerCasingEjector2D>();if(ejector!=null)UnityEngine.Object.DestroyImmediate(ejector);
                        UnityEngine.Object.DestroyImmediate(content.GetComponent<PlayerAudio2D>());
                        var old=content.transform.Find("Audio");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
                        PrefabUtility.SaveAsPrefabAsset(content,playerPath);
                    }
                }
                finally{PrefabUtility.UnloadPrefabContents(content);}
            }
            if(audioRoot!=null && PrefabUtility.IsAddedGameObjectOverride(audioRoot.gameObject))
                PrefabUtility.ApplyAddedGameObject(audioRoot.gameObject,playerPath,InteractionMode.AutomatedAction);
            foreach(var c in new Component[]{existingPlayer.GetComponent<PlayerAudio2D>(),existingPlayer.GetComponent<PlayerCasingEjector2D>()})
                if(c!=null && PrefabUtility.IsAddedComponentOverride(c))PrefabUtility.ApplyAddedComponent(c,playerPath,InteractionMode.AutomatedAction);
            var selected=AssetDatabase.LoadAssetAtPath<PlayerAudioSet>("Assets/EmergencyShift/Audio/PistolIndoor/PistolIndoorAudio.asset");
            var data=new SerializedObject(selected);
            Clips(data,"ammoPickup","pickup_ammo_");Clips(data,"weaponPickup","pickup_weapon_");Clips(data,"upgradePickup","pickup_upgrade_");data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(selected);
            var set=AssetDatabase.LoadAssetAtPath<DoorAudioSet>(Folder+"/DoorFoley.asset");
            if(set==null){set=ScriptableObject.CreateInstance<DoorAudioSet>();AssetDatabase.CreateAsset(set,Folder+"/DoorFoley.asset");}
            data=new SerializedObject(set);Clips(data,"openStart","door_start_open_");Clips(data,"closeStart","door_start_close_");Clips(data,"stop","door_stop_");data.FindProperty("slideLoop").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/door_slide_loop.wav");data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(set);
            Edit("Assets/EmergencyShift/Prefabs/Player.prefab",root=>Player(root,selected));
            Edit("Assets/EmergencyShift/Prefabs/Door.prefab",root=>Door(root,set,null));
            foreach(var p in UnityEngine.Object.FindObjectsByType<PlayerWeapon2D>(FindObjectsInactive.Include))Player(p.gameObject,selected);
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerLife2D>();
            foreach(var door in UnityEngine.Object.FindObjectsByType<Door2D>(FindObjectsInactive.Include))Door(door.gameObject,set,player.transform);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        private static void Player(GameObject root,PlayerAudioSet set)
        {
            bool added=root.GetComponent<PlayerAudio2D>()==null;
            var audio=PlayerAudioSetup.Configure(root,set);var data=new SerializedObject(audio);
            data.FindProperty("sounds").objectReferenceValue=set;
            if(added){data.FindProperty("combatAudioEnabled").boolValue=true;data.FindProperty("indoorShot").boolValue=true;data.FindProperty("impactAudioEnabled").boolValue=false;data.FindProperty("shotVolume").floatValue=.8f;}
            if(added || Mathf.Approximately(data.FindProperty("pickupVolume").floatValue,.65f))data.FindProperty("pickupVolume").floatValue=.9f;
            data.ApplyModifiedPropertiesWithoutUndo();
            var eject=root.GetComponent<PlayerCasingEjector2D>()??root.AddComponent<PlayerCasingEjector2D>();data=new SerializedObject(eject);
            if(data.FindProperty("casingPrefab").objectReferenceValue==null)data.FindProperty("casingPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/PistolCasing.prefab").GetComponent<ShellCasing2D>();
            data.ApplyModifiedPropertiesWithoutUndo();Mark(audio);Mark(eject);
        }
        private static void Door(GameObject root,DoorAudioSet set,Transform listener)
        {
            var a=root.GetComponent<DoorAudio2D>()??root.AddComponent<DoorAudio2D>();
            a.Configure(set,Source(root,"Audio — slide",true),Source(root,"Audio — latch",false),listener);Mark(a);
        }
        private static AudioSource Source(GameObject root,string name,bool loop)
        {
            var child=root.transform.Find(name);if(child==null){child=new GameObject(name).transform;child.SetParent(root.transform,false);}
            var a=child.GetComponent<AudioSource>();if(a==null)a=child.gameObject.AddComponent<AudioSource>();
            a.playOnAwake=false;a.spatialBlend=0;a.loop=loop;a.priority=70;a.volume=0;return a;
        }
        private static void Clips(SerializedObject data,string field,string prefix)
        {
            var paths=new System.Collections.Generic.List<string>();foreach(string guid in AssetDatabase.FindAssets("t:AudioClip",new[]{Folder})){string path=AssetDatabase.GUIDToAssetPath(guid);if(System.IO.Path.GetFileName(path).StartsWith(prefix,StringComparison.Ordinal))paths.Add(path);}paths.Sort(StringComparer.Ordinal);
            var a=data.FindProperty(field);a.arraySize=paths.Count;for(int i=0;i<paths.Count;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>(paths[i]);
        }
        private static void Edit(string path,Action<GameObject> edit){var r=PrefabUtility.LoadPrefabContents(path);try{edit(r);PrefabUtility.SaveAsPrefabAsset(r,path);}finally{PrefabUtility.UnloadPrefabContents(r);}}
        private static void Mark(Component c){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
    }
}
