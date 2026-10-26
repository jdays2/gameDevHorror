using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using EmergencyShift.Features.Enemies.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AuditStalkerMotion
{
    public static string CheckSkin()
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        Texture2D texture = new Texture2D(2, 2);
        try
        {
            var profile = AssetDatabase.LoadAssetAtPath<StalkerRigProfile>("Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset");
            texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(profile.Atlas)));
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
            var actor = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
            foreach (Behaviour behaviour in actor.GetComponentsInChildren<Behaviour>(true))
            {
                behaviour.enabled = false;
            }
            var rig = actor.GetComponent<StalkerLimbRig2D>();
            typeof(StalkerLimbRig2D).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(rig, null);
            Transform visual = actor.transform.Find("Visual");
            Mesh mesh = visual.Find("Stalker articulated skin").GetComponent<MeshFilter>().sharedMesh;
            Vector2[] uv = mesh.uv;
            int[] triangles = mesh.triangles;
            var visible = new List<int>();
            for (int triangle = 0; triangle < triangles.Length; triangle += 3)
            {
                Vector2 a = uv[triangles[triangle]];
                Vector2 b = uv[triangles[triangle + 1]];
                Vector2 c = uv[triangles[triangle + 2]];
                foreach (Vector2 sample in new[] { a, b, c, (a+b+c)/3, (a+b)/2, (b+c)/2, (a+c)/2 })
                {
                    if (texture.GetPixelBilinear(sample.x, sample.y).a > .1f)
                    {
                        visible.Add(triangle);
                        break;
                    }
                }
            }
            var failures = new List<object>();
            int totalInverted = 0;
            float minimumAreaRatio = float.MaxValue;
            foreach (float speed in new[] { 1.2f, 1.5f, 3.8f, -1f })
            {
                var gait = new StalkerGaitModel(profile.Limbs, 1, profile.CycleDistance, profile.SupportFraction,
                    profile.LiftHeight, profile.BodyHeight, profile.WeightTransfer);
                typeof(StalkerLimbRig2D).GetField("gait", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rig, gait);
                Vector2 position = Vector2.zero;
                float heading = 0;
                float headingVelocity = 0;
                int count = speed < 0 ? 480 : 240;
                for (int frame = 0; frame < count; frame++)
                {
                    float actualSpeed = speed;
                    float intendedHeading = 0;
                    if (speed < 0)
                    {
                        actualSpeed = 1.2f;
                        if (frame >= 180 && frame < 240)
                        {
                            actualSpeed = 1.5f;
                        }
                        else if (frame >= 240 && frame < 360)
                        {
                            actualSpeed = 3.8f;
                        }
                        if (frame >= 405 && frame < 450)
                        {
                            actualSpeed = 0;
                        }
                        intendedHeading = Mathf.Clamp((frame - 360) * 2.25f, 0, 90);
                    }
                    position += (Vector2)(Quaternion.Euler(0, 0, intendedHeading) * Vector2.up) * (actualSpeed / 60);
                    if (actualSpeed > 0)
                    {
                        heading = Mathf.SmoothDampAngle(heading, intendedHeading, ref headingVelocity, .1f, 160, 1f / 60);
                    }
                    actor.transform.position = position;
                    visual.rotation = Quaternion.Euler(0, 0, heading);
                    gait.Step(position, heading, 1f / 60, true, 0);
                    rig.RenderPose();
                    Vector3[] vertices = mesh.vertices;
                    int inverted = 0;
                    foreach (int triangle in visible)
                    {
                        Vector3 a = vertices[triangles[triangle]];
                        Vector3 b = vertices[triangles[triangle + 1]];
                        Vector3 c = vertices[triangles[triangle + 2]];
                        float sourceArea = (uv[triangles[triangle + 1]].x - uv[triangles[triangle]].x) *
                            (uv[triangles[triangle + 2]].y - uv[triangles[triangle]].y) -
                            (uv[triangles[triangle + 1]].y - uv[triangles[triangle]].y) *
                            (uv[triangles[triangle + 2]].x - uv[triangles[triangle]].x);
                        sourceArea *= profile.Atlas.width * profile.Atlas.height / (profile.PixelsPerUnit * profile.PixelsPerUnit);
                        float ratio = Vector3.Cross(b - a, c - a).z / sourceArea;
                        minimumAreaRatio = Mathf.Min(minimumAreaRatio, ratio);
                        if (!float.IsFinite(ratio) || ratio <= .001f)
                        {
                            inverted++;
                        }
                    }
                    totalInverted += inverted;
                    if (inverted > 0)
                    {
                        failures.Add(new { speed, frame, inverted });
                    }
                }
            }
            var report = new { vertices = mesh.vertexCount, triangles = triangles.Length / 3,
                visibleTriangles = visible.Count, totalInverted, minimumAreaRatio, failures };
            string json = JsonConvert.SerializeObject(report, Formatting.Indented);
            File.WriteAllText("Concepts/Used/Stalker/Locomotion02/MotionReview/skin-triangles.json", json);
            return "Vertices=" + mesh.vertexCount + "; visible triangles=" + visible.Count + "; inverted instances=" + totalInverted + "; minimum area ratio=" + minimumAreaRatio;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    public static string Tune()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Tuning requires stopped Editor.");
        }
        var profile = AssetDatabase.LoadAssetAtPath<StalkerRigProfile>("Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset");
        Undo.RecordObject(profile, "Stalker longer planted stride");
        var serialized = new SerializedObject(profile);
        serialized.FindProperty("cycleDistance").floatValue = 1f;
        serialized.ApplyModifiedProperties();
        profile.Validate();
        AssetDatabase.SaveAssetIfDirty(profile);
        return "Cycle distance 1 world unit at scale 1; gameplay speeds preserved.";
    }

    public static string Measure()
    {
        var scenes = new List<object>();
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene scene = SceneManager.GetSceneAt(index);
            scenes.Add(new { scene.path, scene.isDirty });
        }
        var profile = AssetDatabase.LoadAssetAtPath<StalkerRigProfile>("Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset");
        var results = new List<object>();
        foreach (float distance in new[] { profile.CycleDistance })
        {
            foreach (float speed in new[] { 1.2f, 1.5f, 3.8f })
            {
                var gait = new StalkerGaitModel(profile.Limbs, 1, distance, profile.SupportFraction,
                    profile.LiftHeight, profile.BodyHeight, profile.WeightTransfer);
                int[] lifts = new int[6];
                int minimumSupport = 6;
                const int rate = 240;
                const int seconds = 6;
                gait.Step(Vector2.zero, 0, 1f / rate, true, 0);
                for (int frame = 1; frame <= seconds * rate; frame++)
                {
                    var previous = (bool[])gait.Swinging.Clone();
                    gait.Step(Vector2.up * (frame * speed / rate), 0, 1f / rate, true, 0);
                    int support = 0;
                    for (int limb = 0; limb < 6; limb++)
                    {
                        if (gait.Swinging[limb] && !previous[limb])
                        {
                            lifts[limb]++;
                        }
                        if (!gait.Swinging[limb])
                        {
                            support++;
                        }
                    }
                    if (frame > rate)
                    {
                        minimumSupport = Mathf.Min(minimumSupport, support);
                    }
                }
                results.Add(new { distance, speed, cyclesPerSecond = speed / distance, liftsInSixSeconds = lifts,
                    minimumSupport, gait.ReachCorrections, gait.RecoverySteps });
            }
        }
        var report = new { scenes, startScene = EditorSceneManager.playModeStartScene, results };
        string json = JsonConvert.SerializeObject(report, Formatting.Indented);
        Directory.CreateDirectory("Concepts/Used/Stalker/Locomotion02/MotionReview");
        File.WriteAllText("Concepts/Used/Stalker/Locomotion02/MotionReview/cadence-current.json", json);
        return json;
    }
}
