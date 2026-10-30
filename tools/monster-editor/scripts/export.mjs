import { mkdir, mkdtemp, readFile, writeFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { PrecheckError, runPowerShell } from './precheck.mjs';

const scratch = fileURLToPath(new URL('../.runtime/export/', import.meta.url));
const unavailable = () => new PrecheckError(503, 'ExportUnavailable', '导出未完成。请检查本机 .NET、编译器和服务器日志；仍可保存作者项目。');

export function createExporter({ sdkRoot = '', root = scratch, run = runPowerShell } = {}) {
  let busy = false;
  async function execute(body) {
    if (body !== null && (!body || Object.keys(body).sort().join(',') !== 'catalogText,projectText' ||
        ['projectText', 'catalogText'].some(key => typeof body[key] !== 'string')))
      throw new PrecheckError(400, 'InvalidRequest', '请提交当前项目和目录文本，不接受路径或命令。');
    if (busy) throw new PrecheckError(409, 'ExportBusy', '另一项导出检查尚未结束。');
    busy = true;
    let session, temporaryDirectory;
    try {
      await mkdir(root, { recursive: true });
      session = await mkdtemp(path.join(root, 'request-'));
      // Own child scratch trees so a process timeout can clean up its partial output.
      temporaryDirectory = await mkdtemp(path.join(tmpdir(), 'DTMAPI-MonsterEditorExport-'));
      const output = path.join(session, 'output');
      const args = ['-OutputRoot', output];
      if (sdkRoot) args.push('-SdkRoot', sdkRoot);
      if (body === null) args.push('-CheckEnvironment');
      else {
        const projectPath = path.join(session, 'project.json'), catalogPath = path.join(session, 'catalog.json');
        await writeFile(projectPath, body.projectText, 'utf8'); await writeFile(catalogPath, body.catalogText, 'utf8');
        args.push('-ProjectPath', projectPath, '-CatalogPath', catalogPath);
      }
      const result = await run('export-monster-project.ps1', args, {
        timeoutMs: 180000, timeoutCode: 'ExportTimeout', timeoutMessage: '导出超过 180 秒，已停止本次工具进程；请检查环境后重试。', temporaryDirectory
      });
      try {
        const report = JSON.parse(await readFile(path.join(output, 'export-report.json'), 'utf8'));
        if (typeof report.success !== 'boolean' || report.scope !== 'content-pack' ||
            !Array.isArray(report.diagnostics) || report.diagnostics.some(d => !d || typeof d.code !== 'string' || typeof d.message !== 'string') ||
            (report.success && (result.code !== 0 || report.diagnostics.length)) ||
            (!report.success && (result.code === 0 || !report.diagnostics.length))) throw new Error('Invalid export report');
        if (report.success && body !== null) {
          const pack = report.package;
          if (!pack || typeof pack.fileName !== 'string' || !/^[\p{L}\p{N}_.-]+\.zip$/u.test(pack.fileName) ||
              typeof pack.relativePath !== 'string') throw new Error('Invalid package output');
          const packagePath = path.resolve(output, pack.relativePath);
          const relative = path.relative(output, packagePath);
          if (!relative || relative.startsWith('..') || path.isAbsolute(relative) || path.extname(relative) !== '.zip')
            throw new Error('Package path escapes export output');
          const bytes = await readFile(packagePath);
          pack.base64 = bytes.toString('base64');
          delete pack.relativePath;
        } else if (!report.success) delete report.package;
        return report;
      } catch (error) { console.error('Monster export:', error.message, result.logs); throw unavailable(); }
    } catch (error) {
      if (error instanceof PrecheckError) throw error;
      console.error('Monster export service:', error.message); throw unavailable();
    } finally {
      try {
        try {
          if (session && path.dirname(session) === path.resolve(root) && /^request-[\w-]+$/.test(path.basename(session)))
            await rm(session, { recursive: true, force: true });
        } finally {
          if (temporaryDirectory && path.dirname(temporaryDirectory) === path.resolve(tmpdir()) && /^DTMAPI-MonsterEditorExport-[\w-]+$/.test(path.basename(temporaryDirectory)))
            await rm(temporaryDirectory, { recursive: true, force: true });
        }
      } finally { busy = false; }
    }
  }
  return { exportProject: body => {
    if (body === null) throw new PrecheckError(400, 'InvalidRequest', '请提交项目和目录文本。');
    return execute(body);
  }, environment: () => execute(null) };
}
