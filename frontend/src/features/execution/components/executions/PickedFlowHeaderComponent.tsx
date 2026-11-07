import { useNavigate } from "react-router-dom";
import { Button } from "primereact/button";

import type { FlowExecutionSummaryDto } from "@/shared/models/database/flow-execution-summary-dto";
import OutcomeStripComponent from "@/features/execution/components/executions/OutcomeStripComponent";

/** The flow picked on the left, and the two ways on from here: its editor, and its execution page. */
export default function PickedFlowHeaderComponent({ summary }: { summary: FlowExecutionSummaryDto }) {
  const navigate = useNavigate();

  return (
    <div className="flex align-items-center flex-wrap gap-3">
      <h2 className="m-0 text-xl font-semibold">{summary.flowName}</h2>
      <OutcomeStripComponent outcomes={summary.recentOutcomes} />

      <div className="flex align-items-center gap-2 ml-auto">
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
