import { useEffect, useRef } from "react";
import { setDiagnostics } from "@codemirror/lint";
import { unifiedMergeView } from "@codemirror/merge";
import { EditorState, type Extension } from "@codemirror/state";
import { EditorView } from "@codemirror/view";

import { diagnosticsOf, scriptViewSetup } from "@/shared/components/script-view/script-view-setup";
import type { FlowScriptErrorDto } from "@/shared/models/flow-script-import-result-dto";

interface Props {
  script: string;

  /** When given, what changed from it is shown in place: its lines above the ones that replaced them. */
  original?: string;

  /** What the import said, marked at its line and column. */
  diagnostics?: FlowScriptErrorDto[];

  maxHeight?: number;
}

/**
 * A flow script in CodeMirror 6, read-only: coloured by a small tokenizer, its branches folding,
 * and the import's diagnostics as lint marks. The one script view: Check reads a draft with it,
 * and the export and import buttons are to show a file with it.
 */
export default function ScriptViewComponent({ script, original, diagnostics, maxHeight = 640 }: Props) {
  const parent = useRef<HTMLDivElement>(null);

  // CodeMirror owns what is inside the element. A new script builds a new view rather than being
  // dispatched into the old one, since nothing here is edited.
  useEffect(() => {
    if (!parent.current) return;

    const extensions: Extension[] = [scriptViewSetup(maxHeight)];
    if (original) extensions.push(unifiedMergeView({ original, mergeControls: false, syntaxHighlightDeletions: true }));

    const view = new EditorView({
      parent: parent.current,
      state: EditorState.create({ doc: script, extensions }),
    });

    const marks = diagnosticsOf(view.state.doc, diagnostics ?? []);
    view.dispatch(setDiagnostics(view.state, marks));

    return () => view.destroy();
  }, [script, original, diagnostics, maxHeight]);

  return <div ref={parent} />;
}
