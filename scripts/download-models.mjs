// The model files are too big for git, so each lives in a release of its own
// (.github/workflows/publish-embedding-model.yml) and is fetched once. A file already there is kept.
import { existsSync, mkdirSync, renameSync } from "node:fs";
import { writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const RELEASES = "https://github.com/alekoundas/StepinFlow/releases/download";
const TARGET = path.join(path.dirname(fileURLToPath(import.meta.url)), "../backend/Core/AiModels");

// A new model is a new release tag, which is also what the workflow's cache is keyed on.
const FILES = [
  ["model-v1", "model.onnx"],
  ["model-v1", "vocab.txt"],
  ["vision-v1", "icon_detect_v3.onnx"],
  ["vision-v1", "icon_detect_v3.LICENSE.txt"],
];

mkdirSync(TARGET, { recursive: true });

for (const [tag, name] of FILES) {
  const file = path.join(TARGET, name);
  if (existsSync(file)) continue;

  const response = await fetch(`${RELEASES}/${tag}/${name}`);
  if (!response.ok) throw new Error(`${name}: ${response.status} ${response.statusText}`);

  // Written aside and moved in, so a download cut short is never taken for the file.
  await writeFile(`${file}.part`, Buffer.from(await response.arrayBuffer()));
  renameSync(`${file}.part`, file);
  console.log(`Downloaded ${name}`);
}
