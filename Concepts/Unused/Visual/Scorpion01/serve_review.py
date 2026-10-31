"""Loopback-only art review, with a whitelist for the two production families."""
from pathlib import Path
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from urllib.parse import unquote, urlsplit

ROOT=Path(__file__).resolve().parent
VISUAL=ROOT.parent
PROJECT=ROOT.parents[3]
ART=PROJECT/'Assets/EmergencyShift/Art/Enemies'
AUDIO=PROJECT/'Concepts/Used/Audio/Scorpion01/Previews'
FLESH=PROJECT/'Assets/EmergencyShift/Art/Effects/CreatureFlesh04'
GAME_AUDIO=PROJECT/'Assets/EmergencyShift/Audio/Enemies/Scorpion'


class Handler(SimpleHTTPRequestHandler):
    def translate_path(self,path):
        parts=unquote(urlsplit(path).path).strip('/').split('/')
        if parts[:1]==['production'] and len(parts)==3 and parts[1] in ['ClawVisual','Scorpion']:
            target=ART/parts[1]/parts[2]
        elif len(parts)==2 and parts[0]=='scorpion-audio' and parts[1] in ['HeavyShellA.wav','HeavyShellB.wav']:
            target=AUDIO/parts[1]
        elif len(parts)==2 and parts[0]=='scorpion-audio' and parts[1] in ['Detection.wav','Squeak1.wav','Squeak2.wav','Squeak3.wav']:
            target=GAME_AUDIO/parts[1]
        elif len(parts)==2 and parts[0]=='scorpion-audio' and parts[1]=='Death.wav':
            target=PROJECT/'Assets/EmergencyShift/Audio/CreatureRupture06/rupture_06_01.wav'
        elif len(parts)==2 and parts[0]=='scorpion-flesh' and parts[1] in ['MuscleWad.png','DenseClot.png','TissueStrip.png']:
            target=FLESH/parts[1]
        elif parts and parts[0] in ['Scorpion01','FastGroupEnemy23AssetPack'] and '..' not in parts:
            target=VISUAL.joinpath(*parts)
        elif parts==['']:
            target=ROOT/'viewer.html'
        else:
            target=ROOT/'__not_found__'
        return str(target)


if __name__=='__main__':
    ThreadingHTTPServer(('127.0.0.1',8103),Handler).serve_forever()
