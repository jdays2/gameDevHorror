"""Package complete generated paintings. Only authorized exterior alpha1/2 cleanup."""
from pathlib import Path
from PIL import Image,ImageFilter,ImageDraw,ImageFont
import numpy as np,json,shutil,hashlib,zipfile

OUT=Path(__file__).resolve().parent
DELIVERY=OUT/'DeliveryKeys'
DELIVERY.mkdir(exist_ok=True)
KEYS=[('neutral','master-r2.png','master-r2-prompt.txt'),('sleep','compact-source-r1.png','compact-source-r1-prompt.txt'),('attack','attack-source-r1.png','attack-source-r1-prompt.txt')]
manifest=[]
for name,art,prompt in KEYS:
 im=Image.open(OUT/art).convert('RGBA');raw=np.array(im);clean=raw.copy()
 near=np.array(Image.fromarray((raw[:,:,3]>=3).astype('uint8')*255).filter(ImageFilter.MaxFilter(5)))>0
 mask=(raw[:,:,3]>0)&(raw[:,:,3]<=2)&~near
 clean[mask,3]=0
 assert np.array_equal(raw[:,:,:3],clean[:,:,:3])
 assert np.array_equal(raw[:,:,3][raw[:,:,3]>=3],clean[:,:,3][raw[:,:,3]>=3])
 assert np.all(raw[:,:,3][~mask]==clean[:,:,3][~mask])
 path=DELIVERY/(name+'.png');Image.fromarray(clean).save(path)
 shutil.copy2(OUT/prompt,DELIVERY/(name+'-prompt.txt'))
 manifest.append({'key':name,'rawSource':art,'rawSha256':hashlib.sha256((OUT/art).read_bytes()).hexdigest(),'file':path.name,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'size':im.size,'method':'Built-in ImageGen complete painting. No parts cut/assembled, UV deformation, RGB painting or local tip repair.','postprocess':'Only previously authorized detached exterior alpha1/2 zeroed outside2px of all alpha>=3. RGB and every alpha>=3 exactly unchanged.','alphaPixelsCleared':int(mask.sum()),'artistAcceptance':False,'completeAnimation':False})
(DELIVERY/'provenance.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
readme='''# TASK-007 · три цельных сгенерированных ключа

Это три отдельные цельные пробы: neutral.png, sleep.png и attack.png. Не комплект анимаций. Созданы встроенным ImageGen по исходному Сталкеру до рига и исходным позам; точные промпты приложены. Части тела не нарезались, не переносились, не растягивались; программной RGB-дорисовки не было. Выполнена только ранее разрешённая очистка отделённой внешней alpha1/2, с точной сохранностью RGB и alpha>=3. Raw-источники и хеши сохраняются рядом с этим каталогом; provenance.json связывает их с выдаваемыми PNG.

Ограниченная проверка видимых2Dсуставов/кончиков трёх ключей пройдена в заранее заданной полной системе координат, без подгонки отдельных лап. У атаки номинальные5px не везде соблюдены (максимум5.68px); результат укладывается в отдельно записанную2px погрешность наблюдения. Скрытые корни/кончики и настоящая3Dанатомия не восстановлены. Это не полный PASS состава мышц, кожи, анимации или художественная приёмка за владельца. Поверхностные объёмы/стыки остаются на художественной оценке.

Новые серии движения24/сна16/пробуждения16/атаки32 не готовы. Цельные листы и видео-пробы отклонены: менялись пропорции, появлялись лишние лапы/раздвоенные наконечники, нарушалась заданная геометрия. Эти ошибки не исправлялись нарезкой, сборкой частей или программной ретушью. Отвергнутые рисунки/промпты и покадровые контактные листы сохранены отдельно. Из этого архива не следует, что32/16/16/24кадра созданы или приняты.

В игре восстановлен базовый выбор исходных цельных рисунков, новые таблицы поз и атаки отключены у ShadowStalker/Sleeper. Коммит27daf5b0, опубликован; PlayMode2/2, без ошибок компиляции/Console, открытая MechanicsSandbox сохранена clean/stopped, entry=null. Новые рисунки не импортированы. TASK-007 и BUG-054 остаются в работе.
'''
(DELIVERY/'README.md').write_text(readme,encoding='utf-8')
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22)
page=Image.new('RGB',(1200,880),'#deded8')
for i,(name,_,_) in enumerate(KEYS):
 im=Image.open(DELIVERY/(name+'.png'));im.thumbnail((400,400))
 for row,color in [(0,'#202128'),(1,'#e5e5df')]:
  tile=Image.new('RGBA',(400,400),color);tile.alpha_composite(im,((400-im.width)//2,(400-im.height)//2));page.paste(tile.convert('RGB'),(i*400,row*440+40))
 ImageDraw.Draw(page).text((i*400+16,10),name,font=font,fill='#242424')
page.save(DELIVERY/'three-keys-review.png')
archive=OUT/'Stalker-whole-generated-keys.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as zip:
 for path in sorted(DELIVERY.iterdir()):zip.write(path,path.name)
with zipfile.ZipFile(archive) as zip:
 assert zip.testzip() is None
 for entry in manifest:assert hashlib.sha256(zip.read(entry['file'])).hexdigest()==entry['sha256']
print(json.dumps({'archive':str(archive),'bytes':archive.stat().st_size,'keys':3,'completeAnimations':0,'alphaOnly':[{x['key']:x['alphaPixelsCleared']} for x in manifest]}))
