from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parent
VALID=ROOT.parents[4]/'Docs/Validation/EnemyAnimationAlternativesTask010/TextureStable02'
im=Image.new('RGBA',(640,640),(24,32,38,255));im.alpha_composite(Image.open(ROOT/'master.png').convert('RGBA'))
d=ImageDraw.Draw(im);d.polygon([(198,447),(221,451),(283,514)],outline=(255,210,0),width=2);d.ellipse((265,497,277,509),outline=(255,210,0),width=2)
d.line((310,490,276,501),fill=(255,210,0),width=2)
d.text((300,470),'Extend ivory over target',fill=(255,210,0))
im.convert('RGB').save(VALID/'lr-tip-focus-guide.png')
im.crop((185,425,300,535)).resize((690,660),Image.Resampling.NEAREST).convert('RGB').save(VALID/'lr-tip-focus-zoom.png')
