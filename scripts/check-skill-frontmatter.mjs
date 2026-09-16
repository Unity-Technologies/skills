#!/usr/bin/env node
// Fails when any skills/*/SKILL.md has frontmatter that a strict YAML parser rejects,
// or that lacks a non-empty `name` matching its folder or a non-empty `description`.
// Agents parse the frontmatter with a real YAML parser and silently drop a skill that
// fails, so this is the only place the defect becomes visible.
import { readdirSync, readFileSync, existsSync } from 'node:fs';
import { join } from 'node:path';
import yaml from 'js-yaml';

const root = process.argv[2] ?? 'skills';
let checked = 0;
const failures = [];

for (const dir of readdirSync(root, { withFileTypes: true })) {
  if (!dir.isDirectory()) continue;
  const path = join(root, dir.name, 'SKILL.md');
  if (!existsSync(path)) {
    failures.push(`${path}: missing`);
    continue;
  }
  const text = readFileSync(path, 'utf8');
  const match = text.match(/^---\r?\n([\s\S]*?)\r?\n---(?:\r?\n|$)/);
  if (!match) {
    failures.push(`${path}: no YAML frontmatter block`);
    continue;
  }
  let data;
  try {
    data = yaml.load(match[1]);
  } catch (error) {
    failures.push(`${path}: frontmatter is not valid YAML: ${error.message.split('\n')[0]}`);
    continue;
  }
  if (typeof data !== 'object' || data === null) {
    failures.push(`${path}: frontmatter is not a mapping`);
    continue;
  }
  if (typeof data.name !== 'string' || data.name.trim() === '') {
    failures.push(`${path}: missing or empty name`);
  } else if (data.name !== dir.name) {
    failures.push(`${path}: name "${data.name}" does not match folder "${dir.name}"`);
  }
  if (typeof data.description !== 'string' || data.description.trim() === '') {
    failures.push(`${path}: missing or empty description`);
  }
  checked += 1;
}

if (failures.length > 0) {
  console.error(`${failures.length} problem(s) in ${checked} skill(s):`);
  for (const failure of failures) console.error(`  ${failure}`);
  process.exit(1);
}
console.log(`${checked} skills: frontmatter OK`);
