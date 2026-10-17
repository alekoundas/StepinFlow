import type { CSSProperties } from "react";

import { type DiffLine, type LineMark, diffLines, hunksOf, linesOf, marksOf } from "@/features/wizard/check/script-diff";

const MONO: CSSProperties = { fontFamily: "ui-monospace, Consolas, monospace", fontSize: 12.5, lineHeight: 1.75 };
const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

const ADDED = "var(--green-400)";
const CHANGED = "var(--yellow-400)";
const REMOVED = "var(--red-400)";

/**
 * The saved script, numbered, with what the model added or changed marked against the draft it was
 * given. No draft, no marks. Colouring and folding come with CodeMirror.
 */
export function CheckScriptComponent({ script, draft }: { script: string; draft: string }) {
  const lines = linesOf(script);

  let marks: LineMark[] = [];
  if (draft) marks = marksOf(diffLines(draft, script));

  return (
    <div className="flex flex-column">
      <div
        className="overflow-auto py-2"
        style={{ maxHeight: 640 }}
      >
        <div style={{ ...MONO, whiteSpace: "pre", minWidth: "max-content" }}>
          {lines.map((text, index) => (
            <ScriptLineComponent
              key={index}
              mark={marks[index] ?? null}
              number={index + 1}
              text={text}
            />
          ))}
        </div>
      </div>

      {draft && (
        <div
          className="flex flex-wrap gap-4 px-3 py-2 border-top-1 text-xs"
          style={{ ...MUTED, borderColor: "var(--surface-border)" }}
        >
          <span>
            <span style={{ ...MONO, color: ADDED }}>+</span> added by the model
          </span>
          <span>
            <span style={{ ...MONO, color: CHANGED }}>~</span> changed from the draft
          </span>
        </div>
      )}
    </div>
  );
}

/** Each run of changes with two lines either side: the draft's lines struck, the saved ones added. */
export function CheckChangesComponent({ script, draft }: { script: string; draft: string }) {
  const hunks = hunksOf(diffLines(draft, script), 2);

  if (hunks.length === 0)
    return (
      <p
        className="m-0 p-4"
        style={MUTED}
      >
        The model changed nothing.
      </p>
    );

  return (
    <div
      className="overflow-auto py-2"
      style={{ maxHeight: 640 }}
    >
      <div style={{ ...MONO, whiteSpace: "pre", minWidth: "max-content" }}>
        {hunks.map((hunk, index) => (
          <div key={index}>
            {index > 0 && (
              <div
                className="pl-6"
                style={MUTED}
              >
                ⋯
              </div>
            )}
            {hunk.map((line, lineIndex) => (
              <ChangeLineComponent
                key={lineIndex}
                line={line}
              />
            ))}
          </div>
        ))}
      </div>
    </div>
  );
}

function ScriptLineComponent({ mark, number, text }: { mark: LineMark; number: number; text: string }) {
  let sign = "";
  let color = "transparent";
  let background = "transparent";

  if (mark === "added") {
    sign = "+";
    color = ADDED;
    background = "color-mix(in srgb, var(--green-500) 8%, transparent)";
  }

  if (mark === "changed") {
    sign = "~";
    color = CHANGED;
    background = "color-mix(in srgb, var(--yellow-500) 7%, transparent)";
  }

  return (
    <div
      className="flex"
      style={{ background }}
    >
      <span
        className="flex-none text-center"
        style={{ width: 18, color }}
      >
        {sign}
      </span>
      <span
        className="flex-none text-right pr-3"
        style={{ width: 44, ...MUTED }}
      >
        {number}
      </span>
      <span className="pr-3">{text || " "}</span>
    </div>
  );
}

function ChangeLineComponent({ line }: { line: DiffLine }) {
  let sign = " ";
  let color = "var(--text-color-secondary)";
  let background = "transparent";

  if (line.kind === "added") {
    sign = "+";
    color = ADDED;
    background = "color-mix(in srgb, var(--green-500) 8%, transparent)";
  }

  if (line.kind === "removed") {
    sign = "-";
    color = REMOVED;
    background = "color-mix(in srgb, var(--red-500) 8%, transparent)";
  }

  return (
    <div
      className="flex"
      style={{ background }}
    >
      <span
        className="flex-none text-center"
        style={{ width: 18, color }}
      >
        {sign}
      </span>
      <span
        className="flex-none text-right pr-3"
        style={{ width: 44, ...MUTED }}
      >
        {line.number ?? ""}
      </span>
      <span
        className="pr-3"
        style={{ color: line.kind === "same" ? "var(--text-color-secondary)" : "var(--text-color)" }}
      >
        {line.text || " "}
      </span>
    </div>
  );
}
