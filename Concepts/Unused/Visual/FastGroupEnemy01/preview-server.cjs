// Optional local review server: node preview-server.cjs. No dependencies.
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = __dirname;
http.createServer((request, response) => {
  const route = decodeURIComponent(request.url.split('?')[0]);
  const target = path.resolve(root, route === '/' ? 'preview.html' : '.' + route);
  if (!target.startsWith(root + path.sep)) { response.writeHead(403); response.end(); return; }
  fs.readFile(target, (error, data) => {
    if (error) { response.writeHead(404); response.end(); return; }
    response.setHeader('Content-Type', target.endsWith('.html') ? 'text/html; charset=utf-8' : 'image/png');
    response.end(data);
  });
}).listen(18765, '127.0.0.1', () => process.stdout.write('TASK-005 preview: http://127.0.0.1:18765/preview.html\n'));
