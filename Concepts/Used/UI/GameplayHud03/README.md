# HUD 03 — текущая проба и история источников

HP опущен к нижнему краю. Фонарь находится над левой частью полосы, найденные карточки — над правой. Число HP остаётся внутри полосы; цифр у карточек нет. Активные изображения карт берутся из [AccessCards02](../../Items/AccessCards02/README.md) через сохранённые пути/GUID набора 01.

Фонарь — оригинальная плоская фронтальная иконка. Выключенное состояние имеет ту же рамку 32×32; диагональ от (3,29) до (29,3) выступает за корпус. Разрыв вокруг линии сохраняет её читаемость. Текст F остаётся отдельным элементом. Обе добавленные затемняющие подложки `Local contrast` отключены по замечанию владельца; авторинг больше их не создаёт.

Оружейная компоновка остаётся рабочей пробой, **не одобрена**. Следующие альтернативы находятся в [WeaponHud04](../../../Unused/UI/WeaponHud04/README.md); их перенос в prefab ждёт выбора владельца. Новые силуэты не означают утверждения текущей расстановки.

## Источники и лицензии

- `Sources/pistol.svg` и `pistol.png`: **Pistol gun**, John Colburn, [Game-icons](https://game-icons.net/1x1/john-colburn/pistol-gun.html), [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/). Изменения для HUD: белая одноцветная версия, обрезка прозрачных полей, растеризация. Атрибуция также в [THIRD_PARTY_NOTICES](../../../../THIRD_PARTY_NOTICES.md).
- `Sources/pump-shotgun.svg`: **Shotgun**, nateobomb / OpenClipart, [копия Wikimedia Commons](https://commons.wikimedia.org/wiki/File:Shotgun.svg), CC0. `pump-shotgun-hud.svg`: белая версия; прозрачные поля обрезаны при растеризации.
- `Sources/flashlight-on-flat.svg`, `flashlight-off-flat.svg`: оригинальные векторные формы проекта, созданы Codex по указанию владельца. Production PNG — 128×128.
- `Sources/flashlight.svg`, `flashlight.png`, `flashlight-off-hud.svg`: отвергнутая наклонная форма **Flashlight**, Delapouite, [Game-icons](https://game-icons.net/1x1/delapouite/flashlight.html), CC BY 3.0. Сейчас не используется.
- `Sources/shotgun.svg`, `shotgun.png`: отвергнутый символ патронов **Shotgun**, sbed, [Game-icons](https://game-icons.net/1x1/sbed/shotgun.html), CC BY 3.0. Не используется как силуэт оружия.

Рабочие sprites: `Assets/EmergencyShift/Art/UI/GameplayHud03`. Старый `underlay.png` сохранён с выключенными узлами prefab как история, на экране не рисуется. Восстановление актуальных четырёх sprites: `node Tools/UI/prepare_gameplay_hud_03.cjs` с установленным `sharp`. Настройки импорта: Sprite Single / Full Rect, Bilinear, alpha, без mipmaps и сжатия; смена GUID не нужна.
