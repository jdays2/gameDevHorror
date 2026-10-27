# Писк обнаружения Скорпиона · TASK-005

По уточнению владельца от2026-10-27: короткий писк монстра-насекомого при обнаружении героя. Production: [Detection.wav](../../../../Assets/EmergencyShift/Audio/Enemies/Scorpion/Detection.wav), .68025s mono PCM16/32kHz. Один сигнал на стаю, отдельный канал; прежний Alarm.wav одновременно не играет. Громкость в Presentation.asset, исходный файл не усиливается игровым состоянием.

Оригинальная генерация Runway Sound Effects, не запись настоящего скорпиона. Промпт/taskID в provenance.json, MP3 в Originals, воспроизводимая конвертация prepare.py --ffmpeg <path>. Только mono/resample, ограничение peak−3dBFS и2/15ms fades; без EQ, pitch, слоёв. Хэши/метаданные в processing.json. Технически проверены клип/воспроизведение; слуховая и художественная оценка остаются владельцу.

Референсы для направления:

- [Insect Attack Screech](https://www.prosoundeffects.com/sound-effects/PSE_MCV2/RWOJD/insect-attack-screech) — профессиональный ориентир атакующего насекомого.
- [Spider monster screech](https://freesound.org/people/Patrick_Corra/sounds/540050/) — отдельный авторский паучий визг.
- [Стридуляция Jaguajir rochae](https://datadryad.org/dataset/doi:10.5061/dryad.b8gtht7t4) — исследовательская запись импульсного трения частей тела.

Страницы и описания прочитаны; агент не прослушивал записи и не скачивал/не смешивал их. Первоначальная проба низкого акцента отложена после уточнения владельца в Concepts/Unused/Audio/ScorpionPressureAccent01 и в игру не импортировалась. Это SFX, новая музыкальная тема не создавалась.
