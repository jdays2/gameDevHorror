using EmergencyShift.Features.ScriptedEncounters.View;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Editor
{
    [CustomEditor(typeof(ScriptedStalkerRoute2D))]
    public sealed class ScriptedStalkerRouteEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var route = (ScriptedStalkerRoute2D)target;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Current step", route.State.ToString());
                EditorGUILayout.IntField("Next point", route.PointIndex + 1);
                EditorGUILayout.Toggle("Controls actor", route.ControlsActor);
            }
        }

        private void OnSceneGUI()
        {
            var route = (ScriptedStalkerRoute2D)target;
            Transform[] points = route.RoutePoints;
            Vector3 previous = route.transform.position;
            Handles.color = new Color(.9f, .3f, .15f);
            for (int index = 0; index < points.Length; index++)
            {
                if (points[index] == null)
                {
                    continue;
                }

                Vector3 point = points[index].position;
                Handles.Label(point + Vector3.up * .25f, (index + 1) + ". " + points[index].name);
                Vector3 direction = point - previous;
                if (direction.sqrMagnitude > .001f)
                {
                    Handles.ArrowHandleCap(0, Vector3.Lerp(previous, point, .6f), Quaternion.LookRotation(direction), .55f, EventType.Repaint);
                }

                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.PositionHandle(point, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(points[index], "Move scripted route point");
                    points[index].position = new Vector3(moved.x, moved.y, point.z);
                }

                previous = point;
            }
        }
    }
}
