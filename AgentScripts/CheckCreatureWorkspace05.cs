using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using EmergencyShift;
using EmergencyShift.Features.Enemies.Config;

public static class CheckCreatureWorkspace05
{
    public static object Read()
    {
        return new
        {
            EditorApplication.isPlaying,
            EditorApplication.isCompiling,
            BuildPipeline.isBuildingPlayer,
            playModeStartScene = EditorSceneManager.playModeStartScene,
            scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(index =>
            {
                Scene scene = SceneManager.GetSceneAt(index);
                return new { scene.path, scene.isLoaded, scene.isDirty, active = scene == SceneManager.GetActiveScene() };
            }).ToArray(),
            hasSleeperAnimator = typeof(SleeperSpriteAnimator).FullName,
            hasDurationAwareAttack = typeof(CreatureAttackSpriteSet).GetMethod("FrameIndex").GetParameters().Length == 4
        };
    }
}
