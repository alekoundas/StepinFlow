// electron-builder can finish with success and leave a release the updater cannot use: the files split
// across two drafts of one tag, with latest.yml naming an installer that is on the other draft.
import { readFileSync } from "node:fs";

const TAG = process.argv[2];
const RELEASES = `https://api.github.com/repos/${process.env.GITHUB_REPOSITORY}/releases?per_page=100`;

const installer = readFileSync("release/latest.yml", "utf8").match(/^path: (.+)$/m)?.[1];
if (!installer) throw new Error("release/latest.yml names no installer.");

// What the updater reads.
const NEEDED = ["latest.yml", installer, `${installer}.blockmap`];

const response = await fetch(RELEASES, {
  headers: { Accept: "application/vnd.github+json", Authorization: `Bearer ${process.env.GH_TOKEN}` },
});
if (!response.ok) throw new Error(`Releases: ${response.status} ${response.statusText}`);

const filesOn = (release) =>
  release.assets.filter((asset) => asset.state === "uploaded").map((asset) => asset.name);

const releases = (await response.json()).filter((release) => release.tag_name === TAG);
const problems = [];

if (releases.length !== 1) {
  const files = releases.map((release) => `[${filesOn(release).join(", ")}]`).join(" and ");
  problems.push(`${TAG} has ${releases.length} releases instead of one: ${files}`);
}

for (const name of NEEDED) {
  if (!releases.some((release) => filesOn(release).includes(name))) problems.push(`${name} is not on ${TAG}.`);
}

for (const problem of problems) console.log(`::error::${problem}`);

// Not process.exit: on Windows it can cut off what was just printed to a pipe.
if (problems.length > 0) process.exitCode = 1;
else console.log(`${TAG} has ${NEEDED.join(", ")}.`);
