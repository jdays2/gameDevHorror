using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Editor
{
    [CustomEditor(typeof(LevelAudioDirector2D))]
    public sealed class LevelAudioDirectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if(!Application.isPlaying)return;
            var director=(LevelAudioDirector2D)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime — read only",EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Mood",director.Mood.ToString());
            EditorGUILayout.LabelField("Perceived / engaged",director.PerceivedEnemies+" / "+director.EngagedEnemies);
            EditorGUILayout.LabelField("Explore / threat / combat gains",director.LayerGains.ToString("F3"));
            EditorGUILayout.LabelField("Background duck",director.IsDucking?"Active":"Off");
        }
        public override bool RequiresConstantRepaint(){return Application.isPlaying;}
    }
}
