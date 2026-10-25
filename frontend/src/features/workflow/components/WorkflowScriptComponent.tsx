import type { CSSProperties } from "react";
import { Button } from "primereact/button";
import { SelectButton } from "primereact/selectbutton";

import LabelComponent from "@/shared/components/LabelComponent";
import ScriptViewComponent from "@/shared/components/script-view/ScriptViewComponent";
import { useFlowScript } from "@/features/workflow/hooks/use-flow-parts";
import { useWorkflowStore, type ScriptView } from "@/features/workflow/store/workflow-store";
import FlowChangesComponent from "@/features/repository/components/FlowChangesComponent";
import FlowHistoryComponent from "@/features/repository/components/FlowHistoryComponent";
import { useBringBackFlow, useFlowRepositoryChanges, useRepositoryStatus } from "@/features/repository/hooks/use-repository";
import { UNCOMMITTED_COLOR, plural } from "@/features/repository/repository-format";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

const VIEWS: { value: ScriptView; label: string }[] = [
  { value: "script", label: "Script" },
  { value: "changes", label: "Changes" },
  { value: "history", label: "History" },
];

const NOTES: Record<ScriptView, string> = {
  script: "The flow as its file in the repository, the margin marking what changed since the last commit. Read only: change the flow in the other tabs.",
  changes: "What changed since the last commit.",
  history: "Every commit that changed this flow, each against the one before it.",
};

interface Props {
  flowId: number;
}

/** The Script tab: the flow's file, what changed in it since the last commit, and its commits. */
export function WorkflowScriptComponent({ flowId }: Props) {
  const { scriptView, setScriptView } = useWorkflowStore();
  const { data: status } = useRepositoryStatus();
  const { data: script, isLoading, error } = useFlowScript(flowId);
  const { data: changes } = useFlowRepositoryChanges(flowId);

  // Against the last commit; a flow no commit holds yet is all new. Without the repository, no marks.
  let committedScript: string | undefined;
  if (changes) committedScript = changes.committedScript ?? "";

  const changeCount = status?.flows.find((x) => x.flowId === flowId)?.changeCount ?? 0;

  const { discard } = useBringBackFlow(flowId);
  const { openConfirm } = useDialogStore();

  const askToDiscard = () =>
    openConfirm("flow-discard", {
      headerText: `Discard ${plural(changeCount, "change")}?`,
      confirmLabel: "Discard changes",
      confirmIcon: "pi pi-undo",
      confirmSeverity: "danger",
      children: <LabelComponent text="The flow goes back to how it was at the last commit, and what changed since is lost." />,
      onConfirm: () => discard.mutate(),
    });

  const viewTemplate = (option: { value: ScriptView; label: string }) => (
    <span className="flex align-items-center gap-2">
      {option.label}
      {option.value === "changes" && changeCount > 0 && (
        <span
          className="inline-flex align-items-center justify-content-center border-round-2xl text-xs font-bold px-2"
          style={{ minWidth: 20, height: 20, color: UNCOMMITTED_COLOR, background: "color-mix(in srgb, var(--yellow-500) 16%, transparent)" }}
        >
          {changeCount}
        </span>
      )}
    </span>
  );

  return (
    <div className="flex flex-column gap-3 p-3">
      <div className="flex align-items-center flex-wrap gap-3">
        <SelectButton
          value={scriptView}
          options={VIEWS}
          onChange={(e) => setScriptView(e.value as ScriptView)}
          itemTemplate={viewTemplate}
          allowEmpty={false}
        />

        <span
          className="text-sm flex-1"
          style={{ ...MUTED, minWidth: 220 }}
        >
          {NOTES[scriptView]}
        </span>

        {scriptView === "script" && (
          <Button
            type="button"
            label="Copy"
            icon="pi pi-copy"
            size="small"
            outlined
            disabled={!script}
            onClick={() => void navigator.clipboard.writeText(script ?? "")}
          />
        )}

        {scriptView === "changes" && changeCount > 0 && (
          <Button
            type="button"
            label="Discard changes"
            icon="pi pi-undo"
            size="small"
            severity="danger"
            outlined
            loading={discard.isPending}
            tooltip="Puts the flow back as it was at the last commit"
            tooltipOptions={{ position: "bottom" }}
            onClick={askToDiscard}
          />
        )}
      </div>

      {discard.error && (
        <LabelComponent
          text={discard.error.message}
          color="error"
        />
      )}

      {scriptView === "script" && (
        <>
          {isLoading && (
            <LabelComponent
              text="Writing the script..."
              color="secondary"
            />
          )}
          {error && (
            <LabelComponent
              text={error instanceof Error ? error.message : String(error)}
              color="error"
            />
          )}
          {script && (
            <ScriptViewComponent
              script={script}
              marksFrom={committedScript}
              maxHeight={2000}
            />
          )}
        </>
      )}

      {scriptView === "changes" && <FlowChangesComponent flowId={flowId} />}
      {scriptView === "history" && <FlowHistoryComponent flowId={flowId} />}
    </div>
  );
}
