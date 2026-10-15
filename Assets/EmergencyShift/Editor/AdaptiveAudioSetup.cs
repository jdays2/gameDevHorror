using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>Additive editor migration only; saved prefab objects, no runtime level construction.</summary>
    public static class AdaptiveAudioSetup
    {
        private const string Root = "Assets/EmergencyShift";
        public const string ProfilePath = Root+"/Settings/SurvivalAudio.asset";
        public const string WorldPath = Root+"/Audio/PlayerWorld/PlayerWorldAudio.asset";
        public const string PrefabPath = Root+"/Prefabs/LevelAudio.prefab";
        private static AudioMoodProfile profile;
        private static PlayerWorldAudioSet world;

        [MenuItem("Emergency Shift/Audio/Install adaptive audio (preserve level)")]
        public static void Apply()
        {
            if (Application.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stop Play and wait for compilation.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != PrototypeSceneBuilder.ScenePath) throw new InvalidOperationException("Open BasementPrototype.");
            if (scene.isDirty) throw new InvalidOperationException("Scene has unsaved edits; preserve them before this migration.");
            ImportClips();
            profile = AssetDatabase.LoadAssetAtPath<AudioMoodProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<AudioMoodProfile>();
                profile.exploration = Clip("Atmosphere/ExploreLoop");
                profile.threat = Clip("Atmosphere/ThreatLoop");
                profile.combat = Clip("Atmosphere/CombatLoop");
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            world = AssetDatabase.LoadAssetAtPath<PlayerWorldAudioSet>(WorldPath);
            if (world == null)
            {
                world = ScriptableObject.CreateInstance<PlayerWorldAudioSet>();
                world.concreteSteps = new[]{Clip("PlayerWorld/step-concrete-01"),Clip("PlayerWorld/step-concrete-02")};
                world.tileSteps = new[]{Clip("PlayerWorld/step-tile-01"),Clip("PlayerWorld/step-tile-02")};
                world.woodSteps = new[]{Clip("PlayerWorld/step-wood-01"),Clip("PlayerWorld/step-wood-02")};
                world.flashlightSwitch = Clip("PlayerWorld/flashlight-switch");
                world.emptyTrigger = Clip("PlayerWorld/pistol-empty");
                AssetDatabase.CreateAsset(world,WorldPath);
            }
            CreateAudioPrefab();
            EditPrefab(Root+"/Prefabs/Player.prefab",ConfigurePlayer);
            EditPrefab(Root+"/Prefabs/ShadowStalker.prefab",root=>{
                if (root.GetComponent<StalkerAudioThreat2D>() == null) root.AddComponent<StalkerAudioThreat2D>();
            });
            EditPrefab(LevelAuthoringSetup.RigPath,root=>{
                var player = root.GetComponentInChildren<PlayerLife2D>(true);
                ConfigurePlayer(player.gameObject);
                var director = root.GetComponentInChildren<LevelAudioDirector2D>(true);
                if (director == null)
                {
                    var audio = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),root.scene);
                    audio.transform.SetParent(root.transform,false);
                    director = audio.GetComponent<LevelAudioDirector2D>();
                }
                BindDirector(director,player);
            });
            var listener = UnityEngine.Object.FindFirstObjectByType<PlayerLife2D>();
            if (listener == null) throw new InvalidOperationException("No player in source scene.");
            ConfigurePlayer(listener.gameObject);
            var activeDirector = UnityEngine.Object.FindFirstObjectByType<LevelAudioDirector2D>();
            if (activeDirector == null)
                activeDirector = ((GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene)).GetComponent<LevelAudioDirector2D>();
            BindDirector(activeDirector,listener);
            foreach (var actor in UnityEngine.Object.FindObjectsByType<ShadowStalker2D>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if (actor.gameObject.scene == scene && actor.GetComponent<StalkerAudioThreat2D>() == null)
                    actor.gameObject.AddComponent<StalkerAudioThreat2D>();
            ConfigureImpacts();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        private static AudioClip Clip(string relative)
        {
            var result = AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/"+relative+".wav");
            if (result == null) throw new InvalidOperationException("Missing audio: "+relative);
            return result;
        }
        private static void ImportClips()
        {
            foreach (string folder in new[]{ "Atmosphere", "PlayerWorld", "WorldImpacts" })
            {
                foreach (var file in System.IO.Directory.GetFiles(Application.dataPath+"/EmergencyShift/Audio/"+folder,"*.wav"))
                {
                    string path="Assets"+file.Substring(Application.dataPath.Length).Replace('\\','/');
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                    var importer=(AudioImporter)AssetImporter.GetAtPath(path);
                    importer.forceToMono=folder!="Atmosphere";
                    var settings=importer.defaultSampleSettings;
                    settings.preloadAudioData=true;
                    settings.loadType=AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat=AudioCompressionFormat.PCM;
                    settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
                    importer.defaultSampleSettings=settings;importer.SaveAndReimport();
                }
            }
        }
        private static void EditPrefab(string path,Action<GameObject> action)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try { action(root);PrefabUtility.SaveAsPrefabAsset(root,path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static T Ensure<T>(GameObject obj) where T:Component
        { var component=obj.GetComponent<T>();return component!=null?component:obj.AddComponent<T>(); }
        private static AudioSource Source(Transform parent,string name)
        {
            var child=parent.Find(name);
            if(child==null){child=new GameObject(name).transform;child.SetParent(parent,false);}
            var source=Ensure<AudioSource>(child.gameObject);
            source.playOnAwake=false;source.spatialBlend=0;source.dopplerLevel=0;
            return source;
        }
        private static void Assign(Component owner,string field,UnityEngine.Object value)
        {
            var data=new SerializedObject(owner);var property=data.FindProperty(field);
            if(property==null)throw new InvalidOperationException(owner.GetType().Name+"."+field);
            if(property.objectReferenceValue==null)
            {
                property.objectReferenceValue=value;data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(owner);
                if(PrefabUtility.IsPartOfPrefabInstance(owner))PrefabUtility.RecordPrefabInstancePropertyModifications(owner);
            }
        }
        private static void ConfigurePlayer(GameObject player)
        {
            var feet=Ensure<PlayerFootsteps2D>(player);
            var utility=Ensure<PlayerUtilityAudio2D>(player);
            var group=player.transform.Find("World audio");
            if(group==null){group=new GameObject("World audio").transform;group.SetParent(player.transform,false);}
            Assign(feet,"sounds",world);
            Assign(feet,"leftSource",Source(group,"Step left"));
            Assign(feet,"rightSource",Source(group,"Step right"));
            Assign(utility,"sounds",world);
            Assign(utility,"source",Source(group,"Utility"));
        }
        private static void CreateAudioPrefab()
        {
            if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)!=null)return;
            var preview=EditorSceneManager.NewPreviewScene();
            var root=new GameObject("Level audio — Explore Threat Combat");
            SceneManager.MoveGameObjectToScene(root,preview);
            try
            {
                var director=root.AddComponent<LevelAudioDirector2D>();
                Assign(director,"profile",profile);
                Assign(director,"explorationSource",Source(root.transform,"Explore loop"));
                Assign(director,"threatSource",Source(root.transform,"Threat loop"));
                Assign(director,"combatSource",Source(root.transform,"Combat loop"));
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally{EditorSceneManager.ClosePreviewScene(preview);}
        }
        private static void BindDirector(LevelAudioDirector2D director,PlayerLife2D player)
        {
            Assign(director,"profile",profile);Assign(director,"player",player);
            Assign(director,"flashlight",player.GetComponent<Flashlight2D>());
        }
        private static void ConfigureImpacts()
        {
            var set=AssetDatabase.LoadAssetAtPath<PlayerAudioSet>(Root+"/Audio/PistolIndoor/PistolIndoorAudio.asset");
            if(set==null)throw new InvalidOperationException("Missing selected pistol audio set.");
            var labels=new System.Collections.Generic.List<string>(AssetDatabase.GetLabels(set));
            const string installed="es-world-impacts-v011";
            if(labels.Contains(installed))return; // Later manual material/volume edits belong to the user.
            var data=new SerializedObject(set);
            // Replace only the previously disabled placeholder material impacts.
            SetClips(data,"concreteImpact",new[]{Clip("WorldImpacts/impact-concrete-01"),Clip("WorldImpacts/impact-concrete-02")});
            SetClips(data,"woodImpact",new[]{Clip("WorldImpacts/impact-wood-01")});
            var stalker=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/ShadowStalker.prefab").GetComponent<StalkerAudio2D>();
            if(stalker.Sounds!=null && stalker.Sounds.Clips(StalkerAudioSet.Cue.Hit).Length>0)
                SetClips(data,"fleshImpact",stalker.Sounds.Clips(StalkerAudioSet.Cue.Hit));
            data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(set);
            EditPrefab(Root+"/Prefabs/Player.prefab",root=>EnableImpacts(root.GetComponent<PlayerAudio2D>()));
            EditPrefab(LevelAuthoringSetup.RigPath,root=>EnableImpacts(root.GetComponentInChildren<PlayerAudio2D>(true)));
            EnableImpacts(UnityEngine.Object.FindFirstObjectByType<PlayerAudio2D>());
            labels.Add(installed);AssetDatabase.SetLabels(set,labels.ToArray());
        }
        private static void EnableImpacts(PlayerAudio2D audio)
        {
            if(audio.ImpactAudioEnabled)return;
            var data=new SerializedObject(audio);
            data.FindProperty("impactAudioEnabled").boolValue=true;
            data.FindProperty("impactVolume").floatValue=.55f;
            data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(audio);
            if(PrefabUtility.IsPartOfPrefabInstance(audio))PrefabUtility.RecordPrefabInstancePropertyModifications(audio);
        }
        private static void SetClips(SerializedObject owner,string field,AudioClip[] clips)
        {
            var property=owner.FindProperty(field);property.arraySize=clips.Length;
            for(int i=0;i<clips.Length;i++)property.GetArrayElementAtIndex(i).objectReferenceValue=clips[i];
        }
    }
}
