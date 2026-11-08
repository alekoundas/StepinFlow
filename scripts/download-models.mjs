// The model files are too big for git, so each family lives in a release of its own, published by a
// workflow in .github/workflows, and is fetched once. Every file is checked against the SHA-256 its
// release listed: one already here is kept when it matches and fetched again when it does not, and
// a download that does not match is never kept.
import { createHash } from "node:crypto";
import { existsSync, mkdirSync, renameSync } from "node:fs";
import { readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const RELEASES = "https://github.com/alekoundas/StepinFlow/releases/download";
const TARGET = path.join(path.dirname(fileURLToPath(import.meta.url)), "../backend/Core/AiModels");

// A new model is a new release tag, which is also what the workflow's cache is keyed on.
const FILES = [
  // publish-text-embedder.yml
  ["text-embedder-v1", "text-embedder.onnx", "828e1496d7fabb79cfa4dcd84fa38625c0d3d21da474a00f08db0f559940cf35"],
  ["text-embedder-v1", "text-embedder-vocab.txt", "07eced375cec144d27c900241f3e339478dec958f92fddbc551f295c992038a3"],
  ["text-embedder-v1", "BGE.LICENSE.txt", "af18df4f7030de4b68169fad1f41404d1f719193edf1571a0bc0bcadc0596f43"],

  // publish-element-detector.yml
  ["element-detector-v1", "element-detector.onnx", "a18ad00938933ee8bb1aeb3181c651c2fd5041ff8715389b8d25930c9bc92bf1"],
  ["element-detector-v1", "OmniParser.LICENSE.txt", "773fee11c75a7bb3a4d6c3b2706589ece0087f2cb2dcb79420bcbe2fcb2bb342"],

  // publish-ocr-models.yml. The line finder and the Latin reader ship; the other readers are
  // downloaded from Settings.
  ["ocr-v1", "ocr-line-finder.onnx", "a431985659dc921974177a95adcfbb90fd9e51989a5e04d70d0b75f597b6e61d"],
  ["ocr-v1", "ocr-reader-latin.onnx", "7888113072263cb471b93f66dd5e2ad70548dc526fa1ace760d0d973dd121498"],
  ["ocr-v1", "ocr-reader-latin.yml", "0bbe984570f597af3638e50bdf2e8276f3ab26a61966096538b3b0d1849f5c84"],
  ["ocr-v1", "PaddleOCR.LICENSE.txt", "cdb7d3c73c70b102e47b7dc68e8b7c801c282faaca07f48a3c36a633fe9d7ba8"],
];

function sha256(bytes) {
  return createHash("sha256").update(bytes).digest("hex");
}

mkdirSync(TARGET, { recursive: true });

for (const [tag, name, expected] of FILES) {
  const file = path.join(TARGET, name);
  if (existsSync(file)) {
    if (sha256(await readFile(file)) === expected) continue;
    console.log(`${name} is not the file ${tag} holds; fetching it again`);
  }

  const response = await fetch(`${RELEASES}/${tag}/${name}`);
  if (!response.ok) throw new Error(`${name}: ${response.status} ${response.statusText}`);

  const bytes = Buffer.from(await response.arrayBuffer());
  const actual = sha256(bytes);
  if (actual !== expected) throw new Error(`${name}: SHA-256 ${actual}, expected ${expected}. Not kept.`);

  // Written aside and moved in, so a download cut short is never taken for the file.
  await writeFile(`${file}.part`, bytes);
  renameSync(`${file}.part`, file);
  console.log(`Downloaded ${name}`);
}
