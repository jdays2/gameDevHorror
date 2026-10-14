using System.IO;
using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport;

namespace EmergencyShift.Editor
{
    /// <summary>Первоначальная подготовка. Никогда не пересобирает существующий уровень.</summary>
    [InitializeOnLoad]
    public static class StarterBootstrap
    {
        static StarterBootstrap() => EditorApplication.delayCall += Prepare;

        private static async void Prepare()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += Prepare; return; }
            if (!File.Exists(PrototypeSceneBuilder.ScenePath)) PrototypeSceneBuilder.Build();
            EditorConfigurationCache.Instance.SetUseHttpTransport(false);
            await MCPServiceLocator.TransportManager.StartAsync(TransportMode.Stdio);
        }
    }
}
