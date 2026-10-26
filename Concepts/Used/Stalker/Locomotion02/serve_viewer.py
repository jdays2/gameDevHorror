"""Loopback-only viewer server with byte ranges for deterministic video seeking."""
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import argparse
import re
import subprocess
import sys

ROOT=Path(__file__).resolve().parent

class Handler(SimpleHTTPRequestHandler):
    def __init__(self,*args,**kwargs):
        self.remaining=None
        super().__init__(*args,directory=str(ROOT),**kwargs)

    def send_head(self):
        path=Path(self.translate_path(self.path))
        if path.is_file() and self.headers.get('Range'):
            match=re.fullmatch(r'bytes=(\d+)-(\d*)',self.headers['Range'])
            if match:
                total=path.stat().st_size
                first=int(match[1]);last=min(int(match[2]) if match[2] else total-1,total-1)
                if first>=total or first>last:
                    self.send_response(416);self.send_header('Content-Range',f'bytes */{total}');self.end_headers();return None
                source=path.open('rb');source.seek(first)
                self.remaining=last-first+1
                self.send_response(206)
                self.send_header('Content-Type',self.guess_type(str(path)))
                self.send_header('Accept-Ranges','bytes')
                self.send_header('Content-Range',f'bytes {first}-{last}/{total}')
                self.send_header('Content-Length',str(self.remaining))
                self.end_headers()
                return source
        return super().send_head()

    def end_headers(self):
        self.send_header('Accept-Ranges','bytes')
        super().end_headers()

    def copyfile(self,source,outputfile):
        if self.remaining is None:
            return super().copyfile(source,outputfile)
        while self.remaining:
            data=source.read(min(65536,self.remaining))
            if not data:break
            outputfile.write(data);self.remaining-=len(data)

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--port',type=int,default=8796)
    args=parser.parse_args()
    if not (ROOT/'Frames/close/479.webp').exists():
        subprocess.run([sys.executable,str(ROOT/'pack_frame_cache.py')],check=True)
    ThreadingHTTPServer(('127.0.0.1',args.port),Handler).serve_forever()
