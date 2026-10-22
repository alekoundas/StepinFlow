import { Button } from "primereact/button";

import LabelComponent from "@/shared/components/LabelComponent";
import ScriptViewComponent from "@/shared/components/script-view/ScriptViewComponent";
import { useFlowScript } from "@/features/workflow/hooks/use-flow-parts";

interface Props {
  flowId: number;
}

/** The Script tab: the flow as Export writes it. Read only. */
export function WorkflowScriptComponent({ flowId }: Props) {
  const { data: script, isLoading, error } = useFlowScript(flowId);

  return (
    <div className="flex flex-column gap-2 p-3">
      <div className="flex align-items-center justify-content-between flex-wrap gap-3">
        <span className="text-sm text-color-secondary">
          What Export writes. Read only: change the flow in the other tabs.
        </span>
        <Button
          type="button"
          label="Copy"
          icon="pi pi-copy"
          size="small"
          outlined
          disabled={!script}
          onClick={() => void navigator.clipboard.writeText(script ?? "")}
        />
      </div>

      {isLoading && <LabelComponent text="Writing the script..." color="secondary" />}
      {error && (
        <LabelComponent
          text={error instanceof Error ? error.message : String(error)}
          color="error"
        />
      )}
      {script && (
        <ScriptViewComponent
          script={script}
          maxHeight={2000}
        />
      )}
    </div>
  );
}
