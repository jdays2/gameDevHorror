using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EmergencyShift.Editor.Authoring;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class ProbeStalkerFold
{
    public static object Capture(bool bent = false)
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Requires stopped Editor.");
        }
        string folder = "Concepts/Used/Stalker/Postures05/" + (bent ? "StudyBent" : "Study");
        Directory.CreateDirectory(folder);
        Scene scene = EditorSceneManager.NewPreviewScene();
        Mesh mesh = null;
        RenderTexture target = null;
        Texture2D pixels = null;
        try
        {
            var profile = AssetDatabase.LoadAssetAtPath<StalkerRigProfile>("Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset");
            var sprites = AssetDatabase.LoadAssetAtPath<StalkerSpriteSet>("Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset");
            var root = New("Fold study", scene);
            Type skinType = typeof(StalkerRigProfile).Assembly.GetType("EmergencyShift.Features.Enemies.View.StalkerSkinMesh");
            object skin = Activator.CreateInstance(skinType, new object[] { profile });
            mesh = (Mesh)skinType.GetProperty("Mesh").GetValue(skin);
            MethodInfo update = skinType.GetMethod("Update");
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = profile.SurfaceMaterial;
            var block = new MaterialPropertyBlock();
            block.SetTexture("_MainTex", profile.Atlas);
            renderer.SetPropertyBlock(block);
            var torso = New("Torso", scene).AddComponent<SpriteRenderer>();
            torso.sprite = profile.Torso;
            torso.sharedMaterial = profile.SurfaceMaterial;
            torso.sortingOrder = 1;
            var light = New("Light", scene).AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            var camera = New("Camera", scene).AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = scene;
            camera.orthographic = true;
            camera.orthographicSize = 576f / 448;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.transform.position = Vector3.back * 10;
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>().SetRenderer(0);
            target = new RenderTexture(576, 576, 24, RenderTextureFormat.ARGB32);
            target.Create();
            pixels = new Texture2D(576, 576, TextureFormat.RGBA32, false);
            var rows = new List<object>();
            int sample = 0;
            foreach (float footHeight in bent ? new[] { .12f, .20f, .28f } : new[] { 0f, .5f, 1f })
            {
                foreach (float rise in bent ? new[] { 2f, 4f, 6f, 8f } : new[] { 0f, .5f, 1f, 2f })
                {
                    var pose = StalkerPoseComposer.Pose(profile, sprites, .25f, 1);
                    float minCross = float.MaxValue;
                    for (int leg = 0; leg < 6; leg++)
                    {
                        StalkerLimb anatomy = profile.Limbs[leg];
                        float side = leg % 2 == 0 ? -1 : 1;
                        float direction = leg < 2 ? 1 : 0;
                        if (leg >= 4)
                        {
                            direction = -1;
                        }
                        Vector3 pole = new Vector3(side, direction, rise);
                        Vector3 foot = pose.Feet[leg];
                        foot.z = pose.Hips[leg].z * footHeight;
                        if (bent)
                        {
                            Vector2 offset = new Vector2(-side * .02f, direction * footHeight);
                            if (leg >= 2 && leg < 4)
                            {
                                offset = new Vector2(side * footHeight * .6f, -.07f);
                            }
                            foot = pose.Hips[leg] + (Vector3)offset;
                        }
                        pose.Knees[leg] = StalkerGaitModel.SolveKnee(pose.Hips[leg], ref foot, pole,
                            anatomy.UpperLength, anatomy.LowerLength);
                        pose.Feet[leg] = foot;
                        Vector2 sourceFoot = anatomy.Tip - anatomy.Hip;
                        Vector2 sourceKnee = anatomy.Knee - anatomy.Hip;
                        Vector2 actualFoot = foot - pose.Hips[leg];
                        Vector2 actualKnee = pose.Knees[leg] - pose.Hips[leg];
                        minCross = Mathf.Min(minCross, (sourceFoot.x * sourceKnee.y - sourceFoot.y * sourceKnee.x) *
                            (actualFoot.x * actualKnee.y - actualFoot.y * actualKnee.x));
                    }
                    update.Invoke(skin, new object[] { pose, root.transform, Color.white });
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, 576, 576), 0, 0);
                    pixels.Apply();
                    File.WriteAllBytes(folder + "/fold-" + sample.ToString("D2") + ".png", pixels.EncodeToPNG());
                    rows.Add(new { sample, footHeight, rise, minCross,
                        hips = Array.ConvertAll(pose.Hips, p => new[] { p.x, p.y, p.z }),
                        knees = Array.ConvertAll(pose.Knees, p => new[] { p.x, p.y, p.z }),
                        feet = Array.ConvertAll(pose.Feet, p => new[] { p.x, p.y, p.z }) });
                    sample++;
                }
            }
            File.WriteAllText(folder + "/poses.json", Newtonsoft.Json.JsonConvert.SerializeObject(rows));
            return new { samples = sample, productionChanges = 0 };
        }
        finally
        {
            RenderTexture.active = null;
            if (pixels != null)
            {
                UnityEngine.Object.DestroyImmediate(pixels);
            }
            if (target != null)
            {
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
            EditorSceneManager.ClosePreviewScene(scene);
            if (mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }
    }

    private static GameObject New(string name, Scene scene)
    {
        var result = new GameObject(name);
        SceneManager.MoveGameObjectToScene(result, scene);
        return result;
    }
}
