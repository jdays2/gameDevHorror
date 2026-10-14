using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>Адресная подготовка v0.6.2; не перестраивает комнату/раскладку предметов и врагов.</summary>
    public static class CombatFeelSetup
    {
        [MenuItem("Emergency Shift/Apply combat feel revision")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Остановите Play.");
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=PrototypeSceneBuilder.ScenePath)throw new InvalidOperationException("Откройте BasementPrototype.");
            EditPrefab("Assets/EmergencyShift/Prefabs/Player.prefab",ConfigurePlayer);
            EditPrefab("Assets/EmergencyShift/Prefabs/ShadowStalker.prefab",ConfigureEnemy);
            EditPrefab(WeaponPrototypeSetup.EffectPath,ConfigureEffect);
            foreach(var p in UnityEngine.Object.FindObjectsByType<PlayerWeapon2D>())ConfigurePlayer(p.gameObject);
            foreach(var e in UnityEngine.Object.FindObjectsByType<ShadowStalker2D>())ConfigureEnemy(e.gameObject);
            foreach(var c in UnityEngine.Object.FindObjectsByType<CameraFollow2D>())
            {
                var so=new SerializedObject(c);var smooth=so.FindProperty("smoothing");
                if(Mathf.Approximately(smooth.floatValue,.12f))smooth.floatValue=.18f;so.ApplyModifiedPropertiesWithoutUndo();
                if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            }
            float[] oldPower={6.5f,5.8f,7.8f},power={8,7.2f,9};
            float[] oldRadius={2.8f,2.5f,3.1f},radius={3.6f,3.4f,3.9f};
            float[] oldTime={.14f,.12f,.16f},duration={.16f,.14f,.18f};
            for(int i=0;i<3;i++)
            {
                var p=AssetDatabase.LoadAssetAtPath<ShotVisualProfile>(WeaponPrototypeSetup.ProfilePath(i));
                if(Mathf.Approximately(p.flashIntensity,oldPower[i]))p.flashIntensity=power[i];
                if(Mathf.Approximately(p.flashRadius,oldRadius[i]))p.flashRadius=radius[i];
                if(Mathf.Approximately(p.flashSeconds,oldTime[i]))p.flashSeconds=duration[i];EditorUtility.SetDirty(p);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        private static void EditPrefab(string path,Action<GameObject> edit)
        {
            var obj=PrefabUtility.LoadPrefabContents(path);
            try{edit(obj);PrefabUtility.SaveAsPrefabAsset(obj,path);}
            finally{PrefabUtility.UnloadPrefabContents(obj);}
        }
        private static void ConfigurePlayer(GameObject obj)
        {
            var body=obj.transform.Find("Capsule visual").GetComponent<SpriteRenderer>();
            var component=obj.GetComponent<PlayerShotShadow2D>()??obj.AddComponent<PlayerShotShadow2D>();
            var existing=obj.transform.Find("Shot shadow");
            var shadow=existing!=null?existing.GetComponent<SpriteRenderer>():new GameObject("Shot shadow",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            shadow.transform.SetParent(obj.transform,false);shadow.sprite=body.sprite;
            shadow.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergencyShift/Materials/CreatureSilhouetteShadow.mat");
            shadow.sortingOrder=0;shadow.color=Color.black;shadow.enabled=false;
            component.Configure(body,shadow);EditorUtility.SetDirty(component);
            if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        private static void ConfigureEnemy(GameObject obj)
        {
            var component=obj.GetComponent<StalkerHitResponse2D>()??obj.AddComponent<StalkerHitResponse2D>();
            var visual=obj.transform.Find("Visual");
            component.Configure(visual,visual.Find("Body").GetComponent<SpriteRenderer>(),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EmergencyShift/Art/Stalker/BloodBlot.asset"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergencyShift/Materials/FlatLit.mat"));
            EditorUtility.SetDirty(component);
            if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        private static void ConfigureEffect(GameObject obj)
        {
            const string materialPath="Assets/EmergencyShift/Materials/TransientGlow.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(mat==null){mat=new Material(Shader.Find("EmergencyShift/Transient Glow"));AssetDatabase.CreateAsset(mat,materialPath);}
            var glow=EnsureGlow();
            foreach(var renderer in obj.GetComponentsInChildren<SpriteRenderer>())
            {
                renderer.sharedMaterial=mat;
                if(!renderer.name.StartsWith("Spark"))renderer.sprite=glow;
            }
            obj.transform.Find("Outer flame").localScale=new Vector3(.35f,.70f,1);
            obj.transform.Find("Left flame").localScale=new Vector3(.14f,.32f,1);
            obj.transform.Find("Right flame").localScale=new Vector3(.14f,.32f,1);
            obj.GetComponentInChildren<Light2D>().pointLightInnerRadius=.55f;
            MuzzlePlumeSetup.Configure(obj);
        }
        private static Sprite EnsureGlow()
        {
            const string path="Assets/EmergencyShift/Shapes/MuzzleGlow.asset";
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite!=null)return sprite;
            const int size=96;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Soft muzzle glow",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float t=(y+.5f)/size,px=(x+.5f)/size-.5f;
                float envelope=Mathf.Sin(t*Mathf.PI);
                float width=.035f+.24f*Mathf.Pow(envelope,.7f);
                float center=.018f*Mathf.Sin(t*12);
                float alpha=Mathf.Pow(Mathf.Clamp01(1-Mathf.Pow((px-center)/width,2)),1.5f)*Mathf.Pow(envelope,.55f);
                pixels[y*size+x]=new Color32(255,255,255,(byte)(255*alpha));
            }
            texture.SetPixels32(pixels);texture.Apply();
            sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);sprite.name="Soft muzzle glow";
            AssetDatabase.CreateAsset(sprite,path);AssetDatabase.AddObjectToAsset(texture,path);return sprite;
        }
    }
}
