var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (UnityEngine.Application.isPlaying || scene.path != "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity")
{
    throw new System.InvalidOperationException("Expected MorgueRouteTest in Edit mode.");
}
if (scene.GetRootGameObjects().Any(r => r.name.StartsWith("17 Room skirting")))
{
    throw new System.InvalidOperationException("Room finishes already exist; edit them in place.");
}
var roots = scene.GetRootGameObjects();
var blocks = roots.First(r => r.name.StartsWith("07 ")).transform.Cast<UnityEngine.Transform>().OrderBy(t => t.name).ToArray();
var originalComponents = roots.SelectMany(r => r.GetComponentsInChildren<UnityEngine.Component>(true)).Where(c => c != null).ToArray();
System.Func<UnityEngine.Component, string> snapshot = component =>
{
    var wrapper = Newtonsoft.Json.Linq.JObject.Parse(UnityEditor.EditorJsonUtility.ToJson(component));
    var json = wrapper.Properties().First().Value as Newtonsoft.Json.Linq.JObject ?? wrapper;
    if (component is UnityEngine.Transform)
    {
        json.Remove("m_Children");
        if (blocks.Contains((UnityEngine.Transform)component))
        {
            json.Remove("m_LocalPosition");
        }
    }
    if (component is UnityEngine.SpriteRenderer && blocks.Contains(component.transform))
    {
        json.Remove("m_Color");
    }
    return json.ToString(Newtonsoft.Json.Formatting.None);
};
var before = originalComponents.ToDictionary(c => c, c => snapshot(c));
var directory = "Assets/EmergencyShift/Art/Environment/RoomFinish01";
var prefabDirectory = "Assets/EmergencyShift/Prefabs/Environment/RoomFinish01";
if (System.IO.Directory.Exists(directory) || System.IO.Directory.Exists(prefabDirectory))
{
    throw new System.InvalidOperationException("Refusing to overwrite existing room finish assets.");
}
System.IO.Directory.CreateDirectory(directory);
System.IO.Directory.CreateDirectory(prefabDirectory);
UnityEditor.AssetDatabase.Refresh();
var lit = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/EmergencyShift/Materials/FlatLit.mat");
var unlit = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/EmergencyShift/Materials/FlatUnlit.mat");
var rectangle = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/EmergencyShift/Art/Shared/Shapes/Rectangle.asset");
if (lit == null || unlit == null || rectangle == null || blocks.Length != 4)
{
    throw new System.InvalidOperationException("Missing established materials, rectangle or blocks.");
}
System.Func<string, int, int, System.Func<int, int, UnityEngine.Color>, float, UnityEngine.Sprite> makeSprite = (name, width, height, pixel, ppu) =>
{
    var texture = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGBA32, false);
    texture.name = name + " texture";
    texture.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    texture.filterMode = UnityEngine.FilterMode.Bilinear;
    var pixels = new UnityEngine.Color[width * height];
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            pixels[y * width + x] = pixel(x, y);
        }
    }
    texture.SetPixels(pixels);
    texture.Apply();
    UnityEditor.AssetDatabase.CreateAsset(texture, directory + "/" + name + "-texture.asset");
    var sprite = UnityEngine.Sprite.Create(texture, new UnityEngine.Rect(0, 0, width, height), UnityEngine.Vector2.one * .5f, ppu, 0, UnityEngine.SpriteMeshType.FullRect);
    sprite.name = name;
    UnityEditor.AssetDatabase.CreateAsset(sprite, directory + "/" + name + ".asset");
    return sprite;
};
var trimSprite = makeSprite("matte-sage-skirting", 120, 48, (x, y) =>
{
    var baseColor = new UnityEngine.Color(.34f, .39f, .36f, 1);
    if (x < 2 || x > 117 || y < 3)
    {
        return new UnityEngine.Color(.14f, .18f, .16f, 1);
    }
    if (y > 43)
    {
        return new UnityEngine.Color(.42f, .46f, .42f, 1);
    }
    float rub = .009f * UnityEngine.Mathf.Sin(x * .035f + y * .07f);
    return baseColor + new UnityEngine.Color(rub, rub, rub, 0);
}, 200);
System.Func<int, int, float> moistureField = (x, y) =>
{
    float u = (x - 95.5f) / 96;
    float v = (y - 63.5f) / 64;
    float body = UnityEngine.Mathf.Exp(-((u + .18f) * (u + .18f) / .30f + (v - .06f) * (v - .06f) / .24f));
    float tail = .67f * UnityEngine.Mathf.Exp(-((u - .41f) * (u - .41f) / .16f + (v + .15f) * (v + .15f) / .09f));
    float side = .38f * UnityEngine.Mathf.Exp(-((u + .52f) * (u + .52f) / .07f + (v + .28f) * (v + .28f) / .13f));
    return body + tail + side;
};
var dampSprite = makeSprite("moisture-film", 192, 128, (x, y) =>
{
    float alpha = UnityEngine.Mathf.SmoothStep(0, 1, UnityEngine.Mathf.Clamp01((moistureField(x, y) - .30f) / .62f));
    return new UnityEngine.Color(1, 1, 1, alpha);
}, 64);
var tideSprite = makeSprite("moisture-tide-edge", 192, 128, (x, y) =>
{
    float field = moistureField(x, y);
    float alpha = UnityEngine.Mathf.Exp(-UnityEngine.Mathf.Pow((field - .43f) / .075f, 2));
    return new UnityEngine.Color(1, 1, 1, alpha);
}, 64);
System.Func<UnityEngine.Transform, string, UnityEngine.Vector2, UnityEngine.Vector2, UnityEngine.Color, int, UnityEngine.Material, UnityEngine.SpriteRenderer> flat = (parent, name, position, size, color, order, material) =>
{
    var go = new UnityEngine.GameObject(name);
    go.transform.SetParent(parent, false);
    go.transform.localPosition = new UnityEngine.Vector3(position.x, position.y, 0);
    go.transform.localScale = new UnityEngine.Vector3(size.x, size.y, 1);
    var renderer = go.AddComponent<UnityEngine.SpriteRenderer>();
    renderer.sprite = rectangle;
    renderer.sharedMaterial = material;
    renderer.color = color;
    renderer.sortingOrder = order;
    return renderer;
};
var trimTemplate = new UnityEngine.GameObject("Room skirting strip");
var trimRenderer = trimTemplate.AddComponent<UnityEngine.SpriteRenderer>();
trimRenderer.sprite = trimSprite;
trimRenderer.sharedMaterial = lit;
trimRenderer.drawMode = UnityEngine.SpriteDrawMode.Tiled;
trimRenderer.size = new UnityEngine.Vector2(1, .24f);
trimRenderer.sortingOrder = -5;
var trimPrefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(trimTemplate, prefabDirectory + "/RoomSkirtingStrip.prefab");
UnityEngine.Object.DestroyImmediate(trimTemplate);

