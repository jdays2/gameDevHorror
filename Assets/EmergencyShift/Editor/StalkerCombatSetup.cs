using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmergencyShift.Editor
{
    /// <summary>Добавляет боевые кадры, жизнь игрока и переиспользуемый префаб крови.</summary>
    public static class StalkerCombatSetup
    {
        [Serializable] private sealed class FrameInfo
        {public string name;public float x,y,width,height,anchorX,anchorY,ppu;}
        [Serializable] private sealed class Layout
        {public FrameInfo[] attack,death,gibs;}
        private const string Art="Assets/EmergencyShift/Art/Stalker";
        private const string EnemyPrefab="Assets/EmergencyShift/Prefabs/ShadowStalker.prefab";
        private const string PlayerPrefab="Assets/EmergencyShift/Prefabs/Player.prefab";
        private const string PoolPrefab="Assets/EmergencyShift/Prefabs/BloodPool.prefab";

        [MenuItem("Emergency Shift/Add attack and death")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Остановите Play.");
            Scene scene=SceneManager.GetActiveScene();
            if(scene.path!=PrototypeSceneBuilder.ScenePath)throw new InvalidOperationException("Откройте BasementPrototype.");
            StalkerSceneSetup.Apply();
            Texture2D attackTexture=StalkerSceneSetup.ImportTexture(Art+"/AttackSheet.png");
            Texture2D deathTexture=StalkerSceneSetup.ImportTexture(Art+"/DeathSheet.png");
            var layout=JsonUtility.FromJson<Layout>(AssetDatabase.LoadAssetAtPath<TextAsset>(Art+"/CombatFrameLayout.json").text);
            Sprite[] attack=Frames(layout.attack,attackTexture);
            Sprite[] death=Frames(layout.death,deathTexture);
            Sprite[] gibs=Frames(layout.gibs,deathTexture);
            // Начальная/конечная позы атаки совпадают с настоящей позой ходьбы.
            Sprite ready=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/Frames/Walk00-corrected.asset");
            attack[0]=ready;attack[attack.Length-1]=ready;
            Material lit=AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergencyShift/Materials/FlatLit.mat");
            Sprite disc=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/EyeDisc.asset");
            BloodPool2D blood=EnsureBloodPool(disc,CreateBloodBlot(),lit);

            var playerContents=PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                if(playerContents.GetComponent<PlayerLife2D>()==null)playerContents.AddComponent<PlayerLife2D>();
                PrefabUtility.SaveAsPrefabAsset(playerContents,PlayerPrefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(playerContents);}
            var enemyContents=PrefabUtility.LoadPrefabContents(EnemyPrefab);
            try
            {
                ConfigureEnemy(enemyContents,attack,death,gibs,blood,lit);
                PrefabUtility.SaveAsPrefabAsset(enemyContents,EnemyPrefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(enemyContents);}
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerMotor2D>();
            PlayerLife2D life=player.GetComponent<PlayerLife2D>();
            if(life==null)life=player.gameObject.AddComponent<PlayerLife2D>();
            var enemy=UnityEngine.Object.FindAnyObjectByType<ShadowStalker2D>();
            ConfigureEnemy(enemy.gameObject,attack,death,gibs,blood,lit);
            ConfigureHud(life);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("STALKER_COMBAT_READY: attack, death, health/restart, blood and fragments");
        }

        private static Sprite[] Frames(FrameInfo[] entries,Texture2D texture)
        {
            var result=new Sprite[entries.Length];
            for(int i=0;i<entries.Length;i++)
            {
                FrameInfo f=entries[i];
                result[i]=StalkerSceneSetup.Frame(f.name,texture,new Rect(f.x,f.y,f.width,f.height),new Vector2(f.anchorX,f.anchorY),f.ppu);
            }
            return result;
        }

        private static void ConfigureEnemy(GameObject obj,Sprite[] attack,Sprite[] death,Sprite[] gibs,BloodPool2D pool,Material lit)
        {
            var view=obj.GetComponent<StalkerSpriteAnimator>();view.ConfigureCombat(attack,death);
            var gore=obj.GetComponent<GoreBurst2D>();if(gore==null)gore=obj.AddComponent<GoreBurst2D>();
            gore.Configure(pool,gibs,lit);EditorUtility.SetDirty(view);EditorUtility.SetDirty(gore);
            if(PrefabUtility.IsPartOfPrefabInstance(obj))
            {PrefabUtility.RecordPrefabInstancePropertyModifications(view);PrefabUtility.RecordPrefabInstancePropertyModifications(gore);}
        }

        private static BloodPool2D EnsureBloodPool(Sprite disc,Sprite blot,Material lit)
        {
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(PoolPrefab);
            if(existing!=null)
            {
                var contents=PrefabUtility.LoadPrefabContents(PoolPrefab);
                try
                {
                    foreach(SpriteRenderer renderer in contents.GetComponentsInChildren<SpriteRenderer>())
                        if(renderer.name.StartsWith("Pool patch"))renderer.sprite=blot;
                    PrefabUtility.SaveAsPrefabAsset(contents,PoolPrefab);
                }
                finally{PrefabUtility.UnloadPrefabContents(contents);}
                return existing.GetComponent<BloodPool2D>();
            }
            var obj=new GameObject("Blood pool",typeof(SortingGroup),typeof(BloodPool2D));
            obj.GetComponent<SortingGroup>().sortingOrder=0;
            var patches=new SpriteRenderer[7];var drops=new SpriteRenderer[14];
            for(int i=0;i<patches.Length;i++)patches[i]=BloodPart("Pool patch "+i,obj.transform,blot,lit,i);
            for(int i=0;i<drops.Length;i++)drops[i]=BloodPart("Droplet "+i,obj.transform,disc,lit,patches.Length+i);
            obj.GetComponent<BloodPool2D>().Configure(patches,drops);
            var prefab=PrefabUtility.SaveAsPrefabAsset(obj,PoolPrefab);
            UnityEngine.Object.DestroyImmediate(obj);
            return prefab.GetComponent<BloodPool2D>();
        }

        private static Sprite CreateBloodBlot()
        {
            string path=Art+"/BloodBlot.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(existing!=null)return existing;
            const int side=256;var texture=new Texture2D(side,side,TextureFormat.RGBA32,false){name="Blood blot",filterMode=FilterMode.Bilinear};
            var pixels=new Color[side*side];
            for(int y=0;y<side;y++)for(int x=0;x<side;x++)
            {
                Vector2 p=new Vector2((x+0.5f)/side*2-1,(y+0.5f)/side*2-1);
                float angle=Mathf.Atan2(p.y,p.x);
                float edge=0.81f+0.08f*Mathf.Sin(angle*3)+0.05f*Mathf.Cos(angle*7)+0.025f*Mathf.Sin(angle*11);
                pixels[y*side+x]=new Color(1,1,1,Mathf.Clamp01((edge-p.magnitude)*side));
            }
            texture.SetPixels(pixels);texture.Apply();
            var sprite=Sprite.Create(texture,new Rect(0,0,side,side),new Vector2(0.5f,0.5f),side);
            sprite.name="Blood blot";AssetDatabase.CreateAsset(sprite,path);AssetDatabase.AddObjectToAsset(texture,sprite);return sprite;
        }

        private static SpriteRenderer BloodPart(string name,Transform parent,Sprite disc,Material lit,int order)
        {
            var obj=new GameObject(name,typeof(SpriteRenderer));obj.transform.SetParent(parent,false);
            var renderer=obj.GetComponent<SpriteRenderer>();renderer.sprite=disc;renderer.sharedMaterial=lit;
            renderer.color=new Color(0.8f,0.01f,0.025f);renderer.sortingOrder=order;return renderer;
        }

        private static void ConfigureHud(PlayerLife2D life)
        {
            var hud=UnityEngine.Object.FindAnyObjectByType<PrototypeHud>();
            Transform root=hud.transform;
            Transform healthObject=root.Find("Health status");
            Text health=healthObject!=null?healthObject.GetComponent<Text>():Label("Health status",root,new Vector2(1,0),new Vector2(-28,27),new Vector2(280,32),16);
            health.alignment=TextAnchor.MiddleRight;health.rectTransform.pivot=new Vector2(1,0);
            Transform defeatObject=root.Find("Defeat prompt");
            Text defeat=defeatObject!=null?defeatObject.GetComponent<Text>():Label("Defeat prompt",root,new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(520,150),28);
            defeat.rectTransform.pivot=new Vector2(0.5f,0.5f);defeat.alignment=TextAnchor.MiddleCenter;
            defeat.text="ВЫ ПОГИБЛИ\n[R]  Начать заново";defeat.color=new Color(1,0.35f,0.35f);defeat.gameObject.SetActive(false);
            health.text="ЗДОРОВЬЕ  2 / 2";hud.ConfigureLife(life,health,defeat);EditorUtility.SetDirty(hud);
        }

        private static Text Label(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,int fontSize)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=anchor;rect.anchoredPosition=position;rect.sizeDelta=size;
            var text=obj.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;text.raycastTarget=false;
            text.color=new Color(0.75f,0.77f,0.79f);return text;
        }

        [MenuItem("Emergency Shift/Preview/Kill selected creature")]
        public static void PreviewDeath()
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Проверка смерти работает в Play.");
            var actor=Selection.activeGameObject!=null?Selection.activeGameObject.GetComponentInParent<ShadowStalker2D>():null;
            if(actor==null)actor=UnityEngine.Object.FindAnyObjectByType<ShadowStalker2D>();
            if(actor!=null)actor.TakeDamage(int.MaxValue);
        }
    }
}
