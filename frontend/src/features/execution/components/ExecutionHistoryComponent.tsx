import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";

import StatusPillComponent from "@/shared/components/StatusPillComponent";
import { executionStatusSeverity, executionStatusText } from "@/shared/utils/execution-status";
import { toTook } from "@/shared/utils/clock";
import { ExecutionHistoryLevelEnum } from "@/shared/enums/backend/execution/execution-history-level-enum";
import { useExecutionList } from "@/features/execution/hooks/use-execution";
import ExecutedVersionComponent from "@/features/repository/components/ExecutedVersionComponent";
import type { ExecutionDto } from "@/shared/models/database/execution-dto";

interface Props {
  flowId: number;

  /** The flow as it is now. A run whose shape no longer matches cannot be lined up against it. */
  currentStructureHash?: string;

  onOpen: (execution: ExecutionDto) => void;
}

/** Past runs, newest first. Opening one loads its steps into the run panel. */
export default function ExecutionHistoryComponent({
  flowId,
  currentStructureHash,
  onOpen,
}: Props) {
  const { data: executions, isLoading } = useExecutionList(flowId);

  return (
    <DataTable
      value={executions ?? []}
      loading={isLoading}
      dataKey="id"
      size="small"
      selectionMode="single"
      onSelectionChange={(e) => onOpen(e.value as ExecutionDto)}
      emptyMessage="Not executed yet."
      className="cursor-pointer"
    >
      <Column
        header="Execution"
        body={(execution: ExecutionDto) => `#${execution.id}`}
      />
      <Column
        header="Started"
        body={(execution: ExecutionDto) =>
          new Date(execution.createdOn).toLocaleString()
        }
      />
      <Column
        header="Result"
        body={(execution: ExecutionDto) => (
          <StatusPillComponent
            text={executionStatusText(execution.status)}
            severity={executionStatusSeverity(execution.status)}
          />
        )}
      />
      <Column
        header="Steps"
        align="right"
        body={(execution: ExecutionDto) =>
          execution.historyLevel === ExecutionHistoryLevelEnum.NONE
            ? "—"
            : execution.stepCount
        }
      />
      <Column
        header="Took"
        align="right"
        body={(execution: ExecutionDto) => duration(execution)}
      />
      <Column
        header="Version"
        body={(execution: ExecutionDto) =>
          execution.commitSha || execution.hasUncommittedChanges ? (
            <ExecutedVersionComponent
              commitSha={execution.commitSha}
              hasUncommittedChanges={execution.hasUncommittedChanges}
            />
          ) : (
            "—"
          )
        }
      />
      <Column
        header="Screenshots"
        body={(execution: ExecutionDto) => execution.screenshotFolderName ?? "—"}
      />
      <Column
        header="Kept"
        body={(execution: ExecutionDto) =>
          currentStructureHash &&
          execution.flowStructureHash !== currentStructureHash ? (
            <span className="p-tag text-xs">flow has changed</span>
          ) : (
            historyLevelText(execution.historyLevel)
          )
        }
      />
    </DataTable>
  );
}

function historyLevelText(level: ExecutionHistoryLevelEnum): string {
  switch (level) {
    case ExecutionHistoryLevelEnum.STEPS_AND_IMAGES:
      return "Steps and images";
    case ExecutionHistoryLevelEnum.STEPS:
      return "Steps only";
    default:
      return "Nothing";
  }
}

function duration(execution: ExecutionDto): string {
  if (!execution.completedAt) return "—";

  return toTook(new Date(execution.completedAt).getTime() - new Date(execution.createdOn).getTime());
}