var drainTemplate = new UnityEngine.GameObject("Morgue floor drain");
flat(drainTemplate.transform, "Recessed mortar collar", UnityEngine.Vector2.zero, new UnityEngine.Vector2(.58f, .58f), new UnityEngine.Color(.28f, .32f, .30f), -6, lit);
flat(drainTemplate.transform, "Square metal frame", UnityEngine.Vector2.zero, new UnityEngine.Vector2(.51f, .51f), new UnityEngine.Color(.44f, .47f, .45f), -5, lit);
flat(drainTemplate.transform, "Dark drain opening", UnityEngine.Vector2.zero, new UnityEngine.Vector2(.41f, .41f), new UnityEngine.Color(.055f, .075f, .069f), -4, lit);
for (int index = 0; index < 7; index++)
{
    var tint = index == 2 ? new UnityEngine.Color(.42f, .45f, .43f) : new UnityEngine.Color(.34f, .38f, .36f);
    flat(drainTemplate.transform, "Grate bar " + (index + 1), new UnityEngine.Vector2((index - 3) * .054f, 0), new UnityEngine.Vector2(.024f, .41f), tint, -3, lit);
}
flat(drainTemplate.transform, "Under-grate support", new UnityEngine.Vector2(0, -.075f), new UnityEngine.Vector2(.40f, .022f), new UnityEngine.Color(.23f, .27f, .25f), -2, lit);
foreach (var corner in new[] { new UnityEngine.Vector2(-.224f, -.224f), new UnityEngine.Vector2(.224f, .224f) })
{
    flat(drainTemplate.transform, "Small retaining screw", corner, new UnityEngine.Vector2(.026f, .026f), new UnityEngine.Color(.20f, .23f, .21f), -2, lit);
}
var drainPrefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(drainTemplate, prefabDirectory + "/MorgueFloorDrain.prefab");
UnityEngine.Object.DestroyImmediate(drainTemplate);

