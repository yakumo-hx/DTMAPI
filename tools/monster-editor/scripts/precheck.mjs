import { spawn } from 'node:child_process';
import { mkdir, mkdtemp, readFile, writeFile, readdir, rm, access } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const toolRoot = fileURLToPath(new URL('../../../', import.meta.url));
const scratch = fileURLToPath(new URL('../.runtime/precheck/', import.meta.url));

export class PrecheckError extends Error {
  constructor(status, code, message) { super(message); this.status = status; this.code = code; }
}
const unavailable = (detail = '') => new PrecheckError(503, 'PrecheckUnavailable', '本地编译器不可用。检查完整套件、启动时的 -SdkRoot 和 tools/monster-editor/.runtime/server-*.err.log；仍可保存作者项目。' + (detail ? `\n${detail}` : ''));

// Fixed executable/script/argument names. Browser input is data, never a path or command.
export async function runCompiler(projectPath, catalogPath, outputPath, sdkRoot = '') {
  return runPowerShell('compile-monster-project.ps1', ['-ProjectPath', projectPath,
    '-CatalogPath', catalogPath, '-OutputRoot', outputPath, '-ValidateOnly', ...(sdkRoot ? ['-SdkRoot', sdkRoot] : [])]);
}

export async function runPowerShell(script, args, { timeoutMs = 60000, timeoutCode = 'PrecheckTimeout', timeoutMessage = '预检查超过 60 秒，请检查本地编译环境后重试；仍可保存作者项目。', temporaryDirectory } = {}) {
  if (process.platform !== 'win32') throw unavailable();
  const powershell = path.join(process.env.SystemRoot, 'System32/WindowsPowerShell/v1.0/powershell.exe');
  const entry = path.join(toolRoot, 'tools/scripts', script);
  for (const file of [powershell, entry]) {
    try { await access(file); }
    catch { throw new PrecheckError(503, 'CompilerEntryMissing', `缺少工具入口 ${file}。请恢复完整套件或 Windows PowerShell 5.1；作者原文未改变。`); }
  }
  const child = spawn(powershell, ['-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File',
    entry, ...args],
  { cwd: toolRoot, windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'],
    env: temporaryDirectory ? { ...process.env, TEMP: temporaryDirectory, TMP: temporaryDirectory } : process.env });
  let logs = '', timedOut = false;
  for (const stream of [child.stdout, child.stderr]) stream.on('data', data => { logs = (logs + data.toString()).slice(-8192); });
  const timer = setTimeout(() => {
    timedOut = true;
    // Only terminate the process tree started by this request, including dotnet.
    if (child.pid) {
      const killer = spawn(path.join(process.env.SystemRoot, 'System32/taskkill.exe'), ['/PID', String(child.pid), '/T', '/F'], { windowsHide: true, stdio: 'ignore' });
      killer.on('error', () => child.kill());
    }
  }, timeoutMs);
  try {
    const code = await new Promise((resolve, reject) => { child.once('error', reject); child.once('close', resolve); });
    if (timedOut) throw new PrecheckError(503, timeoutCode, timeoutMessage);
    // The wrapper throws on CLI rejection. The report, not host exit 1, identifies it.
    return { code, logs };
  } finally { clearTimeout(timer); }
}

export function createPrechecker({ sdkRoot = '', run = (project, catalog, output) => runCompiler(project, catalog, output, sdkRoot), root = scratch } = {}) {
  let busy = false;
  return async function precheck(body) {
    if (!body || Object.keys(body).sort().join(',') !== 'catalogText,projectText' ||
        ['projectText', 'catalogText'].some(key => typeof body[key] !== 'string'))
      throw new PrecheckError(400, 'InvalidRequest', '请提交当前项目和目录文本。');
    if (busy) throw new PrecheckError(409, 'PrecheckBusy', '另一项预检查尚未结束，请稍后重试。');
    busy = true;
    let session;
    try {
      await mkdir(root, { recursive: true });
      session = await mkdtemp(path.join(root, 'request-'));
      const projectPath = path.join(session, 'project.json'), catalogPath = path.join(session, 'catalog.json'), output = path.join(session, 'output');
      await writeFile(projectPath, body.projectText, 'utf8');
      await writeFile(catalogPath, body.catalogText, 'utf8');
      const result = await run(projectPath, catalogPath, output);
      let report;
      try {
        const entries = await readdir(output);
        if (entries.length !== 1 || entries[0] !== 'validation.json') throw new Error('Unexpected precheck artifacts');
        report = JSON.parse(await readFile(path.join(output, 'validation.json'), 'utf8'));
        if (typeof report.success !== 'boolean' || !Array.isArray(report.diagnostics) ||
            report.diagnostics.some(d => !d || typeof d.code !== 'string' || typeof d.message !== 'string') ||
            (report.success && (result.code !== 0 || report.diagnostics.length || report.scope !== 'offline-reference-validation' || report.runtimeRevalidationRequired !== true)) ||
            (!report.success && (result.code === 0 || !report.diagnostics.length))) throw new Error('Invalid compiler report');
      } catch (error) { console.error('Monster precheck compiler:', error.message, result.logs); throw unavailable(result.logs); }
      return report;
    } catch (error) {
      if (error instanceof PrecheckError) throw error;
      console.error('Monster precheck:', error.message);
      throw unavailable();
    } finally {
      try {
        // The compiler never receives an existing output. Delete only our generated session.
        if (session && path.dirname(session) === path.resolve(root) && /^request-[\w-]+$/.test(path.basename(session)))
          await rm(session, { recursive: true, force: true });
      } finally { busy = false; }
    }
  };
}

export function createPrecheckHandler(precheck = createPrechecker(), { header = 'x-dtmapi-precheck', kind = '预检查' } = {}) {
  return async (req, res) => {
    const send = (status, value) => { res.writeHead(status, { 'Content-Type': 'application/json; charset=utf-8' }); res.end(JSON.stringify(value)); };
    try {
      if (req.method !== 'POST') { res.setHeader('Allow', 'POST'); throw new PrecheckError(405, 'MethodNotAllowed', kind + '需要 POST。'); }
      if (req.headers.origin !== `http://${req.headers.host}` || req.headers[header] !== '1')
        throw new PrecheckError(403, 'SameOriginRequired', '只接受本编辑器页面发起的' + kind + '。');
      if (req.headers['content-type']?.split(';')[0].trim() !== 'application/json')
        throw new PrecheckError(415, 'JsonRequired', '预检查需要 JSON 请求。');
      const chunks = [];
      for await (const chunk of req) {
        chunks.push(chunk);
      }
      let body;
      try { body = JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(Buffer.concat(chunks))); }
      catch { throw new PrecheckError(400, 'InvalidRequest', '预检查请求不是有效 JSON。'); }
      send(200, await precheck(body));
    } catch (error) { send(error.status ?? 503, { error: { code: error.code ?? 'PrecheckUnavailable', message: error.message } }); }
  };
}
