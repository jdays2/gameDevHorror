# Конфиги камеры, света и звука ламп

Дата: 2026-10-21. База `3e79740`. CameraFramingProfile, LocalLightProfile и LocalLampAudioSet используют private SerializeField с прежними именами и readonly API. Dark intervals остаются внутри светового профиля; contacts читаются по Count/At без выдачи массива. Ламповые clips, HDR RGB, радиусы/яркость/циклы и camera framing values сохранены. Максимальная панорама .5 перенесена из audio adapter в профиль с прежним default.

LocalLightProfile дополнительно проверяет цвет/alpha и clock, не передаёт NaN/Infinity из времени/арифметики в PerlinNoise. CameraFramingProfile проверяет конечность итогового OrthographicSize при экстремальных конечных range/aspect. Исходные формулы нормальных входов, dropouts и сглаживание сохранены. Consumers читают getters, NarrativeEnvironmentBuilder использует Editor SerializedObject; его recipe values/непереопределяемые поля сохранены. Генератор не запускался, сценовый авторинг не регенерировался.

**802/802 EditMode**, 20 новых случаев: камера/цвет/clock/derived overflow, старые JSON/интервалы/HDR, clips по индексу и все authored profiles без mutation. Обновлены прежние fixtures с прямой записью public fields, их сценарии сохранены. **32/32 PlayMode**, включая реальные ownership/upgrade фонаря, portrait framing, события/переподписку камеры и загрузку сцен с лампами. Обычная Windows64 success; собственная Player-сборка только EmergencyShift.Runtime.

Source parity, 1794 неизменённых ресурса и raw hashes — [LightingConfig14.json](../Validation/LightingConfig14.json). Копия включает исходную незавершённую паузу; LicensingClient остаётся нефатальным сообщением окружения. Художественная/слуховая приёмка и FPS не заявляются.

Новых Update, голосов, физических запросов, массивов/кешей или runtime-реестров нет. На flicker Evaluate добавлены проверки конечности трёх промежуточных значений; камера проверяет результат при пересчёте framing, не при каждом чтении оптики. Clip getter имеет bounds guard на контакт. LocalLamp2D power/visibility/static registry и lifecycle/transaction authoring tool остаются partial. Уровень, cookie, теневой алгоритм и состояние врагов этим config-pass не менялись.