var wetTemplate = new UnityEngine.GameObject("Morgue moisture residue");
var film = flat(wetTemplate.transform, "Soft damp film", UnityEngine.Vector2.zero, UnityEngine.Vector2.one, new UnityEngine.Color(.10f, .16f, .15f, .28f), -8, lit);
film.sprite = dampSprite;
var tide = flat(wetTemplate.transform, "Faint drying tide", UnityEngine.Vector2.zero, UnityEngine.Vector2.one, new UnityEngine.Color(.54f, .57f, .49f, .16f), -7, lit);
tide.sprite = tideSprite;
var wetPrefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(wetTemplate, prefabDirectory + "/MorgueMoistureResidue.prefab");
UnityEngine.Object.DestroyImmediate(wetTemplate);

var finishRoot = new UnityEngine.GameObject("17 Room skirting - matte sage tile");
UnityEditor.Undo.RegisterCreatedObjectUndo(finishRoot, "Add room finishes");
int stripCount = 0;
System.Action<UnityEngine.Transform, string, UnityEngine.Vector2, float, bool> strip = (parent, name, center, length, vertical) =>
{
    if (length <= .1f)
    {
        return;
    }
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(trimPrefab, parent);
    go.name = name;
    go.transform.localPosition = new UnityEngine.Vector3(center.x, center.y, 0);
    go.transform.localRotation = UnityEngine.Quaternion.Euler(0, 0, vertical ? 90 : 0);
    go.GetComponent<UnityEngine.SpriteRenderer>().size = new UnityEngine.Vector2(length, .24f);
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(go.GetComponent<UnityEngine.SpriteRenderer>());
    stripCount++;
};
var floors = roots.First(r => r.name.StartsWith("01 ")).GetComponentsInChildren<UnityEngine.SpriteRenderer>();
var roomIds = new[] { "B", "S", "A", "H", "O3", "O1", "O2", "G" };
var walls = roots.First(r => r.name.StartsWith("02 ")).GetComponentsInChildren<UnityEngine.BoxCollider2D>();
foreach (var floor in floors.Where(f => roomIds.Contains(f.name.Split(' ')[0])))
{
    var room = new UnityEngine.GameObject(floor.name.Split(' ')[0] + " - perimeter skirting");
    room.transform.SetParent(finishRoot.transform, false);
    var bounds = floor.bounds;
    foreach (var wall in walls)
    {
        var wb = wall.bounds;
        bool horizontal = wb.size.x > wb.size.y;
        if (horizontal && (UnityEngine.Mathf.Abs(wb.center.y - bounds.min.y) < .04f || UnityEngine.Mathf.Abs(wb.center.y - bounds.max.y) < .04f))
        {
            float from = UnityEngine.Mathf.Max(wb.min.x, bounds.min.x + .15f);
            float to = UnityEngine.Mathf.Min(wb.max.x, bounds.max.x - .15f);
            float sign = UnityEngine.Mathf.Sign(bounds.center.y - wb.center.y);
            strip(room.transform, wall.name + " - floor border", new UnityEngine.Vector2((from + to) / 2, wb.center.y + sign * (wb.size.y / 2 + .12f)), to - from, false);
        }
        if (!horizontal && (UnityEngine.Mathf.Abs(wb.center.x - bounds.min.x) < .04f || UnityEngine.Mathf.Abs(wb.center.x - bounds.max.x) < .04f))
        {
            float from = UnityEngine.Mathf.Max(wb.min.y, bounds.min.y + .15f);
            float to = UnityEngine.Mathf.Min(wb.max.y, bounds.max.y - .15f);
            float sign = UnityEngine.Mathf.Sign(bounds.center.x - wb.center.x);
            strip(room.transform, wall.name + " - floor border", new UnityEngine.Vector2(wb.center.x + sign * (wb.size.x / 2 + .12f), (from + to) / 2), to - from, true);
        }
    }
}
var centers = new[] { new UnityEngine.Vector2(-2.85f, 13.25f), new UnityEngine.Vector2(3.75f, 13.25f), new UnityEngine.Vector2(23.25f, -.3f), new UnityEngine.Vector2(30.45f, -.3f) };
var blockRecords = new System.Collections.Generic.List<object>();
for (int index = 0; index < blocks.Length; index++)
{
    var block = blocks[index];
    var old = block.position;
    UnityEditor.Undo.RecordObject(block, "Align internal blocks");
    UnityEditor.Undo.RecordObject(block.GetComponent<UnityEngine.SpriteRenderer>(), "Darken block interior");
    block.position = new UnityEngine.Vector3(centers[index].x, centers[index].y, old.z);
    block.GetComponent<UnityEngine.SpriteRenderer>().color = UnityEngine.Color.black;
    if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(block))
    {
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(block);
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(block.GetComponent<UnityEngine.SpriteRenderer>());
    }
    var rim = new UnityEngine.GameObject("Perimeter trim - follows block");
    rim.transform.SetParent(block, false);
    rim.transform.localScale = new UnityEngine.Vector3(1 / block.lossyScale.x, 1 / block.lossyScale.y, 1);
    UnityEditor.Undo.RegisterCreatedObjectUndo(rim, "Add block perimeter");
    var size = block.GetComponent<UnityEngine.BoxCollider2D>().bounds.size;
    strip(rim.transform, "South skirting", new UnityEngine.Vector2(0, -size.y / 2 - .12f), size.x + .48f, false);
    strip(rim.transform, "North skirting", new UnityEngine.Vector2(0, size.y / 2 + .12f), size.x + .48f, false);
    strip(rim.transform, "West skirting", new UnityEngine.Vector2(-size.x / 2 - .12f, 0), size.y, true);
    strip(rim.transform, "East skirting", new UnityEngine.Vector2(size.x / 2 + .12f, 0), size.y, true);
    var rimColor = new UnityEngine.Color(.045f, .052f, .049f, 1);
    flat(rim.transform, "South wall rim", new UnityEngine.Vector2(0, -size.y / 2 + .045f), new UnityEngine.Vector2(size.x, .09f), rimColor, 3, unlit);
    flat(rim.transform, "North wall rim", new UnityEngine.Vector2(0, size.y / 2 - .045f), new UnityEngine.Vector2(size.x, .09f), rimColor, 3, unlit);
    flat(rim.transform, "West wall rim", new UnityEngine.Vector2(-size.x / 2 + .045f, 0), new UnityEngine.Vector2(.09f, size.y), rimColor, 3, unlit);
    flat(rim.transform, "East wall rim", new UnityEngine.Vector2(size.x / 2 - .045f, 0), new UnityEngine.Vector2(.09f, size.y), rimColor, 3, unlit);
    blockRecords.Add(new { block = block.name, before = new[] {old.x, old.y}, after = new[] {block.position.x, block.position.y}, size = new[] {size.x, size.y} });
}
var wetRoot = new UnityEngine.GameObject("18 Morgue drain and moisture - static overlays");
UnityEditor.Undo.RegisterCreatedObjectUndo(wetRoot, "Add morgue floor details");
var drain = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(drainPrefab, wetRoot.transform);
drain.transform.localPosition = new UnityEngine.Vector3(-1.25f, 1.25f, 0);
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(drain.transform);
var wet = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(wetPrefab, wetRoot.transform);
wet.name = "Damp residue toward drain";
wet.transform.localPosition = new UnityEngine.Vector3(-1.50f, 1.32f, 0);
wet.transform.localScale = new UnityEngine.Vector3(1.05f, .85f, 1);
wet.transform.localRotation = UnityEngine.Quaternion.Euler(0, 0, 12);
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(wet.transform);
var oldWet = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(wetPrefab, wetRoot.transform);
oldWet.name = "Older moisture rub near north wall";
oldWet.transform.localPosition = new UnityEngine.Vector3(-1.4f, 2.65f, 0);
oldWet.transform.localScale = new UnityEngine.Vector3(.52f, .30f, 1);
oldWet.transform.localRotation = UnityEngine.Quaternion.Euler(0, 0, -9);
foreach (var renderer in oldWet.GetComponentsInChildren<UnityEngine.SpriteRenderer>())
{
    var color = renderer.color;
    color.a *= .65f;
    renderer.color = color;
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
}
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(oldWet.transform);

UnityEngine.Physics2D.SyncTransforms();
var changed = originalComponents.Where(c => c == null || before[c] != snapshot(c)).Select(c => c == null ? "missing" : c.name + ":" + c.GetType().Name).ToArray();
if (changed.Length != 0)
{
    throw new System.InvalidOperationException("Unexpected original changes: " + string.Join(",", changed));
}
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
var result = new { scene = scene.path, saved, dirty = scene.isDirty, protectedComponents = before.Count, protectedValuesUnchanged = true, rooms = roomIds.Length, stripCount, skirtingWidth = .24f, blocks = blockRecords, drain = new[] {-1.25f, 1.25f}, moisturePatches = 2, prefabDirectory, artDirectory = directory };
System.IO.File.WriteAllText(@"C:/Users/Mark/Documents/Codex/2026-10-13/gkf/work/room-floor-detail/apply-result.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
return result;
