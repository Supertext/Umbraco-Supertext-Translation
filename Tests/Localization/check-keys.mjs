// Checks the backoffice localization: de, fr and it have exactly the keys of en (same kind:
// text or function with the same number of arguments), and every `supertext_…` key the
// extension uses exists. Run: node Tests/Localization/check-keys.mjs
import { readFileSync, readdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '../..');
const pluginDir = join(root, 'src/Supertext.Umbraco.Translation/wwwroot/App_Plugins/SupertextTranslation');
const languages = ['en', 'de', 'fr', 'it'];
const load = async (c) => (await import(pathToFileURL(join(pluginDir, 'lang', `${c}.js`)).href)).default.supertext;
const shape = (v) => (typeof v === 'function' ? `function(${v.length})` : typeof v);

const errors = [];
const en = await load('en');
for (const c of languages.slice(1)) {
  const other = await load(c);
  for (const key of Object.keys(en)) {
    if (!(key in other)) errors.push(`${c}.js: missing ${key}`);
    else if (shape(other[key]) !== shape(en[key])) errors.push(`${c}.js: ${key} is ${shape(other[key])}, en has ${shape(en[key])}`);
  }
  for (const key of Object.keys(other)) if (!(key in en)) errors.push(`${c}.js: ${key} is not in en.js`);
}

// Keys referenced in the extension (code and manifest).
const sources = readdirSync(pluginDir).filter((f) => f.endsWith('.js') || f.endsWith('.json'));
for (const file of sources) {
  const text = readFileSync(join(pluginDir, file), 'utf8');
  for (const [, key] of text.matchAll(/supertext_(\w+)/g)) {
    if (key === 'error_') continue; // built from the server's error code
    if (!(key in en)) errors.push(`${file}: uses supertext_${key}, which en.js does not define`);
  }
}

// Error codes the server sends (SupertextException codes) need a supertext_error_<code> text.
const walk = (dir) => readdirSync(dir, { withFileTypes: true }).flatMap((d) =>
  d.isDirectory() ? (['bin', 'obj', 'wwwroot'].includes(d.name) ? [] : walk(join(dir, d.name))) : [join(dir, d.name)]);
for (const file of walk(join(root, 'src/Supertext.Umbraco.Translation')).filter((f) => f.endsWith('.cs'))) {
  const text = readFileSync(file, 'utf8');
  const codes = [...text.matchAll(/new SupertextException\("(\w+)"/g), ...text.matchAll(/=> \("(\w+)", "/g)].map((m) => m[1]);
  for (const code of codes) {
    if (!(`error_${code}` in en)) errors.push(`${file.slice(root.length + 1)}: error code ${code} has no supertext_error_${code} in en.js`);
  }
}

// Each language needs a localization manifest.
const manifest = JSON.parse(readFileSync(join(pluginDir, 'umbraco-package.json'), 'utf8'));
for (const c of languages) {
  if (!manifest.extensions.some((e) => e.type === 'localization' && e.meta?.culture === c && e.js?.endsWith(`/lang/${c}.js`))) {
    errors.push(`umbraco-package.json: no localization extension for ${c}`);
  }
}

if (errors.length) {
  console.error(errors.join('\n'));
  process.exit(1);
}
console.log(`Localization OK: ${Object.keys(en).length} keys in ${languages.join(', ')}.`);
