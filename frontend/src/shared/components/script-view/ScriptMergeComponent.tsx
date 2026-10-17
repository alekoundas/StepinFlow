import { useEffect, useRef } from "react";
import { MergeView } from "@codemirror/merge";

import { scriptViewSetup } from "@/shared/components/script-view/script-view-setup";

interface Props {
  original: string;
  script: string;
  originalLabel: string;
  scriptLabel: string;
  maxHeight?: number;
}

const MUTED = { color: "var(--text-color-secondary)", borderColor: "var(--surface-border)" };

/**
 * Two scripts side by side, the lines they share lined up and the long unchanged stretches
 * folded away, so what is left is what changed.
 */
export default function ScriptMergeComponent({ original, script, originalLabel, scriptLabel, maxHeight = 640 }: Props) {
  const parent = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!parent.current) return;

    // Each side grows with its script; the element around them scrolls.
    const view = new MergeView({
      a: { doc: original, extensions: scriptViewSetup(null) },
      b: { doc: script, extensions: scriptViewSetup(null) },
      parent: parent.current,
      gutter: true,
      collapseUnchanged: { margin: 2, minSize: 4 },
    });

    return () => view.destroy();
  }, [original, script]);

  return (
    <div className="flex flex-column">
      <div
        className="flex border-bottom-1 text-xs"
        style={MUTED}
      >
        <span className="flex-1 px-3 py-2">{originalLabel}</span>
        <span className="flex-1 px-3 py-2">{scriptLabel}</span>
      </div>
      <div
        ref={parent}
        className="overflow-auto"
        style={{ maxHeight }}
      />
    </div>
  );
}
