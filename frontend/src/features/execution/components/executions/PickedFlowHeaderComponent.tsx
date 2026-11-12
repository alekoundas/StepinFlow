import { useNavigate } from "react-router-dom";
import { Button } from "primereact/button";

import type { FlowExecutionSummaryDto } from "@/shared/models/database/flow-execution-summary-dto";
import OutcomeStripComponent from "@/features/execution/components/executions/OutcomeStripComponent";

interface Props {
  summary: FlowExecutionSummaryDto;

  /** One of its executions is going now, and stays when the rest are deleted. */
  isExecuting: boolean;
  onDeleteExecutions: () => void;
}

/** The flow picked on the left, and the ways on from here: its editor, its execution page, and clearing out its executions. */
export default function PickedFlowHeaderComponent({ summary, isExecuting, onDeleteExecutions }: Props) {
  const navigate = useNavigate();

  let deletable = summary.runCount;
  if (isExecuting) deletable -= 1;

  return (
    <div className="flex align-items-center flex-wrap gap-3">
      <h2 className="m-0 text-xl font-semibold">{summary.flowName}</h2>
      <OutcomeStripComponent outcomes={summary.recentOutcomes} />

      <div className="flex align-items-center gap-2 ml-auto">
        <Button
          type="button"
          label="Delete executions"
          icon="pi pi-trash"
          size="small"
          severity="danger"
          outlined
          disabled={deletable <= 0}
          onClick={onDeleteExecutions}
        />
        <Button
          type="button"
          label="Edit"
          icon="pi pi-pencil"
          size="small"
          severity="secondary"
          outlined
          onClick={() => navigate(`/workflow/${summary.flowId}`)}
        />
        <Button
          type="button"
          label="Execute"
          icon="pi pi-play"
          size="small"
          onClick={() => navigate(`/execution/${summary.flowId}`)}
        />
      </div>
    </div>
  );
}
