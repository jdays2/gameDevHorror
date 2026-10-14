using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>Обновляет только фонарь и качество света; сохраняет существующую расстановку уровня.</summary>
    public static class FlashlightRevision
    {
        [MenuItem("Emergency Shift/Apply flashlight revision 0.2")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Перед настройкой фонаря остановите Play.");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != PrototypeSceneBuilder.ScenePath)
                throw new InvalidOperationException("Откройте BasementPrototype перед настройкой фонаря.");
            // Для первого проекта важнее воспроизводимый старт, чем ускоренный вход в Play.
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.None;
            EditorSettings.enterPlayModeOptionsEnabled = false;
            const string prefabPath = "Assets/EmergencyShift/Prefabs/Player.prefab";
            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Tune(prefab.GetComponent<Flashlight2D>(), null);
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            Flashlight2D player = UnityEngine.Object.FindAnyObjectByType<Flashlight2D>();
            Tune(player, Camera.main);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            foreach (string path in new[] { "Assets/EmergencyShift/Prefabs/Wall.prefab", "Assets/EmergencyShift/Prefabs/Door.prefab" })
            {
                GameObject opaque = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (ShadowCaster2D caster in opaque.GetComponentsInChildren<ShadowCaster2D>()) caster.selfShadows = true;
                    PrefabUtility.SaveAsPrefabAsset(opaque, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(opaque); }
            }
            foreach (ShadowCaster2D caster in UnityEngine.Object.FindObjectsByType<ShadowCaster2D>())
            {
                caster.selfShadows = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(caster);
            }
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>("Assets/EmergencyShift/Settings/Prototype2DRenderer.asset");
            var settings = new SerializedObject(renderer);
            settings.FindProperty("m_LightRenderTextureScale").floatValue = 1;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("FLASHLIGHT_REVISION_02_READY: range 4.2, angle 80, close spill 0.65, lighting scale 1");
        }

        private static void Tune(Flashlight2D flashlight, Camera camera)
        {
            if (flashlight == null) throw new InvalidOperationException("Не найден компонент фонаря.");
            var fields = new SerializedObject(flashlight);
            Light2D beam = fields.FindProperty("beam").objectReferenceValue as Light2D;
            Transform aim = fields.FindProperty("aimPivot").objectReferenceValue as Transform;
            if (beam == null || aim == null) throw new InvalidOperationException("Не назначен луч или поворотный узел.");
            Light2D glow = fields.FindProperty("nearGlow").objectReferenceValue as Light2D;
            if (glow == null)
            {
                Transform existing = flashlight.transform.Find("Flashlight spill — close range");
                if (existing != null) glow = existing.GetComponent<Light2D>();
                else
                {
                    var obj = new GameObject("Flashlight spill — close range", typeof(Light2D));
                    obj.transform.SetParent(flashlight.transform, false);
                    glow = obj.GetComponent<Light2D>();
                }
            }
            beam.transform.localPosition = new Vector3(0, 0.18f, 0);
            beam.intensity = 1.5f;
            beam.pointLightInnerRadius = 0.35f;
            beam.shadowsEnabled = true;
            beam.shadowIntensity = 1;
            glow.lightType = Light2D.LightType.Point;
            glow.color = beam.color;
            glow.intensity = 0.18f;
            glow.pointLightInnerRadius = 0;
            glow.pointLightOuterRadius = 0.65f;
            glow.pointLightInnerAngle = glow.pointLightOuterAngle = 360;
            glow.shadowsEnabled = true;
            glow.shadowIntensity = 1;
            beam.enabled = glow.enabled = false;
            flashlight.Configure(camera, aim, beam, glow);
            flashlight.SetOptics(4.2f, 80);
            EditorUtility.SetDirty(flashlight);
            EditorUtility.SetDirty(beam);
            EditorUtility.SetDirty(glow);
        }
    }
}
