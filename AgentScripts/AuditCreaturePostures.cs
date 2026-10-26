using System.IO;
using System.Linq;
using EmergencyShift;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AuditCreaturePostures
{
    public static object Inspect()
    {
        const string folder="Concepts/Used/Stalker/Postures04";
        Directory.CreateDirectory(folder);
        var prefabs=new[] {"ShadowStalker","Sleeper"}.Select(name => {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/"+name+".prefab");
            var animator=new SerializedObject(prefab.GetComponent<CreatureSpriteAnimator2D>());
            return new {name,body=prefab.transform.Find("Visual/Body").GetComponent<SpriteRenderer>().sprite.name,
                walk=Enumerable.Range(0,animator.FindProperty("walkFrames").arraySize).Select(i => animator.FindProperty("walkFrames").GetArrayElementAtIndex(i).objectReferenceValue.name).ToArray(),
                compact=Enumerable.Range(0,animator.FindProperty("recoilFrames").arraySize).Select(i => animator.FindProperty("recoilFrames").GetArrayElementAtIndex(i).objectReferenceValue.name).ToArray()};
        }).ToArray();
        var live=Resources.FindObjectsOfTypeAll<Sleeper2D>().Where(actor=> actor.gameObject.scene.IsValid()&&actor.gameObject.scene.isLoaded&&actor.gameObject.activeInHierarchy)
            .Select(actor=>new {actor.name,scene=actor.gameObject.scene.name,state=actor.State.ToString(),
                closure=actor.Closure,body=actor.transform.Find("Visual/Body").GetComponent<SpriteRenderer>().sprite.name,
                width=actor.transform.Find("Visual/Body").GetComponent<SpriteRenderer>().bounds.size.x}).ToArray();
        var result=new {playing=EditorApplication.isPlaying,prefabs,live,scenes=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Select(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i)).Select(scene=>new {scene.name,scene.isDirty}).ToArray()};
        File.WriteAllText(folder+(EditorApplication.isPlaying?"/live-before.json":"/prefabs-before.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
