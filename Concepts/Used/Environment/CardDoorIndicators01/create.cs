var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (UnityEngine.Application.isPlaying || scene.path != "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity")
{
    throw new System.InvalidOperationException("Expected route test in Edit mode.");
}
string artFolder = "Assets/EmergencyShift/Art/Environment/CardDoorIndicators01";
if (!UnityEditor.AssetDatabase.IsValidFolder(artFolder))
{
    UnityEditor.AssetDatabase.CreateFolder("Assets/EmergencyShift/Art/Environment", "CardDoorIndicators01");
}
string prefabPath = "Assets/EmergencyShift/Prefabs/Environment/CardDoorIndicators.prefab";
if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath) != null)
{
    throw new System.InvalidOperationException("Existing indicators: preserve manual changes.");
}
var originalTransforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.Transform>(true)).ToArray();
var originalComponents = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.Component>(true)).Where(c => c != null && !(c is UnityEngine.Transform)).ToArray();
System.Func<string> snapshot = () => Newtonsoft.Json.JsonConvert.SerializeObject(new
{
    transforms = originalTransforms.Select(t => new { id = t.GetEntityId().ToString(), p = t.localPosition.ToString("R"), r = t.localRotation.ToString("R"), s = t.localScale.ToString("R"), active = t.gameObject.activeSelf }).ToArray(),
    components = originalComponents.Select(c => new { id = c.GetEntityId().ToString(), json = UnityEditor.EditorJsonUtility.ToJson(c) }).ToArray()
});
string before = snapshot();
var texture = new UnityEngine.Texture2D(32, 32, UnityEngine.TextureFormat.RGBA32, false);
texture.name = "IndicatorLens";
texture.filterMode = UnityEngine.FilterMode.Bilinear;
texture.wrapMode = UnityEngine.TextureWrapMode.Clamp;
for (int y = 0; y < 32; y++)
{
    for (int x = 0; x < 32; x++)
    {
        float radius = new UnityEngine.Vector2((x + .5f) / 32 - .5f, (y + .5f) / 32 - .5f).magnitude;
        float alpha = 1 - UnityEngine.Mathf.SmoothStep(0, 1, UnityEngine.Mathf.InverseLerp(.43f, .49f, radius));
        texture.SetPixel(x, y, new UnityEngine.Color(1, 1, 1, alpha));
    }
}
texture.Apply();
UnityEditor.AssetDatabase.CreateAsset(texture, artFolder + "/IndicatorLensTexture.asset");
var sprite = UnityEngine.Sprite.Create(texture, new UnityEngine.Rect(0, 0, 32, 32), new UnityEngine.Vector2(.5f, .5f), 32, 0, UnityEngine.SpriteMeshType.FullRect);
sprite.name = "IndicatorLens";
UnityEditor.AssetDatabase.CreateAsset(sprite, artFolder + "/IndicatorLens.asset");
var unlit = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/EmergencyShift/Materials/FlatUnlit.mat");
var lit = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/EmergencyShift/Materials/FlatLit.mat");
var blue = new UnityEngine.Color(.18f, .49f, .78f, 1);
var pair = new UnityEngine.GameObject("CardDoorIndicators");
foreach (float side in new[] { -1f, 1f })
{
    var lamp = new UnityEngine.GameObject(side < 0 ? "Left indicator" : "Right indicator");
    lamp.transform.SetParent(pair.transform, false);
    lamp.transform.localPosition = new UnityEngine.Vector3(side * 1.43f, -.17f, 0);
    var housing = new UnityEngine.GameObject("Small dark bezel");
    housing.transform.SetParent(lamp.transform, false);
    housing.transform.localScale = UnityEngine.Vector3.one * .075f;
    var bezel = housing.AddComponent<UnityEngine.SpriteRenderer>();
    bezel.sprite = sprite;
    bezel.sharedMaterial = lit;
    bezel.color = new UnityEngine.Color(.09f, .105f, .11f, 1);
    bezel.sortingOrder = 3;
    var lens = new UnityEngine.GameObject("Colored lens");
    lens.transform.SetParent(lamp.transform, false);
    lens.transform.localScale = UnityEngine.Vector3.one * .045f;
    var core = lens.AddComponent<UnityEngine.SpriteRenderer>();
    core.sprite = sprite;
    core.sharedMaterial = unlit;
    core.color = UnityEngine.Color.Lerp(blue, UnityEngine.Color.white, .08f);
    core.sortingOrder = 4;
    var light = lamp.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
    light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
    light.color = blue;
    light.intensity = .12f;
    light.pointLightInnerRadius = .025f;
    light.pointLightOuterRadius = .18f;
    light.pointLightInnerAngle = 360;
    light.pointLightOuterAngle = 360;
    light.falloffIntensity = .5f;
    light.blendStyleIndex = 2;
    light.shadowsEnabled = false;
    light.shadowIntensity = 0;
}
var prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(pair, prefabPath);
UnityEngine.Object.DestroyImmediate(pair);
var doors = UnityEngine.Object.FindObjectsByType<EmergencyShift.DoorAccess2D>(UnityEngine.FindObjectsSortMode.None);
foreach (var access in doors)
{
    var marking = access.transform.Find("CardAccessZone");
    var color = marking.transform.Find("Access color").GetComponent<UnityEngine.SpriteRenderer>().color;
    var instance = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, access.transform);
    UnityEditor.Undo.RegisterCreatedObjectUndo(instance, "Card door indicator lamps");
    instance.transform.SetPositionAndRotation(access.transform.position, marking.rotation);
    instance.transform.localScale = UnityEngine.Vector3.one;
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
    foreach (var light in instance.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>())
    {
        light.color = color;
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(light);
    }
    foreach (var core in instance.GetComponentsInChildren<UnityEngine.SpriteRenderer>().Where(r => r.name == "Colored lens"))
    {
        core.color = UnityEngine.Color.Lerp(color, UnityEngine.Color.white, .08f);
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(core);
    }
}
if (snapshot() != before)
{
    throw new System.InvalidOperationException("Existing scene components or transforms changed.");
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.AssetDatabase.SaveAssets();
var result = new { doors = doors.Length, lights = doors.Length * 2, prefab = prefabPath, lensDiameter = .045f, radius = .18f, intensity = .12f, existingSceneDataPreserved = true };
System.IO.File.WriteAllText(@"C:/Users/Mark/Documents/Codex/2026-10-13/gkf/work/card-access-audio-all/indicator-result.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
return result;
