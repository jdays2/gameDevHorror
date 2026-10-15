using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace EmergencyShift.Editor
{
    /// <summary>Границы патруля редактируются на сцене; данные остаются на конкретном враге.</summary>
    [CustomEditor(typeof(EnemyPatrol2D)), CanEditMultipleObjects]
    public sealed class EnemyPatrol2DEditor : UnityEditor.Editor
    {
        private readonly BoxBoundsHandle bounds = new BoxBoundsHandle();
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Patrol Enabled — иногда передвигаться до обнаружения игрока. Use Patrol Area ограничивает мирные точки жёлтым прямоугольником; без флага — короткие шаги от текущего места. Detection Radius и прямая видимость нужны только для первого обнаружения. Возврата домой нет.", MessageType.Info);
            if (targets.Length == 1 && ((EnemyPatrol2D)target).UsePatrolArea)
            {
                var patrol = (EnemyPatrol2D)target;
                var body = patrol.GetComponent<CircleCollider2D>();
                float padding = body.radius * Mathf.Max(Mathf.Abs(patrol.transform.lossyScale.x), Mathf.Abs(patrol.transform.lossyScale.y)) + .04f;
                Rect area = patrol.Area;
                if (area.width <= padding * 2 || area.height <= padding * 2)
                    EditorGUILayout.HelpBox("Коллайдер не помещается в область: увеличьте Area Size. В тесной области враг безопасно остаётся на месте.", MessageType.Warning);
            }
        }
        private void OnSceneGUI()
        {
            // Unity invokes this callback for each selected target; targets is invalid here.
            var patrol = target as EnemyPatrol2D;
            if (Application.isPlaying || patrol == null || !patrol.UsePatrolArea) return;
            Rect area = patrol.Area;
            bounds.center = new Vector3(area.center.x, area.center.y, patrol.transform.position.z);
            bounds.size = new Vector3(area.width, area.height, 0);
            bounds.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
            EditorGUI.BeginChangeCheck();
            using (new Handles.DrawingScope(new Color(.95f, .72f, .2f))) bounds.DrawHandle();
            if (!EditorGUI.EndChangeCheck()) return;
            Undo.RecordObject(patrol, "Resize patrol area");
            var patrolObject = new SerializedObject(patrol);
            patrolObject.Update();
            patrolObject.FindProperty("areaOffset").vector2Value = (Vector2)bounds.center - patrol.AreaOrigin;
            patrolObject.FindProperty("areaSize").vector2Value = Vector2.Max((Vector2)bounds.size, Vector2.one * .2f);
            patrolObject.ApplyModifiedProperties();
        }
    }
}
