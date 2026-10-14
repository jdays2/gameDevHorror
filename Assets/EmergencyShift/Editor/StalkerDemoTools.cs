using System;
using System.Reflection;
using UnityEditor;

namespace EmergencyShift.Editor
{
    /// <summary>Настраивает размер и видимую область Game View через API редактора Unity 6.6.</summary>
    public static class StalkerDemoTools
    {
        public static void PrepareGameView()
        {
            Assembly assembly = typeof(UnityEditor.Editor).Assembly;
            Type viewType = assembly.GetType("UnityEditor.GameView");
            Type sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            Type sizeType = assembly.GetType("UnityEditor.GameViewSize");
            Type kindType = assembly.GetType("UnityEditor.GameViewSizeType");
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            EditorWindow window = EditorWindow.GetWindow(viewType);
            object sizes = sizesType.GetProperty("instance", flags).GetValue(null);
            object groupKind = viewType.GetProperty("currentSizeGroupType", flags).GetValue(window);
            object group = sizesType.GetMethod("GetGroup", flags).Invoke(sizes, new[] { groupKind });
            Type groupType = group.GetType();
            int count = (int)groupType.GetMethod("GetTotalCount", flags).Invoke(group, null);
            int selected = -1;
            for (int i = 0; i < count; i++)
            {
                object size = groupType.GetMethod("GetGameViewSize", flags).Invoke(group, new object[] { i });
                int width = (int)sizeType.GetProperty("width", flags).GetValue(size);
                int height = (int)sizeType.GetProperty("height", flags).GetValue(size);
                if (width == 1280 && height == 720) { selected = i; break; }
            }
            if (selected < 0)
            {
                object custom = Activator.CreateInstance(sizeType, flags, null,
                    new object[] { Enum.Parse(kindType, "FixedResolution"), 1280, 720, "Stalker demo 1280x720" }, null);
                groupType.GetMethod("AddCustomSize", flags).Invoke(group, new[] { custom });
                selected = count;
            }
            viewType.GetProperty("selectedSizeIndex", flags).SetValue(window, selected);
            PropertyInfo gizmos = viewType.GetProperty("drawGizmos", flags);
            if (gizmos != null && gizmos.CanWrite) gizmos.SetValue(window, false);
            Fit(window, viewType, flags);
            window.Focus(); window.Repaint();
        }

        [MenuItem("Emergency Shift/Preview/Fit Game View")]
        public static void FitGameView()
        {
            Type viewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            EditorWindow window = EditorWindow.GetWindow(viewType);
            Fit(window, viewType, flags);
            window.Focus(); window.Repaint();
        }

        private static void Fit(EditorWindow window, Type viewType, BindingFlags flags)
        {
            // Fixed-resolution кадр может быть больше вкладки. Native SnapZoom учитывает DPI
            // и сбрасывает панорамирование; масштаб камеры/Canvas и разрешение не меняются.
            MethodInfo update = viewType.GetMethod("UpdateZoomAreaAndParent", flags);
            MethodInfo snap = viewType.GetMethod("SnapZoom", flags);
            PropertyInfo minimum = viewType.GetProperty("minScale", flags);
            if (snap == null || minimum == null)
                throw new InvalidOperationException("Не найден API масштаба Game View в этой версии Unity.");
            if (update != null) update.Invoke(window, null);
            float fitScale = (float)minimum.GetValue(window);
            snap.Invoke(window, new object[] { fitScale });
        }
    }
}
