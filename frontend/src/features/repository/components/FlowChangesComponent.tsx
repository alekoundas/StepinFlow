import type { CSSProperties } from "react";

import LabelComponent from "@/shared/components/LabelComponent";
import ScriptViewComponent from "@/shared/components/script-view/ScriptViewComponent";
import TemplateImageChangesComponent from "@/features/repository/components/TemplateImageChangesComponent";
import { useFlowRepositoryChanges } from "@/features/repository/hooks/use-repository";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

interface Props {
  flowId: number;
}

/** The flow's file against the last commit, its unchanged lines folded away, and its images. */
export default function FlowChangesComponent({ flowId }: Props) {
  const { data: changes, isLoading, error } = useFlowRepositoryChanges(flowId);

  if (isLoading)
    return (
      <LabelComponent
        text="Reading what changed..."
        color="secondary"
      />
    );

  if (error)
    return (
      <LabelComponent
        text={error instanceof Error ? error.message : String(error)}
        color="error"
      />
    );

  if (!changes) return null;

  if (changes.changeCount === 0)
    return (
      <div
        className="flex flex-column align-items-center gap-2 p-5 border-round-lg text-center"
        style={{ border: "1px dashed var(--surface-border)" }}
      >
        <i
          className="pi pi-check-circle text-2xl"
          style={{ color: "var(--green-400)" }}
          aria-hidden="true"
        />
        <span className="font-medium">Nothing to commit</span>
        <span
          className="text-sm"
          style={MUTED}
        >
          The flow is exactly as it was at the last commit.
        </span>
      </div>
    );

  const isScriptChanged = changes.script !== (changes.committedScript ?? "");

  return (
    <div className="flex flex-column gap-3">
      {isScriptChanged ? (
        <ScriptViewComponent
          script={changes.script}
          original={changes.committedScript ?? ""}
          collapseUnchanged
          maxHeight={2000}
        />
      ) : (
        <span
          className="text-sm"
          style={MUTED}
        >
          The script itself did not change.
        </span>
      )}

      <TemplateImageChangesComponent images={changes.images} />
    </div>
  );
}
