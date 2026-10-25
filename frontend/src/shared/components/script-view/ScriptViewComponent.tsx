import { useEffect, useRef } from "react";
import { setDiagnostics } from "@codemirror/lint";
import { unifiedMergeView } from "@codemirror/merge";
import { EditorState, type Extension } from "@codemirror/state";
import { EditorView } from "@codemirror/view";

import { changeGutter } from "@/shared/components/script-view/change-gutter";
import { diagnosticsOf, scriptViewSetup } from "@/shared/components/script-view/script-view-setup";
import type { FlowScriptErrorDto } from "@/shared/models/flow-script-import-result-dto";

interface Props {
  script: string;

  /** When given, what changed from it is shown in place: its lines above the ones that replaced them. */
  original?: string;

  /** With an original, the lines that did not change fold away to a few either side of each change. */
  collapseUnchanged?: boolean;

  /** When given, each line that differs from it is marked in the margin, and the script stays as it is. */
  marksFrom?: string;

  /** What the import said, marked at its line and column. */
  diagnostics?: FlowScriptErrorDto[];

  maxHeight?: number;
}

/**
 * A flow script in CodeMirror 6, read-only: coloured by a small tokenizer, its branches folding,
 * and the import's diagnostics as lint marks. The one script view: Check reads a draft with it,
 * and the export and import buttons are to show a file with it.
 */
export default function ScriptViewComponent({ script, original, collapseUnchanged, marksFrom, diagnostics, maxHeight = 640 }: Props) {
  const parent = useRef<HTMLDivElement>(null);

  // CodeMirror owns what is inside the element. A new script builds a new view rather than being
  // dispatched into the old one, since nothing here is edited.
  useEffect(() => {
    if (!parent.current) return;

    // Before the setup, so its bar sits left of the line numbers.
    const extensions: Extension[] = [];
    if (marksFrom !== undefined) extensions.push(changeGutter(marksFrom, script));
    extensions.push(scriptViewSetup(maxHeight));
    // An empty original is a file that is all new, every line of it added.
    if (original !== undefined) {
      extensions.push(
        unifiedMergeView({
          original,
          mergeControls: false,
          syntaxHighlightDeletions: true,
          collapseUnchanged: collapseUnchanged ? { margin: 3, minSize: 6 } : undefined,
        }),
      );
    }

    const view = new EditorView({
      parent: parent.current,
      state: EditorState.create({ doc: script, extensions }),
    });

    const marks = diagnosticsOf(view.state.doc, diagnostics ?? []);
    view.dispatch(setDiagnostics(view.state, marks));

    return () => view.destroy();
  }, [script, original, collapseUnchanged, marksFrom, diagnostics, maxHeight]);

  return <div ref={parent} />;
}
