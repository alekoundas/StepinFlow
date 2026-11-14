// onnxruntime.dll and libusearch_c.dll need the Visual C++ runtime, which a bare Windows lacks; without
// it Windows' own older onnxruntime.dll in System32 loads instead. The runtime ships beside App.exe,
// from the redist folder of the Visual Studio that builds it.
import { execFileSync } from "node:child_process";
import { copyFileSync, readdirSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const TARGET = path.join(path.dirname(fileURLToPath(import.meta.url)), "../dist/backend");
const VSWHERE = path.join(process.env["ProgramFiles(x86)"], "Microsoft Visual Studio/Installer/vswhere.exe");

const folders = execFileSync(VSWHERE, ["-latest", "-products", "*", "-find", "VC\\Redist\\MSVC\\*\\x64\\Microsoft.VC*.CRT"], { encoding: "utf8" })
  .split(/\r?\n/)
  .filter(Boolean)
  .sort((a, b) => a.localeCompare(b, undefined, { numeric: true }));

const folder = folders.at(-1);
if (!folder) throw new Error("No Visual C++ runtime to ship: install Visual Studio's MSVC C++ build tools.");

const dlls = readdirSync(folder).filter((name) => name.toLowerCase().endsWith(".dll"));
for (const name of dlls) copyFileSync(path.join(folder, name), path.join(TARGET, name));

console.log(`Copied the Visual C++ runtime from ${folder}: ${dlls.join(", ")}`);
