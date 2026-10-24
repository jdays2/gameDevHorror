using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmergencyShift.Editor
{
    public static class MainMenuCatalogAuthoring
    {
        public static void InstallScrollView()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Edit the menu in Edit Mode.");
            }
            GameObject root = PrefabUtility.LoadPrefabContents(MainMenuAuthoring.PrefabPath);
            try
            {
                var view = root.GetComponent<EmergencyShift.Features.MainMenu.View.MainMenuView>();
                var data = new SerializedObject(view);
                var rows = (RectTransform)data.FindProperty("sceneRows").objectReferenceValue;
                ConfigureRows(rows);
                PrefabUtility.SaveAsPrefabAsset(root, MainMenuAuthoring.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void ConfigureRows(RectTransform rows)
        {
            if (rows.GetComponentInParent<ScrollRect>() != null)
            {
                return;
            }
            var scrollRoot = new GameObject("Scene list", typeof(RectTransform), typeof(ScrollRect));
            var scrollRect = (RectTransform)scrollRoot.transform;
            scrollRect.SetParent(rows.parent, false);
            scrollRect.anchorMin = rows.anchorMin;
            scrollRect.anchorMax = rows.anchorMax;
            scrollRect.pivot = rows.pivot;
            scrollRect.anchoredPosition = rows.anchoredPosition;
            scrollRect.sizeDelta = rows.sizeDelta;
            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(Mask));
            var viewport = (RectTransform)viewportObject.transform;
            viewport.SetParent(scrollRect, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;
            rows.SetParent(viewport, false);
            rows.anchorMin = new Vector2(0, 1);
            rows.anchorMax = Vector2.one;
            rows.pivot = new Vector2(0.5f, 1);
            rows.anchoredPosition = Vector2.zero;
            rows.sizeDelta = Vector2.zero;
            var fitter = rows.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = scrollRoot.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = rows;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = 35;
        }
    }
}
