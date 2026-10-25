import { GitChangeKindEnum } from "@/shared/enums/backend/git-change-kind-enum";
import type { RepositoryFileChangeDto } from "@/shared/models/repository-dtos";

export const plural = (count: number, noun: string) => `${count} ${noun}${count === 1 ? "" : "s"}`;

/** How each kind of change looks, wherever one is listed. */
export const CHANGE_KIND: Record<GitChangeKindEnum, { letter: string; word: string; color: string }> = {
  ADDED: { letter: "A", word: "added", color: "var(--green-400)" },
  MODIFIED: { letter: "M", word: "changed", color: "var(--blue-400)" },
  DELETED: { letter: "D", word: "removed", color: "var(--red-400)" },
};

/** Saved, not committed yet. */
export const UNCOMMITTED_COLOR = "var(--yellow-500)";

export const MONO = "ui-monospace, Consolas, monospace";

const MIME: Record<string, string> = {
  png: "image/png",
  jpg: "image/jpeg",
  jpeg: "image/jpeg",
  bmp: "image/bmp",
  gif: "image/gif",
  webp: "image/webp",
  tif: "image/tiff",
  tiff: "image/tiff",
};

/** A template image's bytes, as the backend sends them, ready for an img. */
export const imageSource = (name: string, base64: string): string => {
  const extension = name.split(".").pop()?.toLowerCase() ?? "";
  return `data:${MIME[extension] ?? "image/png"};base64,${base64}`;
};

/**
 * A commit message from what changed in one flow, for the person to keep or rewrite:
 * "Login: script changed; 1 template added, 2 templates removed".
 */
export const describeChanges = (flowName: string, files: RepositoryFileChangeDto[]): string => {
  const parts: string[] = [];

  const script = files.find((x) => !x.isImage && x.path.toLowerCase().endsWith(".sflw"));
  if (script?.kind === GitChangeKindEnum.ADDED) parts.push("new flow");
  else if (script?.kind === GitChangeKindEnum.DELETED) parts.push("flow removed");
  else if (script) parts.push("script changed");

  const images = files.filter((x) => x.isImage);
  const templates = (Object.keys(CHANGE_KIND) as GitChangeKindEnum[])
    .map((kind) => ({ kind, count: images.filter((x) => x.kind === kind).length }))
    .filter((x) => x.count > 0)
    .map((x) => `${plural(x.count, "template")} ${CHANGE_KIND[x.kind].word}`);

  if (templates.length > 0) parts.push(templates.join(", "));

  const what = parts.length > 0 ? parts.join("; ") : "changed";
  return flowName ? `${flowName}: ${what}` : `Repository: ${what}`;
};

export const formatWhen = (value: string): string =>
  new Date(value).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" });
