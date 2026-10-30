// Re-locate researched symbols without copying native method bodies into this repository.
import { readFile, mkdir, writeFile } from 'node:fs/promises';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
const root = fileURLToPath(new URL('../../../', import.meta.url));
const manifest = JSON.parse(await readFile(resolve(root, 'docs/planning/monster-content/native-chain.json'), 'utf8'));
const rows = [], failures = [];
for (const group of manifest.groups) {
  const path = resolve(root, group.scope === 'workspace' ? '' : manifest.nativeRoot, group.file);
  try {
    const source = await readFile(path), lines = source.toString('utf8').split(/\r?\n/);
    const sha256 = createHash('sha256').update(source).digest('hex').toUpperCase();
    for (const member of group.members) {
      const escaped = member.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
      const prefix = group.implicitPublic ? '(?:(?:public|private)\\s+)?(?:bool|void|Monster(?:\\[\\])?)\\s+' : '(?:public|private|protected|internal)\\s+.*?\\b';
      const declaration = new RegExp(`^\\s*${prefix}${escaped}(?:<[^>]+>)?\\s*\\(`);
      const locations = lines.flatMap((line, i) => declaration.test(line) ? [i + 1] : []);
      rows.push({ scope: group.scope || 'native', file: group.file, member, lines: locations, sha256 });
      if (!locations.length) failures.push(`${group.file} :: ${member}`);
    }
  } catch (error) { failures.push(`${group.file}: ${error.message}`); }
}
const report = { baseline: manifest.baseline, declaredAssemblySha256: manifest.assemblySha256, checkedAt: new Date().toISOString(), kind: 'static-symbol-locations-only', failures, rows };
const destination = resolve(root, 'tools/monster-editor/evidence/native-chain-check.json');
await mkdir(dirname(destination), { recursive: true });
await writeFile(destination, JSON.stringify(report, null, 2) + '\n');
console.log(`${failures.length ? 'FAIL' : 'PASS'}: ${rows.length} symbol locations; ${failures.length} missing. ${destination}`);
for (const failure of failures) console.error(failure);
process.exitCode = failures.length ? 1 : 0;
