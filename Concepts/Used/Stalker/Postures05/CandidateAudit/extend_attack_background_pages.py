"""Add native-scale dark/light art pages to an already frozen green manifest."""
from pathlib import Path
import json
from PIL import Image,ImageDraw,ImageFont
from make_native_attack_pages import composite,FAMILY,HERE
from audit_native_family import digest,validate_composition

def main():
    out=HERE/'FinalAttack792Art01'
    manifest=json.loads((out/'manifest.json').read_text())
    _,_,sha=validate_composition('attack')
    assert sha==manifest['compositionManifestSha256']
    box=tuple(manifest['fixedNativeCrop'])
    width,height=box[2]-box[0],box[3]-box[1]
    font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
    for folder in ['Dark','Light']:
        (out/folder).mkdir(exist_ok=False)
    for index,page in enumerate(manifest['pages']):
        sheets={folder:Image.new('RGB',((width+12)*4,(height+32)*2),color)
                for folder,color in [('Dark','#202020'),('Light','#eeeeee')]}
        for n,name in enumerate(page['names']):
            path=FAMILY/'Raw'/f'{name}.png'
            assert digest(path)==manifest['rawPngSha256'][name]
            with Image.open(path) as image:
                source=image.convert('RGBA')
                for folder,bg in [('Dark',[.015,.015,.015]),('Light',[.88,.88,.88])]:
                    sheet=sheets[folder]
                    x,y=n%4*(width+12),n//4*(height+32)
                    sheet.paste(composite(source,bg).crop(box),(x,y+32))
                    ImageDraw.Draw(sheet).text((x+4,y+7),name+' | native1x',font=font,
                                              fill='white' if folder=='Dark' else 'black')
        for folder,sheet in sheets.items():
            sheet.save(out/folder/Path(page['page']).name)
        if (index+1)%12==0:
            print(f'Dark/light pages: {index+1}/99',flush=True)
    assert digest(FAMILY/'attack-composition.json')==sha
    (out/'background-pages.json').write_text(json.dumps({'compositionManifestSha256':sha,
        'baseManifestSha256':digest(out/'manifest.json'),'frames':792,'pagesPerBackground':99,
        'backgrounds':['Green','Dark','Light'],'resizing':False},indent=2))
    print('READY all 792 frames on three backgrounds',flush=True)

if __name__=='__main__': main()
