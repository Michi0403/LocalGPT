import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const proofPath = path.join(root, 'WORKSPACE-CONTENT-PROOF-v2.0.0.json');
const proof = JSON.parse(fs.readFileSync(proofPath, 'utf8'));
const ignoredParts = new Set(['bin', 'obj', 'node_modules', '.git', '.vs']);
const ignoredNames = new Set([
  'WORKSPACE-BASELINE-MANIFEST-v0.1.7.json',
  'WORKSPACE-CONTENT-PROOF-v2.0.0.json',
  'WORKSPACE-CONTENT-PROOF-v2.0.0.md'
]);
const sha256 = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const normalize = value => value.split(path.sep).join('/');
function inventory(directory) {
  const result = new Map();
  const visit = current => {
    for (const entry of fs.readdirSync(current, { withFileTypes: true })) {
      const absolute = path.join(current, entry.name);
      const relative = normalize(path.relative(directory, absolute));
      if (entry.isDirectory()) {
        if (!ignoredParts.has(entry.name)) visit(absolute);
      } else if (entry.isFile() && !ignoredNames.has(entry.name)) {
        result.set(relative, sha256(absolute));
      }
    }
  };
  visit(directory);
  return result;
}
const current = inventory(root);
assert.equal(proof.missingBaselineFiles.length, 0, 'The proof records missing baseline files.');
for (const item of proof.baselineFiles) {
  assert.ok(current.has(item.path), `Baseline workspace file disappeared: ${item.path}`);
  assert.equal(current.get(item.path), item.currentSha256, `Current workspace hash changed after proof generation: ${item.path}`);
}
for (const item of proof.addedFiles)
  assert.equal(current.get(item.path), item.currentSha256, `Added workspace file is missing or changed: ${item.path}`);
const expected = new Set([...proof.baselineFiles.map(item => item.path), ...proof.addedFiles.map(item => item.path)]);
assert.deepEqual([...current.keys()].filter(key => !expected.has(key)).sort(), [], 'The workspace contains unrecorded files; regenerate the preservation proof.');
console.log(`LocalGPT 2.0.0 workspace preservation proof passed: ${proof.baselineFileCount} baseline files retained, ${proof.addedFileCount} files added, zero baseline paths lost.`);
