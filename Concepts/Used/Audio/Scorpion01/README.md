# Скорпион · оригинальные звуки · TASK-005

Production — Assets/EmergencyShift/Audio/Enemies/Scorpion. Четыре контакта лап, попадание, клешни, тревога и присутствие: восемь WAV mono PCM16/32kHz. MP3 — Sources; хеши/длительности — processing.json, промпты — provenance.json. Сэмплы референсов не копировались.

Владелец отверг клацанье r1, затем влажный хруст r2 как не соответствующие концепту. Sources/attack.mp3 и attack-r2-1/2.mp3, процессор/отчёт r2 сохранены Historical. r3 следует массивным реальным клешням: короткое плотное смыкание панциря. Две новые пробы: [A — одиночное смыкание](Previews/HeavyShellA.wav), [B — парный хват](Previews/HeavyShellB.wav). B подключён в Attack.wav как кандидат для оценки.

Обработка r3 минимальна: mono/resample, выбор контакта по5ms RMS, нормализация, fade4/45ms. Нет смеси r2, pitch исходника, EQ или искажения. Обе пробы340ms, peak.75, clipped0, акцент+44.8125ms: cue.32 и strike.40 атаки.56s дают44.8ms. Игровой pitch.96..1.03 остаётся; ошибка акцента до1.9ms без Update/DSP-буфера. [Обработка r3](attack-r3-processing.json).

Четыре AudioSource: голос/попадания, клешни, контакты, присутствие. Попадание не обрывает клешни. Дальность/стены ослабляют звук, тревога — у одного члена. Idle прекращается при тревоге; контакты требуют перемещения. Disable/смерть останавливают каналы и сбрасывают latch; пауза сохраняет playhead. Каналы проверены в [ScorpionAudio02](../../../../Docs/Validation/ScorpionAudio02/README.md); r3 меняет только WAV.

Полный rebuild: `python process_audio.py` вызывает process_attack_r3.py. Только клешни: `python process_attack_r3.py`. Отвергнутый r2 не возвращается. В [просмотрщике](http://127.0.0.1:8103/Scorpion01/viewer.html?revision=heavy-chela-r8) есть A/B и опциональная синхронизация первой особи.

Первое вдохновение владельца: [SCORPION SOUNDS](https://www.youtube.com/watch?v=2C0146IKxi0), [stridulating/hissing](https://www.youtube.com/watch?v=SMBj8VAFoPw). Страницы/названия проверялись; акустическое сравнение не выполнено, видео не скачивались. Аудиовход агента недоступен: WAV/воспроизведение не заменяют оценку тембра на слух. Художественная приёмка открыта; stress/Profiler/10/20 не запускались.
