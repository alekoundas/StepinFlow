import type { CSSProperties } from "react";
import type { FlowScriptErrorDto } from "@/shared/models/flow-script-error-dto";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

const MONO: CSSProperties = { fontFamily: "ui-monospace, Consolas, monospace" };

// Each line as the scanner said it, which is also what went back to the model.
export function RefusedComponent({ refused, note }: { refused: FlowScriptErrorDto[]; note?: string }) {
  return (
    <span
      className="block mt-2 px-3 py-2 border-1 border-round-lg text-xs"
      style={{ ...MONO, ...MUTED, background: "var(--surface-ground)", borderColor: "var(--surface-border)", whiteSpace: "pre-wrap" }}
    >
      {refused.map((x) => `line ${x.line}, column ${x.column} · ${x.code}\n${x.message}`).join("\n")}
      {note && `\n${note}`}
    </span>
  );
}
