import http from 'node:http';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { randomUUID } from 'node:crypto';
import path from 'node:path';
import { createPrecheckHandler, createPrechecker } from './precheck.mjs';
import { createExporter } from './export.mjs';

const root = new URL('../', import.meta.url);
const types = { '.html': 'text/html; charset=utf-8', '.css': 'text/css; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.json': 'application/json; charset=utf-8' };
// Fixed document endpoints and explicit static allowlist; no arbitrary filesystem API.
const paths = ['styles.css', 'inheritance.html', 'inheritance.css', 'src/app-v2.js', 'src/model-v2.js', 'src/action-animation.js', 'src/precheck.js', 'src/export.js', 'samples/inheritance.dtmonster.json'];
paths.push('src/protected-cache.js', 'src/png.js', 'src/wav.js', 'src/physical.js', 'src/native-attacks.js', 'src/native-parameters.js');
const assets = new Map(await Promise.all(paths.map(async path => [path, await readFile(new URL(path, root))])));
export function createEditorServer({ precheck = createPrecheckHandler(), exporter = createExporter() } = {}) {
const exportHandler = createPrecheckHandler(exporter.exportProject, { header: 'x-dtmapi-export', kind: '游戏包导出' });
let working = false;
const server = http.createServer((req, res) => {
  const port = server.address().port, hosts = [`127.0.0.1:${port}`, `localhost:${port}`];
  if (!hosts.includes(req.headers.host)) { res.writeHead(403); res.end('Loopback Host required'); return; }
  res.setHeader('Content-Security-Policy', "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; media-src 'self' data:; connect-src 'self'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'");
  res.setHeader('X-Content-Type-Options', 'nosniff'); res.setHeader('Referrer-Policy', 'no-referrer'); res.setHeader('Cache-Control', 'no-store');
  if (['/api/precheck', '/api/export', '/api/export-environment'].includes(req.url)) {
    const send = (status, value) => { res.writeHead(status, { 'Content-Type': 'application/json; charset=utf-8' }); res.end(JSON.stringify(value)); };
    if (working) { send(409, { error: { code: 'EditorBusy', message: '另一项检查或导出尚未结束，请稍后重试。' } }); return; }
    working = true;
    void (async () => {
      try {
        if (req.url === '/api/precheck') await precheck(req, res);
        else if (req.url === '/api/export') await exportHandler(req, res);
        else if (req.method !== 'GET') send(405, { error: { code: 'MethodNotAllowed', message: '环境检查需要 GET。' } });
        else if (req.headers['x-dtmapi-export'] !== '1') send(403, { error: { code: 'SameOriginRequired', message: '请从本编辑器检查导出环境。' } });
        else send(200, await exporter.environment());
      } catch (error) { send(error.status ?? 503, { error: { code: error.code ?? 'ExportUnavailable', message: error.message } }); }
      finally { working = false; }
    })();
    return;
  }
  if (req.method !== 'GET' && req.method !== 'HEAD') { res.writeHead(405, { Allow: 'GET, HEAD' }); res.end(); return; }
  const raw = req.url.split('?')[0], assetPath = raw === '/' ? 'inheritance.html' : raw.slice(1), data = assets.get(assetPath);
  if (!data) { res.writeHead(404); res.end('Not found'); return; }
  const ext = assetPath.slice(assetPath.lastIndexOf('.')); res.writeHead(200, { 'Content-Type': types[ext], 'Content-Length': data.byteLength }); res.end(req.method === 'HEAD' ? undefined : data);
});
server.requestTimeout = 30000;
return server;
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
const args = process.argv.slice(2), value = (key, fallback) => args.includes(key) ? args[args.indexOf(key) + 1] : fallback;
const instance = value('--instance', randomUUID());
if (!/^[a-f0-9-]{36}$/.test(instance)) throw new Error('Invalid instance id');
const firstPort = Number(value('--port', '43139'));
if (!Number.isInteger(firstPort) || firstPort < 1024 || firstPort > 65515) throw new Error('Port must be 1024–65515');
const sdkRoot = value('--sdk-root', '');
const server = createEditorServer({ precheck: createPrecheckHandler(createPrechecker({ sdkRoot })), exporter: createExporter({ sdkRoot }) });
// Atomic bind is the port availability check. Busy ports are skipped, never reclaimed.
let listeningPort;
for (let port = firstPort; port < firstPort + 20; port++) {
  try {
    await new Promise((resolve, reject) => { server.once('error', reject); server.listen(port, '127.0.0.1', () => { server.removeListener('error', reject); resolve(); }); });
    listeningPort = port; break;
  } catch (error) { if (error.code !== 'EADDRINUSE') throw error; console.log(`Port ${port} occupied; trying next port.`); }
}
if (!listeningPort) throw new Error('No free port in requested range. Other services were left running.');
const url = `http://127.0.0.1:${listeningPort}`;
await mkdir(new URL('.runtime/', root), { recursive: true });
await writeFile(new URL(`.runtime/server-${instance}.json`, root), JSON.stringify({ instance, pid: process.pid, url, startedAt: new Date().toISOString(), script: fileURLToPath(import.meta.url) }));
console.log(`DTMAPI Monster Editor: ${url}\nInstance: ${instance}\nCtrl+C stops this server. Static assets are loaded at startup; restart after source edits.`);
for (const signal of ['SIGINT', 'SIGTERM']) process.on(signal, () => server.close(() => process.exit(0)));
}
