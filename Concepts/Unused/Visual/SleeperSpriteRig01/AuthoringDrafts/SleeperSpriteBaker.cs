using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>
    /// Offline six-bone source-image rig. It never opens, saves or modifies an authored scene.
    /// The closed source head is intentionally unchanged; an optional preview callback can overlay it.
    /// </summary>
    public static class SleeperSpriteBaker
    {
        public const int SourceWidth = 1024;
        public const int SourceHeight = 1536;
        public const int FramePixels = 512;
        public const float CanvasUnits = 2.8f;
        public const float UnitsPerSourcePixel = 0.00155f;
        public const float WalkStride = 0.32f;
        private const int GridSpacing = 24;
        private const float SwingFraction = 0.28f;
        private const float ReachMargin = 0.0001f;
        private static readonly Vector2 SourceCenter = new Vector2(512, 730);
        private static readonly Vector2[] HeadRegion =
        {
            new Vector2(512, 35), new Vector2(625, 125), new Vector2(710, 305), new Vector2(680, 398),
            new Vector2(512, 430), new Vector2(335, 398), new Vector2(310, 305), new Vector2(400, 125)
        };
        private static readonly Vector2[] BodyRegion =
        {
            new Vector2(512, 420), new Vector2(620, 500), new Vector2(720, 680), new Vector2(740, 810),
            new Vector2(655, 1000), new Vector2(512, 1060), new Vector2(355, 960), new Vector2(280, 790),
            new Vector2(310, 660), new Vector2(400, 500)
        };
        private static readonly LegDefinition[] Legs = CreateLegDefinitions();

        public enum Clip
        {
            Sleep,
            Wake,
            Walk,
            Attack,
            Hit,
            Death
        }

        [Serializable]
        public sealed class LegPose
        {
            public string id;
            public Vector2 root;
            public Vector2 knee;
            public Vector2 contact;
            public Vector2 requestedContact;
            public bool planted;
            public int plantIndex;
            public float lift;
            public float clampedDistance;
        }

        [Serializable]
        public sealed class PoseData
        {
            public string clip;
            public float time;
            public Vector2 rootPosition;
            public Vector2 bodyOffset;
            public float bodyAngle;
            public float breathing;
            public float wakeProgress;
            public float eyeProgress;
            public float jawProgress;
            public float hitWeight;
            public float detachWeight;
            public float deathProgress;
            public Color tint = Color.white;
            public LegPose[] legs;
        }

        [Serializable]
        public sealed class ClipExport
        {
            public string name;
            public int frameCount;
            public int columns;
            public int rows;
            public bool loop;
            public float previewDurationSeconds;
            public string durationScope = "Presentation proposal, not runtime combat tuning";
            public string atlas;
            public FrameExport[] frames;
        }

        [Serializable]
        public sealed class FrameExport
        {
            public int index;
            public int rectX;
            public int rectY;
            public int rectWidth = FramePixels;
            public int rectHeight = FramePixels;
            public PoseData pose;
        }

        [Serializable]
        public sealed class NumericQa
        {
            public int walkIntervals;
            public int attackIntervals;
            public int minimumPlantedFeet = 3;
            public int minimumAttackPlantedFeet = 3;
            public float maximumStanceSlipUnits;
            public float maximumContactTargetErrorUnits;
            public float maximumBoneLengthErrorUnits;
            public float maximumReachClampUnits;
            public float maximumVisibleTriangleFlipFraction;
            public int framesTouchingCanvasBorder;
            public string scope = "Sampled offline source rig only. No runtime collision, turns or interpolated playback guarantee.";
        }

        [Serializable]
        public sealed class BakeManifest
        {
            public string source;
            public string sourceSha256;
            public int sourceWidth = SourceWidth;
            public int sourceHeight = SourceHeight;
            public Vector2 sourceCenterTopLeft = SourceCenter;
            public float unitsPerSourcePixel = UnitsPerSourcePixel;
            public int framePixels = FramePixels;
            public float canvasUnits = CanvasUnits;
            public Vector2 pivot = new Vector2(0.5f, 0.5f);
            public float walkStrideUnits = WalkStride;
            public float swingFraction = SwingFraction;
            public string sourceCoordinates = "Source anchors use top-left pixels; exported poses use world XY units.";
            public string atlasCoordinates = "Rects are bottom-left pixels. No trimming or per-frame scaling.";
            public string headScope = "Closed source head retained. jawProgress is metadata for a separately supplied overlay; this baker does not invent jaw art.";
            public string deathScope = "Each source triangle belongs to one body/limb partition. Limbs separate once; no intact duplicate body or additional gibs are spawned.";
            public string limitations = "Soft mesh deformation can shear skin near joints. Bone/contact checks do not prove visual quality, muscle volume, mouth anatomy or gameplay readiness.";
            public SourceLeg[] sourceLegs;
            public ClipExport[] clips;
            public NumericQa qa;
        }

        [Serializable]
        public sealed class SourceLeg
        {
            public string id;
            public Vector2 rootTopLeft;
            public Vector2 kneeTopLeft;
            public Vector2 contactTopLeft;
            public float upperLengthUnits;
            public float lowerLengthUnits;
        }

        private sealed class LegDefinition
        {
            public string id;
            public Vector2 rootPixel;
            public Vector2 kneePixel;
            public Vector2 contactPixel;
            public Vector2 root;
            public Vector2 knee;
            public Vector2 contact;
            public float upperLength;
            public float lowerLength;
            public float bendSign;
            public float swingOffset;
            public Vector2[] region;
        }

        private struct VertexBinding
        {
            public Vector2 source;
            public float bodyWeight;
            public Vector3 upperWeights;
            public Vector3 lowerWeights;
            public int region;
        }

        private readonly struct ClipDefinition
        {
            public readonly Clip clip;
            public readonly int count;
            public readonly int columns;
            public readonly bool loop;
            public readonly float previewSeconds;

            public ClipDefinition(Clip clip, int count, int columns, bool loop, float previewSeconds)
            {
                this.clip = clip;
                this.count = count;
                this.columns = columns;
                this.loop = loop;
                this.previewSeconds = previewSeconds;
            }
        }

        /// <summary>Writes only named PNG atlases and animation-metadata.json under outputDir.</summary>
        public static BakeManifest Bake(string sourcePngPath, string outputDir)
        {
            return Bake(sourcePngPath, outputDir, null);
        }

        /// <summary>The callback is for already supplied head parts, after pose evaluation and before capture.</summary>
        public static BakeManifest Bake(string sourcePngPath, string outputDir, Action<PreviewRig, PoseData> beforeCapture)
        {
            string sourcePath = Path.GetFullPath(sourcePngPath);
            string destination = Path.GetFullPath(outputDir);
            Directory.CreateDirectory(destination);
            var definitions = new[]
            {
                new ClipDefinition(Clip.Sleep, 8, 4, true, 2.8f),
                new ClipDefinition(Clip.Wake, 10, 5, false, 0.9f),
                new ClipDefinition(Clip.Walk, 16, 4, true, 1.2f),
                new ClipDefinition(Clip.Attack, 12, 4, false, 0.9f),
                new ClipDefinition(Clip.Hit, 6, 3, false, 0.3f),
                new ClipDefinition(Clip.Death, 12, 4, false, 1.2f)
            };
            var manifest = new BakeManifest
            {
                source = sourcePath,
                sourceSha256 = FileHash(sourcePath),
                sourceLegs = ExportSourceLegs(),
                clips = new ClipExport[definitions.Length],
                qa = EvaluateNumericQa()
            };

            using (var rig = CreatePreview(sourcePath))
            {
                for (int clipIndex = 0; clipIndex < definitions.Length; clipIndex++)
                {
                    ClipDefinition definition = definitions[clipIndex];
                    manifest.clips[clipIndex] = BakeClip(rig, definition, destination, beforeCapture, manifest.qa);
                }
            }

            File.WriteAllText(Path.Combine(destination, "animation-metadata.json"), JsonUtility.ToJson(manifest, true));
            return manifest;
        }

        public static PreviewRig CreatePreview(string sourcePngPath)
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Sleeper source baking requires Edit mode.");
            }

            return new PreviewRig(Path.GetFullPath(sourcePngPath));
        }

        /// <summary>Walk time may exceed one cycle; other clips use normalized time in [0,1].</summary>
        public static PoseData EvaluatePose(Clip clip, float normalizedTime)
        {
            if (float.IsNaN(normalizedTime) || float.IsInfinity(normalizedTime))
            {
                throw new ArgumentOutOfRangeException(nameof(normalizedTime));
            }

            float time = Mathf.Clamp01(normalizedTime);
            var pose = new PoseData
            {
                clip = clip.ToString().ToLowerInvariant(),
                time = normalizedTime,
                legs = new LegPose[Legs.Length]
            };

            switch (clip)
            {
                case Clip.Sleep:
                    pose.breathing = 0.5f - 0.5f * Mathf.Cos(normalizedTime * Mathf.PI * 2f);
                    break;
                case Clip.Wake:
                    pose.wakeProgress = SmoothRange(0.1f, 0.8f, time);
                    pose.eyeProgress = SmoothRange(0.1f, 0.65f, time);
                    pose.jawProgress = SmoothRange(0.25f, 0.85f, time);
                    pose.bodyOffset = new Vector2(0, -0.018f * Mathf.Sin(time * Mathf.PI));
                    pose.bodyAngle = 1.4f * Mathf.Sin(time * Mathf.PI * 2f) * (1f - time);
                    break;
                case Clip.Walk:
                    pose.rootPosition = Vector2.up * (WalkStride * normalizedTime);
                    pose.bodyOffset = new Vector2(0.009f * Mathf.Sin(normalizedTime * Mathf.PI * 2f), 0.004f * Mathf.Cos(normalizedTime * Mathf.PI * 6f));
                    pose.bodyAngle = 0.8f * Mathf.Sin(normalizedTime * Mathf.PI * 2f);
                    pose.eyeProgress = 1;
                    pose.jawProgress = 0.35f;
                    break;
                case Clip.Attack:
                    pose.rootPosition = Vector2.up * (0.42f * SmoothRange(0.28f, 0.67f, time));
                    pose.bodyOffset = new Vector2(0, -0.025f * Pulse(0, 0.28f, time));
                    pose.eyeProgress = 1;
                    pose.jawProgress = SmoothRange(0, 0.26f, time) * (1f - 0.6f * SmoothRange(0.56f, 0.85f, time));
                    break;
                case Clip.Hit:
                    pose.hitWeight = Mathf.Sin(Mathf.PI * time) * (1f - time);
                    pose.bodyOffset = new Vector2(-0.018f, -0.025f) * pose.hitWeight;
                    pose.bodyAngle = -3f * pose.hitWeight;
                    pose.eyeProgress = 1f - 0.6f * pose.hitWeight;
                    pose.jawProgress = 0.35f;
                    break;
                case Clip.Death:
                    pose.deathProgress = SmoothRange(0, 0.85f, time);
                    pose.detachWeight = SmoothRange(0.16f, 0.24f, time);
                    pose.bodyAngle = -5f * SmoothRange(0.1f, 0.7f, time);
                    pose.bodyOffset = Vector2.down * (0.025f * SmoothRange(0.1f, 0.7f, time));
                    pose.eyeProgress = 1f - SmoothRange(0, 0.18f, time);
                    pose.jawProgress = 0.35f * (1f - SmoothRange(0, 0.25f, time));
                    pose.tint = Color.Lerp(Color.white, new Color(0.58f, 0.55f, 0.60f, 1), SmoothRange(0.1f, 0.85f, time));
                    break;
            }

            for (int i = 0; i < Legs.Length; i++)
            {
                pose.legs[i] = EvaluateLeg(clip, normalizedTime, pose, Legs[i], i);
            }

            return pose;
        }

        private static LegPose EvaluateLeg(Clip clip, float time, PoseData pose, LegDefinition leg, int index)
        {
            Vector2 root = BodyPoint(leg.root, pose);
            Vector2 target = leg.contact;
            float lift = 0;
            bool planted = true;
            int plantIndex = 0;
            if (clip == Clip.Walk)
            {
                float shifted = time - leg.swingOffset;
                int cycle = Mathf.FloorToInt(shifted);
                float phase = shifted - cycle;
                float stanceCenter = (1f + SwingFraction) * 0.5f;
                Vector2 landing = leg.contact + Vector2.up * (WalkStride * (cycle + leg.swingOffset + stanceCenter));
                target = landing;
                plantIndex = cycle;
                planted = phase >= SwingFraction;
                if (!planted)
                {
                    float swing = phase / SwingFraction;
                    float travel = SmootherStep(swing);
                    target = landing - Vector2.up * WalkStride * (1f - travel);
                    lift = 16f * swing * swing * (1f - swing) * (1f - swing);
                    float outward = index == 1 ? 1f : -1f;
                    target += Vector2.right * (outward * 0.025f * lift);
                }
            }
            else if (clip == Clip.Attack)
            {
                EvaluateAttackContact(time, leg, index, out target, out planted, out lift, out plantIndex);
            }
            else if (clip == Clip.Death)
            {
                float progress = SmoothRange(0.18f, 0.8f, Mathf.Clamp01(time));
                Vector2[] offsets = { new Vector2(-0.14f, 0.08f), new Vector2(0.14f, 0.05f), new Vector2(-0.07f, -0.13f) };
                float[] angles = { 13f, -16f, 18f };
                Vector2 baseRoot = BodyPoint(leg.root, pose);
                Vector2 baseKnee = BodyPoint(leg.knee, pose);
                Vector2 baseContact = BodyPoint(leg.contact, pose);
                Vector2 offset = offsets[index] * progress;
                float angle = angles[index] * progress;
                return new LegPose
                {
                    id = leg.id,
                    root = baseRoot + offset,
                    knee = baseRoot + Rotate(baseKnee - baseRoot, angle) + offset,
                    contact = baseRoot + Rotate(baseContact - baseRoot, angle) + offset,
                    requestedContact = baseRoot + Rotate(baseContact - baseRoot, angle) + offset,
                    planted = false,
                    plantIndex = 0
                };
            }

            return SolveLeg(leg, root, target, planted, plantIndex, lift);
        }

        private static void EvaluateAttackContact(float time, LegDefinition leg, int index, out Vector2 target, out bool planted, out float lift, out int plantIndex)
        {
            float normalized = Mathf.Clamp01(time);
            // The rear support pushes until both forefeet have landed: this is a ground lunge, not a leap.
            float release = index == 2 ? 0.46f : 0.27f;
            float land = index == 2 ? 0.74f : 0.44f;
            float progress = Mathf.InverseLerp(release, land, normalized);
            float travel = SmootherStep(progress);
            target = leg.contact + Vector2.up * (0.42f * travel);
            planted = normalized <= release || normalized >= land;
            plantIndex = normalized >= land ? 1 : 0;
            lift = 16f * progress * progress * (1f - progress) * (1f - progress);
            if (normalized >= land)
            {
                target = leg.contact + Vector2.up * 0.42f;
            }
        }

        private static LegPose SolveLeg(LegDefinition leg, Vector2 root, Vector2 target, bool planted, int plantIndex, float lift)
        {
            Vector2 delta = target - root;
            float requestedDistance = delta.magnitude;
            Vector2 direction = requestedDistance > ReachMargin ? delta / requestedDistance : (leg.contact - leg.root).normalized;
            float distance = Mathf.Clamp(requestedDistance, Mathf.Abs(leg.upperLength - leg.lowerLength) + ReachMargin, leg.upperLength + leg.lowerLength - ReachMargin);
            Vector2 contact = root + direction * distance;
            float along = (leg.upperLength * leg.upperLength - leg.lowerLength * leg.lowerLength + distance * distance) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(0, leg.upperLength * leg.upperLength - along * along));
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            return new LegPose
            {
                id = leg.id,
                root = root,
                knee = root + direction * along + perpendicular * (height * leg.bendSign),
                contact = contact,
                requestedContact = target,
                planted = planted,
                plantIndex = plantIndex,
                lift = lift,
                clampedDistance = Vector2.Distance(contact, target)
            };
        }

        private static ClipExport BakeClip(PreviewRig rig, ClipDefinition definition, string destination, Action<PreviewRig, PoseData> beforeCapture, NumericQa qa)
        {
            int rows = Mathf.CeilToInt((float)definition.count / definition.columns);
            var atlas = new Texture2D(definition.columns * FramePixels, rows * FramePixels, TextureFormat.RGBA32, false, false);
            var empty = new Color32[atlas.width * atlas.height];
            atlas.SetPixels32(empty);
            var result = new ClipExport
            {
                name = definition.clip.ToString().ToLowerInvariant(),
                frameCount = definition.count,
                columns = definition.columns,
                rows = rows,
                loop = definition.loop,
                previewDurationSeconds = definition.previewSeconds,
                frames = new FrameExport[definition.count]
            };
            result.atlas = result.name + "-atlas.png";
            try
            {
                for (int frame = 0; frame < definition.count; frame++)
                {
                    float time = definition.loop ? (float)frame / definition.count : (float)frame / (definition.count - 1);
                    PoseData pose = EvaluatePose(definition.clip, time);
                    rig.ApplyPose(pose);
                    beforeCapture?.Invoke(rig, pose);
                    Texture2D captured = rig.CaptureFrame();
                    try
                    {
                        int x = frame % definition.columns * FramePixels;
                        int y = (rows - 1 - frame / definition.columns) * FramePixels;
                        atlas.SetPixels32(x, y, FramePixels, FramePixels, captured.GetPixels32());
                        result.frames[frame] = new FrameExport { index = frame, rectX = x, rectY = y, pose = pose };
                        qa.maximumVisibleTriangleFlipFraction = Mathf.Max(qa.maximumVisibleTriangleFlipFraction, rig.VisibleTriangleFlipFraction);
                        if (TouchesBorder(captured))
                        {
                            qa.framesTouchingCanvasBorder++;
                        }
                        AccumulateBoneQa(qa, pose);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(captured);
                    }
                }

                atlas.Apply();
                File.WriteAllBytes(Path.Combine(destination, result.atlas), atlas.EncodeToPNG());
                return result;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(atlas);
            }
        }

        private static NumericQa EvaluateNumericQa()
        {
            const int intervals = 240;
            var qa = new NumericQa { walkIntervals = intervals };
            PoseData previous = null;
            for (int sample = 0; sample <= intervals; sample++)
            {
                PoseData pose = EvaluatePose(Clip.Walk, (float)sample / intervals);
                int plantedCount = 0;
                for (int leg = 0; leg < pose.legs.Length; leg++)
                {
                    LegPose current = pose.legs[leg];
                    if (current.planted)
                    {
                        plantedCount++;
                        qa.maximumContactTargetErrorUnits = Mathf.Max(qa.maximumContactTargetErrorUnits, Vector2.Distance(current.contact, current.requestedContact));
                    }
                    if (previous != null && current.planted && previous.legs[leg].planted && current.plantIndex == previous.legs[leg].plantIndex)
                    {
                        qa.maximumStanceSlipUnits = Mathf.Max(qa.maximumStanceSlipUnits, Vector2.Distance(current.contact, previous.legs[leg].contact));
                    }
                }

                qa.minimumPlantedFeet = Mathf.Min(qa.minimumPlantedFeet, plantedCount);
                AccumulateBoneQa(qa, pose);
                previous = pose;
            }

            qa.attackIntervals = intervals;
            previous = null;
            for (int sample = 0; sample <= intervals; sample++)
            {
                PoseData pose = EvaluatePose(Clip.Attack, (float)sample / intervals);
                int plantedCount = 0;
                for (int leg = 0; leg < pose.legs.Length; leg++)
                {
                    LegPose current = pose.legs[leg];
                    if (current.planted)
                    {
                        plantedCount++;
                        qa.maximumContactTargetErrorUnits = Mathf.Max(qa.maximumContactTargetErrorUnits, Vector2.Distance(current.contact, current.requestedContact));
                    }
                    if (previous != null && current.planted && previous.legs[leg].planted && current.plantIndex == previous.legs[leg].plantIndex)
                    {
                        qa.maximumStanceSlipUnits = Mathf.Max(qa.maximumStanceSlipUnits, Vector2.Distance(current.contact, previous.legs[leg].contact));
                    }
                }
                qa.minimumAttackPlantedFeet = Mathf.Min(qa.minimumAttackPlantedFeet, plantedCount);
                AccumulateBoneQa(qa, pose);
                previous = pose;
            }

            return qa;
        }

        private static void AccumulateBoneQa(NumericQa qa, PoseData pose)
        {
            for (int i = 0; i < Legs.Length; i++)
            {
                LegPose leg = pose.legs[i];
                float upperError = Mathf.Abs(Vector2.Distance(leg.root, leg.knee) - Legs[i].upperLength);
                float lowerError = Mathf.Abs(Vector2.Distance(leg.knee, leg.contact) - Legs[i].lowerLength);
                qa.maximumBoneLengthErrorUnits = Mathf.Max(qa.maximumBoneLengthErrorUnits, Mathf.Max(upperError, lowerError));
                qa.maximumReachClampUnits = Mathf.Max(qa.maximumReachClampUnits, leg.clampedDistance);
            }
        }

        private static bool TouchesBorder(Texture2D image)
        {
            Color32[] pixels = image.GetPixels32();
            for (int i = 0; i < FramePixels; i++)
            {
                if (pixels[i].a > 10 || pixels[(FramePixels - 1) * FramePixels + i].a > 10 || pixels[i * FramePixels].a > 10 || pixels[i * FramePixels + FramePixels - 1].a > 10)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Owns an isolated unsaved scene and all transient native objects it creates.</summary>
        public sealed class PreviewRig : IDisposable
        {
            private readonly Texture2D texture;
            private readonly Material material;
            private readonly Mesh[] meshes = new Mesh[4];
            private readonly MeshRenderer[] renderers = new MeshRenderer[4];
            private readonly Vector3[][] deformed = new Vector3[4][];
            private readonly VertexBinding[] bindings;
            private readonly int[][] triangles;
            private readonly bool[][] visibleTriangles;
            private readonly RenderTexture target;
            private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
            private bool disposed;
            public Scene Scene { get; }
            public Camera Camera { get; }
            public Transform AttachmentRoot { get; }
            public float VisibleTriangleFlipFraction { get; private set; }

            internal PreviewRig(string sourcePath)
            {
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException("Sleeper source PNG was not found.", sourcePath);
                }

                Scene = EditorSceneManager.NewPreviewScene();
                try
                {
                    texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                    texture.name = "Sleeper source (transient)";
                    texture.hideFlags = HideFlags.HideAndDontSave;
                    if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(sourcePath), false))
                    {
                        throw new InvalidOperationException("Could not decode Sleeper PNG.");
                    }
                    if (texture.width != SourceWidth || texture.height != SourceHeight)
                    {
                        throw new InvalidOperationException("Sleeper rig requires the registered 1024 x 1536 source, not an automatically resized image.");
                    }

                    ValidateAlpha(texture);
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    Shader shader = Shader.Find("Universal Render Pipeline/2D/Mesh2D-Unlit-Default");
                    if (shader == null)
                    {
                        throw new InvalidOperationException("The project's URP Mesh2D unlit shader is unavailable.");
                    }
                    material = new Material(shader) { name = "Sleeper offline unlit", hideFlags = HideFlags.HideAndDontSave };
                    material.SetTexture("_MainTex", texture);
                    material.SetColor("_White", Color.white);
                    BuildGeometry(texture, out bindings, out triangles, out visibleTriangles, out Vector2[] uv);
                    for (int part = 0; part < meshes.Length; part++)
                    {
                        meshes[part] = new Mesh { name = "Sleeper source partition " + part, hideFlags = HideFlags.HideAndDontSave };
                        meshes[part].MarkDynamic();
                        deformed[part] = new Vector3[bindings.Length];
                        meshes[part].vertices = deformed[part];
                        meshes[part].uv = uv;
                        meshes[part].triangles = triangles[part];
                        var obj = CreateObject("Sleeper source partition " + part, Scene);
                        obj.AddComponent<MeshFilter>().sharedMesh = meshes[part];
                        renderers[part] = obj.AddComponent<MeshRenderer>();
                        renderers[part].sharedMaterial = material;
                        renderers[part].shadowCastingMode = ShadowCastingMode.Off;
                        renderers[part].receiveShadows = false;
                        renderers[part].sortingOrder = part;
                    }

                    AttachmentRoot = CreateObject("Sleeper optional head attachment", Scene).transform;
                    GameObject cameraObject = CreateObject("Sleeper offline camera", Scene);
                    Camera = cameraObject.AddComponent<Camera>();
                    Camera.scene = Scene;
                    Camera.orthographic = true;
                    Camera.orthographicSize = CanvasUnits * 0.5f;
                    Camera.aspect = 1f;
                    Camera.transparencySortMode = TransparencySortMode.Orthographic;
                    Camera.nearClipPlane = 0.1f;
                    Camera.farClipPlane = 30f;
                    Camera.clearFlags = CameraClearFlags.SolidColor;
                    Camera.backgroundColor = Color.clear;
                    Camera.allowHDR = false;
                    Camera.allowMSAA = false;
                    cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                    target = new RenderTexture(FramePixels, FramePixels, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
                    {
                        name = "Sleeper offline capture",
                        hideFlags = HideFlags.HideAndDontSave,
                        antiAliasing = 1
                    };
                    target.Create();
                    ApplyPose(EvaluatePose(Clip.Sleep, 0));
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            public void ApplyPose(PoseData pose)
            {
                ThrowIfDisposed();
                if (pose == null || pose.legs == null || pose.legs.Length != Legs.Length)
                {
                    throw new ArgumentException("A complete three-support pose is required.", nameof(pose));
                }

                int visible = 0;
                int flipped = 0;
                for (int part = 0; part < meshes.Length; part++)
                {
                    for (int vertex = 0; vertex < bindings.Length; vertex++)
                    {
                        deformed[part][vertex] = SkinVertex(bindings[vertex], pose, part);
                    }
                    meshes[part].vertices = deformed[part];
                    meshes[part].RecalculateBounds();
                    properties.Clear();
                    properties.SetColor("_White", pose.tint);
                    renderers[part].SetPropertyBlock(properties);
                    for (int triangle = 0; triangle < visibleTriangles[part].Length; triangle++)
                    {
                        if (!visibleTriangles[part][triangle])
                        {
                            continue;
                        }
                        visible++;
                        int offset = triangle * 3;
                        Vector2 a = deformed[part][triangles[part][offset]];
                        Vector2 b = deformed[part][triangles[part][offset + 1]];
                        Vector2 c = deformed[part][triangles[part][offset + 2]];
                        if (Cross(b - a, c - a) >= 0)
                        {
                            flipped++;
                        }
                    }
                }
                VisibleTriangleFlipFraction = visible == 0 ? 0 : (float)flipped / visible;
                Camera.transform.position = new Vector3(pose.rootPosition.x, pose.rootPosition.y, -10);
                AttachmentRoot.position = pose.rootPosition + pose.bodyOffset;
                AttachmentRoot.rotation = Quaternion.Euler(0, 0, pose.bodyAngle);
            }

            /// <summary>Returns a caller-owned straight-alpha sRGB PNG-ready texture.</summary>
            public Texture2D CaptureFrame()
            {
                ThrowIfDisposed();
                RenderTexture previous = RenderTexture.active;
                Texture2D image = null;
                try
                {
                    RenderPipeline.SubmitRenderRequest(Camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    RenderTexture.active = target;
                    image = new Texture2D(FramePixels, FramePixels, TextureFormat.RGBA32, false, true);
                    image.ReadPixels(new Rect(0, 0, FramePixels, FramePixels), 0, 0);
                    image.Apply();
                    ConvertCaptureToStraightAlpha(image);
                    return image;
                }
                catch
                {
                    if (image != null)
                    {
                        UnityEngine.Object.DestroyImmediate(image);
                    }
                    throw;
                }
                finally
                {
                    RenderTexture.active = previous;
                }
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }
                disposed = true;
                if (Scene.IsValid())
                {
                    EditorSceneManager.ClosePreviewScene(Scene);
                }
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                for (int i = 0; i < meshes.Length; i++)
                {
                    if (meshes[i] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(meshes[i]);
                    }
                }
                if (material != null)
                {
                    UnityEngine.Object.DestroyImmediate(material);
                }
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }

            private void ThrowIfDisposed()
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(PreviewRig));
                }
            }
        }

        private static void BuildGeometry(Texture2D texture, out VertexBinding[] bindings, out int[][] triangles, out bool[][] visibleTriangles, out Vector2[] uv)
        {
            int columns = Mathf.CeilToInt((float)SourceWidth / GridSpacing) + 1;
            int rows = Mathf.CeilToInt((float)SourceHeight / GridSpacing) + 1;
            bindings = new VertexBinding[columns * rows];
            uv = new Vector2[bindings.Length];
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int index = y * columns + x;
                    Vector2 source = new Vector2(Mathf.Min(SourceWidth, x * GridSpacing), Mathf.Min(SourceHeight, y * GridSpacing));
                    bindings[index] = BindVertex(source);
                    uv[index] = new Vector2(source.x / SourceWidth, 1f - source.y / SourceHeight);
                }
            }

            var groups = new List<int>[4];
            var visibility = new List<bool>[4];
            for (int i = 0; i < groups.Length; i++)
            {
                groups[i] = new List<int>();
                visibility[i] = new List<bool>();
            }
            for (int y = 0; y < rows - 1; y++)
            {
                for (int x = 0; x < columns - 1; x++)
                {
                    int topLeft = y * columns + x;
                    AddTriangle(topLeft, topLeft + 1, topLeft + columns, bindings, texture, groups, visibility);
                    AddTriangle(topLeft + 1, topLeft + columns + 1, topLeft + columns, bindings, texture, groups, visibility);
                }
            }
            triangles = new int[groups.Length][];
            visibleTriangles = new bool[groups.Length][];
            for (int i = 0; i < groups.Length; i++)
            {
                triangles[i] = groups[i].ToArray();
                visibleTriangles[i] = visibility[i].ToArray();
            }
        }

        private static void AddTriangle(int a, int b, int c, VertexBinding[] bindings, Texture2D texture, List<int>[] groups, List<bool>[] visibility)
        {
            Vector2 center = (bindings[a].source + bindings[b].source + bindings[c].source) / 3f;
            int region = RegionAt(center);
            groups[region].Add(a);
            groups[region].Add(b);
            groups[region].Add(c);
            int px = Mathf.Clamp(Mathf.RoundToInt(center.x), 0, SourceWidth - 1);
            int py = Mathf.Clamp(SourceHeight - 1 - Mathf.RoundToInt(center.y), 0, SourceHeight - 1);
            visibility[region].Add(texture.GetPixel(px, py).a >= 0.2f);
        }

        private static VertexBinding BindVertex(Vector2 pixel)
        {
            var result = new VertexBinding { source = pixel, bodyWeight = 1, region = RegionAt(pixel) };
            if (InsideHead(pixel) || InsidePolygon(pixel, BodyRegion))
            {
                return result;
            }
            float total = 0;
            for (int i = 0; i < Legs.Length; i++)
            {
                LegDefinition leg = Legs[i];
                float regionWeight = RegionWeight(pixel, leg.region);
                float rootFade = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(12, 110, Vector2.Distance(pixel, leg.rootPixel)));
                float influence = regionWeight * rootFade;
                float upperDistance = DistanceToSegment(pixel, leg.rootPixel, leg.kneePixel);
                float lowerDistance = DistanceToSegment(pixel, leg.kneePixel, leg.contactPixel);
                float lower = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-28, 28, upperDistance - lowerDistance));
                result.upperWeights[i] = influence * (1f - lower);
                result.lowerWeights[i] = influence * lower;
                total += influence;
            }
            if (total > 1)
            {
                result.upperWeights /= total;
                result.lowerWeights /= total;
                total = 1;
            }
            result.bodyWeight = 1f - total;
            return result;
        }

        private static Vector3 SkinVertex(VertexBinding vertex, PoseData pose, int partition)
        {
            Vector2 point = SourcePoint(vertex.source);
            Vector2 body = BodyPoint(BreathePoint(point, vertex.source, pose.breathing), pose);
            Vector2 result = body * vertex.bodyWeight;
            Vector2 selectedLimb = Vector2.zero;
            float selectedWeight = 0;
            for (int i = 0; i < Legs.Length; i++)
            {
                LegPose current = pose.legs[i];
                Vector2 upper = RigidPoint(point, Legs[i].root, Legs[i].knee, current.root, current.knee);
                Vector2 lower = RigidPoint(point, Legs[i].knee, Legs[i].contact, current.knee, current.contact);
                Vector2 contribution = upper * vertex.upperWeights[i] + lower * vertex.lowerWeights[i];
                result += contribution;
                if (i == partition - 1)
                {
                    selectedLimb = contribution;
                    selectedWeight = vertex.upperWeights[i] + vertex.lowerWeights[i];
                }
            }
            if (pose.detachWeight > 0)
            {
                Vector2 separated = body;
                if (partition > 0)
                {
                    int leg = partition - 1;
                    if (selectedWeight > 0.0001f)
                    {
                        separated = selectedLimb / selectedWeight;
                    }
                    else
                    {
                        separated = RigidPoint(point, Legs[leg].root, Legs[leg].knee, pose.legs[leg].root, pose.legs[leg].knee);
                    }
                }
                result = Vector2.Lerp(result, separated, pose.detachWeight);
            }
            return new Vector3(result.x, result.y, 0);
        }

        private static Vector2 RigidPoint(Vector2 point, Vector2 sourceRoot, Vector2 sourceEnd, Vector2 targetRoot, Vector2 targetEnd)
        {
            float angle = Vector2.SignedAngle(sourceEnd - sourceRoot, targetEnd - targetRoot);
            return targetRoot + Rotate(point - sourceRoot, angle);
        }

        private static int RegionAt(Vector2 pixel)
        {
            if (InsideHead(pixel) || InsidePolygon(pixel, BodyRegion))
            {
                return 0;
            }
            for (int i = 0; i < Legs.Length; i++)
            {
                if (InsidePolygon(pixel, Legs[i].region))
                {
                    return i + 1;
                }
            }
            return 0;
        }

        private static bool InsideHead(Vector2 pixel)
        {
            return InsidePolygon(pixel, HeadRegion);
        }

        private static Vector2 BreathePoint(Vector2 point, Vector2 source, float breathing)
        {
            // Move only the interior membrane. The outer silhouette, sockets and skeleton stay fixed.
            Vector2 centered = source - new Vector2(512, 740);
            float radius = new Vector2(centered.x / 220f, centered.y / 260f).magnitude;
            float weight = 1f - Mathf.SmoothStep(0, 1, radius);
            return point + new Vector2(centered.x * 0.007f, -centered.y * 0.004f) * (UnitsPerSourcePixel * breathing * weight);
        }

        private static float RegionWeight(Vector2 point, Vector2[] polygon)
        {
            if (InsidePolygon(point, polygon))
            {
                return 1;
            }
            float distance = float.PositiveInfinity;
            for (int i = 0; i < polygon.Length; i++)
            {
                distance = Mathf.Min(distance, DistanceToSegment(point, polygon[i], polygon[(i + 1) % polygon.Length]));
            }
            return 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, 26, distance));
        }

        private static bool InsidePolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, previous = polygon.Length - 1; i < polygon.Length; previous = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[previous];
                if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 delta = end - start;
            float amount = Mathf.Clamp01(Vector2.Dot(point - start, delta) / Mathf.Max(0.0001f, delta.sqrMagnitude));
            return Vector2.Distance(point, start + delta * amount);
        }

        private static Vector2 BodyPoint(Vector2 local, PoseData pose)
        {
            return pose.rootPosition + pose.bodyOffset + Rotate(local, pose.bodyAngle);
        }

        private static Vector2 SourcePoint(Vector2 pixel)
        {
            return new Vector2(pixel.x - SourceCenter.x, SourceCenter.y - pixel.y) * UnitsPerSourcePixel;
        }

        private static Vector2 Rotate(Vector2 point, float degrees)
        {
            float angle = degrees * Mathf.Deg2Rad;
            float sine = Mathf.Sin(angle);
            float cosine = Mathf.Cos(angle);
            return new Vector2(point.x * cosine - point.y * sine, point.x * sine + point.y * cosine);
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private static float SmoothRange(float start, float end, float value)
        {
            return SmootherStep(Mathf.InverseLerp(start, end, value));
        }

        private static float SmootherStep(float value)
        {
            float x = Mathf.Clamp01(value);
            return x * x * x * (x * (x * 6f - 15f) + 10f);
        }

        private static float Pulse(float start, float end, float value)
        {
            if (value <= start || value >= end)
            {
                return 0;
            }
            float x = Mathf.InverseLerp(start, end, value);
            float pulse = Mathf.Sin(x * Mathf.PI);
            return pulse * pulse;
        }

        private static LegDefinition[] CreateLegDefinitions()
        {
            Vector2[] left =
            {
                new Vector2(180, 40), new Vector2(360, 50), new Vector2(355, 215), new Vector2(220, 360),
                new Vector2(290, 380), new Vector2(400, 380), new Vector2(435, 485), new Vector2(400, 610),
                new Vector2(280, 600), new Vector2(195, 570), new Vector2(35, 515), new Vector2(0, 440), new Vector2(85, 280)
            };
            Vector2[] right = new Vector2[left.Length];
            for (int i = 0; i < left.Length; i++)
            {
                right[i] = new Vector2(SourceWidth - left[i].x, left[i].y);
            }
            Vector2[] rear =
            {
                new Vector2(240, 910), new Vector2(400, 890), new Vector2(450, 1015), new Vector2(585, 1130),
                new Vector2(620, 1240), new Vector2(570, 1400), new Vector2(455, 1460), new Vector2(330, 1420),
                new Vector2(300, 1310), new Vector2(330, 1200), new Vector2(220, 1060)
            };
            var result = new[]
            {
                DefineLeg("left", new Vector2(330, 470), new Vector2(65, 460), new Vector2(330, 170), 0, left),
                DefineLeg("right", new Vector2(695, 470), new Vector2(953, 460), new Vector2(690, 170), 2f / 3f, right),
                DefineLeg("rear", new Vector2(350, 980), new Vector2(530, 1200), new Vector2(350, 1250), 1f / 3f, rear)
            };
            return result;
        }

        private static LegDefinition DefineLeg(string id, Vector2 root, Vector2 knee, Vector2 contact, float offset, Vector2[] region)
        {
            Vector2 rootUnits = SourcePoint(root);
            Vector2 kneeUnits = SourcePoint(knee);
            Vector2 contactUnits = SourcePoint(contact);
            return new LegDefinition
            {
                id = id,
                rootPixel = root,
                kneePixel = knee,
                contactPixel = contact,
                root = rootUnits,
                knee = kneeUnits,
                contact = contactUnits,
                upperLength = Vector2.Distance(rootUnits, kneeUnits),
                lowerLength = Vector2.Distance(kneeUnits, contactUnits),
                bendSign = Mathf.Sign(Cross(contactUnits - rootUnits, kneeUnits - rootUnits)),
                swingOffset = offset,
                region = region
            };
        }

        private static SourceLeg[] ExportSourceLegs()
        {
            var result = new SourceLeg[Legs.Length];
            for (int i = 0; i < Legs.Length; i++)
            {
                result[i] = new SourceLeg
                {
                    id = Legs[i].id,
                    rootTopLeft = Legs[i].rootPixel,
                    kneeTopLeft = Legs[i].kneePixel,
                    contactTopLeft = Legs[i].contactPixel,
                    upperLengthUnits = Legs[i].upperLength,
                    lowerLengthUnits = Legs[i].lowerLength
                };
            }
            return result;
        }

        private static void ValidateAlpha(Texture2D source)
        {
            bool transparent = false;
            bool visible = false;
            foreach (Color32 pixel in source.GetPixels32())
            {
                transparent |= pixel.a == 0;
                visible |= pixel.a >= 128;
            }
            if (!transparent || !visible)
            {
                throw new InvalidOperationException("Sleeper bake requires a prepared RGBA cutout with visible pixels and alpha-zero background. It does not remove or repaint a backdrop.");
            }
        }

        private static void ConvertCaptureToStraightAlpha(Texture2D image)
        {
            Color[] pixels = image.GetPixels();
            bool linearProject = QualitySettings.activeColorSpace == ColorSpace.Linear;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];
                if (pixel.a <= 0)
                {
                    pixels[i] = Color.clear;
                    continue;
                }
                pixel.r = Mathf.Clamp01(pixel.r / pixel.a);
                pixel.g = Mathf.Clamp01(pixel.g / pixel.a);
                pixel.b = Mathf.Clamp01(pixel.b / pixel.a);
                if (linearProject)
                {
                    pixel.r = Mathf.LinearToGammaSpace(pixel.r);
                    pixel.g = Mathf.LinearToGammaSpace(pixel.g);
                    pixel.b = Mathf.LinearToGammaSpace(pixel.b);
                }
                pixels[i] = pixel;
            }
            image.SetPixels(pixels);
            image.Apply();
        }

        private static GameObject CreateObject(string name, Scene scene)
        {
            var obj = new GameObject(name);
            obj.hideFlags = HideFlags.HideAndDontSave;
            SceneManager.MoveGameObjectToScene(obj, scene);
            return obj;
        }

        private static string FileHash(string path)
        {
            using (SHA256 hash = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}
