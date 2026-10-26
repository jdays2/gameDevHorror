"""Loopback-only art review, with a whitelist for the two production families."""
from pathlib import Path
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from urllib.parse import unquote, urlsplit

ROOT=Path(__file__).resolve().parent
VISUAL=ROOT.parent
PROJECT=ROOT.parents[3]
ART=PROJECT/'Assets/EmergencyShift/Art/Enemies'


class Handler(SimpleHTTPRequestHandler):
    def translate_path(self,path):
        parts=unquote(urlsplit(path).path).strip('/').split('/')
        if parts[:1]==['production'] and len(parts)==3 and parts[1] in ['ClawVisual','Scorpion']:
            target=ART/parts[1]/parts[2]
        elif parts and parts[0] in ['Scorpion01','FastGroupEnemy23AssetPack'] and '..' not in parts:
            target=VISUAL.joinpath(*parts)
        elif parts==['']:
            target=ROOT/'viewer.html'
        else:
            target=ROOT/'__not_found__'
        return str(target)


if __name__=='__main__':
    ThreadingHTTPServer(('127.0.0.1',8103),Handler).serve_forever()
