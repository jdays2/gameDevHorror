# Главное меню для тестирования

Источник — [карточка концептов](https://app.notion.com/p/4018d6dc06b681869da6e199d20e3006). Владелец выбрал «Ночную смену», отклонил первый перенос в Unity, затем поручил убрать фонарик и оставить максимально простой чёрно-белый вид без пояснений назначения пунктов. Красный акцент разрешён как возможность; текущая версия монохромная. Герой и финальный арт не определены.

UI и компоновка авторские, выполнены uGUI; стоковый фон и сгенерированная графика не используются. Runtime: `Prefabs/UI/MainMenu`, `Data/UI/MainMenu`, `Scenes/MainMenu.unity` под Assets/EmergencyShift.

Шрифты — SIL Open Font License 1.1; тексты лицензий сохранены рядом с TTF в `Assets/EmergencyShift/Fonts/MainMenu`:

- [Barlow Condensed Bold / Google Fonts](https://github.com/google/fonts/tree/main/ofl/barlowcondensed).
- [Noto Sans Medium / Noto fonts](https://github.com/notofonts/noto-fonts/blob/main/hinted/ttf/NotoSans/NotoSans-Medium.ttf), готовый статический файл с кириллицей, без изменения контуров.

Музыка — существующие BelowTheWardPauseIntro.wav и BelowTheWardPauseLoop.wav, общие с паузой, по прямому поручению владельца. [Происхождение трека/обработки](../../Audio/PauseMenuMusic01/provenance.json). Новой генерации и редактирования WAV не было.

[Контракт](../../../../Docs/Features/MainMenu.md), [кадр Unity](../../../../Docs/Validation/MainMenu01/home.png). Изображение реализации не означает художественного одобрения владельцем.
