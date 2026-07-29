import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';
import { fileURLToPath } from 'node:url';

const testsRoot = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(testsRoot, '..', '..');
const read = relative => fs.readFileSync(path.join(repositoryRoot, relative), 'utf8');

test('LocalGPT architecture enforcement has no static or method debt baseline', () => {
  assert.deepEqual(JSON.parse(read('build/application-static-baseline.json')), []);
  assert.deepEqual(JSON.parse(read('build/method-diagnostics-baseline.json')), []);
  assert.deepEqual(JSON.parse(read('build/runtime-value-ownership-baseline.json')), []);
  assert.match(read('docs/SAFE_STATIC_RUNTIME_AND_DIAGNOSTICS_POLICY.md'), /Program\.cs/);
  assert.match(read('build/Assert-ApplicationStaticPolicy.ps1'), /Invoke-ArchitectureAudit/);
  assert.match(read('build/Assert-MethodDiagnostics.ps1'), /Invoke-ArchitectureAudit/);
});
