using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>Усиливает только VFX и настройки разрыва; сохраняет расстановку, AI и баланс оружия.</summary>
    public static class CombatEffectsRevision
    {
        [MenuItem("Emergency Shift/Apply combat effects revision")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Остановите Play.");
            Scene scene=SceneManager.GetActiveScene();
            if(scene.path!=PrototypeSceneBuilder.ScenePath)throw new InvalidOperationException("Откройте BasementPrototype.");
            var root=PrefabUtility.LoadPrefabContents(WeaponPrototypeSetup.EffectPath);
            try{ConfigureShotEffect(root);PrefabUtility.SaveAsPrefabAsset(root,WeaponPrototypeSetup.EffectPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            float[] oldPower={2,1.7f,2.4f},power={6.5f,5.8f,7.8f};
            float[] oldRadius={1.9f,1.5f,2.3f},radius={2.8f,2.5f,3.1f};
            float[] oldTime={.08f,.06f,.12f},duration={.14f,.12f,.16f};
            for(int i=0;i<3;i++)
            {
                var p=AssetDatabase.LoadAssetAtPath<ShotVisualProfile>(WeaponPrototypeSetup.ProfilePath(i));
                if(Mathf.Approximately(p.flashIntensity,oldPower[i]))p.flashIntensity=power[i];
                if(Mathf.Approximately(p.flashRadius,oldRadius[i]))p.flashRadius=radius[i];
                if(Mathf.Approximately(p.flashSeconds,oldTime[i]))p.flashSeconds=duration[i];
                if(i==1&&Mathf.Approximately(p.muzzleSize,1))p.muzzleSize=.9f;
                if(i==2&&Mathf.Approximately(p.muzzleSize,1))p.muzzleSize=1.15f;
                EditorUtility.SetDirty(p);
            }
            const string enemyPath="Assets/EmergencyShift/Prefabs/ShadowStalker.prefab";
            root=PrefabUtility.LoadPrefabContents(enemyPath);
            try{ConfigureGore(root.GetComponent<GoreBurst2D>());PrefabUtility.SaveAsPrefabAsset(root,enemyPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            foreach(var gore in UnityEngine.Object.FindObjectsByType<GoreBurst2D>(FindObjectsInactive.Include))
            {
                ConfigureGore(gore);
                if(PrefabUtility.IsPartOfPrefabInstance(gore))PrefabUtility.RecordPrefabInstancePropertyModifications(gore);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        private static void ConfigureGore(GoreBurst2D gore)
        {
            gore.ConfigureRupture(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EmergencyShift/Art/Stalker/BloodBlot.asset"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EmergencyShift/Shapes/Capsule.asset"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergencyShift/Materials/FlatUnlit.mat"));
            EditorUtility.SetDirty(gore);
        }

        public static void ConfigureShotEffect(GameObject root)
        {
            foreach(string name in new[]{"Forward spark","Cross spark","Core"})
            {var old=root.transform.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            var rect=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EmergencyShift/Shapes/Rectangle.asset");
            var flame=EnsureFlame();
            var layers=new[]{
                Part(root,"Outer flame",flame,new Vector2(0,.22f),new Vector2(.42f,.65f),0),
                Part(root,"Left flame",flame,new Vector2(-.065f,.14f),new Vector2(.20f,.42f),25),
                Part(root,"Right flame",flame,new Vector2(.065f,.14f),new Vector2(.20f,.42f),-25)};
            var core=Part(root,"White core",flame,new Vector2(0,.10f),new Vector2(.16f,.30f),0);
            var sparks=new[]{
                Part(root,"Spark 1",rect,new Vector2(-.16f,.12f),new Vector2(.025f,.16f),45),
                Part(root,"Spark 2",rect,new Vector2(.16f,.13f),new Vector2(.025f,.15f),-45),
                Part(root,"Spark 3",rect,new Vector2(-.11f,.29f),new Vector2(.02f,.13f),25),
                Part(root,"Spark 4",rect,new Vector2(.11f,.31f),new Vector2(.02f,.14f),-25)};
            var light=root.GetComponentInChildren<Light2D>();
            if(light==null)
            {
                var obj=new GameObject("Temporary flash",typeof(Light2D));obj.transform.SetParent(root.transform,false);light=obj.GetComponent<Light2D>();
                light.lightType=Light2D.LightType.Point;light.pointLightOuterAngle=360;light.pointLightInnerAngle=360;
                light.shadowIntensity=1;light.shadowVolumeIntensity=1;
                var so=new SerializedObject(light);var ids=so.FindProperty("m_ApplyToSortingLayers");ids.arraySize=1;ids.GetArrayElementAtIndex(0).intValue=0;so.ApplyModifiedPropertiesWithoutUndo();
            }
            light.intensity=0;root.GetComponent<ShotEffect2D>().ConfigureFlame(light,sparks,core,layers);
        }
        private static SpriteRenderer Part(GameObject root,string name,Sprite sprite,Vector2 position,Vector2 scale,float angle)
        {
            var existing=root.transform.Find(name);
            var obj=existing!=null?existing.gameObject:new GameObject(name,typeof(SpriteRenderer));
            if(existing==null)obj.transform.SetParent(root.transform,false);
            obj.transform.localPosition=new Vector3(position.x,position.y,name=="White core"?-.003f:-.001f);
            obj.transform.localScale=new Vector3(scale.x,scale.y,1);obj.transform.localRotation=Quaternion.Euler(0,0,angle);
            var renderer=obj.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=Color.white;
            renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergencyShift/Materials/FlatUnlit.mat");renderer.sortingOrder=2;
            return renderer;
        }
        private static Sprite EnsureFlame()
        {
            const string path="Assets/EmergencyShift/Shapes/MuzzleFlame.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(existing!=null&&existing.name=="Jagged muzzle flame v2")return existing;
            var vertices=new[]{Vector2.zero,new Vector2(0,-.48f),new Vector2(-.16f,-.12f),new Vector2(-.42f,.10f),new Vector2(-.18f,.21f),new Vector2(-.12f,.38f),new Vector2(0,.50f),new Vector2(.12f,.28f),new Vector2(.38f,.16f),new Vector2(.18f,-.06f)};
            // Собственная геометрическая маска: сохраняется в asset и не требует runtime-генерации.
            const int size=64,samples=4;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Muzzle flame mask",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                int covered=0;
                for(int sy=0;sy<samples;sy++)for(int sx=0;sx<samples;sx++)
                {
                    Vector2 p=new Vector2((x+(sx+.5f)/samples)/size-.5f,(y+(sy+.5f)/samples)/size-.5f);
                    bool inside=false;
                    for(int i=1,j=vertices.Length-1;i<vertices.Length;j=i++)
                    {
                        Vector2 a=vertices[i],b=vertices[j];
                        if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
                    }
                    if(inside)covered++;
                }
                pixels[y*size+x]=new Color32(255,255,255,(byte)(255*covered/(samples*samples)));
            }
            texture.SetPixels32(pixels);texture.Apply();
            var sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size,0,SpriteMeshType.FullRect);
            sprite.name="Jagged muzzle flame v2";
            if(existing==null){AssetDatabase.CreateAsset(sprite,path);existing=sprite;}
            else{EditorUtility.CopySerialized(sprite,existing);UnityEngine.Object.DestroyImmediate(sprite);EditorUtility.SetDirty(existing);}
            AssetDatabase.AddObjectToAsset(texture,path);return existing;
        }
    }
}
