import { Link } from "react-router-dom";
import { Button } from "primereact/button";

import type { FlowDto } from "@/shared/models/database/flow-dto";
import { ValidationSeverityEnum } from "@/shared/models/database/flow-validation-result-dto";
import { useFlowValidation } from "@/features/flow/hooks/use-flow-validation";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import RepositoryChipComponent from "@/features/repository/components/RepositoryChipComponent";

interface Props {
  flow: FlowDto | undefined;
  onExport: () => void;
  onExecute: () => void;
}

/**
 * Where the flow sits, what kind it is, whether it would execute, what is not committed yet, and
 * the things to do with it.
 */
export function WorkflowHeaderComponent({ flow, onExport, onExecute }: Props) {
  const { data: validation } = useFlowValidation(flow?.id ?? null);
  const { setActiveTab, setScriptView } = useWorkflowStore();

  const showChanges = () => {
    setActiveTab("script");
    setScriptView("changes");
  };

  const issues = validation?.issues ?? [];
  const errors = issues.filter((x) => x.severity === ValidationSeverityEnum.ERROR).length;
  const warnings = issues.filter((x) => x.severity === ValidationSeverityEnum.WARNING).length;
  const notes = issues.filter((x) => x.severity === ValidationSeverityEnum.INFO).length;
  const health = healthOf(errors, warnings, notes);

  return (
    <header className="flex align-items-center justify-content-between flex-wrap gap-3 px-4 py-3 border-bottom-1 surface-border">
      <div className="flex align-items-center flex-wrap gap-3 min-w-0">
        <Link
          to={flow?.isSubFlow ? "/sub-flows" : "/flows"}
          className="text-sm text-color-secondary no-underline"
        >
          {flow?.isSubFlow ? "Sub-flows" : "Flows"}
        </Link>
        <span
          aria-hidden="true"
          className="text-color-secondary"
        >
          /
        </span>
        <h1 className="m-0 text-lg font-semibold">{flow?.name}</h1>

        {flow && (
          <span className="text-xs text-color-secondary border-1 surface-border border-round px-2">
            {flow.isSubFlow ? "Sub-flow" : flow.isQaTest ? "QA test" : "Automation"}
          </span>
        )}

        {validation && (
          <span
            className="flex align-items-center gap-2 text-xs"
            style={{ color: health.color }}
          >
            <span
              className="border-circle"
              style={{ width: 7, height: 7, background: health.color }}
            />
            {health.text}
          </span>
        )}
      </div>

      <div className="flex flex-wrap gap-2">
        <RepositoryChipComponent
          flowId={flow?.id}
          onShowChanges={showChanges}
        />
        <Button
          label="Export .sflw"
          icon="pi pi-download"
          onClick={onExport}
          disabled={!flow}
          severity="secondary"
          outlined
        />
        <Button
          label="Execute"
          icon="pi pi-play"
          onClick={onExecute}
          disabled={!flow}
        />
      </div>
    </header>
  );
}

const plural = (count: number, noun: string) => `${count} ${noun}${count === 1 ? "" : "s"}`;

const healthOf = (errors: number, warnings: number, notes: number) => {
  if (errors > 0) return { color: "var(--red-400)", text: plural(errors, "error") };
  if (warnings > 0) return { color: "var(--yellow-500)", text: plural(warnings, "warning") };
  if (notes > 0) return { color: "var(--text-color-secondary)", text: plural(notes, "note") };
  return { color: "var(--green-400)", text: "No issues" };
};
