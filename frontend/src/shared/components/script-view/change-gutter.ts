import { diff } from "@codemirror/merge";
import { RangeSet, type Extension } from "@codemirror/state";
import { gutter, GutterMarker } from "@codemirror/view";

type LineChange = "added" | "changed" | "removedAbove";

const BAR_WIDTH = 3;

const LOOKS: Record<LineChange, { background: string; title: string }> = {
  added: { background: "var(--green-400)", title: "Added since the last commit" },
  changed: { background: "var(--indigo-300)", title: "Changed since the last commit" },
  removedAbove: {
    background: "linear-gradient(var(--red-400) 0 5px, transparent 5px)",
    title: "A line above it was removed since the last commit",
  },
};

/**
 * A bar left of the line numbers on each line that differs from the original: added, changed, or
 * with lines removed above it. The original is the last commit, so the margin says what a commit
 * would take without leaving the script.
 */
export function changeGutter(original: string, script: string): Extension {
  const changes = lineChanges(original, script);

  return gutter({
    class: "cm-changeGutter",
    markers: (view) => {
      const doc = view.state.doc;
      const ranges = [...changes.entries()]
        .filter(([line]) => line <= doc.lines)
        .sort(([a], [b]) => a - b)
        .map(([line, change]) => new ChangeMarker(change).range(doc.line(line).from));

      return RangeSet.of(ranges);
    },
    initialSpacer: () => new ChangeMarker(null),
  });
}

/** Each line of the script that differs from the original, by its line number. */
export function lineChanges(original: string, script: string): Map<number, LineChange> {
  const before = original.split(/\r?\n/);
  const after = script.split(/\r?\n/);

  // One character per distinct line, so a character diff is a line diff.
  const codes = new Map<string, string>();
  const encode = (lines: string[]) => lines.map((x) => codeOf(codes, x)).join("");

  const marks = new Map<number, LineChange>();
  for (const change of diff(encode(before), encode(after))) {
    // Only removed: marked on the line now where they were, or the last line when they ended the file.
    if (change.fromB === change.toB) {
      const line = Math.min(change.fromB + 1, after.length);
      if (!marks.has(line)) marks.set(line, "removedAbove");
      continue;
    }

    let kind: LineChange = "changed";
    if (change.fromA === change.toA) kind = "added";

    for (let index = change.fromB; index < change.toB; index++) marks.set(index + 1, kind);
  }

  return marks;
}

// Characters from U+0100 up, stepping over the surrogate range, which a string cannot hold alone.
function codeOf(codes: Map<string, string>, line: string): string {
  let code = codes.get(line);
  if (code !== undefined) return code;

  let point = 0x100 + codes.size;
  if (point >= 0xd800) point += 0x800;

  code = String.fromCharCode(point);
  codes.set(line, code);

  return code;
}

class ChangeMarker extends GutterMarker {
  private readonly change: LineChange | null;

  constructor(change: LineChange | null) {
    super();
    this.change = change;
  }

  eq(other: GutterMarker): boolean {
    return other instanceof ChangeMarker && other.change === this.change;
  }

  toDOM(): Node {
    const bar = document.createElement("div");
    bar.style.width = `${BAR_WIDTH}px`;
    bar.style.height = "100%";

    if (this.change) {
      bar.style.background = LOOKS[this.change].background;
      bar.title = LOOKS[this.change].title;
    }

    return bar;
  }
}
