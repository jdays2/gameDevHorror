using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>Adds only the dialogue fixture; never rebuilds the sandbox.</summary>
    public static class DialogueSandboxAuthoring
    {
        private const string Root = "Assets/EmergencyShift";
        private const string FixtureName = "Dialogue test - start corridor";

        [MenuItem("Emergency Shift/Sandbox/Add dialogue test near start")]
        public static void Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != SandboxSceneBuilder.ScenePath || scene.isDirty)
            {
                throw new InvalidOperationException("Stop Play, save edits, and open MechanicsSandbox first.");
            }

            if (GameObject.Find(FixtureName) != null)
            {
                throw new InvalidOperationException("The dialogue fixture already exists; edit its data instead.");
            }

            var hud = UnityEngine.Object.FindAnyObjectByType<PrototypeHud>();
            if (hud == null || hud.gameObject.scene != scene)
            {
                throw new InvalidOperationException("Sandbox HUD is missing.");
            }

            var font = hud.GetComponentInChildren<UnityEngine.UI.Text>(true).font;
            var content = PrepareSequence();
            var style = PrepareStyle(font);
            var fixture = new GameObject(FixtureName);
            SceneManager.MoveGameObjectToScene(fixture, scene);
            var ui = Rect("Dialogue canvas", fixture.transform);
            var canvas = ui.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = ui.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            ui.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var scrim = Image("World dimmer", ui, new Color(.02f, .03f, .04f, .35f));
            Stretch(scrim.rectTransform);
            scrim.raycastTarget = true;

            var backdrop = Image("Solid dialogue backing", ui, new Color(.035f, .043f, .055f));
            Anchor(backdrop.rectTransform, Vector2.zero, Vector2.right, new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 380));
            var fade = Rect("Soft upper edge", backdrop.transform);
            Anchor(fade, Vector2.up, Vector2.one, new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 180));
            var graphic = fade.gameObject.AddComponent<DialogueFadeGraphic>();
            graphic.color = backdrop.color;
            graphic.raycastTarget = false;

            var portrait = Image("Active portrait - white square", ui, Color.white);
            Anchor(portrait.rectTransform, new Vector2(.06f, 0), new Vector2(.06f, 0), Vector2.zero, new Vector2(0, 450), Vector2.one * style.portraitSize);
            portrait.preserveAspect = true;

            var panel = Rect("Dialogue content", ui);
            Anchor(panel, new Vector2(.06f, 0), new Vector2(.94f, 0), new Vector2(.5f, 0), new Vector2(0, 48), new Vector2(0, 300));
            var speaker = Text("Speaker", panel, font, 24, style.accentColor);
            Anchor(speaker.rectTransform, Vector2.up, Vector2.one, Vector2.up, Vector2.zero, new Vector2(0, 36));
            var body = Text("Line", panel, font, 40, style.textColor);
            Anchor(body.rectTransform, Vector2.up, Vector2.one, Vector2.up, new Vector2(0, -60), new Vector2(0, 120));
            body.lineSpacing = 1.2f;
            var buttonRect = Rect("Continue - click or keyboard", panel);
            Anchor(buttonRect, Vector2.right, Vector2.right, Vector2.right, Vector2.zero, new Vector2(650, 64));
            var hitArea = buttonRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            hitArea.color = new Color(1, 1, 1, 0);
            var button = buttonRect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = hitArea;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            var buttonLabel = Text("Continue label", buttonRect, font, 20, style.hintColor);
            Stretch(buttonLabel.rectTransform);
            buttonLabel.alignment = TextAnchor.MiddleRight;

            var view = ui.gameObject.AddComponent<DialogueView>();
            Assign(view, "style", style);
            Assign(view, "overlay", canvas);
            Assign(view, "panel", panel);
            Assign(view, "backdrop", backdrop.rectTransform);
            Assign(view, "portrait", portrait);
            Assign(view, "speakerName", speaker);
            Assign(view, "body", body);
            Assign(view, "advance", button);
            Assign(view, "advanceLabel", buttonLabel);
            var session = fixture.AddComponent<DialogueSession>();
            Assign(session, "view", view);
            Assign(session, "gameplayHud", hud.GetComponent<Canvas>());
            Assign(session, "reticle", hud.GetComponentInChildren<AimReticle2D>(true));
            canvas.enabled = false;

            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Dialogue EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(fixture.transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            AddInteraction(fixture.transform, session, content, font);
            ConfigureBranchingView(fixture, style);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = fixture;
        }

        private static DialogueSequence PrepareSequence()
        {
            const string folder = Root + "/Data/Dialogue";
            EnsureFolder(folder);
            const string path = folder + "/SandboxConversation.asset";
            var existing = AssetDatabase.LoadAssetAtPath<DialogueSequence>(path);
            if (existing != null)
            {
                return existing;
            }

            var sequence = ScriptableObject.CreateInstance<DialogueSequence>();
            sequence.speakers = new[]
            {
                new DialogueSequence.Speaker { displayName = "Дежурный врач" },
                new DialogueSequence.Speaker { displayName = "Вы" }
            };
            SetBranchingConversation(sequence);
            AssetDatabase.CreateAsset(sequence, path);
            return sequence;
        }

        private static void SetBranchingConversation(DialogueSequence sequence)
        {
            sequence.entryId = "warning";
            sequence.lines = new[]
            {
                new DialogueSequence.Line
                {
                    id = "warning", speaker = 0, nextId = "question",
                    text = "Не открывайте дверь. Что бы вы ни услышали с той стороны — не открывайте."
                },
                new DialogueSequence.Line
                {
                    id = "question", speaker = 0, text = "Вы ведь тоже слышали этот голос? Что будем делать?",
                    choices = new[]
                    {
                        new DialogueSequence.Choice { id = "help", text = "Там просят о помощи. Я хочу проверить.", targetId = "help-reply" },
                        new DialogueSequence.Choice { id = "leave", text = "Не будем рисковать. Найдём другой путь.", targetId = "leave-reply" }
                    }
                },
                new DialogueSequence.Line
                {
                    id = "help-reply", speaker = 1, nextId = "help-warning",
                    text = "Я не могу просто уйти, если там ещё кто-то живой."
                },
                new DialogueSequence.Line
                {
                    id = "help-warning", speaker = 0, nextId = "rejoin",
                    text = "Я тоже так думал. Пока оно не заговорило моим голосом. Сначала заглянем через служебное окно."
                },
                new DialogueSequence.Line
                {
                    id = "leave-reply", speaker = 1, nextId = "leave-direction",
                    text = "Хорошо. Дверь останется закрытой. Где ближайший выход?"
                },
                new DialogueSequence.Line
                {
                    id = "leave-direction", speaker = 0, nextId = "rejoin",
                    text = "За постом есть служебный коридор. Если держаться вместе, мы доберёмся до лестницы."
                },
                new DialogueSequence.Line
                {
                    id = "rejoin", speaker = 1, text = "Тогда пойдём через служебный коридор. Я останусь рядом."
                }
            };
            sequence.Validate();
        }

        [MenuItem("Emergency Shift/Sandbox/Update dialogue test with choices")]
        public static void UpgradeDialogueTest()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != SandboxSceneBuilder.ScenePath || scene.isDirty)
            {
                throw new InvalidOperationException("Stop Play, save edits, and open MechanicsSandbox first.");
            }

            var fixture = GameObject.Find(FixtureName);
            if (fixture == null)
            {
                throw new InvalidOperationException("Add the dialogue test first.");
            }

            var content = PrepareSequence();
            SetBranchingConversation(content);
            EditorUtility.SetDirty(content);
            var style = AssetDatabase.LoadAssetAtPath<DialogueStyle>(Root + "/Data/Dialogue/CinematicDialogueStyle.asset");
            EditorUtility.SetDirty(style);
            ConfigureBranchingView(fixture, style);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureBranchingView(GameObject fixture, DialogueStyle style)
        {
            var view = fixture.GetComponentInChildren<DialogueView>(true);
            Transform ui = view.transform;
            var scrim = ui.Find("World dimmer").GetComponent<UnityEngine.UI.Image>();
            scrim.color = Color.clear;
            var background = scrim.GetComponent<UnityEngine.UI.Button>();
            if (background == null)
            {
                background = scrim.gameObject.AddComponent<UnityEngine.UI.Button>();
            }

            background.targetGraphic = scrim;
            background.transition = UnityEngine.UI.Selectable.Transition.None;
            background.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            Assign(view, "backgroundAdvance", background);

            var backing = new SerializedObject(view).FindProperty("backdrop").objectReferenceValue as RectTransform;
            backing.name = "Bottom shadow filter";
            backing.GetComponent<UnityEngine.UI.Image>().enabled = false;
            var fade = backing.GetComponentInChildren<DialogueFadeGraphic>(true);
            if (fade.GetComponent<CanvasRenderer>() == null)
            {
                fade.gameObject.AddComponent<CanvasRenderer>();
            }

            Stretch(fade.rectTransform);
            fade.color = style.shadowColor;
            fade.raycastTarget = false;

            var panel = ui.Find("Dialogue content");
            var existing = panel.Find("Responses");
            RectTransform choices = existing != null ? existing.GetComponent<RectTransform>() : Rect("Responses", panel);
            Anchor(choices, Vector2.zero, Vector2.right, new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 160));
            var templateTransform = choices.Find("Response template");
            UnityEngine.UI.Button template;
            if (templateTransform == null)
            {
                var row = Image("Response template", choices, style.choiceColor);
                Anchor(row.rectTransform, Vector2.up, Vector2.one, new Vector2(.5f, 1), Vector2.zero, new Vector2(0, style.choiceMinHeight));
                row.raycastTarget = true;
                template = row.gameObject.AddComponent<UnityEngine.UI.Button>();
                template.targetGraphic = row;
                var colors = template.colors;
                colors.highlightedColor = new Color(1.5f, 1.4f, 1.25f, 1);
                colors.selectedColor = colors.highlightedColor;
                template.colors = colors;
                var label = Text("Response label", row.transform, style.font, style.choiceSize, style.textColor);
                Stretch(label.rectTransform);
                label.alignment = TextAnchor.MiddleLeft;
            }
            else
            {
                template = templateTransform.GetComponent<UnityEngine.UI.Button>();
            }

            template.gameObject.SetActive(false);
            Assign(view, "choicesRoot", choices);
            Assign(view, "choiceTemplate", template);
        }

        private static DialogueStyle PrepareStyle(Font font)
        {
            const string path = Root + "/Data/Dialogue/CinematicDialogueStyle.asset";
            var style = AssetDatabase.LoadAssetAtPath<DialogueStyle>(path);
            if (style == null)
            {
                style = ScriptableObject.CreateInstance<DialogueStyle>();
                style.font = font;
                AssetDatabase.CreateAsset(style, path);
            }

            return style;
        }

        private static void AddInteraction(Transform parent, DialogueSession session, DialogueSequence sequence, Font font)
        {
            var marker = new GameObject("Conversation point - E", typeof(SpriteRenderer), typeof(CircleCollider2D));
            marker.transform.SetParent(parent, false);
            marker.transform.position = new Vector3(11.2f, -3, 0);
            marker.transform.localScale = Vector3.one * .45f;
            var renderer = marker.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Shared/Shapes/Rectangle.asset");
            renderer.color = new Color(.808f, .624f, .475f);
            renderer.sortingOrder = 5;
            marker.GetComponent<CircleCollider2D>().isTrigger = true;
            var interaction = marker.AddComponent<DialogueInteraction2D>();
            Assign(interaction, "session", session);
            Assign(interaction, "sequence", sequence);

            var sign = Rect("Dialogue test sign", parent);
            sign.position = new Vector3(11.2f, -3.85f, 0);
            sign.localScale = Vector3.one * .01f;
            sign.sizeDelta = new Vector2(550, 75);
            var canvas = sign.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 6;
            var label = Text("Editable sign", sign, font, 28, new Color(.808f, .624f, .475f));
            Stretch(label.rectTransform);
            label.alignment = TextAnchor.MiddleCenter;
            label.text = "ДИАЛОГ · [E]";
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int split = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }

        private static void Assign(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj.GetComponent<RectTransform>();
        }

        private static UnityEngine.UI.Image Image(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static UnityEngine.UI.Text Text(string name, Transform parent, Font font, int size, Color color)
        {
            var rect = Rect(name, parent);
            var text = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            Anchor(rect, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
