using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift.Editor
{
    /// <summary>Bakes height classes into standard URP settings; no per-frame scene scan or runtime reflection.</summary>
    public static class EnvironmentHeightAuthoring
    {
        public static void Apply(EnvironmentHeight2D height)
        {
            string layer = EnvironmentShadowLayers.Surface(height.heightClass);
            if (SortingLayer.NameToID(layer) == 0)
            {
                throw new InvalidOperationException("Missing environment shadow sorting layers. See ENVIRONMENT_SHADOWS.md.");
            }

            foreach (var renderer in height.GetComponentsInChildren<SpriteRenderer>(true))
            {
                // Nested height metadata owns its own surfaces, e.g. contents above a worktop.
                if (renderer.GetComponentInParent<EnvironmentHeight2D>() == height)
                {
                    renderer.sortingLayerName = layer;
                    EditorUtility.SetDirty(renderer);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
            }

            foreach (var group in height.GetComponentsInChildren<UnityEngine.Rendering.SortingGroup>(true))
            {
                if (group.GetComponentInParent<EnvironmentHeight2D>() == height)
                {
                    group.sortingLayerName = layer;
                    EditorUtility.SetDirty(group);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(group);
                }
            }

            var targets = new List<int> { SortingLayer.NameToID(EnvironmentShadowLayers.Floor), 0 };
            foreach (EnvironmentHeightClass receiver in Enum.GetValues(typeof(EnvironmentHeightClass)))
            {
                if (EnvironmentShadowLayers.CanCastOnto(height.heightClass, receiver))
                {
                    targets.Add(SortingLayer.NameToID(EnvironmentShadowLayers.Surface(receiver)));
                }
            }

            foreach (var caster in height.GetComponentsInChildren<ShadowCaster2D>(true))
            {
                if (caster.GetComponentInParent<EnvironmentHeight2D>() != height)
                {
                    continue;
                }

                // Unity 6000.6 exposes Light2D targets publicly, but caster targets only through serialization.
                var serialized = new SerializedObject(caster);
                var layers = serialized.FindProperty("m_ApplyToSortingLayers");
                if (layers == null)
                {
                    throw new InvalidOperationException("URP ShadowCaster2D serialization changed.");
                }

                layers.arraySize = targets.Count;
                for (int i = 0; i < targets.Count; i++)
                {
                    layers.GetArrayElementAtIndex(i).intValue = targets[i];
                }

                serialized.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(caster);
            }

            BakeContours(height);
        }

        private static void BakeContours(EnvironmentHeight2D height)
        {
            var contours = new List<FloorShadowContour2D>();
            foreach (var caster in height.GetComponentsInChildren<ShadowCaster2D>(true))
            {
                if (caster.GetComponentInParent<EnvironmentHeight2D>() != height)
                {
                    continue;
                }

                var renderer = caster.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null)
                {
                    foreach (var points in SpriteOutlines(renderer.sprite))
                    {
                        Vector2 scale = Vector2.one;
                        if (renderer.drawMode != SpriteDrawMode.Simple)
                        {
                            scale = renderer.size / (Vector2)renderer.sprite.bounds.size;
                        }

                        scale.x *= renderer.flipX ? -1 : 1;
                        scale.y *= renderer.flipY ? -1 : 1;
                        var path = new Vector2[points.Length];
                        for (int i = 0; i < points.Length; i++)
                        {
                            path[i] = Vector2.Scale(points[i], scale);
                        }

                        contours.Add(new FloorShadowContour2D { caster = caster, frame = renderer.transform, points = path });
                    }
                }
                else if (caster.shapePath != null && caster.shapePath.Length >= 3)
                {
                    var path = new Vector2[caster.shapePath.Length];
                    for (int i = 0; i < path.Length; i++)
                    {
                        path[i] = caster.shapePath[i];
                    }

                    contours.Add(new FloorShadowContour2D { caster = caster, frame = caster.transform, points = path });
                }
                else
                {
                    throw new InvalidOperationException("Caster needs an offline outline: " + caster.name);
                }
            }

            height.SetBakedContours(contours.ToArray());
            EditorUtility.SetDirty(height);
            PrefabUtility.RecordPrefabInstancePropertyModifications(height);
        }

        private static List<Vector2[]> SpriteOutlines(Sprite sprite)
        {
            // Physics shapes can be simplified/inset independently. URP's sprite provider uses the render mesh.
            var vertices = sprite.vertices;
            var triangles = sprite.triangles;
            var counts = new Dictionary<ulong, int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                CountEdge(counts, triangles[i], triangles[i + 1]);
                CountEdge(counts, triangles[i + 1], triangles[i + 2]);
                CountEdge(counts, triangles[i + 2], triangles[i]);
            }

            var edges = new List<Vector2Int>();
            foreach (var edge in counts)
            {
                if (edge.Value == 1)
                {
                    edges.Add(new Vector2Int((int)(edge.Key >> 32), (int)(edge.Key & uint.MaxValue)));
                }
            }

            var outlines = new List<Vector2[]>();
            while (edges.Count > 0)
            {
                var first = edges[edges.Count - 1];
                edges.RemoveAt(edges.Count - 1);
                var path = new List<Vector2> { vertices[first.x] };
                int current = first.y;
                while (current != first.x)
                {
                    path.Add(vertices[current]);
                    int next = -1;
                    for (int i = 0; i < edges.Count; i++)
                    {
                        if (edges[i].x == current || edges[i].y == current)
                        {
                            next = i;
                            break;
                        }
                    }

                    if (next < 0)
                    {
                        throw new InvalidOperationException("Sprite shadow outline is not closed: " + sprite.name);
                    }

                    var edge = edges[next];
                    current = edge.x == current ? edge.y : edge.x;
                    edges.RemoveAt(next);
                }

                if (path.Count >= 3)
                {
                    outlines.Add(path.ToArray());
                }
            }

            return outlines;
        }

        private static void CountEdge(Dictionary<ulong, int> counts, int a, int b)
        {
            ulong key = ((ulong)(uint)Mathf.Min(a, b) << 32) | (uint)Mathf.Max(a, b);
            counts.TryGetValue(key, out int count);
            counts[key] = count + 1;
        }
    }

    [CustomEditor(typeof(EnvironmentHeight2D)), CanEditMultipleObjects]
    public sealed class EnvironmentHeightEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            bool changed = EditorGUI.EndChangeCheck();
            bool apply = GUILayout.Button("Apply height to surfaces and shadows");
            if (changed || apply)
            {
                foreach (EnvironmentHeight2D height in targets)
                {
                    Undo.RecordObjects(height.GetComponentsInChildren<Component>(true), "Set environment shadow height");
                    EnvironmentHeightAuthoring.Apply(height);
                }
            }

            EditorGUILayout.HelpBox("Floor Shadow Profile sets lengths by height class. Override Floor Length tunes this prefab/instance; Infinite Floor Shadow is for walls and doors. Apply also rebakes outlines after changing the sprite. FiniteFloorLight2D renders floor shadows in Play; other surfaces keep URP height classes.", MessageType.Info);
        }
    }
}
