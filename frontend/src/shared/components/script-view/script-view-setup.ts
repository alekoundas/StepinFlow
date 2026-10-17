import { codeFolding, foldGutter, foldKeymap } from "@codemirror/language";
import { type Diagnostic, lintGutter } from "@codemirror/lint";
import { EditorState, type Extension, type Text } from "@codemirror/state";
import { EditorView, keymap, lineNumbers } from "@codemirror/view";

import { flowScript } from "@/shared/components/script-view/flow-script-language";
import type { FlowScriptErrorDto } from "@/shared/models/flow-script-import-result-dto";

const MONO = "ui-monospace, Consolas, monospace";

/**
 * A script to read: coloured, numbered, its branches folding, and read-only until editing is
 * wanted. The colours are the theme's variables, so the view follows the app rather than CodeMirror.
 * Without a height it grows with the script, as each side of a merge view does inside its scroller.
 */
export function scriptViewSetup(maxHeight: number | null): Extension {
  return [
    flowScript(),
    lineNumbers(),
    codeFolding(),
    foldGutter(),
    lintGutter(),
    keymap.of(foldKeymap),
    EditorState.readOnly.of(true),
    scriptViewTheme(maxHeight),
  ];
}

/** What the import or the model's refusal said, at its line and column. */
export function diagnosticsOf(doc: Text, errors: FlowScriptErrorDto[]): Diagnostic[] {
  return errors
    .filter((x) => x.line >= 1 && x.line <= doc.lines)
    .map((x) => {
      const line = doc.line(x.line);
      const from = Math.min(line.from + Math.max(x.column - 1, 0), line.to);

      let severity: Diagnostic["severity"] = "warning";
      if (x.severity === "ERROR") severity = "error";

      return { from, to: line.to, severity, message: x.message, source: x.code };
    });
}

function scriptViewTheme(maxHeight: number | null): Extension {
  let height = "none";
  if (maxHeight !== null) height = `${maxHeight}px`;

  return EditorView.theme(
    {
      "&": {
        maxHeight: height,
        backgroundColor: "transparent",
        color: "var(--text-color-secondary)",
        fontSize: "12.5px",
      },
      "&.cm-focused": { outline: "none" },
      ".cm-scroller": { fontFamily: MONO, lineHeight: "1.75", overflow: "auto" },
      ".cm-content": { caretColor: "var(--primary-color)", padding: "8px 0" },
      ".cm-gutters": {
        backgroundColor: "transparent",
        color: "var(--text-color-secondary)",
        border: "none",
      },
      ".cm-lineNumbers .cm-gutterElement": { padding: "0 8px 0 12px", opacity: "0.6" },
      ".cm-foldPlaceholder": {
        backgroundColor: "var(--surface-hover)",
        border: "1px solid var(--surface-border)",
        color: "var(--text-color-secondary)",
        padding: "0 6px",
      },
      ".cm-selectionBackground, &.cm-focused .cm-selectionBackground, ::selection": {
        backgroundColor: "var(--highlight-bg)",
      },
      ".cm-tooltip": {
        backgroundColor: "var(--surface-overlay)",
        border: "1px solid var(--surface-border)",
        color: "var(--text-color)",
      },
      ".cm-diagnostic-warning": { borderLeftColor: "var(--yellow-400)" },
      ".cm-diagnostic-error": { borderLeftColor: "var(--red-400)" },
      ".cm-lintRange-warning": { backgroundImage: "none", textDecoration: "underline wavy var(--yellow-400)" },
      ".cm-lintRange-error": { backgroundImage: "none", textDecoration: "underline wavy var(--red-400)" },
      // What the draft had is red, what replaced it green, side by side or one above the other.
      "&.cm-merge-a .cm-changedLine, .cm-deletedChunk": {
        backgroundColor: "color-mix(in srgb, var(--red-500) 10%, transparent)",
      },
      "&.cm-merge-b .cm-changedLine, .cm-inlineChangedLine": {
        backgroundColor: "color-mix(in srgb, var(--green-500) 10%, transparent)",
      },
      "&.cm-merge-a .cm-changedText, .cm-deletedChunk .cm-deletedText": {
        background: "color-mix(in srgb, var(--red-500) 28%, transparent)",
      },
      "&.cm-merge-b .cm-changedText": { background: "color-mix(in srgb, var(--green-500) 24%, transparent)" },
      "&.cm-merge-a .cm-changedLineGutter, .cm-deletedLineGutter": { background: "var(--red-400)" },
      "&.cm-merge-b .cm-changedLineGutter": { background: "var(--green-400)" },
      ".cm-collapsedLines": { color: "var(--text-color-secondary)", background: "var(--surface-hover)" },
    },
    { dark: true },
  );
}
