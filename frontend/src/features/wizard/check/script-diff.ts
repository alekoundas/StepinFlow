export type DiffLineKind = "same" | "added" | "removed";

/** A line of either script. Its number is in the saved script, and null for a line only the draft had. */
export interface DiffLine {
  kind: DiffLineKind;
  text: string;
  number: number | null;
}

export type LineMark = "added" | "changed" | null;

/**
 * The draft against the saved script, line by line, keeping the longest run the two have in
 * common. Within a run of changes the draft's lines come first, as a reader compares them. The
 * merge view the script view gets with CodeMirror replaces this.
 */
export function diffLines(before: string, after: string): DiffLine[] {
  const a = linesOf(before);
  const b = linesOf(after);

  // The common head and tail are most of a script, and cost nothing to set aside.
  let start = 0;
  while (start < a.length && start < b.length && a[start] === b[start]) start++;

  let endA = a.length;
  let endB = b.length;
  while (endA > start && endB > start && a[endA - 1] === b[endB - 1]) {
    endA--;
    endB--;
  }

  const n = endA - start;
  const m = endB - start;
  const width = m + 1;

  // common[i * width + j]: the longest run in common from line i of the draft and line j of the save.
  const common = new Uint32Array((n + 1) * width);
  for (let i = n - 1; i >= 0; i--) {
    for (let j = m - 1; j >= 0; j--) {
      if (a[start + i] === b[start + j]) common[i * width + j] = common[(i + 1) * width + j + 1] + 1;
      else common[i * width + j] = Math.max(common[(i + 1) * width + j], common[i * width + j + 1]);
    }
  }

  const lines: DiffLine[] = [];
  for (let k = 0; k < start; k++) lines.push({ kind: "same", text: b[k], number: k + 1 });

  let i = 0;
  let j = 0;
  while (i < n || j < m) {
    const isSame = i < n && j < m && a[start + i] === b[start + j];
    const isRemoved = !isSame && i < n && (j === m || common[(i + 1) * width + j] >= common[i * width + j + 1]);

    if (isSame) {
      lines.push({ kind: "same", text: b[start + j], number: start + j + 1 });
      i++;
      j++;
    } else if (isRemoved) {
      lines.push({ kind: "removed", text: a[start + i], number: null });
      i++;
    } else {
      lines.push({ kind: "added", text: b[start + j], number: start + j + 1 });
      j++;
    }
  }

  for (let k = endB; k < b.length; k++) lines.push({ kind: "same", text: b[k], number: k + 1 });

  return lines;
}

/**
 * One mark per line of the saved script: a line that took the place of one of the draft's is
 * changed, any more in that run are added.
 */
export function marksOf(lines: DiffLine[]): LineMark[] {
  const marks: LineMark[] = [];
  let removed = 0;

  for (const line of lines) {
    if (line.kind === "removed") {
      removed++;
      continue;
    }

    if (line.kind === "same") {
      removed = 0;
      marks.push(null);
      continue;
    }

    marks.push(removed > 0 ? "changed" : "added");
    if (removed > 0) removed--;
  }

  return marks;
}

/** The runs of changes, each with `context` unchanged lines either side, as a unified diff shows them. */
export function hunksOf(lines: DiffLine[], context: number): DiffLine[][] {
  const isNear = lines.map(() => false);

  lines.forEach((line, index) => {
    if (line.kind === "same") return;

    const from = Math.max(0, index - context);
    const to = Math.min(lines.length - 1, index + context);
    for (let k = from; k <= to; k++) isNear[k] = true;
  });

  const hunks: DiffLine[][] = [];
  let hunk: DiffLine[] = [];

  for (let index = 0; index < lines.length; index++) {
    if (!isNear[index]) {
      if (hunk.length > 0) hunks.push(hunk);
      hunk = [];
      continue;
    }

    hunk.push(lines[index]);
  }

  if (hunk.length > 0) hunks.push(hunk);

  return hunks;
}

export function linesOf(script: string): string[] {
  return script.replace(/\r\n/g, "\n").replace(/\n$/, "").split("\n");
}
