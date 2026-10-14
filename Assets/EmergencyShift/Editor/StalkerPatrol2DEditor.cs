using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace EmergencyShift.Editor
{
    /// <summary>Границы патруля редактируются на сцене; данные остаются на конкретном враге.</summary>
    [CustomEditor(typeof(StalkerPatrol2D)), CanEditMultipleObjects]
    public sealed class StalkerPatrol2DEditor : UnityEditor.Editor
    {
        private readonly BoxBoundsHandle bounds = new BoxBoundsHandle();
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Patrol Enabled выключен — ждать у места появления. Включён — патрулировать жёлтый прямоугольник. Голубой Habitat Radius отвечает за обнаружение игрока. Область должна вмещать коллайдер и стартовую позицию.", MessageType.Info);
            if (targets.Length == 1)
            {
                var patrol = (StalkerPatrol2D)target;
                var body = patrol.GetComponent<CircleCollider2D>();
                float padding = body.radius * Mathf.Max(Mathf.Abs(patrol.transform.lossyScale.x), Mathf.Abs(patrol.transform.lossyScale.y)) + .04f;
                Rect area = patrol.Area;
                if (!area.Contains(patrol.AreaOrigin - Vector2.one * padding) || !area.Contains(patrol.AreaOrigin + Vector2.one * padding))
                    EditorGUILayout.HelpBox("Стартовая позиция с коллайдером не помещается в область. Увеличьте Area Size или поправьте Area Offset. В тесной области враг безопасно остаётся на месте.", MessageType.Warning);
            }
        }
        private void OnSceneGUI()
        {
            if (Application.isPlaying || targets.Length != 1) return;
            var patrol = (StalkerPatrol2D)target;
            Rect area = patrol.Area;
            bounds.center = new Vector3(area.center.x, area.center.y, patrol.transform.position.z);
            bounds.size = new Vector3(area.width, area.height, 0);
            bounds.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
            EditorGUI.BeginChangeCheck();
            using (new Handles.DrawingScope(new Color(.95f, .72f, .2f))) bounds.DrawHandle();
            if (!EditorGUI.EndChangeCheck()) return;
            Undo.RecordObject(patrol, "Resize patrol area");
            serializedObject.Update();
            serializedObject.FindProperty("areaOffset").vector2Value = (Vector2)bounds.center - patrol.AreaOrigin;
            serializedObject.FindProperty("areaSize").vector2Value = Vector2.Max((Vector2)bounds.size, Vector2.one * .2f);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
