using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrganizeMaterials
{
    private const string Root = "Assets/EmergencyShift/Materials/";
    private static readonly Dictionary<string, string> Families = new Dictionary<string, string>
    {
        { "FlatLit.mat", "Shared" },
        { "FlatUnlit.mat", "Shared" },
        { "Floor-Corridor-Vinyl.mat", "Environment/Floors" },
        { "Floor-Room-cement-slabs.mat", "Environment/Floors" },
        { "Floor-Room-ceramic-small-sage.mat", "Environment/Floors" },
        { "Floor-Room-checker-gray-ivory.mat", "Environment/Floors" },
        { "Floor-Room-Porcelain.mat", "Environment/Floors" },
        { "FloorWorldLit.shader", "Environment/Floors" },
        { "HospitalLampHousing.mat", "Environment/Lighting" },
        { "HospitalLampHousingMask.asset", "Environment/Lighting" },
        { "HospitalTubeGlow.mat", "Environment/Lighting" },
        { "TubeGlow.shader", "Environment/Lighting" },
        { "PropEmission.shader", "Environment/Lighting" },
        { "emergency-beacon-emission.mat", "Environment/Lighting" },
        { "lab-desk-lamp-emission.mat", "Environment/Lighting" },
        { "working-laptop-emission.mat", "Environment/Lighting" },
        { "CreatureSilhouetteShadow.mat", "Characters" },
        { "CreatureSilhouetteShadow.shader", "Characters" },
        { "LocalizedEyeBurn.shader", "Characters" },
        { "stalker-eye-burn.mat", "Characters" },
        { "MuzzlePlume.mat", "Effects" },
        { "MuzzlePlume.shader", "Effects" },
        { "TransientGlow.mat", "Effects" },
        { "TransientGlow.shader", "Effects" },
        { "NoFriction.physicsMaterial2D", "Physics" },
        { "FurnitureLightStudy.mat", "Development" },
        { "FurnitureLightStudy.shader", "Development" }
    };

    public static object Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Stop Play and finish compilation before organizing assets.");
        }

        var moves = Families.Select(pair => new
        {
            from = Root + pair.Key,
            to = Root + pair.Value + "/" + pair.Key
        }).ToArray();
        foreach (var move in moves)
        {
            if (!File.Exists(move.from) || File.Exists(move.to))
            {
                throw new InvalidOperationException("Expected one source and an empty destination: " + move.from);
            }
        }

        string[] loose = Directory.GetFiles(Root).Where(path => !path.EndsWith(".meta", StringComparison.Ordinal)).ToArray();
        if (loose.Length != Families.Count)
        {
            throw new InvalidOperationException("The root inventory changed; review the move plan before applying it.");
        }

        var identities = moves.Select(move => new
        {
            move.from,
            move.to,
            guid = AssetDatabase.AssetPathToGUID(move.from),
            contentSha256 = Hash(move.from),
            metaSha256 = Hash(move.from + ".meta")
        }).ToArray();
        Dictionary<string, string> serializedBefore = CaptureSerializedFiles();
        Dictionary<string, string> dependenciesBefore = CaptureDependencies();
        string liveBefore = CaptureOpenScenes();
        string reportFolder = "work/material-organization-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(reportFolder);
        File.WriteAllText(reportFolder + "/before.json", JsonConvert.SerializeObject(new
        {
            identities,
            serializedBefore,
            dependenciesBefore,
            liveBefore
        }, Formatting.Indented));

        foreach (string family in Families.Values.Distinct())
        {
            EnsureFolder(Root.TrimEnd('/') + "/" + family);
        }

        var completed = new List<KeyValuePair<string, string>>();
        try
        {
            foreach (var move in moves)
            {
                string error = AssetDatabase.MoveAsset(move.from, move.to);
                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException(error);
                }

                completed.Add(new KeyValuePair<string, string>(move.from, move.to));
            }

            AssetDatabase.Refresh();
            foreach (var identity in identities)
            {
                if (AssetDatabase.AssetPathToGUID(identity.to) != identity.guid ||
                    Hash(identity.to) != identity.contentSha256 || Hash(identity.to + ".meta") != identity.metaSha256)
                {
                    throw new InvalidOperationException("Asset content, meta or GUID changed: " + identity.to);
                }
            }

            RequireEqual(serializedBefore, CaptureSerializedFiles(), "saved serialized asset bytes");
            RequireEqual(dependenciesBefore, CaptureDependencies(), "saved scene/prefab/settings dependency identities");
            if (liveBefore != CaptureOpenScenes())
            {
                throw new InvalidOperationException("Open scene hierarchy, transforms, material links or dirty state changed.");
            }
        }
        catch
        {
            foreach (var move in completed.AsEnumerable().Reverse())
            {
                string error = AssetDatabase.MoveAsset(move.Value, move.Key);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError("Asset move rollback failed: " + error);
                }
            }

            throw;
        }

        var result = new
        {
            moved = identities.Length,
            identities,
            serializedAssetsCompared = serializedBefore.Count,
            dependencyGraphsCompared = dependenciesBefore.Count,
            contentMetaAndGuidsUnchanged = true,
            savedAssetBytesUnchanged = true,
            dependencyIdentitiesUnchanged = true,
            openScenesUnchanged = true,
            scenesSaved = false,
            reportFolder
        };
        File.WriteAllText(reportFolder + "/result.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    private static Dictionary<string, string> CaptureSerializedFiles()
    {
        return AssetDatabase.GetAllAssetPaths().Where(path => path.StartsWith("Assets/", StringComparison.Ordinal) &&
                new[] { ".unity", ".prefab", ".asset", ".mat", ".physicsMaterial2D" }.Contains(Path.GetExtension(path)))
            .ToDictionary(path => AssetDatabase.AssetPathToGUID(path), Hash);
    }

    public static object Verify()
    {
        var errors = new List<string>();
        int materials = 0;
        int shaders = 0;
        foreach (var pair in Families)
        {
            string path = Root + pair.Value + "/" + pair.Key;
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
            {
                errors.Add("Cannot load " + path);
                continue;
            }

            if (asset is Material material)
            {
                materials++;
                if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                {
                    errors.Add("Missing material shader: " + path);
                }
            }

            if (asset is Shader shader)
            {
                shaders++;
                errors.AddRange(ShaderUtil.GetShaderMessages(shader).Where(message => message.severity.ToString() == "Error")
                    .Select(message => path + ": " + message.message));
            }
        }

        string[] looseFiles = Directory.GetFiles(Root).Where(path => !path.EndsWith(".meta", StringComparison.Ordinal)).ToArray();
        if (looseFiles.Length != 0)
        {
            errors.AddRange(looseFiles.Select(path => "Loose root asset: " + path));
        }

        if (errors.Count != 0)
        {
            throw new InvalidOperationException(string.Join("\n", errors));
        }

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(Root.TrimEnd('/'));
        EditorGUIUtility.PingObject(Selection.activeObject);
        return new { assetsLoaded = Families.Count, materials, shaders, looseFiles = looseFiles.Length, errors };
    }

    private static Dictionary<string, string> CaptureDependencies()
    {
        return AssetDatabase.GetAllAssetPaths().Where(path => path.StartsWith("Assets/", StringComparison.Ordinal) &&
                new[] { ".unity", ".prefab", ".asset" }.Contains(Path.GetExtension(path)))
            .ToDictionary(path => AssetDatabase.AssetPathToGUID(path), path => string.Join("\n", AssetDatabase.GetDependencies(path, true)
                .Select(dependency => AssetDatabase.AssetPathToGUID(dependency)).Distinct().OrderBy(guid => guid)));
    }

    private static string CaptureOpenScenes()
    {
        var scenes = new List<object>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            scenes.Add(new
            {
                scene.path,
                scene.isDirty,
                active = scene == SceneManager.GetActiveScene(),
                objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Select(transform => new
                    {
                        id = transform.GetEntityId().ToString(),
                        parent = transform.parent == null ? "" : transform.parent.GetEntityId().ToString(),
                        sibling = transform.GetSiblingIndex(),
                        transform.name,
                        transform.gameObject.activeSelf,
                        position = transform.localPosition.ToString("R"),
                        rotation = transform.localRotation.ToString("R"),
                        scale = transform.localScale.ToString("R"),
                        materials = transform.GetComponents<Renderer>().Select(renderer => renderer.sharedMaterials
                            .Select(material => material != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out string guid, out long fileId)
                                ? guid + ":" + fileId : "null").ToArray()).ToArray()
                    }).ToArray()
            });
        }

        return JsonConvert.SerializeObject(scenes);
    }

    private static void RequireEqual(Dictionary<string, string> expected, Dictionary<string, string> actual, string label)
    {
        if (expected.Count != actual.Count || expected.Any(pair => !actual.TryGetValue(pair.Key, out string value) || value != pair.Value))
        {
            throw new InvalidOperationException("Changed " + label + ".");
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        string guid = AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        if (string.IsNullOrEmpty(guid))
        {
            throw new InvalidOperationException("Could not create asset folder " + path);
        }
    }

    private static string Hash(string path)
    {
        using (SHA256 sha = SHA256.Create())
        {
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
    }
}
